using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using Nest;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Elasticsearch.Net;
using System.Text.RegularExpressions;
using System;
using process.Types;

namespace process.FileProcessors
{
    class CsvProcessor : IFileProcessor
    {
        private readonly ILogger _logger;
        private readonly IConfigurationRoot _configuration;
        private readonly ElasticClient _client;

        public CsvProcessor(ILogger<CsvProcessor> logger,
            IConfigurationRoot configuration,
            ElasticClient client)
        {
            _logger = logger;
            _configuration = configuration;
            _client = client;
        }

        public async Task Process(string filepath)
        {
            _logger.LogInformation($"Processing {filepath}");

            using (var tr = File.OpenText(filepath))
            {
                var csv = new CsvParser(tr);

                // Parse the header row
                var headers = await csv.ReadAsync();

                // TODO try and normalize header names
                // e.g. trim, lowercase, `_` for space
                // That may match up some otherwise "different" columns

                var rowCount = 0;

                // Parse the rest
                while (true)
                {
                    try
                    {
                        rowCount++;

                        var row = await csv.ReadAsync();

                        if (row == null)
                        {
                            _logger.LogInformation($"EOF {filepath}");
                            Utils.MoveFile(PathTypes.Queued, PathTypes.Complete, filepath, _logger);
                            break;
                        }

                        // build a json object to send to ES
                        var json = new JObject();
                        for (var i = 0; i < headers.Length; i++)
                        {
                            var rowString = row[i]
                                .Replace("\\", "\\\\") // escape `\` in the actual data
                                .Trim(); // trimming should increase matches on querying later

                            json.Add(headers[i], JToken.Parse($"\"{rowString}\""));
                        }

                        // Send to ES!
                        var response = await _client.LowLevel
                            .IndexAsync<StringResponse>(
                                _configuration["es:index"],
                                _configuration["es:type"],
                                json.ToString());

                        if (!response.Success)
                        {
                            _logger.LogError(response.Body);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception at: row: {rowCount}, file: {filepath}");

                        _logger.LogError(e.Message);

                        Utils.MoveFile(PathTypes.Queued, PathTypes.Incomplete, filepath, _logger);

                        break;
                    }
                }
            }
        }
    }
}

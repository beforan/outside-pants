using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using Nest;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Elasticsearch.Net;

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

        public async Task Process(string filename)
        {
            _logger.LogInformation($"Processing {filename}");

            using (var tr = File.OpenText(filename))
            {
                var csv = new CsvParser(tr);

                // Parse the header row
                var headers = await csv.ReadAsync();

                // TODO try and normalize header names
                // e.g. trim, lowercase, `_` for space
                // That may match up some otherwise "different" columns

                // Parse the rest
                while (true)
                {
                    var row = await csv.ReadAsync();

                    if (row == null)
                    {
                        _logger.LogInformation($"EOF {filename}");
                        break;
                    }

                    // build a json object to send to ES
                    var json = new JObject();
                    for(var i = 0; i < headers.Length; i++)
                    {
                        json.Add(headers[i], JToken.Parse($"\"{row[i]}\""));
                    }

                    // Send to ES!
                    var response = await _client.LowLevel.IndexAsync<StringResponse>(
                        _configuration["es:index"],
                        _configuration["es:type"],
                        json.ToString());

                    if(!response.Success)
                    {
                        _logger.LogError(response.Body);
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using process.Types;

namespace process.Services
{
    public class RedisQueueService : IRedisQueueService
    {
        private readonly ILogger _logger;
        private readonly IConfigurationRoot _configuration;
        private readonly HttpClient _client;

        public RedisQueueService(ILogger logger,
            IConfigurationRoot configuration,
            IIndex<HttpClients, HttpClient> clients)
        {
            _logger = logger;
            _configuration = configuration;
            _client = clients[HttpClients.RedisMessageQueue];
        }

        public async Task<IList<string>> ListQueues()
        {
            var response = await _client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get,
                    new Uri(_client.BaseAddress, _configuration["rsmq:queues"])));

            if(!response.IsSuccessStatusCode) throw new HttpRequestException(
                JsonConvert.SerializeObject(new
                {
                    Message = "Request to the Message Queue API failed.",
                    Status = response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                }));

            return JsonConvert.DeserializeObject<List<string>>(
                await response.Content.ReadAsStringAsync());
        }
    }
}

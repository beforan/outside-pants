﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using process.RedisModels;
using process.Types;

namespace process.Services
{
    public class RedisQueueService : IRedisQueueService
    {
        private readonly ILogger _logger;
        private readonly IConfigurationRoot _configuration;
        private readonly HttpClient _client;

        public RedisQueueService(ILogger<RedisQueueService> logger,
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

            if (!response.IsSuccessStatusCode) throw new HttpRequestException(
                 JsonConvert.SerializeObject(new
                 {
                     Message = "Request to the Message Queue API failed.",
                     Status = response.StatusCode,
                     Content = await response.Content.ReadAsStringAsync()
                 }));

            return JsonConvert.DeserializeObject<dynamic>(
                await response.Content.ReadAsStringAsync()).queues
                .ToObject<List<string>>();
        }

        public async Task<ReceiveMessageResponseModel> ReceiveMessage(string queue)
        {
            var uriBuilder = new UriBuilder(
                new Uri(_client.BaseAddress,
                $"{_configuration["rsmq:messages"]}/{queue}"))
            {
                Query = $"vt={_configuration["rsmq:visibilityTimer"]}"
            };

            var response = await _client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri));

            if (!response.IsSuccessStatusCode) throw new HttpRequestException(
                 JsonConvert.SerializeObject(new
                 {
                     Message = "Request to the Message Queue API failed.",
                     Status = response.StatusCode,
                     Content = await response.Content.ReadAsStringAsync()
                 }));

            return JsonConvert.DeserializeObject<ReceiveMessageResponseModel>(
                await response.Content.ReadAsStringAsync());
        }

        public async Task DeleteMessage(string queue, string messageId)
        {
            var uriBuilder = new UriBuilder(
                new Uri(_client.BaseAddress,
                $"{_configuration["rsmq:messages"]}/{queue}/{messageId}"));

            var response = await _client.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, uriBuilder.Uri));

            if (!response.IsSuccessStatusCode) throw new HttpRequestException(
                 JsonConvert.SerializeObject(new
                 {
                     Message = "Request to the Message Queue API failed.",
                     Status = response.StatusCode,
                     Content = await response.Content.ReadAsStringAsync()
                 }));

            return;
        }
    }
}

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using process.Services;

namespace process
{
    public class App
    {
        private readonly ILogger _logger;
        private readonly IRedisQueueService _redis;

        public App(ILogger<App> logger, IRedisQueueService redis)
        {
            _logger = logger;
            _redis = redis;
        }

        public async Task Run()
        {
            _logger.LogInformation("App started");

            // First keep trying to get queues out of the mq api to ensure it's up
            var mqUp = false;
            while(!mqUp)
            {
                try
                {
                    await TryGetQueues();
                    mqUp = true;
                } catch (HttpRequestException e)
                {
                    _logger.LogError(e.Message);
                    _logger.LogError(e.InnerException?.Message ?? "no inner exception");
                    Thread.Sleep(3000); // wait to retry
                }
            }

            Console.Read();
        }

        private async Task TryGetQueues()
        {
            var queues = await _redis.ListQueues();

            foreach (var queue in queues)
            {
                _logger.LogInformation($"found queue: {queue}");
            }
        }
    }
}

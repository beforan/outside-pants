using System;
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

            var queues = await _redis.ListQueues();

            foreach (var queue in queues)
            {
                _logger.LogInformation($"found queue: {queue}");
            }

            Console.Read();
        }
    }
}

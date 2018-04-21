using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using process.Services;

namespace process
{
    public class App
    {
        private readonly ILogger _logger;
        private readonly IRedisQueueService _redis;
        private readonly IConfigurationRoot _configuration;
        private readonly IFileProcessor _fileProcessor;

        public bool Processing { get; set; }

        public App(ILogger<App> logger,
            IConfigurationRoot configuration,
            IRedisQueueService redis,
            IFileProcessor fileProcessor)
        {
            _logger = logger;
            _redis = redis;
            _configuration = configuration;
            _fileProcessor = fileProcessor;
        }

        private string GetPath(string type, string filename)
            => $@"{Path.Combine(
                    _configuration["paths:base"],
                    _configuration[$"paths:{type}"],
                    filename)}";

        public async Task Run()
        {
            _logger.LogInformation("doing work");
            return;

            // First keep trying to get the queue out of the mq api
            // to ensure it's up
            // and then to keep polling for the queue to exist
            await MessageQueueAvailable();

            // Once the service api is up and the queue is present
            // we need to poll the queue or process messages
            while (true)
            {
                _logger.LogInformation("Checking for messages...");

                Processing = true;

                var message = await _redis.ReceiveMessage(
                    _configuration["rsmq:queueName"]);

                if (!string.IsNullOrWhiteSpace(message.Id))
                {
                    _logger.LogInformation($"Message found: {message.Id}");

                    // process it
                    // TODO if we support multiple file types
                    // we'll need a way to DI the correct processor
                    // but for now...
                    var fullPath = GetPath("queued", message.Message);

                    var ext = Path.GetExtension(fullPath);

                    if (!_configuration["fileExtensions"].Contains(ext)) // TODO split once multiple
                    {
                        _logger.LogInformation($"unsupported file type detected: {message.Message}");
                        var newPath = GetPath("badtype", message.Message);
                        File.Move(fullPath, newPath);
                        _logger.LogInformation($"moved to: {newPath}");

                        // TODO delete message
                    }
                    else
                    {
                        // TODO really processors should work on streams
                        // so the ES bit is in App, and not recreated by every processor?
                        await _fileProcessor.Process(fullPath);

                        // TODO delete message when done
                    }
                }
                // else no messages

                Processing = false;

                Thread.Sleep(10000); // wait to poll again
            }
        }

        private async Task MessageQueueAvailable()
        {
            var mqUp = false;
            while (!mqUp)
            {
                try
                {
                    var queues = await _redis.ListQueues();
                    mqUp = queues.Contains(_configuration["rsmq:queueName"]);
                }
                catch (HttpRequestException e) // TODO filter harder
                {
                    _logger.LogError(e.Message);
                    _logger.LogError(e.InnerException?.Message ?? "no inner exception");
                }

                if (!mqUp) Thread.Sleep(3000); // wait to retry
            }
        }
    }
}

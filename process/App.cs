using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using process.Services;
using process.Types;

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

        private async Task<bool> QueueReady(string queue)
        {
            try
            {
                var queues = await _redis.ListQueues();

                // if our queue isn't there, just return and poll again with the timer
                if (!queues.Contains(queue))
                {
                    _logger.LogWarning($"'{queue}' queue not present");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException e) // RSMQ didn't respond as expected, quit and try again
            {
                _logger.LogWarning("mqapi didn't respond as expected.");
                _logger.LogError(e.Message);
                return false;
            }
        }

        public async Task Run()
        {
            _logger.LogDebug("App.Run");

            var processQueue = _configuration["rsmq:queueName"];

            // Ensure the Message Queue API is up, and the queue we want exists
            if (!await QueueReady(processQueue)) return;

            // now continue with our work
            _logger.LogDebug("Checking for messages...");

            try
            {
                var message = await _redis.ReceiveMessage(processQueue);

                if (!string.IsNullOrWhiteSpace(message.Id))
                {
                    _logger.LogInformation($"Message received: {message.Id}");

                    // process it
                    // TODO if we support multiple file types
                    // we'll need a way to DI the correct processor
                    // but for now just CSV so it's ok

                    var ext = Path.GetExtension(message.Message);

                    if (Array.FindIndex(
                        _configuration["fileExtensions"].Split(),
                        x => x == ext) < 0)
                    {
                        _logger.LogInformation($"unsupported file type detected: {message.Message}");

                        Utils.MoveFile(PathTypes.Queued, PathTypes.BadType, message.Message, _logger);

                        // TODO delete message
                    }
                    else
                    {
                        // TODO really processors should work on streams
                        // so the ES bit is in App, and not recreated by every processor?
                        await _fileProcessor.Process(message.Message);

                        // TODO delete message when done
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
    }
}

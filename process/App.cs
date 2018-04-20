using System;
using Microsoft.Extensions.Logging;

namespace process
{
    public class App
    {
        private readonly ILogger _logger;

        public App(ILogger<App> logger)
        {
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogInformation("App started");
            Console.Read();
        }
    }
}

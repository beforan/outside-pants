using Microsoft.Extensions.Logging;

namespace process.FileProcessors
{
    class CsvProcessor
    {
        private readonly ILogger _logger;

        public CsvProcessor(ILogger logger)
        {
            _logger = logger;
        }

        public void Process(string filename)
        {

        }
    }
}

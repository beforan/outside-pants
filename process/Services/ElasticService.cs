using Microsoft.Extensions.Configuration;
using Nest;

namespace process.Services
{
    class ElasticService : IElasticService
    {
        private readonly IConfigurationRoot _configuration;
        private readonly ElasticClient _client;

        public ElasticService(IConfigurationRoot configuration,
            ElasticClient client)
        {
            _configuration = configuration;
            _client = client;
        }
    }
}

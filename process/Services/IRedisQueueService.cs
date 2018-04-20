using process.RedisModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace process.Services
{
    public interface IRedisQueueService
    {
        Task<IList<string>> ListQueues();
        Task<ReceiveMessageResponseModel> ReceiveMessage(string queue);
    }
}
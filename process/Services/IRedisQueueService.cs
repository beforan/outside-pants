using System.Collections.Generic;
using System.Threading.Tasks;

namespace process.Services
{
    public interface IRedisQueueService
    {
        Task<IList<string>> ListQueues();
    }
}
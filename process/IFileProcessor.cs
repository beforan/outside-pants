using System.Threading.Tasks;

namespace process {
    public interface IFileProcessor {
        Task Process(string filename);
    }
}
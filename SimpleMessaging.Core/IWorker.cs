using System.Threading.Tasks;

namespace SimpleMessaging.Core
{
    public interface IWorker<T>
    {
        Task<WorkerStatus> GetStatus();
        Task Add(T workItem);
        void Start();
        Task Stop();
    }
}
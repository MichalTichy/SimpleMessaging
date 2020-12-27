using System.Threading.Tasks;

namespace SimpleMessaging.Base
{
    public interface IWorker<T>
    {
        Task<WorkerStatus> GetStatus();
        Task Add(T workItem);
        Task Start();
        Task Stop();
    }
}
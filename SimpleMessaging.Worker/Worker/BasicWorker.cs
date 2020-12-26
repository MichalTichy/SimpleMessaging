using Microsoft.Extensions.Logging;
using SimpleMessaging.Worker.Queue;

namespace SimpleMessaging.Worker.Worker
{
    public abstract class BasicWorker<TItem> : WorkerBase<TItem, ConcurrentWorkerQueue<TItem>>
    {
        protected BasicWorker(ILogger logger) : base(logger)
        {
        }
    }
}
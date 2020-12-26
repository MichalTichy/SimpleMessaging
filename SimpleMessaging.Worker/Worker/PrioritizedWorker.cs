using Microsoft.Extensions.Logging;
using SimpleMessaging.Worker.Item;
using SimpleMessaging.Worker.Queue;

namespace SimpleMessaging.Worker.Worker
{
    public abstract class PrioritizedWorker<TItem> : WorkerBase<TItem, PrioritizedConcurrentWorkerQueue<TItem>> where TItem : IItemWithPriority
    {
        protected PrioritizedWorker(ILogger logger) : base(logger)
        {
        }
    }
}
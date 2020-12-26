using System.Collections.Concurrent;
using SimpleMessaging.Base;

namespace SimpleMessaging.Worker.Queue
{
    public class ConcurrentWorkerQueue<T> : IWorkerQueue<T>
    {
        protected ConcurrentQueue<T> Queue = new ConcurrentQueue<T>();
        public bool IsEmpty()
        {
            return Queue.IsEmpty;
        }

        public bool TryGet(out T item)
        {
            return Queue.TryDequeue(out item);
        }

        public void Add(T item)
        {
            Queue.Enqueue(item);
        }
    }
}
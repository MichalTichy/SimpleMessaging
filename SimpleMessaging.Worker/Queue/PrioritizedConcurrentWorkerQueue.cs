using System;
using System.Collections.Concurrent;
using SimpleMessaging.Core;
using SimpleMessaging.Worker.Item;

namespace SimpleMessaging.Worker.Queue
{
    public class PrioritizedConcurrentWorkerQueue<TItem> : IWorkerQueue<TItem> where TItem: IItemWithPriority
    {
        protected ConcurrentQueue<TItem> HighPriorityQueue = new ConcurrentQueue<TItem>();
        protected ConcurrentQueue<TItem> NormalPriorityQueue = new ConcurrentQueue<TItem>();
        protected ConcurrentQueue<TItem> LowPriorityQueue = new ConcurrentQueue<TItem>();


        public bool IsEmpty()
        {
            return HighPriorityQueue.Count == 0 && NormalPriorityQueue.Count == 0 && LowPriorityQueue.Count == 0;
        }

        public bool TryGet(out TItem item)
        {
            if (HighPriorityQueue.TryDequeue(out item))
                return true;
            if (NormalPriorityQueue.TryDequeue(out item))
                return true;
            if (LowPriorityQueue.TryDequeue(out item))
                return true;

            return false;
        }

        public void Add(TItem item)
        {
            switch (item.ItemPriority)
            {
                case ItemPriority.Low:
                    LowPriorityQueue.Enqueue(item);
                    break;
                case ItemPriority.Normal:
                    NormalPriorityQueue.Enqueue(item);
                    break;
                case ItemPriority.High:
                    HighPriorityQueue.Enqueue(item);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(item.ItemPriority), item.ItemPriority, null);
            }
        }
    }
}
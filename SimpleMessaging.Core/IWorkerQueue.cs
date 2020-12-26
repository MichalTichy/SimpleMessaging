namespace SimpleMessaging.Core
{
    public interface IWorkerQueue<T>
    {
        bool IsEmpty();
        bool TryGet(out T item);
        void Add(T item);
    }
}
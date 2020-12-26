namespace SimpleMessaging.Worker.Item
{
    public interface IItemWithPriority
    {
        ItemPriority ItemPriority { get; }
    }
}
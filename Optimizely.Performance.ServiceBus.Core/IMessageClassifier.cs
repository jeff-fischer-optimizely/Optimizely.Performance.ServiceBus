namespace Optimizely.Performance.ServiceBus.Core
{
    public interface IMessageClassifier
    {
        // Classify by actual System.Type when available for strong-typing
        MessageCategory ClassifyMessage(System.Type messageType, object? messageBody);
        MessagePriority GetPriority(MessageCategory category);
    }
}

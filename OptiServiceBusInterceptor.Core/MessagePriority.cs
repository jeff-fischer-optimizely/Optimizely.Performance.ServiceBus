namespace OptiServiceBusPrioritizer.Core
{
    public enum MessagePriority
    {
        Critical = 0,
        High = 1,
        Normal = 2,
        Low = 3
    }

    public static class MessagePriorityExtensions
    {
        public static int ToServiceBusPriority(this MessagePriority priority)
        {
            return priority switch
            {
                MessagePriority.Critical => 7,
                MessagePriority.High => 5,
                MessagePriority.Normal => 3,
                MessagePriority.Low => 1,
                _ => 3
            };
        }
    }
}

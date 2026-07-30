namespace Optimizely.Performance.ServiceBus.Core
{
    public enum MessageCategory
    {
        Unknown = 0,
        CartSynchronization = 1,
        PricingSynchronization = 2,
        InventorySynchronization = 3,
        ProductSynchronization = 4,
        ContentSynchronization = 5,
        OrderSynchronization = 6,
        CatalogSynchronization = 7
    }
}

using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Optimizely.Performance.ServiceBus.Core
{
    public class OptimizelyMessageClassifier : IMessageClassifier
    {
        private readonly ILogger<OptimizelyMessageClassifier> _logger;
        private readonly PriorityConfiguration _config;

        private static readonly string[] CartMessageTypes = new[]
        {
            "EPiServer.Commerce.Order.CartMessage",
            "EPiServer.Commerce.Order.WishListMessage",
            "EPiServer.Commerce.Order.CartItemMessage",
            "Mediachase.Commerce.Orders.Cart",
            "Mediachase.Commerce.Orders.ShoppingCart"
        };

        private static readonly string[] PricingMessageTypes = new[]
        {
            "EPiServer.Commerce.Catalog.PriceMessage",
            "EPiServer.Commerce.Catalog.PriceValueMessage",
            "Mediachase.Commerce.Catalog.Price",
            "Mediachase.Commerce.Pricing.CatalogEntryPrice"
        };

        private static readonly string[] InventoryMessageTypes = new[]
        {
            "EPiServer.Commerce.Catalog.InventoryMessage",
            "Mediachase.Commerce.Inventory.Warehouse",
            "Mediachase.Commerce.InventoryService.InventoryRecord"
        };

        private static readonly string[] ProductMessageTypes = new[]
        {
            "EPiServer.Commerce.Catalog.CatalogContentMessage",
            "EPiServer.Commerce.Catalog.ProductMessage",
            "EPiServer.Commerce.Catalog.VariationMessage",
            "EPiServer.Commerce.Catalog.BundleMessage",
            "EPiServer.Commerce.Catalog.PackageMessage",
            "Mediachase.Commerce.Catalog.Entry",
            "Mediachase.Commerce.Catalog.CatalogEntry"
        };

        private static readonly string[] ContentMessageTypes = new[]
        {
            "EPiServer.Core.ContentMessage",
            "EPiServer.Core.PageMessage",
            "EPiServer.Core.BlockMessage",
            "EPiServer.ContentMessage",
            "EPiServer.ChangeNotificationMessage"
        };

        private static readonly string[] CatalogMessageTypes = new[]
        {
            "EPiServer.Commerce.Catalog.CatalogMessage",
            "EPiServer.Commerce.Catalog.CategoryMessage",
            "Mediachase.Commerce.Catalog.CatalogNode"
        };

        public OptimizelyMessageClassifier(ILogger<OptimizelyMessageClassifier> logger, PriorityConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // Note: string-based classification has been removed. Callers must supply a System.Type.

        public MessageCategory ClassifyMessage(System.Type messageType, object? messageBody)
        {
            if (messageType == null)
            {
                _logger.LogWarning("Message Type is null, classifying as Unknown");
                return MessageCategory.Unknown;
            }

            // 1) Exact type mapping registered by consumer
            if (_config.TryGetTypeMapping(messageType, out var mappedCategory))
            {
                _logger.LogDebug("Classified message type '{MessageType}' using TypeMappings as {Category}", messageType.FullName, mappedCategory);
                return mappedCategory;
            }

            // 2) Predicate mappings (evaluated in registration order)
            var (predicates, categories) = _config.GetPredicateMappingsSnapshot();
            for (int i = 0; i < predicates.Length; i++)
            {
                try
                {
                    var predicate = predicates[i];
                    if (predicate(messageType))
                    {
                        var cat = categories[i];
                        _logger.LogDebug("Classified message type '{MessageType}' using predicate as {Category}", messageType.FullName, cat);
                        return cat;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Predicate mapping threw for type '{MessageType}'", messageType.FullName);
                }
            }

            var fullname = messageType.FullName ?? messageType.Name;

            // 3) Custom configured string matches (registered by consumers)
            if (_config.CustomCartMessageTypes.Any(s => !string.IsNullOrWhiteSpace(s) && fullname.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger.LogDebug("Classified message type '{MessageType}' as CartSynchronization via CustomCartMessageTypes", fullname);
                return MessageCategory.CartSynchronization;
            }

            if (_config.CustomPricingMessageTypes.Any(s => !string.IsNullOrWhiteSpace(s) && fullname.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger.LogDebug("Classified message type '{MessageType}' as PricingSynchronization via CustomPricingMessageTypes", fullname);
                return MessageCategory.PricingSynchronization;
            }

            if (_config.CustomInventoryMessageTypes.Any(s => !string.IsNullOrWhiteSpace(s) && fullname.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger.LogDebug("Classified message type '{MessageType}' as InventorySynchronization via CustomInventoryMessageTypes", fullname);
                return MessageCategory.InventorySynchronization;
            }

            if (_config.CustomProductMessageTypes.Any(s => !string.IsNullOrWhiteSpace(s) && fullname.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger.LogDebug("Classified message type '{MessageType}' as ProductSynchronization via CustomProductMessageTypes", fullname);
                return MessageCategory.ProductSynchronization;
            }

            if (_config.CustomContentMessageTypes.Any(s => !string.IsNullOrWhiteSpace(s) && fullname.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger.LogDebug("Classified message type '{MessageType}' as ContentSynchronization via CustomContentMessageTypes", fullname);
                return MessageCategory.ContentSynchronization;
            }

            // 4) Fallback to built-in heuristics (string checks)
            try
            {
                // Cart / Order related
                if (fullname.IndexOf("Commerce.Order", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("Cart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("ShoppingCart", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogDebug("Classified message type '{MessageType}' as CartSynchronization", fullname);
                    return MessageCategory.CartSynchronization;
                }

                // Pricing
                if (fullname.IndexOf("Catalog.Price", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("Pricing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("Price", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogDebug("Classified message type '{MessageType}' as PricingSynchronization", fullname);
                    return MessageCategory.PricingSynchronization;
                }

                // Inventory
                if (fullname.IndexOf("Inventory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("InventoryRecord", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("Warehouse", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogDebug("Classified message type '{MessageType}' as InventorySynchronization", fullname);
                    return MessageCategory.InventorySynchronization;
                }

                // Product / Catalog entries
                if (fullname.IndexOf("Catalog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("Product", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("CatalogEntry", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogDebug("Classified message type '{MessageType}' as ProductSynchronization", fullname);
                    return MessageCategory.ProductSynchronization;
                }

                // Content events
                if (fullname.IndexOf("ContentEventArgs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("ContentMessage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("PageMessage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("BlockMessage", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogDebug("Classified message type '{MessageType}' as ContentSynchronization", fullname);
                    return MessageCategory.ContentSynchronization;
                }

                // Catalog-specific
                if (fullname.IndexOf("CategoryMessage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fullname.IndexOf("CatalogNode", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.LogDebug("Classified message type '{MessageType}' as CatalogSynchronization", fullname);
                    return MessageCategory.CatalogSynchronization;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error classifying message type '{MessageType}', falling back to Unknown", fullname);
                return MessageCategory.Unknown;
            }

            _logger.LogWarning("Unable to classify message type '{MessageType}', defaulting to Unknown", fullname);
            return MessageCategory.Unknown;
        }

        public MessagePriority GetPriority(MessageCategory category)
        {
            return _config.GetPriority(category);
        }
    }
}

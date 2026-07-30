using System;
using Microsoft.Extensions.Logging;

namespace Optimizely.Performance.ServiceBus.Core
{
    /// <summary>
    /// Classifies Optimizely event messages based on their actual System.Type.
    ///
    /// IMPORTANT: Optimizely Service Bus messages are EventMessage objects from EPiServer.Events.
    /// The EventMessage.Parameter property contains the actual event data whose type we classify.
    ///
    /// This classifier operates in three modes:
    /// 1. Exact Type Mappings - Direct Type -> MessageCategory registrations (fastest)
    /// 2. Predicate Mappings - Functions that inspect types (for namespace/interface patterns)
    /// 3. Namespace Fallback - Last resort heuristics based on type namespaces
    /// </summary>
    public class OptimizelyMessageClassifier : IMessageClassifier
    {
        private readonly ILogger<OptimizelyMessageClassifier> _logger;
        private readonly PriorityConfiguration _config;

        public OptimizelyMessageClassifier(ILogger<OptimizelyMessageClassifier> logger, PriorityConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Classifies a message based on its System.Type.
        /// </summary>
        /// <param name="messageType">The actual System.Type of the EventMessage.Parameter</param>
        /// <param name="messageBody">Optional message body for inspection (not currently used)</param>
        /// <returns>The classified MessageCategory</returns>
        public MessageCategory ClassifyMessage(System.Type messageType, object? messageBody)
        {
            if (messageType == null)
            {
                _logger.LogWarning("Message Type is null, classifying as Unknown");
                return MessageCategory.Unknown;
            }

            // 1) Exact type mapping registered by consumer (fastest path)
            if (_config.TryGetTypeMapping(messageType, out var mappedCategory))
            {
                _logger.LogDebug("Classified '{MessageType}' via exact type mapping as {Category}",
                    messageType.FullName, mappedCategory);
                return mappedCategory;
            }

            // 2) Predicate mappings (evaluated in registration order)
            // These allow flexible rules like "any type in namespace X" or "implements interface Y"
            var (predicates, categories) = _config.GetPredicateMappingsSnapshot();
            for (int i = 0; i < predicates.Length; i++)
            {
                try
                {
                    var predicate = predicates[i];
                    if (predicate(messageType))
                    {
                        var predicateCategory = categories[i];
                        _logger.LogDebug("Classified '{MessageType}' via predicate mapping as {Category}",
                            messageType.FullName, predicateCategory);
                        return predicateCategory;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Predicate mapping threw exception for type '{MessageType}'",
                        messageType.FullName);
                }
            }

            // 3) Namespace-based fallback heuristics
            // These are last-resort patterns when no explicit mappings exist
            var fallbackCategory = ClassifyByNamespace(messageType);
            if (fallbackCategory != MessageCategory.Unknown)
            {
                _logger.LogDebug("Classified '{MessageType}' via namespace heuristic as {Category}",
                    messageType.FullName, fallbackCategory);
                return fallbackCategory;
            }

            _logger.LogInformation("Unable to classify message type '{MessageType}', defaulting to Unknown. " +
                "Consider registering this type explicitly via PriorityConfiguration.AddTypeMapping() or " +
                "AddPredicateMapping() for better performance.",
                messageType.FullName);

            return MessageCategory.Unknown;
        }

        /// <summary>
        /// Last-resort classification based on namespace patterns.
        /// This is only used when no explicit type or predicate mappings match.
        /// </summary>
        private MessageCategory ClassifyByNamespace(Type messageType)
        {
            var ns = messageType.Namespace ?? string.Empty;
            var name = messageType.Name;

            // Cart / Order events (highest priority)
            // Examples: EPiServer.Commerce.Order.*, Mediachase.Commerce.Orders.*
            if (ns.Contains("Commerce.Order", StringComparison.Ordinal) ||
                (ns.Contains("Commerce", StringComparison.Ordinal) &&
                 (name.Contains("Cart", StringComparison.Ordinal) ||
                  name.Contains("Order", StringComparison.Ordinal))))
            {
                return MessageCategory.CartSynchronization;
            }

            // Pricing events (high priority)
            // Examples: EPiServer.Commerce.Catalog.Pricing.*, Mediachase.Commerce.Pricing.*
            if ((ns.Contains("Commerce", StringComparison.Ordinal) && ns.Contains("Pricing", StringComparison.Ordinal)) ||
                (ns.Contains("Commerce.Catalog", StringComparison.Ordinal) && name.Contains("Price", StringComparison.Ordinal)))
            {
                return MessageCategory.PricingSynchronization;
            }

            // Inventory events (high priority)
            // Examples: EPiServer.Commerce.Catalog.Inventory.*, Mediachase.Commerce.Inventory.*
            if ((ns.Contains("Commerce", StringComparison.Ordinal) && ns.Contains("Inventory", StringComparison.Ordinal)) ||
                name.Contains("Inventory", StringComparison.Ordinal) ||
                name.Contains("Warehouse", StringComparison.Ordinal))
            {
                return MessageCategory.InventorySynchronization;
            }

            // Catalog structure events (catalogs, categories, nodes)
            // Examples: EPiServer.Commerce.Catalog.CatalogMessage, CategoryMessage
            if (ns.Contains("Commerce.Catalog", StringComparison.Ordinal) &&
                (name.Contains("Catalog", StringComparison.Ordinal) ||
                 name.Contains("Category", StringComparison.Ordinal) ||
                 name.Contains("Node", StringComparison.Ordinal)))
            {
                return MessageCategory.CatalogSynchronization;
            }

            // Product events (high priority)
            // Examples: EPiServer.Commerce.Catalog.Product*, Entry*, Variation*
            if (ns.Contains("Commerce.Catalog", StringComparison.Ordinal) &&
                (name.Contains("Product", StringComparison.Ordinal) ||
                 name.Contains("Entry", StringComparison.Ordinal) ||
                 name.Contains("Variation", StringComparison.Ordinal) ||
                 name.Contains("Bundle", StringComparison.Ordinal) ||
                 name.Contains("Package", StringComparison.Ordinal)))
            {
                return MessageCategory.ProductSynchronization;
            }

            // Content events (normal priority)
            // Examples: EPiServer.ContentEventArgs, EPiServer.Core.ContentMessage
            if (ns.StartsWith("EPiServer", StringComparison.Ordinal) &&
                (name.Contains("Content", StringComparison.Ordinal) ||
                 name.Contains("Page", StringComparison.Ordinal) ||
                 name.Contains("Block", StringComparison.Ordinal)))
            {
                return MessageCategory.ContentSynchronization;
            }

            return MessageCategory.Unknown;
        }

        public MessagePriority GetPriority(MessageCategory category)
        {
            return _config.GetPriority(category);
        }
    }
}

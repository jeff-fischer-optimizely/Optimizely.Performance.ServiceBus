using System.Collections.Generic;

namespace Optimizely.Performance.ServiceBus.Core
{
    /// <summary>
    /// Configuration options for message prioritization.
    /// This class is designed to work with Microsoft.Extensions.Configuration
    /// for loading from appsettings.json, environment variables, etc.
    ///
    /// Example appsettings.json:
    /// {
    ///   "Optimizely": {
    ///     "ServiceBus": {
    ///       "MessagePrioritization": {
    ///         "Priorities": {
    ///           "CartSynchronization": "Critical",
    ///           "PricingSynchronization": "High",
    ///           "ContentSynchronization": "Normal"
    ///         },
    ///         "NamespaceMappings": {
    ///           "EPiServer.Commerce.Order": "CartSynchronization",
    ///           "EPiServer.Commerce.Catalog.Pricing": "PricingSynchronization"
    ///         }
    ///       }
    ///     }
    ///   }
    /// }
    /// </summary>
    public class MessagePrioritizationOptions
    {
        /// <summary>
        /// Priority level assignments for each message category.
        /// Key: MessageCategory name (e.g., "CartSynchronization")
        /// Value: MessagePriority name (e.g., "Critical", "High", "Normal", "Low")
        /// </summary>
        public Dictionary<string, string> Priorities { get; set; } = new()
        {
            { "CartSynchronization", "Critical" },
            { "OrderSynchronization", "Critical" },
            { "PricingSynchronization", "High" },
            { "InventorySynchronization", "High" },
            { "ProductSynchronization", "High" },
            { "CatalogSynchronization", "High" },
            { "ContentSynchronization", "Normal" },
            { "Unknown", "Low" }
        };

        /// <summary>
        /// Namespace-to-category mappings for automatic type classification.
        /// Key: Namespace prefix or pattern (e.g., "EPiServer.Commerce.Order")
        /// Value: MessageCategory name (e.g., "CartSynchronization")
        ///
        /// These are registered as predicate mappings that check if a type's namespace
        /// starts with the specified key.
        /// </summary>
        public Dictionary<string, string> NamespaceMappings { get; set; } = new()
        {
            // Commerce
            { "EPiServer.Commerce.Order", "CartSynchronization" },
            { "Mediachase.Commerce.Orders", "CartSynchronization" },
            { "EPiServer.Commerce.Catalog.Pricing", "PricingSynchronization" },
            { "Mediachase.Commerce.Pricing", "PricingSynchronization" },
            { "EPiServer.Commerce.Catalog.Inventory", "InventorySynchronization" },
            { "Mediachase.Commerce.Inventory", "InventorySynchronization" },
            { "EPiServer.Commerce.Catalog", "ProductSynchronization" },
            { "Mediachase.Commerce.Catalog", "ProductSynchronization" },

            // CMS
            { "EPiServer.Core", "ContentSynchronization" },
            { "EPiServer.ContentEvents", "ContentSynchronization" }
        };

        /// <summary>
        /// Fully-qualified type names mapped to specific categories.
        /// Key: Full type name (e.g., "EPiServer.Commerce.Order.CartMessage")
        /// Value: MessageCategory name (e.g., "CartSynchronization")
        ///
        /// These provide exact type-to-category mappings that override namespace-based rules.
        /// </summary>
        public Dictionary<string, string> TypeMappings { get; set; } = new();

        /// <summary>
        /// Whether to enable automatic discovery and registration of types from loaded assemblies.
        /// Default is true.
        /// </summary>
        public bool EnableAutoDiscovery { get; set; } = true;

        /// <summary>
        /// Assembly name prefixes to scan when EnableAutoDiscovery is true.
        /// Defaults to ["EPiServer", "Mediachase", "Optimizely"].
        /// </summary>
        public List<string> AutoDiscoveryPrefixes { get; set; } = new()
        {
            "EPiServer",
            "Mediachase",
            "Optimizely"
        };

        /// <summary>
        /// Applies this configuration to a PriorityConfiguration instance.
        /// </summary>
        public void ApplyTo(PriorityConfiguration config)
        {
            // Apply priority mappings
            foreach (var kvp in Priorities)
            {
                if (TryParseEnumValue<MessageCategory>(kvp.Key, out var category) &&
                    TryParseEnumValue<MessagePriority>(kvp.Value, out var priority))
                {
                    config.PriorityMappings[category] = priority;
                }
            }

            // Apply namespace-based predicate mappings
            foreach (var kvp in NamespaceMappings)
            {
                var namespacePrefix = kvp.Key;
                if (TryParseEnumValue<MessageCategory>(kvp.Value, out var category))
                {
                    config.AddPredicateMapping(
                        type => (type.Namespace ?? string.Empty).StartsWith(namespacePrefix, System.StringComparison.Ordinal),
                        category
                    );
                }
            }

            // Apply exact type mappings (requires loading types at runtime)
            // Note: Type mappings are applied by the consumer when types are available
        }

        private static bool TryParseEnumValue<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            if (System.Enum.TryParse<TEnum>(value, ignoreCase: true, out result))
            {
                return true;
            }
            result = default;
            return false;
        }
    }
}

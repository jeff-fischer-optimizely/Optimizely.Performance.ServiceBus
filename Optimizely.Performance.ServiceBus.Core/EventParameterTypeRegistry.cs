using System;
using System.Collections.Generic;
using System.Linq;

namespace Optimizely.Performance.ServiceBus.Core
{
    /// <summary>
    /// Simplified registry for event parameter types.
    /// For V13+, use EventProviderOptions.ParameterTypes directly.
    /// For V11/V12, register types manually or use the static helper methods.
    /// </summary>
    public class EventParameterTypeRegistry
    {
        private readonly HashSet<Type> _types = new();

        /// <summary>
        /// Registers event parameter types for classification.
        /// </summary>
        public void RegisterTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (type != null)
                    _types.Add(type);
            }
        }

        /// <summary>
        /// Registers a single type.
        /// </summary>
        public void RegisterType(Type type)
        {
            if (type != null)
                _types.Add(type);
        }

        /// <summary>
        /// Gets all registered types.
        /// </summary>
        public IReadOnlyCollection<Type> GetTypes() => _types.ToArray();

        /// <summary>
        /// Auto-registers discovered types with a PriorityConfiguration.
        /// </summary>
        public void ApplyTo(PriorityConfiguration config)
        {
            foreach (var type in _types)
            {
                var category = ClassifyType(type);
                if (category != MessageCategory.Unknown)
                {
                    config.AddTypeMapping(type, category);
                }
            }
        }

        private static MessageCategory ClassifyType(Type type)
        {
            var ns = type.Namespace ?? string.Empty;
            var name = type.Name;

            // Cart/Order (highest priority)
            if (ns.Contains("Commerce.Order", StringComparison.Ordinal) ||
                (ns.Contains("Commerce", StringComparison.Ordinal) &&
                 (name.Contains("Cart", StringComparison.Ordinal) ||
                  name.Contains("Order", StringComparison.Ordinal))))
            {
                return MessageCategory.CartSynchronization;
            }

            // Pricing
            if ((ns.Contains("Commerce", StringComparison.Ordinal) && ns.Contains("Pricing", StringComparison.Ordinal)) ||
                (ns.Contains("Commerce.Catalog", StringComparison.Ordinal) && name.Contains("Price", StringComparison.Ordinal)))
            {
                return MessageCategory.PricingSynchronization;
            }

            // Inventory
            if ((ns.Contains("Commerce", StringComparison.Ordinal) && ns.Contains("Inventory", StringComparison.Ordinal)) ||
                name.Contains("Inventory", StringComparison.Ordinal) ||
                name.Contains("Warehouse", StringComparison.Ordinal))
            {
                return MessageCategory.InventorySynchronization;
            }

            // Catalog
            if (ns.Contains("Commerce.Catalog", StringComparison.Ordinal) &&
                (name.Contains("Catalog", StringComparison.Ordinal) ||
                 name.Contains("Category", StringComparison.Ordinal) ||
                 name.Contains("Node", StringComparison.Ordinal)))
            {
                return MessageCategory.CatalogSynchronization;
            }

            // Product
            if (ns.Contains("Commerce.Catalog", StringComparison.Ordinal) &&
                (name.Contains("Product", StringComparison.Ordinal) ||
                 name.Contains("Entry", StringComparison.Ordinal) ||
                 name.Contains("Variation", StringComparison.Ordinal) ||
                 name.Contains("Bundle", StringComparison.Ordinal) ||
                 name.Contains("Package", StringComparison.Ordinal)))
            {
                return MessageCategory.ProductSynchronization;
            }

            // Content
            if (ns.StartsWith("EPiServer", StringComparison.Ordinal) &&
                (name.Contains("Content", StringComparison.Ordinal) ||
                 name.Contains("Page", StringComparison.Ordinal) ||
                 name.Contains("Block", StringComparison.Ordinal)))
            {
                return MessageCategory.ContentSynchronization;
            }

            return MessageCategory.Unknown;
        }

        /// <summary>
        /// Creates a registry from EventProviderOptions.ParameterTypes (V13+ only).
        /// Requires: using EPiServer.Events; using Microsoft.Extensions.Options;
        ///
        /// Example:
        ///   var registry = EventParameterTypeRegistry.FromEventProviderOptions(eventProviderOptions.Value);
        ///   registry.ApplyTo(config);
        /// </summary>
        public static EventParameterTypeRegistry FromParameterTypes(IEnumerable<Type> parameterTypes)
        {
            var registry = new EventParameterTypeRegistry();
            registry.RegisterTypes(parameterTypes);
            return registry;
        }
    }
}

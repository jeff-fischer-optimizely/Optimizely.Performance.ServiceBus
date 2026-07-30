using System.Collections.Generic;

namespace OptiServiceBusPrioritizer.Core
{
    public class PriorityConfiguration
    {
        public Dictionary<MessageCategory, MessagePriority> PriorityMappings { get; set; } = new()
        {
            { MessageCategory.CartSynchronization, MessagePriority.Critical },
            { MessageCategory.OrderSynchronization, MessagePriority.Critical },
            { MessageCategory.PricingSynchronization, MessagePriority.High },
            { MessageCategory.InventorySynchronization, MessagePriority.High },
            { MessageCategory.ProductSynchronization, MessagePriority.High },
            { MessageCategory.CatalogSynchronization, MessagePriority.High },
            { MessageCategory.ContentSynchronization, MessagePriority.Normal },
            { MessageCategory.Unknown, MessagePriority.Low }
        };

        public List<string> CustomCartMessageTypes { get; set; } = new();
        public List<string> CustomPricingMessageTypes { get; set; } = new();
        public List<string> CustomInventoryMessageTypes { get; set; } = new();
        public List<string> CustomProductMessageTypes { get; set; } = new();
        public List<string> CustomContentMessageTypes { get; set; } = new();

        // Strong-typed mappings registered at runtime by consumers (V12/V13 projects)
        // Use exact Type mappings when you have access to Optimizely/EPiServer types.
        private readonly System.Collections.Generic.Dictionary<System.Type, MessageCategory> _typeMappings = new();

        // Predicate-based mappings allow registering functions that inspect a Type
        // (for example, by namespace or implemented interfaces) and return whether
        // it belongs to a category. These collections are internal to prevent
        // external mutation; use the AddPredicateMapping helper to modify them.
        private readonly System.Collections.Generic.List<System.Func<System.Type, bool>> _predicateKeys = new();
        private readonly System.Collections.Generic.List<MessageCategory> _predicateValues = new();

        // Synchronization object for thread-safe mutation/access
        private readonly object _sync = new();

        // Helper to register a direct Type -> MessageCategory mapping
        public void AddTypeMapping(System.Type type, MessageCategory category)
        {
            if (type == null) return;
            lock (_sync)
            {
                _typeMappings[type] = category;
            }
        }

        // Helper to register a predicate mapping. Predicates are evaluated in
        // registration order; the first matching predicate wins.
        public void AddPredicateMapping(System.Func<System.Type, bool> predicate, MessageCategory category)
        {
            if (predicate == null) return;
            lock (_sync)
            {
                _predicateKeys.Add(predicate);
                _predicateValues.Add(category);
            }
        }

        // Internal accessor used by the classifier to attempt to get an exact mapping
        internal bool TryGetTypeMapping(System.Type type, out MessageCategory category)
        {
            lock (_sync)
            {
                return _typeMappings.TryGetValue(type, out category);
            }
        }

        // Internal accessor used by the classifier to obtain snapshot copies of
        // predicate mappings for safe iteration without holding the lock.
        internal (System.Func<System.Type, bool>[] predicates, MessageCategory[] categories) GetPredicateMappingsSnapshot()
        {
            lock (_sync)
            {
                return (_predicateKeys.ToArray(), _predicateValues.ToArray());
            }
        }

        // Populate TypeMappings and predicate mappings from a set of assemblies.
        // This method attempts to resolve well-known Optimizely/EPiServer types and
        // register them for strong-typed classification. Call this from the
        // consuming application once the EPiServer assemblies are loaded.
        public void PopulateFromAssemblies(System.Reflection.Assembly[] assemblies)
        {
            if (assemblies == null) return;

            // Known candidate full type names grouped by category
            var cartTypes = new[]
            {
                "EPiServer.Commerce.Order.CartMessage",
                "EPiServer.Commerce.Order.WishListMessage",
                "EPiServer.Commerce.Order.CartItemMessage",
                "Mediachase.Commerce.Orders.Cart",
                "Mediachase.Commerce.Orders.ShoppingCart"
            };

            var pricingTypes = new[]
            {
                "EPiServer.Commerce.Catalog.PriceMessage",
                "EPiServer.Commerce.Catalog.PriceValueMessage",
                "Mediachase.Commerce.Catalog.Price",
                "Mediachase.Commerce.Pricing.CatalogEntryPrice"
            };

            var inventoryTypes = new[]
            {
                "EPiServer.Commerce.Catalog.InventoryMessage",
                "Mediachase.Commerce.Inventory.Warehouse",
                "Mediachase.Commerce.InventoryService.InventoryRecord"
            };

            var productTypes = new[]
            {
                "EPiServer.Commerce.Catalog.CatalogContentMessage",
                "EPiServer.Commerce.Catalog.ProductMessage",
                "EPiServer.Commerce.Catalog.VariationMessage",
                "EPiServer.Commerce.Catalog.BundleMessage",
                "EPiServer.Commerce.Catalog.PackageMessage",
                "Mediachase.Commerce.Catalog.Entry",
                "Mediachase.Commerce.Catalog.CatalogEntry"
            };

            var contentTypes = new[]
            {
                "EPiServer.Core.ContentMessage",
                "EPiServer.Core.PageMessage",
                "EPiServer.Core.BlockMessage",
                "EPiServer.ContentMessage",
                "EPiServer.ChangeNotificationMessage",
                "EPiServer.Core.ContentEventArgs"
            };

            void TryRegister(System.Collections.Generic.IEnumerable<string> names, MessageCategory cat)
            {
                foreach (var n in names)
                {
                    var t = ResolveTypeInAssemblies(n, assemblies);
                    if (t != null)
                    {
                        AddTypeMapping(t, cat);
                    }
                }
            }

            TryRegister(cartTypes, MessageCategory.CartSynchronization);
            TryRegister(pricingTypes, MessageCategory.PricingSynchronization);
            TryRegister(inventoryTypes, MessageCategory.InventorySynchronization);
            TryRegister(productTypes, MessageCategory.ProductSynchronization);
            TryRegister(contentTypes, MessageCategory.ContentSynchronization);

            // Add some predicate mappings as sensible defaults
            AddPredicateMapping(t => (t.Namespace ?? string.Empty).Contains("Commerce.Order"), MessageCategory.CartSynchronization);
            AddPredicateMapping(t => (t.Namespace ?? string.Empty).Contains("Commerce.Catalog"), MessageCategory.ProductSynchronization);
            AddPredicateMapping(t => (t.Namespace ?? string.Empty).Contains("EPiServer.Core") && t.Name.Contains("Content"), MessageCategory.ContentSynchronization);
        }

        private static System.Type? ResolveTypeInAssemblies(string fullName, System.Reflection.Assembly[] assemblies)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;
            // Check provided assemblies first
            foreach (var a in assemblies)
            {
                try
                {
                    var found = a.GetType(fullName, throwOnError: false, ignoreCase: true);
                    if (found != null) return found;
                }
                catch { }
            }

            // Fallback to Type.GetType which may resolve types in the default context
            try
            {
                var t = System.Type.GetType(fullName, throwOnError: false, ignoreCase: true);
                if (t != null) return t;
            }
            catch { }

            return null;
        }
    }
}

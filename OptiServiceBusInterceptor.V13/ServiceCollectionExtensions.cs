using Microsoft.Extensions.DependencyInjection;
using OptiServiceBusPrioritizer.Core;

namespace OptiServiceBusPrioritizer.V13
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOptiServiceBusPrioritizer(this IServiceCollection services)
        {
            // Register PriorityConfiguration and attempt to populate type mappings from
            // currently loaded assemblies. Consumers should call PopulateFromAssemblies
            // again in their startup after Optimizely assemblies are loaded if needed.
            services.AddSingleton(sp =>
            {
                var cfg = new PriorityConfiguration();
                try
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    cfg.PopulateFromAssemblies(assemblies);
                }
                catch { }
                return cfg;
            });

            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<OptimizelyMessageClassifier>>();
                var cfg = sp.GetRequiredService<PriorityConfiguration>();
                return new OptimizelyMessageClassifier(logger!, cfg);
            });
            return services;
        }

        public static IServiceCollection AddOptiServiceBusPrioritizer(
            this IServiceCollection services,
            PriorityConfiguration configuration)
        {
            services.AddSingleton(configuration);
            try
            {
                configuration.PopulateFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            }
            catch { }

            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<OptimizelyMessageClassifier>>();
                var cfg = sp.GetRequiredService<PriorityConfiguration>();
                return new OptimizelyMessageClassifier(logger!, cfg);
            });
            return services;
        }


        private static void RegisterDefaultTypeMappings(PriorityConfiguration config)
        {
            if (config == null) return;

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

            void TryRegister(string[] names, MessageCategory cat)
            {
                foreach (var n in names)
                {
                    var t = TryResolveType(n);
                    if (t != null)
                    {
                        config.AddTypeMapping(t, cat);
                    }
                }
            }

            TryRegister(cartTypes, MessageCategory.CartSynchronization);
            TryRegister(pricingTypes, MessageCategory.PricingSynchronization);
            TryRegister(inventoryTypes, MessageCategory.InventorySynchronization);
            TryRegister(productTypes, MessageCategory.ProductSynchronization);
            TryRegister(contentTypes, MessageCategory.ContentSynchronization);

            config.AddPredicateMapping(t => (t.Namespace ?? string.Empty).Contains("Commerce.Order"), MessageCategory.CartSynchronization);
            config.AddPredicateMapping(t => (t.Namespace ?? string.Empty).Contains("Commerce.Catalog"), MessageCategory.ProductSynchronization);
            config.AddPredicateMapping(t => (t.Namespace ?? string.Empty).Contains("EPiServer.Core") && t.Name.Contains("Content"), MessageCategory.ContentSynchronization);
        }

        private static System.Type? TryResolveType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;
            var t = System.Type.GetType(fullName, throwOnError: false, ignoreCase: true);
            if (t != null) return t;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var found = a.GetType(fullName, throwOnError: false, ignoreCase: true);
                    if (found != null) return found;
                }
                catch { }
            }
            return null;
        }
    }
}

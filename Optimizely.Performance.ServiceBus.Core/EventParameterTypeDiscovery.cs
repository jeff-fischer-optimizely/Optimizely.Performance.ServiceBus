using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Optimizely.Performance.ServiceBus.Core
{
    /// <summary>
    /// Discovers and catalogs Optimizely event parameter types from loaded assemblies.
    /// This utility helps identify which types are actually sent via Service Bus.
    ///
    /// Usage:
    ///   var discovery = new EventParameterTypeDiscovery();
    ///   discovery.ScanAssemblies(AppDomain.CurrentDomain.GetAssemblies());
    ///   var types = discovery.GetDiscoveredTypes();
    /// </summary>
    public class EventParameterTypeDiscovery
    {
        private readonly HashSet<Type> _discoveredTypes = new();
        private readonly Dictionary<string, List<Type>> _typesByNamespace = new();

        /// <summary>
        /// Scans the provided assemblies for types that are likely to be sent as EventMessage.Parameter.
        /// This includes types from known Optimizely namespaces and types marked with serialization attributes.
        /// </summary>
        public void ScanAssemblies(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic)
                    continue;

                try
                {
                    ScanAssembly(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be fully loaded
                    continue;
                }
                catch (Exception)
                {
                    // Skip problematic assemblies
                    continue;
                }
            }
        }

        /// <summary>
        /// Scans assemblies whose names match the given prefixes (e.g., "EPiServer", "Mediachase").
        /// </summary>
        public void ScanOptimizelyAssemblies(params string[] assemblyPrefixes)
        {
            var prefixes = assemblyPrefixes.Length > 0
                ? assemblyPrefixes
                : new[] { "EPiServer", "Mediachase", "Optimizely" };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && prefixes.Any(p => a.GetName().Name?.StartsWith(p, StringComparison.OrdinalIgnoreCase) == true))
                .ToArray();

            ScanAssemblies(assemblies);
        }

        private void ScanAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(IsLikelyEventParameterType)
                .ToArray();

            foreach (var type in types)
            {
                _discoveredTypes.Add(type);

                var ns = type.Namespace ?? "(no namespace)";
                if (!_typesByNamespace.ContainsKey(ns))
                    _typesByNamespace[ns] = new List<Type>();

                _typesByNamespace[ns].Add(type);
            }
        }

        private bool IsLikelyEventParameterType(Type type)
        {
            if (!type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            var ns = type.Namespace ?? string.Empty;

            // Known Optimizely event namespaces
            if (ns.StartsWith("EPiServer.Commerce.Order", StringComparison.Ordinal) ||
                ns.StartsWith("EPiServer.Commerce.Catalog", StringComparison.Ordinal) ||
                ns.StartsWith("Mediachase.Commerce", StringComparison.Ordinal) ||
                ns.StartsWith("EPiServer.Core", StringComparison.Ordinal) ||
                ns.StartsWith("EPiServer.Data", StringComparison.Ordinal))
            {
                // Check if it looks like a message or event args type
                var name = type.Name;
                if (name.Contains("Message", StringComparison.Ordinal) ||
                    name.Contains("EventArgs", StringComparison.Ordinal) ||
                    name.Contains("Event", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // Types with serialization attributes are often sent over the wire
            if (type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0)
            {
                if (ns.StartsWith("EPiServer", StringComparison.Ordinal) ||
                    ns.StartsWith("Mediachase", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all discovered types.
        /// </summary>
        public IReadOnlyCollection<Type> GetDiscoveredTypes()
        {
            return _discoveredTypes.ToArray();
        }

        /// <summary>
        /// Returns discovered types grouped by namespace.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<Type>> GetTypesByNamespace()
        {
            return _typesByNamespace.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<Type>)kvp.Value.ToArray()
            );
        }

        /// <summary>
        /// Returns discovered types filtered by namespace pattern.
        /// </summary>
        public IEnumerable<Type> GetTypesInNamespace(string namespacePattern)
        {
            return _discoveredTypes.Where(t =>
                (t.Namespace ?? string.Empty).Contains(namespacePattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Generates a report of discovered types for diagnostics.
        /// </summary>
        public string GenerateReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Discovered {_discoveredTypes.Count} potential event parameter types:");
            report.AppendLine();

            foreach (var kvp in _typesByNamespace.OrderBy(kvp => kvp.Key))
            {
                var ns = kvp.Key;
                var types = kvp.Value;
                report.AppendLine($"Namespace: {ns} ({types.Count} types)");
                foreach (var type in types.OrderBy(t => t.Name))
                {
                    report.AppendLine($"  - {type.Name}");
                    if (type.FullName != null && type.FullName.Length > type.Name.Length + ns.Length + 1)
                    {
                        report.AppendLine($"    Full: {type.FullName}");
                    }
                }
                report.AppendLine();
            }

            return report.ToString();
        }

        /// <summary>
        /// Auto-registers discovered types with a PriorityConfiguration based on namespace patterns.
        /// </summary>
        public void AutoRegisterTypes(PriorityConfiguration config)
        {
            foreach (var type in _discoveredTypes)
            {
                var category = ClassifyType(type);
                if (category != MessageCategory.Unknown)
                {
                    config.AddTypeMapping(type, category);
                }
            }
        }

        private MessageCategory ClassifyType(Type type)
        {
            var ns = type.Namespace ?? string.Empty;
            var name = type.Name;

            // Cart/Order
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
    }
}

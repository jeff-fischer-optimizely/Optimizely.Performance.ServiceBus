using System;
using System.Linq;
using System.Text;

namespace OptiServiceBusPrioritizer.Core
{
    /// <summary>
    /// Runtime helper to discover Optimizely/EPiServer types in the current AppDomain
    /// and emit PriorityConfiguration.AddTypeMapping(...) registration lines that
    /// can be copy-pasted into the consuming project's startup. This avoids hard-
    /// coding string type names into the core library while allowing precise
    /// Type-based mappings to be generated on a machine that has the Optimizely
    /// assemblies available.
    ///
    /// Usage (in consuming app after assemblies loaded):
    ///     var code = TypeMappingGenerator.GenerateMappingsFromLoadedAssemblies();
    ///     Console.WriteLine(code);
    ///
    /// Then paste the printed lines into your startup or commit them back into
    /// PriorityConfiguration.PopulateFromAssemblies as concrete Type registrations.
    /// </summary>
    public static class TypeMappingGenerator
    {
        public static string GenerateMappingsFromLoadedAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var sb = new StringBuilder();

            // Candidate category detectors by simple name heuristics.
            // These heuristics are intentionally broad to surface potential types.
            bool IsCart(Type t)
                => t.Name.IndexOf("Cart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (t.Namespace ?? string.Empty).IndexOf("Commerce.Order", StringComparison.OrdinalIgnoreCase) >= 0;

            bool IsPricing(Type t)
                => t.Name.IndexOf("Price", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (t.Namespace ?? string.Empty).IndexOf("Pricing", StringComparison.OrdinalIgnoreCase) >= 0;

            bool IsInventory(Type t)
                => t.Name.IndexOf("Inventory", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   t.Name.IndexOf("Warehouse", StringComparison.OrdinalIgnoreCase) >= 0;

            bool IsProduct(Type t)
                => t.Name.IndexOf("Product", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (t.Namespace ?? string.Empty).IndexOf("Commerce.Catalog", StringComparison.OrdinalIgnoreCase) >= 0;

            bool IsContent(Type t)
                => t.Name.IndexOf("Content", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   t.Name.IndexOf("PageMessage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   t.Name.IndexOf("ContentEventArgs", StringComparison.OrdinalIgnoreCase) >= 0;

            foreach (var asm in assemblies)
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue; // skip dynamic / reflection-only assemblies
                }

                foreach (var t in types)
                {
                    if (t == null || string.IsNullOrEmpty(t.FullName)) continue;

                    MessageCategory? cat = null;
                    if (IsCart(t)) cat = MessageCategory.CartSynchronization;
                    else if (IsPricing(t)) cat = MessageCategory.PricingSynchronization;
                    else if (IsInventory(t)) cat = MessageCategory.InventorySynchronization;
                    else if (IsProduct(t)) cat = MessageCategory.ProductSynchronization;
                    else if (IsContent(t)) cat = MessageCategory.ContentSynchronization;

                    if (cat != null)
                    {
                        sb.AppendLine($"// Assembly: {asm.GetName().Name}");
                        sb.AppendLine($"config.AddTypeMapping(typeof({t.FullName}), MessageCategory.{cat});");
                        sb.AppendLine();
                    }
                }
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("// No candidate types detected in loaded assemblies.");
                sb.AppendLine("// Ensure the Optimizely assemblies are loaded into the AppDomain before calling this method.");
            }

            return sb.ToString();
        }
    }
}

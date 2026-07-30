// This file shows the recommended V13+ integration approach using EventProviderOptions.
// Copy this code to your V13 project's Startup.cs or ServiceCollectionExtensions.

#if EXAMPLE_CODE_V13
using EPiServer.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Optimizely.Performance.ServiceBus;
using Optimizely.Performance.ServiceBus.Core;

namespace YourProject
{
    /// <summary>
    /// Example V13+ service registration using EventProviderOptions.
    /// This is the SIMPLEST and RECOMMENDED approach for CMS 13+.
    /// </summary>
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            // V13+ RECOMMENDED: Pass EventProviderOptions.ParameterTypes directly
            // This is the cleanest approach - no reflection, no assembly scanning
            services.AddOptiServiceBusPrioritizer(
                getEventParameterTypes: sp => sp.GetRequiredService<IOptions<EventProviderOptions>>()
                                                .Value
                                                .ParameterTypes,
                configureOptions: options =>
                {
                    // Optional: customize priorities
                    options.Priorities["CartSynchronization"] = "Critical";
                    options.Priorities["PricingSynchronization"] = "High";

                    // Optional: add namespace mappings
                    options.NamespaceMappings["YourCustom.Events"] = "ContentSynchronization";
                });

            // Alternative: Use default configuration with assembly scanning (works but slower)
            // services.AddOptiServiceBusPrioritizer();
        }
    }
}
#endif

/*
=================================================================================================
V11/V12 USAGE (in consuming project):
=================================================================================================

using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.ServiceBus;

public static void ConfigureServices(IServiceCollection services)
{
    // V11/V12: Use assembly scanning (no EventProviderOptions available)
    services.AddOptiServiceBusPrioritizer(options =>
    {
        options.Priorities["CartSynchronization"] = "Critical";
        options.EnableAutoDiscovery = true; // Scans loaded assemblies
    });
}

=================================================================================================
V13 USAGE (in consuming project):
=================================================================================================

using EPiServer.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Optimizely.Performance.ServiceBus;

public static void ConfigureServices(IServiceCollection services)
{
    // V13: Use EventProviderOptions.ParameterTypes (RECOMMENDED - fastest, most accurate)
    services.AddOptiServiceBusPrioritizer(
        sp => sp.GetRequiredService<IOptions<EventProviderOptions>>().Value.ParameterTypes,
        options =>
        {
            options.Priorities["CartSynchronization"] = "Critical";
        });
}

=================================================================================================
*/

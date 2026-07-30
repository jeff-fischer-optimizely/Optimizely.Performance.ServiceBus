/*
=================================================================================================
USAGE EXAMPLES - Optimizely Service Bus Message Prioritization
=================================================================================================

ALL VERSIONS (V11/V12/V13) - AUTOMATIC DETECTION:
=================================================================================================
The library automatically detects your Optimizely version:
- V13+: Uses EventProviderOptions.ParameterTypes (fastest, most accurate)
- V11/V12: Falls back to assembly scanning

using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.ServiceBus;

public static void ConfigureServices(IServiceCollection services)
{
    // Simplest - works for all versions with auto-detection
    services.AddOptiServiceBusPrioritizer();

    // With custom configuration
    services.AddOptiServiceBusPrioritizer(options =>
    {
        options.Priorities["CartSynchronization"] = "Critical";
        options.Priorities["PricingSynchronization"] = "High";
        options.Priorities["ContentSynchronization"] = "Normal";
    });
}

=================================================================================================
ADVANCED - CUSTOM NAMESPACE MAPPINGS:
=================================================================================================

services.AddOptiServiceBusPrioritizer(options =>
{
    // Priority levels
    options.Priorities["CartSynchronization"] = "Critical";

    // Map custom namespaces to categories
    options.NamespaceMappings["YourCompany.Commerce.Events"] = "CartSynchronization";
    options.NamespaceMappings["YourCompany.Catalog.Events"] = "ProductSynchronization";

    // Enable/disable auto-discovery (true by default for V11/V12)
    options.EnableAutoDiscovery = true;
});

=================================================================================================
ADVANCED - PRE-CONFIGURED PRIORITY CONFIGURATION:
=================================================================================================

using Optimizely.Performance.ServiceBus.Core;

public static void ConfigureServices(IServiceCollection services)
{
    // Build configuration manually for full control
    var config = PriorityConfigurationBuilder.Create()
        .WithCategoryPriority(MessageCategory.CartSynchronization, MessagePriority.Critical)
        .WithNamespaceMapping("YourCompany.Commerce", MessageCategory.CartSynchronization)
        .Build();

    services.AddOptiServiceBusPrioritizer(config);
}

=================================================================================================
*/

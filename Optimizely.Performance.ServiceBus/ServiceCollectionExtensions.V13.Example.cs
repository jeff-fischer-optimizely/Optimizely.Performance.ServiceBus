// This file shows the recommended V13+ integration approach using EventProviderOptions.
// This is example code - do not include in the actual library.

#if EXAMPLE_CODE_V13
using EPiServer.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus.Examples
{
    /// <summary>
    /// Example V13+ service registration using EventProviderOptions.
    /// This is the SIMPLEST and RECOMMENDED approach for CMS 13+.
    /// </summary>
    public static class V13ServiceCollectionExtensions
    {
        /// <summary>
        /// V13+ recommended approach - leverages EventProviderOptions.ParameterTypes.
        /// This automatically gets all registered event types from Optimizely.
        /// </summary>
        public static IServiceCollection AddOptiServiceBusPrioritizerV13(
            this IServiceCollection services,
            Action<MessagePrioritizationOptions>? configureOptions = null)
        {
            // Register configuration options
            var options = new MessagePrioritizationOptions();
            configureOptions?.Invoke(options);

            // Build PriorityConfiguration using EventProviderOptions
            services.AddSingleton<PriorityConfiguration>(sp =>
            {
                // Get EventProviderOptions from DI - it already contains all registered event types
                var eventProviderOptions = sp.GetService<IOptions<EventProviderOptions>>();

                var builder = PriorityConfigurationBuilder.Create()
                    .WithOptions(options);

                // If EventProviderOptions is available, use it (V13+)
                if (eventProviderOptions?.Value?.ParameterTypes != null)
                {
                    builder.WithEventParameterTypes(eventProviderOptions.Value.ParameterTypes);
                }
                else
                {
                    // Fallback to assembly scanning (V11/V12)
                    builder.WithAutoDiscovery(AppDomain.CurrentDomain.GetAssemblies());
                }

                return builder.Build();
            });

            // Register classifier
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();

            return services;
        }
    }
}
#endif

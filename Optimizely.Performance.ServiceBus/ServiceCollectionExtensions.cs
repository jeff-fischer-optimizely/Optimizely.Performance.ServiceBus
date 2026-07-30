using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Optimizely.Performance.ServiceBus.Core;

#if NET6_0_OR_GREATER
using EPiServer.Events;
#endif

namespace Optimizely.Performance.ServiceBus
{
    /// <summary>
    /// Extension methods for registering Optimizely Service Bus message prioritization.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Service Bus message prioritization with default configuration.
        /// Automatically uses EventProviderOptions.ParameterTypes if available (V13+),
        /// otherwise falls back to assembly scanning (V11/V12).
        /// </summary>
        public static IServiceCollection AddOptiServiceBusPrioritizer(this IServiceCollection services)
        {
            return AddOptiServiceBusPrioritizer(services, configureOptions: null);
        }

        /// <summary>
        /// Adds Service Bus message prioritization with configuration.
        /// Automatically uses EventProviderOptions.ParameterTypes if available (V13+),
        /// otherwise falls back to assembly scanning (V11/V12).
        ///
        /// Example appsettings.json:
        /// {
        ///   "Optimizely": {
        ///     "ServiceBus": {
        ///       "MessagePrioritization": {
        ///         "Priorities": {
        ///           "CartSynchronization": "Critical",
        ///           "PricingSynchronization": "High"
        ///         }
        ///       }
        ///     }
        ///   }
        /// }
        /// </summary>
        public static IServiceCollection AddOptiServiceBusPrioritizer(
            this IServiceCollection services,
            Action<MessagePrioritizationOptions>? configureOptions)
        {
            // Register configuration options
            var options = new MessagePrioritizationOptions();
            configureOptions?.Invoke(options);

            // Build PriorityConfiguration with smart version detection
            services.AddSingleton(sp =>
            {
                var builder = PriorityConfigurationBuilder.Create()
                    .WithOptions(options);

#if NET6_0_OR_GREATER
                // V13+: Try to get EventProviderOptions.ParameterTypes
                var eventProviderOptions = sp.GetService<IOptions<EventProviderOptions>>();
                if (eventProviderOptions?.Value != null)
                {
                    // Try to get ParameterTypes property via reflection (only exists in V13+)
                    var parameterTypesProperty = eventProviderOptions.Value.GetType().GetProperty("ParameterTypes");
                    if (parameterTypesProperty != null)
                    {
                        var parameterTypes = parameterTypesProperty.GetValue(eventProviderOptions.Value) as IEnumerable<Type>;
                        if (parameterTypes != null && parameterTypes.Any())
                        {
                            builder.WithEventParameterTypes(parameterTypes);
                            return builder.Build();
                        }
                    }
                }
#endif

                // V11/V12 or V13 fallback: Use assembly scanning if enabled
                if (options.EnableAutoDiscovery)
                {
                    builder.WithAutoDiscovery(AppDomain.CurrentDomain.GetAssemblies());
                }

                return builder.Build();
            });

            // Register classifier
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();

            return services;
        }

        /// <summary>
        /// Adds Service Bus message prioritization with a pre-configured PriorityConfiguration instance.
        /// Useful for advanced scenarios where you want full control over configuration.
        /// </summary>
        public static IServiceCollection AddOptiServiceBusPrioritizer(
            this IServiceCollection services,
            PriorityConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddSingleton(configuration);
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();

            return services;
        }
    }
}

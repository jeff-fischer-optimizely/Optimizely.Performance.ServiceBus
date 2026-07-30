using System;
using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus
{
    /// <summary>
    /// Extension methods for registering Optimizely Service Bus message prioritization.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Service Bus message prioritization with default configuration.
        /// Auto-discovers types from currently loaded Optimizely assemblies.
        /// </summary>
        public static IServiceCollection AddOptiServiceBusPrioritizer(this IServiceCollection services)
        {
            return AddOptiServiceBusPrioritizer(services, configureOptions: null);
        }

        /// <summary>
        /// Adds Service Bus message prioritization with configuration from appsettings.json.
        ///
        /// For V13+: Automatically uses EventProviderOptions.ParameterTypes if available.
        /// For V11/V12: Falls back to assembly scanning.
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

                // Try to use EventProviderOptions if available (V13+)
                var eventProviderOptionsType = Type.GetType("EPiServer.Events.EventProviderOptions, EPiServer.Framework");
                if (eventProviderOptionsType != null)
                {
                    // V13+ path: Try to get EventProviderOptions from DI
                    try
                    {
                        var optionsType = typeof(Microsoft.Extensions.Options.IOptions<>).MakeGenericType(eventProviderOptionsType);
                        var eventProviderOptions = sp.GetService(optionsType);

                        if (eventProviderOptions != null)
                        {
                            var valueProperty = optionsType.GetProperty("Value");
                            var eventOptions = valueProperty?.GetValue(eventProviderOptions);

                            if (eventOptions != null)
                            {
                                var parameterTypesProperty = eventProviderOptionsType.GetProperty("ParameterTypes");
                                var parameterTypes = parameterTypesProperty?.GetValue(eventOptions) as System.Collections.IEnumerable;

                                if (parameterTypes != null)
                                {
                                    var typeList = new System.Collections.Generic.List<Type>();
                                    foreach (var item in parameterTypes)
                                    {
                                        if (item is Type type)
                                            typeList.Add(type);
                                    }

                                    if (typeList.Count > 0)
                                    {
                                        builder.WithEventParameterTypes(typeList);
                                        return builder.Build();
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fall through to V11/V12 approach
                    }
                }

                // V11/V12 fallback: Use assembly scanning
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

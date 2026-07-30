using System;
using System.Collections.Generic;
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
        /// Uses assembly scanning for type discovery (works for V11/V12/V13).
        ///
        /// For V13+: Use the overload that takes IEnumerable&lt;Type&gt; parameter types for better performance.
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

            // Build PriorityConfiguration using assembly scanning (V11/V12/V13 compatible)
            services.AddSingleton(sp =>
            {
                var builder = PriorityConfigurationBuilder.Create()
                    .WithOptions(options);

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
        /// Adds Service Bus message prioritization with explicit event parameter types.
        /// Recommended for V13+ - pass EventProviderOptions.Value.ParameterTypes directly.
        ///
        /// Example for V13+:
        ///   services.AddOptiServiceBusPrioritizer(
        ///       sp => sp.GetRequiredService&lt;IOptions&lt;EventProviderOptions&gt;&gt;().Value.ParameterTypes,
        ///       options => { ... });
        /// </summary>
        public static IServiceCollection AddOptiServiceBusPrioritizer(
            this IServiceCollection services,
            Func<IServiceProvider, IEnumerable<Type>> getEventParameterTypes,
            Action<MessagePrioritizationOptions>? configureOptions = null)
        {
            if (getEventParameterTypes == null)
                throw new ArgumentNullException(nameof(getEventParameterTypes));

            // Register configuration options
            var options = new MessagePrioritizationOptions();
            configureOptions?.Invoke(options);

            // Build PriorityConfiguration using provided types
            services.AddSingleton(sp =>
            {
                var parameterTypes = getEventParameterTypes(sp);

                var builder = PriorityConfigurationBuilder.Create()
                    .WithOptions(options)
                    .WithEventParameterTypes(parameterTypes);

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

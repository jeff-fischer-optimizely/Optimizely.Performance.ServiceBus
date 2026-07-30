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
        /// Binds "Optimizely:ServiceBus:MessagePrioritization" section.
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

            // Build PriorityConfiguration
            services.AddSingleton(sp =>
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var config = PriorityConfigurationBuilder.Create()
                    .WithOptions(options)
                    .WithAutoDiscovery(assemblies)
                    .Build();

                return config;
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

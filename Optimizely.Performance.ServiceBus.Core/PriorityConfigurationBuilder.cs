using System;
using System.Reflection;

namespace Optimizely.Performance.ServiceBus.Core
{
    /// <summary>
    /// Fluent builder for creating and configuring PriorityConfiguration instances.
    /// Works across .NET Framework 4.72 to .NET 10.
    ///
    /// Usage:
    ///   var config = PriorityConfigurationBuilder.Create()
    ///       .WithOptions(options)
    ///       .WithAutoDiscovery(AppDomain.CurrentDomain.GetAssemblies())
    ///       .Build();
    /// </summary>
    public class PriorityConfigurationBuilder
    {
        private readonly PriorityConfiguration _config = new();
        private MessagePrioritizationOptions? _options;
        private Assembly[]? _assembliesToScan;

        private PriorityConfigurationBuilder()
        {
        }

        /// <summary>
        /// Creates a new configuration builder.
        /// </summary>
        public static PriorityConfigurationBuilder Create()
        {
            return new PriorityConfigurationBuilder();
        }

        /// <summary>
        /// Applies configuration from MessagePrioritizationOptions.
        /// This is typically loaded from appsettings.json via IOptions.
        /// </summary>
        public PriorityConfigurationBuilder WithOptions(MessagePrioritizationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Enables automatic type discovery from the specified assemblies.
        /// </summary>
        public PriorityConfigurationBuilder WithAutoDiscovery(params Assembly[] assemblies)
        {
            _assembliesToScan = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
            return this;
        }

        /// <summary>
        /// Adds a specific namespace-to-category mapping.
        /// </summary>
        public PriorityConfigurationBuilder WithNamespaceMapping(string namespacePrefix, MessageCategory category)
        {
            if (string.IsNullOrWhiteSpace(namespacePrefix))
                throw new ArgumentException("Namespace prefix cannot be null or whitespace.", nameof(namespacePrefix));

            _config.AddPredicateMapping(
                type => (type.Namespace ?? string.Empty).StartsWith(namespacePrefix, StringComparison.Ordinal),
                category
            );
            return this;
        }

        /// <summary>
        /// Adds a specific type-to-category mapping.
        /// </summary>
        public PriorityConfigurationBuilder WithTypeMapping(Type type, MessageCategory category)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            _config.AddTypeMapping(type, category);
            return this;
        }

        /// <summary>
        /// Sets the priority for a specific category.
        /// </summary>
        public PriorityConfigurationBuilder WithCategoryPriority(MessageCategory category, MessagePriority priority)
        {
            _config.PriorityMappings[category] = priority;
            return this;
        }

        /// <summary>
        /// Builds the final PriorityConfiguration instance.
        /// </summary>
        public PriorityConfiguration Build()
        {
            // Apply options if provided
            if (_options != null)
            {
                _options.ApplyTo(_config);
            }

            // Run auto-discovery if enabled
            if (_options?.EnableAutoDiscovery == true && _assembliesToScan != null && _assembliesToScan.Length > 0)
            {
                var discovery = new EventParameterTypeDiscovery();
                discovery.ScanAssemblies(_assembliesToScan);
                discovery.AutoRegisterTypes(_config);
            }
            else if (_assembliesToScan != null && _assembliesToScan.Length > 0 && _options == null)
            {
                // Auto-discovery requested but no options - use default behavior
                var discovery = new EventParameterTypeDiscovery();
                discovery.ScanAssemblies(_assembliesToScan);
                discovery.AutoRegisterTypes(_config);
            }

            return _config;
        }
    }
}

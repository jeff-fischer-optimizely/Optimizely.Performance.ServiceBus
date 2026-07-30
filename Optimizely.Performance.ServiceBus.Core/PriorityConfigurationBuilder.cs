using System;
using System.Collections.Generic;
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
        /// For V11/V12 - scans assemblies heuristically.
        /// For V13+ - prefer using WithEventParameterTypes() with EventProviderOptions.ParameterTypes instead.
        /// </summary>
        public PriorityConfigurationBuilder WithAutoDiscovery(params Assembly[] assemblies)
        {
            _assembliesToScan = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
            return this;
        }

        /// <summary>
        /// Registers event parameter types directly (V13+ recommended approach).
        /// Use this with IOptions&lt;EventProviderOptions&gt;.Value.ParameterTypes for best results.
        ///
        /// Example for V13+:
        ///   builder.WithEventParameterTypes(eventProviderOptions.Value.ParameterTypes)
        /// </summary>
        public PriorityConfigurationBuilder WithEventParameterTypes(IEnumerable<Type> parameterTypes)
        {
            if (parameterTypes == null)
                throw new ArgumentNullException(nameof(parameterTypes));

            var registry = EventParameterTypeRegistry.FromParameterTypes(parameterTypes);
            registry.ApplyTo(_config);
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

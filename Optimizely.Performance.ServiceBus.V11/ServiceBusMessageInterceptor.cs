using System;
using System.Reflection;
// using EPiServer.Events;
// using EPiServer.Framework;
// using EPiServer.Framework.Initialization;
// using EPiServer.ServiceLocation;
using Microsoft.Extensions.Logging;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus.V11
{
    // NOTE: Uncomment when Optimizely CMS 11 packages are available
    // [InitializableModule]
    // [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    // public class ServiceBusMessageInterceptor : IInitializableModule
    public class ServiceBusMessageInterceptor
    {
        private ILogger<ServiceBusMessageInterceptor>? _logger;
        private IMessageClassifier? _classifier;
        private bool _isInitialized;

        // NOTE: Implementation commented out until Optimizely CMS 11 packages are added to consuming project
        /*
        public void Initialize(InitializationEngine context)
        {
            if (_isInitialized) return;

            _logger = context.Locate.Advanced.GetInstance<ILogger<ServiceBusMessageInterceptor>>();
            _classifier = context.Locate.Advanced.GetInstance<IMessageClassifier>();

            try
            {
                var eventRegistry = context.Locate.Advanced.GetInstance<EventRegistry>();
                InterceptEventRegistry(eventRegistry);
                _logger?.LogInformation("Service Bus Message Interceptor initialized successfully for CMS 11");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize Service Bus Message Interceptor");
                throw;
            }

            _isInitialized = true;
        }

        private void InterceptEventRegistry(EventRegistry eventRegistry)
        {
            var providerField = typeof(EventRegistry).GetField("_provider", BindingFlags.NonPublic | BindingFlags.Instance);
            if (providerField == null)
            {
                _logger?.LogWarning("Unable to find _provider field in EventRegistry");
                return;
            }

            var provider = providerField.GetValue(eventRegistry);
            if (provider == null)
            {
                _logger?.LogWarning("EventRegistry provider is null");
                return;
            }

            var clientField = provider.GetType().GetField("_topicClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clientField == null)
            {
                _logger?.LogWarning("Unable to find _topicClient field");
                return;
            }

            var originalClient = clientField.GetValue(provider);
            if (originalClient == null)
            {
                _logger?.LogWarning("Original TopicClient is null");
                return;
            }

            var wrappedClient = new PriorityTopicClientWrapper(originalClient, _classifier!, _logger!);
            clientField.SetValue(provider, wrappedClient);

            _logger?.LogInformation("Successfully wrapped TopicClient with priority interceptor");
        }

        public void Uninitialize(InitializationEngine context)
        {
            _isInitialized = false;
        }
        */
    }

    /*
    internal class PriorityTopicClientWrapper
    {
        private readonly object _innerClient;
        private readonly IMessageClassifier _classifier;
        private readonly ILogger _logger;
        private readonly MethodInfo? _sendMethod;

        public PriorityTopicClientWrapper(object innerClient, IMessageClassifier classifier, ILogger logger)
        {
            _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _sendMethod = _innerClient.GetType().GetMethod("Send", new[] { typeof(object) });
        }

        public void Send(object message)
        {
            if (message == null)
            {
                _sendMethod?.Invoke(_innerClient, new object[] { message! });
                return;
            }

            try
            {
                // Deserialize EventMessage from brokered message and inspect Parameter's Type
                // Then call classifier.ClassifyMessage(parameter.GetType(), parameter)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting message priority, sending with default priority");
            }

            _sendMethod?.Invoke(_innerClient, new object[] { message });
        }
    }
    */
}

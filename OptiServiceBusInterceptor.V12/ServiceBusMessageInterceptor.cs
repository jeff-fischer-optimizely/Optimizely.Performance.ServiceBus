using System;
using System.Reflection;
// using EPiServer.Events;
// using EPiServer.Framework;
// using EPiServer.Framework.Initialization;
// using EPiServer.ServiceLocation;
using Microsoft.Extensions.Logging;
// using Microsoft.ServiceBus.Messaging;
using OptiServiceBusPrioritizer.Core;

namespace OptiServiceBusPrioritizer.V12
{
    // NOTE: Uncomment when Optimizely CMS 12 packages are available
    // [InitializableModule]
    // [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    // public class ServiceBusMessageInterceptor : IInitializableModule
    public class ServiceBusMessageInterceptor
    {
        private ILogger<ServiceBusMessageInterceptor>? _logger;
        private IMessageClassifier? _classifier;
        private bool _isInitialized;

        // NOTE: Implementation commented out until Optimizely CMS 12 packages are added to consuming project
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
                _logger?.LogInformation("Service Bus Message Interceptor initialized successfully for CMS 12");
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

            _sendMethod = _innerClient.GetType().GetMethod("Send", new[] { typeof(BrokeredMessage) });
        }

        public void Send(BrokeredMessage message)
        {
            if (message == null)
            {
                _sendMethod?.Invoke(_innerClient, new object[] { message! });
                return;
            }

            try
            {
                // Prefer strong-typed classification: Optimizely sends an EventMessage where
                // the actual payload is in EventMessage.Parameter. Consumers who enable this
                // interceptor should deserialize the EventMessage and pass the Parameter's
                // System.Type to the classifier. Example pattern:

                // Example strong-typed classification flow (implement in consuming project):
                // 1) Deserialize the EPiServer.Events.EventMessage from the BrokeredMessage body.
                //    EventMessage.Parameter contains the actual payload object.
                // 2) If deserialization succeeds:
                //       var category = _classifier.ClassifyMessage(eventParameter.GetType(), eventParameter);
                //    If deserialization fails but you have a type name, try to resolve the Type:
                //       var resolved = Type.GetType(typeName, throwOnError: false);
                //       if (resolved != null) category = _classifier.ClassifyMessage(resolved, null);
                // 3) If no type can be resolved, use MessageCategory.Unknown.
                // 4) Set BrokeredMessage.Properties["Priority"] and other properties accordingly.

                // NOTE: Implement TryDeserializeEventMessageParameterFromBrokeredMessage in the
                // consuming project where EPiServer types (EventMessage) are available.
                var priority = _classifier.GetPriority(category);
                var serviceBusPriority = priority.ToServiceBusPriority();

                if (!message.Properties.ContainsKey("Priority"))
                {
                    message.Properties["Priority"] = serviceBusPriority;
                }

                message.Properties["MessageCategory"] = category.ToString();
                message.Properties["ClassifiedPriority"] = priority.ToString();

                _logger.LogDebug(
                    "Set message priority: Type={MessageType}, Category={Category}, Priority={Priority}, ServiceBusPriority={ServiceBusPriority}",
                    messageType, category, priority, serviceBusPriority);
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

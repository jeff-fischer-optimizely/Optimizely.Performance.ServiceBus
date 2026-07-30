using System;
using System.Reflection;
#if CMS13
using Azure.Messaging.ServiceBus;
#endif
// using EPiServer.Events;
// using EPiServer.Framework;
// using EPiServer.Framework.Initialization;
// using EPiServer.ServiceLocation;
using Microsoft.Extensions.Logging;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus
{
    // NOTE: Uncomment when appropriate Optimizely CMS packages are available in the consuming project
    // [InitializableModule]
    // [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    // public class ServiceBusMessageInterceptor : IInitializableModule
    public class ServiceBusMessageInterceptor
    {
        private ILogger<ServiceBusMessageInterceptor>? _logger;
        private IMessageClassifier? _classifier;
        private bool _isInitialized;

        // NOTE: Implementation commented out until Optimizely CMS packages are added to consuming project
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
                _logger?.LogInformation("Service Bus Message Interceptor initialized successfully");
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

#if CMS13
            var senderField = provider.GetType().GetField("_sender", BindingFlags.NonPublic | BindingFlags.Instance);
            if (senderField == null)
            {
                _logger?.LogWarning("Unable to find _sender field");
                return;
            }

            var originalSender = senderField.GetValue(provider);
            if (originalSender == null)
            {
                _logger?.LogWarning("Original ServiceBusSender is null");
                return;
            }

            var wrappedSender = new PriorityServiceBusSenderWrapper(originalSender, _classifier!, _logger!);
            senderField.SetValue(provider, wrappedSender);
#else
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
#endif

            _logger?.LogInformation("Successfully wrapped message sender with priority interceptor");
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

#if CMS13
    /*
    internal class PriorityServiceBusSenderWrapper
    {
        private readonly object _innerSender;
        private readonly IMessageClassifier _classifier;
        private readonly ILogger _logger;
        private readonly MethodInfo? _sendMessageMethod;

        public PriorityServiceBusSenderWrapper(object innerSender, IMessageClassifier classifier, ILogger logger)
        {
            _innerSender = innerSender ?? throw new ArgumentNullException(nameof(innerSender));
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _sendMessageMethod = _innerSender.GetType().GetMethod("SendMessageAsync", new[] { typeof(ServiceBusMessage) });
        }

        public async System.Threading.Tasks.Task SendMessageAsync(ServiceBusMessage message)
        {
            if (message == null)
            {
                if (_sendMessageMethod != null)
                {
                    await ((System.Threading.Tasks.Task)_sendMessageMethod.Invoke(_innerSender, new object[] { message! })!).ConfigureAwait(false);
                }
                return;
            }

            // Example strong-typed classification flow (implement in consuming project):
            // 1) Deserialize the EPiServer.Events.EventMessage that Optimizely places into the
            //    ServiceBusMessage body. The EventMessage.Parameter property contains the
            //    actual payload object.
            // 2) If deserialization succeeds, call:
            //       var category = _classifier.ClassifyMessage(eventParameter.GetType(), eventParameter);
            //    If deserialization fails but you have a type name property, attempt to resolve
            //    the type and call the same method:
            //       var resolved = Type.GetType(typeName, throwOnError:false);
            //       if (resolved != null) category = _classifier.ClassifyMessage(resolved, null);
            // 3) If no type can be resolved, mark as MessageCategory.Unknown.
            // 4) Set priority properties on the outgoing ServiceBusMessage accordingly.

            // NOTE: The following lines are intentionally omitted here because they depend on
            // Optimizely types/serialization. Implement the three-step flow above in your
            // CMS project (where EPiServer assemblies are available) and call the classifier
            // with the actual System.Type from EventMessage.Parameter.

            // var category = MessageCategory.Unknown;
            // var priority = _classifier.GetPriority(category);
            // message.Subject = ...
            // message.ApplicationProperties["Priority"] = priority.ToServiceBusPriority();
            // message.ApplicationProperties["MessageCategory"] = category.ToString();
            // message.ApplicationProperties["ClassifiedPriority"] = priority.ToString();
        }
    }
    */
#endif
}

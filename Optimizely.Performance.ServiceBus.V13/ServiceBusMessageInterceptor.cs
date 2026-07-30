using System;
using System.Reflection;
using Azure.Messaging.ServiceBus;
// using EPiServer.Events;
// using EPiServer.Framework;
// using EPiServer.Framework.Initialization;
// using EPiServer.ServiceLocation;
using Microsoft.Extensions.Logging;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus.V13
{
    // NOTE: Uncomment when Optimizely CMS 13 packages are available
    // [InitializableModule]
    // [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    // public class ServiceBusMessageInterceptor : IInitializableModule
    public class ServiceBusMessageInterceptor
    {
        private ILogger<ServiceBusMessageInterceptor>? _logger;
        private IMessageClassifier? _classifier;
        private bool _isInitialized;

        // NOTE: Implementation commented out until Optimizely CMS 13 packages are added to consuming project
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
                _logger?.LogInformation("Service Bus Message Interceptor initialized successfully for CMS 13");
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

            _logger?.LogInformation("Successfully wrapped ServiceBusSender with priority interceptor");
        }

        public void Uninitialize(InitializationEngine context)
        {
            _isInitialized = false;
        }
        */
    }

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
}

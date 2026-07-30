using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus.V11
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOptiServiceBusPrioritizer(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var cfg = new PriorityConfiguration();
                try
                {
                    var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    cfg.PopulateFromAssemblies(assemblies);
                }
                catch { }
                return cfg;
            });

            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();
            return services;
        }

        public static IServiceCollection AddOptiServiceBusPrioritizer(
            this IServiceCollection services,
            PriorityConfiguration configuration)
        {
            services.AddSingleton(configuration);
            try
            {
                configuration.PopulateFromAssemblies(System.AppDomain.CurrentDomain.GetAssemblies());
            }
            catch { }

            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();
            return services;
        }
    }
}

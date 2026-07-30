using Microsoft.Extensions.DependencyInjection;
using OptiServiceBusPrioritizer.Core;

namespace OptiServiceBusPrioritizer.V12
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOptiServiceBusPrioritizer(this IServiceCollection services)
        {
            // Register PriorityConfiguration and attempt to populate type mappings from
            // currently loaded assemblies. Consumers should call PopulateFromAssemblies
            // again in their startup after Optimizely assemblies are loaded if needed.
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

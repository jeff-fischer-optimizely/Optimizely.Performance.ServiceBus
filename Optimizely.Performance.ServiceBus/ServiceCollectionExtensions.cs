using Microsoft.Extensions.DependencyInjection;
using Optimizely.Performance.ServiceBus.Core;

namespace Optimizely.Performance.ServiceBus
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

#if CMS13
            // CMS 13 variant: provide classifier factory so logger + config can be injected
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<OptimizelyMessageClassifier>>();
                var cfg = sp.GetRequiredService<PriorityConfiguration>();
                return new OptimizelyMessageClassifier(logger!, cfg);
            });
#elif CMS12
            // CMS 12 variant: simple registration (net472 classic DI expectations)
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();
#elif CMS11
            // CMS 11 variant: simple registration
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();
#else
            // Default: fall back to CMS12-style registration
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();
#endif

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

#if CMS13
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>(sp =>
            {
                var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<OptimizelyMessageClassifier>>();
                var cfg = sp.GetRequiredService<PriorityConfiguration>();
                return new OptimizelyMessageClassifier(logger!, cfg);
            });
#else
            services.AddSingleton<IMessageClassifier, OptimizelyMessageClassifier>();
#endif
            return services;
        }

    }
}

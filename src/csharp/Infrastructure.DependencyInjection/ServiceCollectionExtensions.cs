using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace EventSorcery.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddComponents(this IServiceCollection serviceCollection, Assembly assembly)
        {
            foreach (var implementationType in assembly.GetTypes())
            {
                var interfaceTypes = implementationType.GetInterfaces();

                if (!implementationType.IsClass)
                {
                    continue;
                }

                if (implementationType.IsAbstract)
                {
                    continue;
                }

                if (interfaceTypes.Contains(typeof(ISingletonComponent)))
                {
                    serviceCollection.AddSingleton(implementationType);
                }
                else if (interfaceTypes.Contains(typeof(ITransientComponent)))
                {
                    serviceCollection.AddTransient(implementationType);
                }
                else
                {
                    // do not auto-inject
                    continue;
                }

                foreach (var interfaceType in interfaceTypes)
                {
                    serviceCollection.AddTransient(interfaceType, serviceProvider => serviceProvider.GetRequiredService(implementationType));
                }
            }

            return serviceCollection;
        }
    }
}

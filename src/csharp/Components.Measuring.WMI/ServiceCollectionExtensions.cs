using System.Reflection;
using EventSorcery.Infrastructure.Configuration;
using EventSorcery.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ORMi.Interfaces;

namespace EventSorcery.Components.Measuring.Sensors.WMI
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSensorsWmi(this IServiceCollection services)
        {
            return services
                .AddConfigurationBinding<WmiQueryingSensorConfiguration>("Sensor", "WmiQuery")
                .AddComponents(Assembly.GetExecutingAssembly());
        }
    }
}

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using EventSorcery.Infrastructure.Configuration;
using EventSorcery.Infrastructure.DependencyInjection;

namespace EventSorcery.Components.Historian
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNpgslComponents(this IServiceCollection services)
        {
            return services
                .AddConfigurationBinding<Configuration.HistorianConfiguration>("Historian")
                .AddConfigurationBinding<Configuration.MeasurementsConfiguration>("Measurements")
                .AddComponents(Assembly.GetExecutingAssembly());
        }
    }
}

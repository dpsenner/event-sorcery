using System.Reflection;
using EventSorcery.Infrastructure.Configuration;
using EventSorcery.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EventSorcery.Components.Measuring
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMeasuringComponents(this IServiceCollection services)
        {
            return services
                .AddConfigurationBinding<Configuration.Generic>()
                .AddConfigurationBinding<Sensors.CpuTemperatureConfiguration>("Sensor", "CpuTemperature")
                .AddConfigurationBinding<Sensors.Dht22SensorConfiguration>("Sensor", "Dht22")
                .AddConfigurationBinding<Sensors.HddTemperatureConfiguration>("Sensor", "HddTemperature")
                .AddConfigurationBinding<Sensors.HddUsageConfiguration>("Sensor", "HddUsage")
                .AddConfigurationBinding<Sensors.HeartbeatConfiguration>("Sensor", "Heartbeat")
                .AddConfigurationBinding<Sensors.LoadConfiguration>("Sensor", "Load")
                .AddConfigurationBinding<Sensors.NsResolveConfiguration>("Sensor", "NsResolve")
                .AddConfigurationBinding<Sensors.PingConfiguration>("Sensor", "Ping")
                .AddConfigurationBinding<Sensors.TcpPortStateConfiguration>("Sensor", "TcpPortState")
                .AddConfigurationBinding<Sensors.UptimeConfiguration>("Sensor", "Uptime")
                .AddConfigurationBinding<Sensors.ApcupsdSensorConfiguration>("Sensor", "apcupsd")
                .AddComponents(Assembly.GetExecutingAssembly())
                .AddTransient<IMeasurementTimingService>(serviceProvider => serviceProvider.GetRequiredService<MeasurementRequestGenerator>());
        }
    }
}

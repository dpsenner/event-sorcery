using EventSorcery.Infrastructure.Configuration;
using EventSorcery.Infrastructure.DependencyInjection;
using EventSorcery.Infrastructure.Mqtt.Internals;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using System.Reflection;

namespace EventSorcery.Infrastructure.Mqtt
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttInfrastructure(this IServiceCollection serviceCollection, bool? cleanSession = null)
        {
            return serviceCollection
                .AddComponents(Assembly.GetExecutingAssembly())
                .AddConfigurationBinding<MqttBrokerConfiguration>((serviceProvider, t) =>
                {
                    if (cleanSession.HasValue)
                    {
                        t.Session.Clean = cleanSession.Value;
                    }
                }, "Mqtt")
                .AddSingleton<IMqttClient>(sp =>
                {
                    var client = new MqttClientFactory().CreateMqttClient();
                    client.ConnectedAsync += sp.GetRequiredService<MqttClientConnectedHandler>().HandleConnectedAsync;
                    client.DisconnectedAsync += sp.GetRequiredService<MqttClientDisconnectedHandler>().HandleDisconnectedAsync;
                    client.ApplicationMessageReceivedAsync += sp.GetRequiredService<MqttApplicationMessageReceivedHandler>().HandleApplicationMessageReceivedAsync;
                    return client;
                });
        }
    }
}

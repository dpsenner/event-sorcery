using EventSorcery.Events.Application;
using EventSorcery.Infrastructure.Configuration;
using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EventSorcery.Infrastructure.Mqtt
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttInfrastructure(this IServiceCollection serviceCollection, bool? cleanSession = null)
        {
            return serviceCollection
                .AddComponents(Assembly.GetExecutingAssembly())
                .AddConfigurationBinding<Internals.MqttBrokerConfiguration>((serviceProvider, t) =>
                {
                    if (cleanSession.HasValue)
                    {
                        t.Session.Clean = cleanSession.Value;
                    }
                }, "Mqtt")
                .AddSingleton(sp => new MqttFactory()
                    .CreateMqttClient()
                    .UseConnectedHandler(sp.GetRequiredService<IMqttClientConnectedHandler>())
                    .UseDisconnectedHandler(sp.GetRequiredService<IMqttClientDisconnectedHandler>())
                    .UseApplicationMessageReceivedHandler(sp.GetRequiredService<IMqttApplicationMessageReceivedHandler>()));
        }
    }
}

using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using MQTTnet.Client.Disconnecting;
using System;
using System.Threading.Tasks;

namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class MqttClientDisconnectedHandler : IMqttClientDisconnectedHandler, ISingletonComponent
    {
        protected IMediator Mediator { get; }

        public MqttClientDisconnectedHandler(IMediator mediator)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            if (!eventArgs.ClientWasConnected)
            {
                return Mediator.Publish(new ConnectingFailed()
                {
                    Reason = eventArgs.Exception.Message,
                });
            }
            else
            {
                if (eventArgs.Exception != null)
                {
                    Console.WriteLine($"Unexpected disconnection from mqtt broker with exception: {eventArgs.Exception}");
                }

                return Mediator.Publish(new ConnectionLost()
                {
                    Exception = eventArgs.Exception,
                });
            }
        }
    }
}

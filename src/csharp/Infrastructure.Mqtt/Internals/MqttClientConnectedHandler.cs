using System;
using System.Threading.Tasks;
using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using MQTTnet.Client.Connecting;

namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class MqttClientConnectedHandler : IMqttClientConnectedHandler, ISingletonComponent
    {
        protected IMediator Mediator { get; }

        protected MqttBrokerConfiguration Configuration { get; }

        public MqttClientConnectedHandler(IMediator mediator, MqttBrokerConfiguration configuration)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            if (eventArgs.AuthenticateResult.ResultCode != MqttClientConnectResultCode.Success)
            {
                Console.WriteLine($"Could not connect to {Configuration.Host}:{Configuration.Port} (reason: {eventArgs.AuthenticateResult.ResultCode}) ..");
                return Mediator.Publish(new ConnectingFailed()
                {
                    Reason = eventArgs.AuthenticateResult.ReasonString,
                });
            }
            else
            {
                Console.WriteLine($"Connected to {Configuration.Host}:{Configuration.Port} ..");
                return Mediator.Publish(new ConnectionEstablished()
                {
                    IsSessionPresent = eventArgs.AuthenticateResult.IsSessionPresent,
                });
            }
        }
    }
}

using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using MQTTnet;
using MQTTnet.Client.Receiving;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class MqttApplicationMessageReceivedHandler : IMqttApplicationMessageReceivedHandler, ISingletonComponent
    {
        protected IMediator Mediator { get; }

        public MqttApplicationMessageReceivedHandler(IMediator mediator)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            return Mediator.Publish(new PublishReceived()
            {
                Topic = eventArgs.ApplicationMessage.Topic,
                IsRetained = eventArgs.ApplicationMessage.Retain,
                Qos = eventArgs.ApplicationMessage.QualityOfServiceLevel.ToQualityOfService(),
                ContentType = eventArgs.ApplicationMessage.ContentType,
                Payload = eventArgs.ApplicationMessage.Payload,
            });
        }
    }
}

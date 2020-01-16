using MediatR;

namespace EventSorcery.Events.Mqtt
{
    public class PublishRequest : INotification
    {
        public string Topic { get; set; }

        public QualityOfService Qos { get; set; }

        public bool Retain { get; set; }

        public byte[] Payload { get; set; }
    }
}

using MediatR;

namespace EventSorcery.Events.Mqtt
{
    public class PublishReceived : INotification
    {
        public string Topic { get; set; }

        public QualityOfService Qos { get; set; }

        public bool IsRetained { get; set; }

        public string ContentType { get; set; }

        public byte[] Payload { get; set; }
    }
}

using MediatR;

namespace EventSorcery.Events.Mqtt
{
    public class ConnectingFailed : INotification
    {
        public string Reason { get; set; }
    }
}

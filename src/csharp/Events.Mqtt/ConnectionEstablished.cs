using MediatR;

namespace EventSorcery.Events.Mqtt
{
    public class ConnectionEstablished : INotification
    {
        public bool IsSessionPresent { get; set; }
    }
}

using MediatR;
using System;

namespace EventSorcery.Events.Mqtt
{
    public class ConnectionLost : INotification
    {
        public Exception Exception { get; set; }
    }
}

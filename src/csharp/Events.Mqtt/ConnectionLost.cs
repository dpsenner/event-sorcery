using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Mqtt
{
    public class ConnectionLost : INotification
    {
        public Exception Exception { get; set; }
    }
}

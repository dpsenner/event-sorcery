using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Mqtt
{
    public class ConnectionEstablished : INotification
    {
        public bool IsSessionPresent { get; set; }
    }
}

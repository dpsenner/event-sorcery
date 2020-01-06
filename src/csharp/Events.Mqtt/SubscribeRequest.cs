using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Mqtt
{
    public class SubscribeRequest : INotification
    {
        public string Topic { get; set; }

        public QualityOfService Qos { get; set; }
    }
}

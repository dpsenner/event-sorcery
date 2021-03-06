﻿using MediatR;

namespace EventSorcery.Events.Mqtt
{
    public class PublishAsJsonRequest : INotification
    {
        public string Topic { get; set; }

        public QualityOfService Qos { get; set; }

        public bool Retain { get; set; }

        public object Payload { get; set; }
    }
}

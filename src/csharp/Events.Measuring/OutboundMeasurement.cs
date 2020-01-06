using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Measuring
{
    public class OutboundMeasurement : INotification
    {
        public string Name { get; set; }

        public IMeasurement Item { get; set; }
    }
}

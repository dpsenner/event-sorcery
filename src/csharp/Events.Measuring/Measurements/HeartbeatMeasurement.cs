using System;
using System.Collections.Generic;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class HeartbeatMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }
    }
}

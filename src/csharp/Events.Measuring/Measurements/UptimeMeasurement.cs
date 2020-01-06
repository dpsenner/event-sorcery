using System;
using System.Collections.Generic;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class UptimeMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }
        
        public DateTime Since { get; set; }

        public TimeSpan Total { get; set; }

        public string TotalHumanReadable { get; set; }
    }
}

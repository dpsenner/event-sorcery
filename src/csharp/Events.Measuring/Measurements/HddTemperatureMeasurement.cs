using System;
using System.Collections.Generic;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class HddTemperatureMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }

        public string Hdd { get; set; }

        public string Alias { get; set; }
        
        public double Temperature { get; set; }
    }
}

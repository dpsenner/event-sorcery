using System;
using System.Collections.Generic;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class HddUsageMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }

        public string Hdd { get; set; }

        public string Alias { get; set; }
        
        public long Total { get; set; }

        public long Available { get; set; }

        public long Used { get; set; }

        public string TotalHumanReadable { get; set; }

        public string AvailableHumanReadable { get; set; }

        public string UsedHumanReadable { get; set; }
    }
}

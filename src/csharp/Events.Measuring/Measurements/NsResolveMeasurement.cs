using System;
using System.Net.NetworkInformation;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class NsResolveMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Source { get; set; }

        public string Target { get; set; }

        public string Alias { get; set; }
        
        public NsResolveStatus Status { get; set; }

        public TimeSpan After { get; set; }

        public TimeSpan Timeout { get; set; }
    }
}

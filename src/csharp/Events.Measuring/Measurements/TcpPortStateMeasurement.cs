using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class TcpPortStateMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Source { get; set; }

        public string Target { get; set; }

        public int Port { get; set; }

        public string Alias { get; set; }

        public TcpPortStatus Status { get; set; }

        public TimeSpan After { get; set; }

        public TimeSpan Timeout { get; set; }
    }
}

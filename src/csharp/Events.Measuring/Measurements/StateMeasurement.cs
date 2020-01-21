using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class StateMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Metric { get; set; }

        public int Status { get; set; }

        public string StatusText { get; set; }

        public string Comment { get; set; }
    }
}

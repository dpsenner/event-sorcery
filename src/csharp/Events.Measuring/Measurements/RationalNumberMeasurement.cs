using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class RationalNumberMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Category { get; set; }

        public string Metric { get; set; }

        public double Value { get; set; }
    }
}

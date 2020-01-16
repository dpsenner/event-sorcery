using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class LoadMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }

        public double LastOneMinute { get; set; }

        public double LastFiveMinutes { get; set; }

        public double LastFifteenMinutes { get; set; }
    }
}

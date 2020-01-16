using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class Dht22Measurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }

        public string Alias { get; set; }

        public double LastTemperature { get; set; }

        public double LastRelativeHumidity { get; set; }

        public bool IsLastReadSuccessful { get; set; }

        public TimeSpan LastReadAge { get; set; }
    }
}

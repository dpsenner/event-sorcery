using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class CpuTemperatureMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public string Hostname { get; set; }

        public string Cpu { get; set; }

        public string Alias { get; set; }

        public double Temperature { get; set; }
    }
}

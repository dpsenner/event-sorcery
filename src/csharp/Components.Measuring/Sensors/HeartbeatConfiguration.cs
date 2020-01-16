using System;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class HeartbeatConfiguration : ISensorScanRateItem
    {
        public bool Enable { get; set; } = false;

        public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(1);
    }
}

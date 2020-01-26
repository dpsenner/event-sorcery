using System;
using System.Collections.Generic;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class ApcupsdSensorConfiguration : ISensorScanRateItem
    {
        public string Alias { get; set; } = string.Empty;

        public bool Enable { get; set; } = false;
        
        public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(5);
    }
}

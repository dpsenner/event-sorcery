using System;
using System.Collections.Generic;

namespace EventSorcery.Components.Measuring.Sensors.WMI
{
    internal class WmiQueryingSensorConfiguration : ISensorScanRateItem
    {
        public string Alias { get; set; } = string.Empty;

        public bool Enable { get; set; } = false;

        public TimeSpan ScanRate { get; set; } = TimeSpan.FromMinutes(1);

        public List<WmiQueryItemConfiguration> Items { get; set; } = new List<WmiQueryItemConfiguration>();
    }
}

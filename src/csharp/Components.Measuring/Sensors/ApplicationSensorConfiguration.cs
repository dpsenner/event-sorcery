using System;
using System.Collections.Generic;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class ApplicationSensorConfiguration
    {
        public List<PathConfiguration> Items { get; private set; } = new List<PathConfiguration>();

        internal class PathConfiguration : ISensorScanRateItem
        {
            public bool Enable { get; set; } = false;
            
            public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(5);

            public string Command { get; set; }

            public string Arguments { get; set; }
        }
    }
}

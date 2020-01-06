using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class CpuTemperatureConfiguration
    {
        public List<PathConfiguration> Items { get; private set; } = new List<PathConfiguration>();

        internal class PathConfiguration : ISensorScanRateItem
        {
            public string Alias { get; set; } = string.Empty;

            public bool Enable { get; set; } = false;

            public string Path { get; set; } = string.Empty;
            
            public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(5);
        }
    }
}

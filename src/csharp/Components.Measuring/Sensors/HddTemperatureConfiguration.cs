using System;
using System.Collections.Generic;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class HddTemperatureConfiguration : ISensorScanRateItem
    {
        public List<DeviceConfiguration> Items { get; private set; } = new List<DeviceConfiguration>();

        public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(10);

        internal class DeviceConfiguration
        {
            public string Alias { get; set; } = string.Empty;

            public string Path { get; set; } = string.Empty;

            public bool Enable { get; set; } = false;
        }
    }
}

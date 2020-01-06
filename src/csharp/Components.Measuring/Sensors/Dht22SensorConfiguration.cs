using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Dht22SensorConfiguration
    {
        public List<Dht22ConfigurationItem> Items { get; private set; } = new List<Dht22ConfigurationItem>();

        internal class Dht22ConfigurationItem : ISensorScanRateItem
        {
            public bool Enable { get; set; }

            public int GpioPin { get; set; }

            public string Alias { get; set; } = string.Empty;

            public TimeSpan ScanRate { get; set; } = TimeSpan.FromMinutes(1);
        }
    }
}

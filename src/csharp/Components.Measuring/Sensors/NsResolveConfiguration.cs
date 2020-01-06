using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class NsResolveConfiguration
    {
        public List<HostConfiguration> Items { get; private set; } = new List<HostConfiguration>();

        internal class HostConfiguration : ISensorScanRateItem
        {
            public string Alias { get; set; } = string.Empty;

            public string Hostname { get; set; } = string.Empty;

            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

            public bool Enable { get; set; } = false;

            public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(5);
        }
    }
}

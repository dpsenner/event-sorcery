using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class LoadConfiguration : ISensorScanRateItem
    {
        public bool Enable { get; set; } = false;

        public TimeSpan ScanRate { get; set; } = TimeSpan.FromSeconds(1);
    }
}

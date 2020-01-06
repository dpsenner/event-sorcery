using System;

namespace EventSorcery.Components.Measuring
{
    internal interface ISensorScanRateItem
    {
        TimeSpan ScanRate { get; }
    }
}

using System;

namespace EventSorcery.Components.Measuring
{
    public interface ISensorScanRateItem
    {
        TimeSpan ScanRate { get; }
    }
}

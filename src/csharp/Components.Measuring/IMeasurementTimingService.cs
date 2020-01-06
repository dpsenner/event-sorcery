using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EventSorcery.Components.Measuring
{
    internal interface IMeasurementTimingService
    {
        void Register<T>(T item, Func<T, CancellationToken, Task> isDueCallback)
            where T : ISensorScanRateItem;

        void Register<T>(IEnumerable<T> items, Func<IEnumerable<T>, CancellationToken, Task> isDueCallback)
            where T : ISensorScanRateItem;

        bool IsDue<T>(T item)
            where T : ISensorScanRateItem;

        void ResetDue<T>(T item)
            where T : ISensorScanRateItem;

        void ResetDue<T>(IEnumerable<T> items)
            where T : ISensorScanRateItem;
    }
}

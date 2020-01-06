using MediatR;
using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class NsResolve : ASensor
    {
        protected IMediator Mediator { get; }

        protected NsResolveConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public NsResolve(IMediator mediator, NsResolveConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            MeasurementTimingService.Register(Configuration.Items.Where(t => t.Enable), OnIsDue);
        }

        protected async Task OnIsDue(IEnumerable<NsResolveConfiguration.HostConfiguration> dueItems, CancellationToken cancellationToken)
        {
            foreach (var item in dueItems)
            {
                if (item is NsResolveConfiguration.HostConfiguration hostConfiguration)
                {
                    await RunAsync(hostConfiguration, cancellationToken);
                }
            }

            MeasurementTimingService.ResetDue(dueItems);
        }

        private async Task RunAsync(NsResolveConfiguration.HostConfiguration item, CancellationToken cancellationToken)
        {
            var hostname = item.Hostname;
            var alias = item.Alias;
            var timeout = item.Timeout;
            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = hostname;
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var asyncResult = Dns.BeginGetHostEntry(hostname, null, null);
            var status = NsResolveStatus.Failure;
            try
            {
                try
                {
                    using var asyncWaitHandle = asyncResult.AsyncWaitHandle;
                    var success = asyncWaitHandle.WaitOne(timeout, true);
                    if (success)
                    {
                        status = NsResolveStatus.Success;
                    }
                }
                finally
                {
                    var ipList = Dns.EndGetHostEntry(asyncResult);
                    if (ipList == null || ipList.AddressList == null || ipList.AddressList.Length == 0)
                    {
                        status = NsResolveStatus.Failure;
                    }
                }
            }
            catch
            {
                status = NsResolveStatus.Failure;
            }

            var elapsed = stopwatch.Elapsed;
            await Mediator.Publish(new OutboundMeasurement()
            {
                Name = "ns-resolve",
                Item = new NsResolveMeasurement()
                {
                    Timestamp = DateTime.UtcNow,
                    Source = System.Net.Dns.GetHostName(),
                    Target = hostname,
                    Alias = alias,
                    Status = status,
                    Timeout = item.Timeout,
                    After = elapsed,
                },
            }, cancellationToken);
        }
    }
}

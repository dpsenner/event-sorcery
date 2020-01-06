using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class TcpPortState : ASensor
    {
        protected IMediator Mediator { get; }

        protected TcpPortStateConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public TcpPortState(IMediator mediator, TcpPortStateConfiguration configuration, IMeasurementTimingService measurementTimingService)
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

        protected async Task OnIsDue(IEnumerable<TcpPortStateConfiguration.HostConfiguration> dueItems, CancellationToken cancellationToken)
        {
            foreach (var item in dueItems)
            {
                await RunAsync(item, cancellationToken);
            }

            MeasurementTimingService.ResetDue(dueItems);
        }

        private async Task RunAsync(TcpPortStateConfiguration.HostConfiguration item, CancellationToken cancellationToken)
        {
            using (var tcpClient = new TcpClient())
            {
                var hostname = item.Hostname;
                var port = item.Port;
                var timeout = item.Timeout;
                var alias = item.Alias;
                if (string.IsNullOrWhiteSpace(alias))
                {
                    alias = $"{hostname}:{port}";
                }

                var stopwatch = Stopwatch.StartNew();
                var asyncResult = tcpClient.BeginConnect(hostname, port, null, null);
                var status = TcpPortStatus.Down;
                try
                {
                    try
                    {
                        using var asyncWaitHandle = asyncResult.AsyncWaitHandle;
                        var success = asyncWaitHandle.WaitOne(timeout, true);
                        if (success)
                        {
                            status = TcpPortStatus.Up;
                        }
                    }
                    finally
                    {
                        tcpClient.EndConnect(asyncResult);
                    }
                }
                catch
                {
                    status = TcpPortStatus.Down;
                }

                var elapsed = stopwatch.Elapsed;
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"tcp-port/{alias}/timeout/milliseconds",
                    Value = $"{timeout}",
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"tcp-port/{alias}/status",
                    Value = $"{status}",
                }, cancellationToken);
                await Mediator.Publish(new OutboundMeasurement()
                {
                    Name = "tcp-port-state",
                    Item = new TcpPortStateMeasurement()
                    {
                        Timestamp = DateTime.UtcNow,
                        Source = System.Net.Dns.GetHostName(),
                        Target = hostname,
                        Port = port,
                        Alias = alias,
                        Status = status,
                        Timeout = timeout,
                        After = elapsed,
                    },
                }, cancellationToken);
            }
        }
    }
}

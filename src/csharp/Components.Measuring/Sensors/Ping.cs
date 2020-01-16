using MediatR;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Ping : ASensor
    {
        protected IMediator Mediator { get; }

        protected PingConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public Ping(IMediator mediator, PingConfiguration configuration, IMeasurementTimingService measurementTimingService)
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

        protected async Task OnIsDue(IEnumerable<PingConfiguration.HostConfiguration> dueItems, CancellationToken cancellationToken)
        {
            await RunAsync(dueItems, cancellationToken);
            MeasurementTimingService.ResetDue(dueItems);
        }

        private async Task RunAsync(IEnumerable<PingConfiguration.HostConfiguration> items, CancellationToken cancellationToken)
        {
            using (var ping = new System.Net.NetworkInformation.Ping())
            {
                foreach (var item in items)
                {
                    var timeout = (int)item.Timeout.TotalMilliseconds;
                    var hostname = item.Hostname;
                    var alias = item.Alias;
                    if (string.IsNullOrWhiteSpace(alias))
                    {
                        alias = hostname;
                    }

                    var pingReply = await ping.SendPingAsync(hostname, timeout);
                    var roundtripTime = TimeSpan.FromMilliseconds(pingReply.RoundtripTime);
                    if (pingReply.Status == IPStatus.TimedOut)
                    {
                        roundtripTime = item.Timeout;
                    }

                    await Mediator.Publish(new OutboundMeasurement()
                    {
                        Name = "ping",
                        Item = new PingMeasurement()
                        {
                            Timestamp = DateTime.UtcNow,
                            Source = System.Net.Dns.GetHostName(),
                            Target = hostname,
                            Alias = alias,
                            Status = pingReply.Status,
                            Timeout = item.Timeout,
                            RoundtripTime = roundtripTime,
                        },
                    }, cancellationToken);
                    await Mediator.Publish(new SensorMeasurement()
                    {
                        Sensor = $"ping/{alias}/status",
                        Value = $"{pingReply.Status}",
                    }, cancellationToken);
                    await Mediator.Publish(new SensorMeasurement()
                    {
                        Sensor = $"ping/{alias}/timeout/milliseconds",
                        Value = $"{timeout}",
                    }, cancellationToken);
                    await Mediator.Publish(new SensorMeasurement()
                    {
                        Sensor = $"ping/{alias}/rtt/milliseconds",
                        Value = $"{pingReply.RoundtripTime}",
                    }, cancellationToken);
                }
            }
        }
    }
}

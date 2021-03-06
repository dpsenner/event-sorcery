using MediatR;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;
using System.Net.Sockets;

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

        private Task RunAsync(IEnumerable<PingConfiguration.HostConfiguration> items, CancellationToken cancellationToken)
        {
            return Task.WhenAll(items.Select(item => PingAsync(item, cancellationToken)));
        }

        private async Task PingAsync(PingConfiguration.HostConfiguration item, CancellationToken cancellationToken)
        {
            var timeout = (int)item.Timeout.TotalMilliseconds;
            var hostname = item.Hostname;
            var alias = item.Alias;
            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = hostname;
            }

            var roundtripTime = item.Timeout;
            var status = IPStatus.Unknown;
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var pingReply = await ping.SendPingAsync(hostname, timeout);
                status = pingReply.Status;
                roundtripTime = TimeSpan.FromMilliseconds(pingReply.RoundtripTime);
                if (pingReply.Status == IPStatus.TimedOut)
                {
                    roundtripTime = item.Timeout;
                }
            }
            catch (PingException ex)
            {
                if (ex.InnerException is SocketException socketException)
                {
                    switch (socketException.SocketErrorCode)
                    {
                        case SocketError.HostNotFound:
                            status = IPStatus.DestinationUnreachable;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ping failed with {ex.GetType().Name}: {ex.Message}");
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
                    Status = status,
                    Timeout = item.Timeout,
                    RoundtripTime = roundtripTime,
                },
            }, cancellationToken);
        }
    }
}

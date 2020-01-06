using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;
using EventSorcery.Infrastructure.DependencyInjection;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Load : ASensor
    {
        protected IMediator Mediator { get; }

        protected LoadConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public Load(IMediator mediator, LoadConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            if (!Configuration.Enable)
            {
                return;
            }

            MeasurementTimingService.Register(Configuration, OnIsDue);
        }

        protected async Task OnIsDue(LoadConfiguration configuration, CancellationToken cancellationToken)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    {
                        var loadAverage = await GetUnixLoadAverage();
                        var loadSplit = loadAverage.Split(", ").Select(x => Convert.ToDouble(x)).ToList();
                        await Mediator.Publish(new OutboundMeasurement()
                        {
                            Name = "load",
                            Item = new LoadMeasurement()
                            {
                                Timestamp = DateTime.UtcNow,
                                Hostname = System.Net.Dns.GetHostName(),
                                LastOneMinute = loadSplit[0],
                                LastFiveMinutes = loadSplit[1],
                                LastFifteenMinutes = loadSplit[2],
                            },
                        }, cancellationToken);
                        await Mediator.Publish(new SensorMeasurement()
                        {
                            Sensor = "load",
                            Value = loadAverage,
                        }, cancellationToken);
                    }
                    break;
                case PlatformID.Win32NT:
                    // not supported
                default:
                    // not implemented
                    return;
            }

            MeasurementTimingService.ResetDue(configuration);
        }

        private static async Task<string> GetUnixLoadAverage()
        {
            using (var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "uptime",
                StandardOutputEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            }))
            {
                var stdout = await process.StandardOutput.ReadToEndAsync();
                var match = Regex.Match(stdout, ".*load average: (.+)$");
                if (!match.Success)
                {
                    return "N/A";
                }

                return match.Groups[1].Value;
            }
        }
    }
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class HddUsage : ASensor
    {
        protected IMediator Mediator { get; }

        protected HddUsageConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public HddUsage(IMediator mediator, HddUsageConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            if (!Configuration.Items.Any(t => t.Enable))
            {
                return;
            }

            MeasurementTimingService.Register(Configuration, OnIsDue);
        }

        protected async Task OnIsDue(HddUsageConfiguration configuration, CancellationToken cancellationToken)
        {
            var items = await ReadFromDf(cancellationToken);
            foreach (var device in configuration.Items.Where(t => t.Enable))
            {
                var path = device.Path;
                var alias = device.Alias;

                var item = items.FirstOrDefault(t => t.device == path);
                if (item == default)
                {
                    Console.WriteLine($"Warn: got no sensor readings for {path}");
                    continue;
                }

                (var _, var bytesTotal, var bytesUsed, var bytesAvailable) = item;
                await Mediator.Publish(new OutboundMeasurement()
                {
                    Name = "hdd-usage",
                    Item = new HddUsageMeasurement()
                    {
                        Timestamp = DateTime.UtcNow,
                        Hostname = System.Net.Dns.GetHostName(),
                        Hdd = path,
                        Alias = alias,
                        Total = bytesTotal,
                        Available = bytesAvailable,
                        Used = bytesUsed,
                        TotalHumanReadable = GetBytesHumanReadable(bytesTotal),
                        AvailableHumanReadable = GetBytesHumanReadable(bytesAvailable),
                        UsedHumanReadable = GetBytesHumanReadable(bytesUsed),
                    },
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/size/bytes",
                    Value = $"{bytesTotal}",
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/size",
                    Value = GetBytesHumanReadable(bytesTotal),
                }, cancellationToken);

                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/used/bytes",
                    Value = $"{bytesUsed}",
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/used",
                    Value = GetBytesHumanReadable(bytesUsed),
                }, cancellationToken);

                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/available/bytes",
                    Value = $"{bytesAvailable}",
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/available",
                    Value = GetBytesHumanReadable(bytesAvailable),
                }, cancellationToken);
            }

            MeasurementTimingService.ResetDue(configuration);
        }

        private static string GetBytesHumanReadable(long bytes)
        {
            var units = new string[] { "B", "KiB", "MiB", "GiB", "TiB" };
            int idx = 0;
            while (idx < units.Length)
            {
                if (bytes < 9999)
                {
                    break;
                }
                else
                {
                    idx++;
                    bytes /= 1024;
                }
            }

            return $"{bytes}{units[idx]}";
        }

        private async Task<List<(string device, long bytesTotal, long bytesUsed, long bytesAvailable)>> ReadFromDf(CancellationToken cancellationToken)
        {
            var result = new List<(string, long, long, long)>();
            using (var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "df",
                Arguments = "-B1",
                StandardOutputEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            }))
            {
                while (!process.HasExited)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
                        var match = Regex.Match(line, "^(.+)[ ]+([0-9]+)[ ]+([0-9]+)[ ]+([0-9]+)[ ]+");
                        if (!match.Success)
                        {
                            // skip
                        }
                        else
                        {
                            var device = match.Groups[1].Value.Trim();
                            var total = match.Groups[2].Value;
                            var used = match.Groups[3].Value;
                            var available = match.Groups[4].Value;
                            result.Add((
                                device,
                                long.Parse(total, CultureInfo.InvariantCulture),
                                long.Parse(used, CultureInfo.InvariantCulture),
                                long.Parse(available, CultureInfo.InvariantCulture)
                            ));
                        }
                    }
                }
            }

            return result;
        }
    }
}

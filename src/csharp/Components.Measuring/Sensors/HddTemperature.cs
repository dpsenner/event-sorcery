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
    internal class HddTemperature : ASensor
    {
        protected IMediator Mediator { get; }

        protected HddTemperatureConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public HddTemperature(IMediator mediator, HddTemperatureConfiguration configuration, IMeasurementTimingService measurementTimingService)
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

        private async Task OnIsDue(HddTemperatureConfiguration configuration, CancellationToken cancellationToken)
        {
            var sensorReadings = await ReadFromHddTemp(cancellationToken);
            foreach (var item in configuration.Items.Where(t => t.Enable))
            {
                var path = item.Path;
                var alias = item.Alias;
                var sensorReading = sensorReadings.FirstOrDefault(t => t.device.Equals(path));
                if (sensorReading == default)
                {
                    // skip?
                    continue;
                }

                double temperatureRaw = sensorReading.temperatureCelsius;
                await Mediator.Publish(new OutboundMeasurement()
                {
                    Name = "hdd-temperature",
                    Item = new HddTemperatureMeasurement()
                    {
                        Timestamp = DateTime.UtcNow,
                        Hostname = System.Net.Dns.GetHostName(),
                        Hdd = path,
                        Alias = alias,
                        Temperature = temperatureRaw,
                    },
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/temperature/celsius",
                    Value = $"{temperatureRaw:n1}",
                }, cancellationToken);

                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"hdd/{alias}/temperature",
                    Value = $"{temperatureRaw:n1}°C",
                }, cancellationToken);
            }

            MeasurementTimingService.ResetDue(configuration);
        }

        private async Task<List<(string device, double temperatureCelsius)>> ReadFromHddTemp(CancellationToken cancellationToken)
        {
            var result = new List<(string, double)>();
            using (var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "hddtemp",
                Arguments = Configuration
                    .Items
                    .Where(t => t.Enable)
                    .Select(t => t.Path)
                    .Aggregate((t1, t2) => $"{t1} {t2}"),
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
                        var match = Regex.Match(line, "^(.*):(.*):(.*)$");
                        if (!match.Success)
                        {
                            // skip
                        }
                        else
                        {
                            var device = match.Groups[1].Value.Trim();
                            var model = match.Groups[2].Value.Trim();
                            var temperature = match.Groups[3].Value.Trim();
                            if (temperature.EndsWith("°C"))
                            {
                                temperature = temperature.Substring(0, temperature.Length - "°C".Length);
                            }

                            result.Add((
                                device,
                                double.Parse(temperature, CultureInfo.InvariantCulture)
                            ));
                        }
                    }
                }
            }

            return result;
        }
    }
}

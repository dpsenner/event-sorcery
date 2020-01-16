using MediatR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class CpuTemperature : ASensor
    {
        protected IMediator Mediator { get; }

        protected CpuTemperatureConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public CpuTemperature(IMediator mediator, CpuTemperatureConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            MeasurementTimingService.Register(Configuration.Items.Where(t => t.Enable), OnIsDue);
        }

        private async Task OnIsDue(IEnumerable<CpuTemperatureConfiguration.PathConfiguration> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                var path = item.Path;
                var alias = item.Alias;

                var temperatureString = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
                double temperatureRaw = double.Parse(temperatureString, CultureInfo.InvariantCulture) / 1000;
                await Mediator.Publish(new OutboundMeasurement()
                {
                    Name = "cpu-temperature",
                    Item = new CpuTemperatureMeasurement()
                    {
                        Timestamp = DateTime.UtcNow,
                        Hostname = System.Net.Dns.GetHostName(),
                        Cpu = path,
                        Alias = alias,
                        Temperature = temperatureRaw,
                    },
                }, cancellationToken);
                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"cpu/{alias}/temperature/celsius",
                    Value = $"{temperatureRaw:n1}",
                }, cancellationToken);

                await Mediator.Publish(new SensorMeasurement()
                {
                    Sensor = $"cpu/{alias}/temperature",
                    Value = $"{temperatureRaw:n1}°C",
                }, cancellationToken);
            }

            MeasurementTimingService.ResetDue(items);
        }
    }
}

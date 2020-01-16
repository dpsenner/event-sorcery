using MediatR;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Uptime : ASensor
    {
        protected IMediator Mediator { get; }

        protected UptimeConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public Uptime(IMediator mediator, UptimeConfiguration configuration, IMeasurementTimingService measurementTimingService)
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

        protected async Task OnIsDue(UptimeConfiguration configuration, CancellationToken cancellationToken)
        {
            string uptimeRaw = await File.ReadAllTextAsync("/proc/uptime", Encoding.UTF8, cancellationToken);
            string uptimeSecondsString = uptimeRaw.Split(' ')[0];
            decimal uptimeSeconds = decimal.Parse(uptimeSecondsString, CultureInfo.InvariantCulture);
            var uptimeTimeSpan = TimeSpan.FromSeconds((double)uptimeSeconds);
            var uptime = $"{Math.Floor(uptimeTimeSpan.TotalDays).ToString("n0", CultureInfo.InvariantCulture)} days, {uptimeTimeSpan.Hours.ToString("n0", CultureInfo.InvariantCulture).PadLeft(2, '0')}:{uptimeTimeSpan.Minutes.ToString("n0", CultureInfo.InvariantCulture).PadLeft(2, '0')}:{uptimeTimeSpan.Seconds.ToString("n0", CultureInfo.InvariantCulture).PadLeft(2, '0')}";
            await Mediator.Publish(new OutboundMeasurement()
            {
                Name = "uptime",
                Item = new UptimeMeasurement()
                {
                    Timestamp = DateTime.UtcNow,
                    Hostname = System.Net.Dns.GetHostName(),
                    Since = DateTime.UtcNow - uptimeTimeSpan,
                    Total = uptimeTimeSpan,
                    TotalHumanReadable = uptime,
                },
            }, cancellationToken);
            await Mediator.Publish(new SensorMeasurement()
            {
                Sensor = "uptime",
                Value = uptime,
            }, cancellationToken);

            MeasurementTimingService.ResetDue(configuration);
        }
    }
}

using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Heartbeat : ASensor
    {
        private static readonly string Hostname = System.Net.Dns.GetHostName();
        
        protected IMediator Mediator { get; }

        protected HeartbeatConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public Heartbeat(IMediator mediator, HeartbeatConfiguration configuration, IMeasurementTimingService measurementTimingService)
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

        private async Task OnIsDue(HeartbeatConfiguration configuration, CancellationToken cancellationToken)
        {
            await Mediator.Publish(new OutboundMeasurement()
            {
                Name = "heartbeat",
                Item = new HeartbeatMeasurement()
                {
                    Timestamp = DateTime.UtcNow,
                    Hostname = Hostname,
                },
            });
            await Mediator.Publish(new SensorMeasurement()
            {
                Sensor = "heartbeat",
                Value = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffff"),
            }, cancellationToken);

            MeasurementTimingService.ResetDue(configuration);
        }
    }
}

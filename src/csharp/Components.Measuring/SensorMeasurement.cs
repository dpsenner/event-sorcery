using MediatR;

namespace EventSorcery.Components.Measuring
{
    internal class SensorMeasurement : INotification
    {
        public string Sensor { get; set; }

        public string Value { get; set; }
    }
}

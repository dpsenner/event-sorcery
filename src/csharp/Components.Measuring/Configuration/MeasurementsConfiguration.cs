using EventSorcery.Events.Mqtt;

namespace EventSorcery.Components.Measuring.Configuration
{
    internal class MeasurementsConfiguration
    {
        public QualityOfService Qos { get; set; } = QualityOfService.AtMostOnce;
    }
}

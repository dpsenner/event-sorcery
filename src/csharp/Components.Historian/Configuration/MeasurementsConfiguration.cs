using EventSorcery.Events.Mqtt;

namespace EventSorcery.Components.Historian.Configuration
{
    internal class MeasurementsConfiguration
    {
        public QualityOfService Qos { get; set; } = QualityOfService.AtMostOnce;
    }
}
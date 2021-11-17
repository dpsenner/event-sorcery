using EventSorcery.Events.Mqtt;

namespace EventSorcery.Components.Measuring.Configuration
{
    public class MeasurementsConfiguration
    {
        public QualityOfService Qos { get; set; } = QualityOfService.AtMostOnce;
    }
}

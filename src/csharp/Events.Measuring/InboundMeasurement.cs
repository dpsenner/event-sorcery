using MediatR;

namespace EventSorcery.Events.Measuring
{
    public class InboundMeasurement : INotification
    {
        public string Name { get; set; }

        public IMeasurement Item { get; set; }
    }
}

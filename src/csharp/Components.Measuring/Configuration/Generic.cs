namespace EventSorcery.Components.Measuring.Configuration
{
    internal class Generic
    {
        public string TopicPrefix { get; set; }

        public MeasurementsConfiguration Measurements { get; set; } = new MeasurementsConfiguration();
    }
}

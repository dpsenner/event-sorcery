namespace EventSorcery.Components.Historian.Configuration
{
    internal class Dht22MeasurementConfiguration
    {
        public bool Enable { get; set; } = false;

        public string InsertQuery { get; set; }
    }
}

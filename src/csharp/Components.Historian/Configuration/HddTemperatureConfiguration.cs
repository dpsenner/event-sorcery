namespace EventSorcery.Components.Historian.Configuration
{
    internal class HddTemperatureConfiguration
    {
        public bool Enable { get; set; } = false;

        public string InsertQuery { get; set; }
    }
}

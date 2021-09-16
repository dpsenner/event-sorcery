namespace EventSorcery.Components.Historian.Configuration
{
    internal class CpuTemperatureConfiguration
    {
        public bool Enable { get; set; } = false;

        public string InsertQuery { get; set; }
    }
}

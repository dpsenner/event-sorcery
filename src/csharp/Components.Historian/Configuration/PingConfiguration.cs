namespace EventSorcery.Components.Historian.Configuration
{
    internal class PingConfiguration
    {
        public bool Enable { get; set; } = false;

        public string InsertQuery { get; set; }
    }
}

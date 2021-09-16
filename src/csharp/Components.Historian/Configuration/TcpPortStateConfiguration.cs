namespace EventSorcery.Components.Historian.Configuration
{
    internal class TcpPortStateConfiguration
    {
        public bool Enable { get; set; } = false;

        public string InsertQuery { get; set; }
    }
}

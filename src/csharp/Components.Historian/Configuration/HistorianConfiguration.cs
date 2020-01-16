namespace EventSorcery.Components.Historian.Configuration
{
    internal class HistorianConfiguration
    {
        public NpgsqlConfiguration Npgsql { get; set; } = new NpgsqlConfiguration();
    }
}

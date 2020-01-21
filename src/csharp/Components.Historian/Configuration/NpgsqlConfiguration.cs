namespace EventSorcery.Components.Historian.Configuration
{
    internal class NpgsqlConfiguration
    {
        public bool Enable { get; set; }

        public string ConnectionString { get; set; }

        public CpuTemperatureConfiguration CpuTemperature { get; set; } = new CpuTemperatureConfiguration();

        public HddTemperatureConfiguration HddTemperature { get; set; } = new HddTemperatureConfiguration();

        public LoadConfiguration Load { get; set; } = new LoadConfiguration();

        public HddUsageConfiguration HddUsage { get; set; } = new HddUsageConfiguration();

        public PingConfiguration Ping { get; set; } = new PingConfiguration();

        public TcpPortStateConfiguration TcpPortState { get; set; } = new TcpPortStateConfiguration();

        public NsResolveConfiguration NsResolve { get; set; } = new NsResolveConfiguration();

        public Dht22MeasurementConfiguration Dht22 { get; set; } = new Dht22MeasurementConfiguration();

        public StateMeasurementConfiguration State { get; set; } = new StateMeasurementConfiguration();
    }
}

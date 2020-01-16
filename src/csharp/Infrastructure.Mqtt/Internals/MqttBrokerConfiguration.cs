namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class MqttBrokerConfiguration
    {
        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 1883;

        public SessionConfiguration Session { get; set; } = new SessionConfiguration();

        public SslConfiguration Ssl { get; set; } = new SslConfiguration();

        public AuthConfiguration Auth { get; set; } = new AuthConfiguration();
    }
}

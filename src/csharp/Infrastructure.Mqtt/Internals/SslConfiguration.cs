namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class SslConfiguration
    {
        public bool Enable { get; set; } = false;

        public bool AllowUntrustedCertificates { get; set; } = false;
    }
}

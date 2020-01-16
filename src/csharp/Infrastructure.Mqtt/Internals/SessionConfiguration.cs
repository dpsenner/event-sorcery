namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class SessionConfiguration
    {
        public string ClientId { get; set; } = string.Empty;

        public bool Clean { get; set; } = false;
    }
}

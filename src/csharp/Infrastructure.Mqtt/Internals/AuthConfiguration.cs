namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class AuthConfiguration
    {
        public bool Enable { get; set; } = false;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}

using EventSorcery.Events.Application;
using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Formatter;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal class MqttConnectionWatchdog : ISingletonComponent,
        INotificationHandler<ApplicationStartCompleted>,
        INotificationHandler<ApplicationShutdownRequested>,
        INotificationHandler<ApplicationShutdownCompleted>,
        INotificationHandler<ConnectionLost>,
        INotificationHandler<ConnectingFailed>,
        INotificationHandler<SubscribeRequest>,
        INotificationHandler<PublishRequest>,
        INotificationHandler<PublishAsJsonRequest>,
        INotificationHandler<ConnectRequest>,
        INotificationHandler<ReconnectRequest>
    {
        protected IMediator Mediator { get; }

        protected IMqttClient Client { get; }

        protected MqttBrokerConfiguration Configuration { get; }

        protected bool IsShutdownRequested { get; private set; }

        protected TimeSpan ReconnectAfter { get; private set; } = TimeSpan.FromSeconds(1);

        public MqttConnectionWatchdog(IMediator mediator, IMqttClient client, MqttBrokerConfiguration configuration)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task Handle(ApplicationStartCompleted notification, CancellationToken cancellationToken)
        {
            return ConnectAsync(cancellationToken);
        }

        public Task Handle(PublishRequest notification, CancellationToken cancellationToken)
        {
            if (!Client.IsConnected)
            {
                // Console.WriteLine($"Not publishing to topic {topic} because of disconnection");
                return Task.CompletedTask;
            }

            var topic = notification
                .Topic
                .Replace("$(hostname)", Dns.GetHostName());
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(notification.Qos.ToQualityOfService())
                .WithPayload(notification.Payload)
                .WithRetainFlag(notification.Retain)
                .Build();

            // Console.WriteLine($"Publishing to topic {message.Topic}: {Encoding.UTF8.GetString(message.Payload)}");
            return Client.PublishAsync(message, cancellationToken);
        }

        public async Task Handle(PublishAsJsonRequest notification, CancellationToken cancellationToken)
        {
            if (!Client.IsConnected)
            {
                // Console.WriteLine($"Not publishing to topic {topic} because of disconnection");
                return;
            }

            var topic = notification
                .Topic
                .Replace("$(hostname)", Dns.GetHostName());
            try
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings()
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    // ContractResolver = new OrderedContractResolver(),
                    Converters =
                    {
                        new Newtonsoft.Json.Converters.StringEnumConverter()
                    },
                };
                var payload = Newtonsoft.Json.JsonConvert.SerializeObject(notification.Payload, settings);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithQualityOfServiceLevel(notification.Qos.ToQualityOfService())
                    .WithPayload(payload)
                    .WithRetainFlag(notification.Retain)
                    .Build();
                await Client.PublishAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Publish to topic {topic} failed with exception: {ex}");
            }
        }

        public async Task Handle(SubscribeRequest notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Attempting to subscribe to topic {notification.Topic} with QOS {notification.Qos} ..");
            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(notification.Topic, notification.Qos.ToQualityOfService())
                .Build();
            var subscribeResult = await Client.SubscribeAsync(options, cancellationToken);
            if (subscribeResult.Items != null)
            {
                foreach (var subscribeResultItem in subscribeResult.Items)
                {
                    Console.WriteLine($"The attempt to subscribe to topic {notification.Topic} with QOS {notification.Qos} completed with result code {subscribeResultItem.ResultCode} ..");
                }
            }
        }

        public async Task Handle(ConnectionLost notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connection to broker {Configuration.Host}:{Configuration.Port} lost, attempting reconnect in {ReconnectAfter.TotalSeconds}s ..");
            await Mediator.Publish(new ReconnectRequest(), cancellationToken);
        }

        public async Task Handle(ConnectingFailed notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connection to broker {Configuration.Host}:{Configuration.Port} failed, attempting reconnect in {ReconnectAfter.TotalSeconds}s ..");
            await Mediator.Publish(new ReconnectRequest(), cancellationToken);
        }

        public Task Handle(ApplicationShutdownRequested notification, CancellationToken cancellationToken)
        {
            IsShutdownRequested = true;
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownCompleted notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Application shutdown, disconnecting from broker {Configuration.Host}:{Configuration.Port} ..");

            var options = GetDisconnectOptions();
            return Client.DisconnectAsync(options, cancellationToken);
        }

        public Task Handle(ConnectRequest notification, CancellationToken cancellationToken)
        {
            if (IsShutdownRequested)
            {
                return Task.CompletedTask;
            }

            return ConnectAsync(cancellationToken);
        }

        public async Task Handle(ReconnectRequest notification, CancellationToken cancellationToken)
        {
            // delay reconnect
            try
            {
                var reconnectAfter = TimeSpan.FromSeconds(1);
                await Task.Delay(reconnectAfter, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // there is some chance that this task gets cancelled
                // and in general it means that reconnect is no longer
                // needed
                return;
            }

            // pass this via the mediator to assert every other handler of the connecting failed
            // is run before the connect request is handled
            await Mediator.Publish(new ConnectRequest(), cancellationToken);
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"Connecting to broker {Configuration.Host}:{Configuration.Port} ..");
                var options = GetConnectOptions();
                await Client.ConnectAsync(options, cancellationToken);
            }
            catch (MQTTnet.Exceptions.MqttCommunicationException ex)
            {
                Console.WriteLine($"Could not connect to broker {Configuration.Host}:{Configuration.Port}: {ex.Message}");
            }
        }

        private IMqttClientOptions GetConnectOptions()
        {
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(Configuration.Host, Configuration.Port)
                .WithProtocolVersion(MqttProtocolVersion.V311);

            if (!string.IsNullOrWhiteSpace(Configuration.Session.ClientId))
            {
                // use a session but expire the session after 6h
                // we accept short outages less than 6h
                if (Configuration.Session.Clean)
                {
                    Console.WriteLine("Starting with a clean mqtt session ..");
                }

                builder
                    .WithClientId(Configuration.Session.ClientId)
                    .WithCleanSession(Configuration.Session.Clean)
                    .WithSessionExpiryInterval(60 * 60 * 6);
            }

            if (Configuration.Auth.Enable)
            {
                builder.WithCredentials(Configuration.Auth.Username, Configuration.Auth.Password);
            }

            if (Configuration.Ssl.Enable)
            {
                builder.WithTls(parameters =>
                {
                    parameters.UseTls = true;
                    parameters.AllowUntrustedCertificates = Configuration.Ssl.AllowUntrustedCertificates;
                });
            }

            return builder.Build();
        }

        private MqttClientDisconnectOptions GetDisconnectOptions()
        {
            var options = GetConnectOptions();
            switch (options.ProtocolVersion)
            {
                case MqttProtocolVersion.V310:
                case MqttProtocolVersion.V311:
                    return null;
                case MqttProtocolVersion.V500:
                    return new MqttClientDisconnectOptions()
                    {
                        ReasonCode = MqttClientDisconnectReason.NormalDisconnection,
                        ReasonString = "Shutdown",
                    };
                default:
                    throw new NotImplementedException();
            }
        }

        private class OrderedContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override System.Collections.Generic.IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(System.Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
            {
                return base
                    .CreateProperties(type, memberSerialization)
                    .OrderBy(ByType)
                    .ThenBy(p => p.PropertyName)
                    .ToList();
            }

            private int ByType(Newtonsoft.Json.Serialization.JsonProperty property)
            {
                if (typeof(string).Equals(property.PropertyType))
                {
                    return 0;
                }

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    return 1;
                }

                return 0;
            }
        }
    }
}

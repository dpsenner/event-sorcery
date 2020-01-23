using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using Npgsql;
using EventSorcery.Components.Historian.Configuration;
using EventSorcery.Events.Application;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;
using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;

namespace EventSorcery.Components.Historian
{
    internal class MeasurementHistorian : ISingletonComponent,
        INotificationHandler<ApplicationStartCompleted>,
        INotificationHandler<ApplicationShutdownRequested>,
        INotificationHandler<ApplicationShutdownCompleted>,
        INotificationHandler<ConnectionEstablished>,
        INotificationHandler<PublishReceived>,
        INotificationHandler<InboundMeasurement>
    {
        protected IMediator Mediator { get; }

        protected HistorianConfiguration HistorianConfiguration { get; }

        protected MeasurementsConfiguration MeasurementsConfiguration { get; }

        protected static string Prefix = "event/measurement";

        protected ConcurrentQueue<IMeasurement> Measurements { get; }

        protected CancellationTokenSource CancellationTokenSource { get; set; }

        public MeasurementHistorian(IMediator mediator, HistorianConfiguration historianConfiguration, MeasurementsConfiguration measurementsConfiguration)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            HistorianConfiguration = historianConfiguration ?? throw new ArgumentNullException(nameof(historianConfiguration));
            MeasurementsConfiguration = measurementsConfiguration ?? throw new ArgumentNullException(nameof(measurementsConfiguration));
            Measurements = new ConcurrentQueue<IMeasurement>();
        }

        public Task Handle(ConnectionEstablished notification, CancellationToken cancellationToken)
        {
            if (!HistorianConfiguration.Npgsql.Enable)
            {
                return Task.CompletedTask;
            }

            return Mediator.Publish(new SubscribeRequest()
            {
                Qos = MeasurementsConfiguration.Qos,
                Topic = $"{Prefix}/+",
            }, cancellationToken);
        }

        public Task Handle(ApplicationStartCompleted notification, CancellationToken cancellationToken)
        {
            // start worker queue that persists measurements
            CancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Main(CancellationTokenSource.Token));
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownRequested notification, CancellationToken cancellationToken)
        {
            // stop worker queue that persists measurements
            CancellationTokenSource.Cancel();

            // wait for stop to complete
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownCompleted notification, CancellationToken cancellationToken)
        {
            CancellationTokenSource.Dispose();
            return Task.CompletedTask;
        }

        public Task Handle(InboundMeasurement notification, CancellationToken cancellationToken)
        {
            Measurements.Enqueue(notification.Item);
            return Task.CompletedTask;
        }

        public Task Handle(PublishReceived notification, CancellationToken cancellationToken)
        {
            if (!HistorianConfiguration.Npgsql.Enable)
            {
                return Task.CompletedTask;
            }

            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                Converters =
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                },
            };

            if ($"{Prefix}/cpu-temperature".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("cpu-temperature", JsonConvert.DeserializeObject<CpuTemperatureMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/hdd-temperature".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("hdd-temperature", JsonConvert.DeserializeObject<HddTemperatureMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/hdd-usage".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("hdd-usage", JsonConvert.DeserializeObject<HddUsageMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/heartbeat".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("heartbeat", JsonConvert.DeserializeObject<HeartbeatMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/load".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("load", JsonConvert.DeserializeObject<LoadMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/ping".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("ping", JsonConvert.DeserializeObject<PingMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/tcp-port-state".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("tcp-port-state", JsonConvert.DeserializeObject<TcpPortStateMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/uptime".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("uptime", JsonConvert.DeserializeObject<UptimeMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/ns-resolve".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("ns-resolve", JsonConvert.DeserializeObject<NsResolveMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/dht22".Equals(notification.Topic))
            {
                // parse ping payload
                OnMeasurementReceived("dht22", JsonConvert.DeserializeObject<Dht22Measurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/state".Equals(notification.Topic))
            {
                // parse payload
                OnMeasurementReceived("state", JsonConvert.DeserializeObject<StateMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            if ($"{Prefix}/rational-number".Equals(notification.Topic))
            {
                // parse payload
                OnMeasurementReceived("rational-number", JsonConvert.DeserializeObject<RationalNumberMeasurement>(Encoding.UTF8.GetString(notification.Payload), settings), cancellationToken);
            }

            return Task.CompletedTask;
        }

        private Task OnMeasurementReceived(string name, IMeasurement measurement, CancellationToken cancellationToken)
        {
            return Mediator
                .Publish(new InboundMeasurement()
                {
                    Name = name,
                    Item = measurement,
                }, cancellationToken);
        }

        private async Task Main(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // flush all events in the queue
                while (Measurements.TryDequeue(out var item))
                {
                    await Handle(item, cancellationToken);
                }

                // delay and wait for next cycle
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // ignore, shutdown requested
                }
            }
        }

        private Task Handle(IMeasurement measurement, CancellationToken cancellationToken)
        {
            if (measurement is CpuTemperatureMeasurement cpuTemperatureMeasurement)
            {
                return Handle(cpuTemperatureMeasurement, cancellationToken);
            }

            if (measurement is HddTemperatureMeasurement hddTemperatureMeasurement)
            {
                return Handle(hddTemperatureMeasurement, cancellationToken);
            }

            if (measurement is HddUsageMeasurement hddUsageMeasurement)
            {
                return Handle(hddUsageMeasurement, cancellationToken);
            }

            if (measurement is HeartbeatMeasurement heartbeatMeasurement)
            {
                return Handle(heartbeatMeasurement, cancellationToken);
            }

            if (measurement is LoadMeasurement loadMeasurement)
            {
                return Handle(loadMeasurement, cancellationToken);
            }

            if (measurement is PingMeasurement pingMeasurement)
            {
                return Handle(pingMeasurement, cancellationToken);
            }

            if (measurement is TcpPortStateMeasurement tcpPortStateMeasurement)
            {
                return Handle(tcpPortStateMeasurement, cancellationToken);
            }

            if (measurement is UptimeMeasurement uptimeMeasurement)
            {
                return Handle(uptimeMeasurement, cancellationToken);
            }

            if (measurement is NsResolveMeasurement nsResolveMeasurement)
            {
                return Handle(nsResolveMeasurement, cancellationToken);
            }

            if (measurement is Dht22Measurement dht22Measurement)
            {
                return Handle(dht22Measurement, cancellationToken);
            }

            if (measurement is StateMeasurement stateMeasurement)
            {
                return Handle(stateMeasurement, cancellationToken);
            }

            if (measurement is RationalNumberMeasurement rationalNumberMeasurement)
            {
                return Handle(rationalNumberMeasurement, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private Task Handle(CpuTemperatureMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.CpuTemperature.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Hostname), measurement.Hostname)
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter(nameof(measurement.Cpu), measurement.Cpu)
                    .AddParameter(nameof(measurement.Temperature), measurement.Temperature);
            }, cancellationToken);
        }

        private Task Handle(HddTemperatureMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.HddTemperature.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Hostname), measurement.Hostname)
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter(nameof(measurement.Hdd), measurement.Hdd)
                    .AddParameter(nameof(measurement.Temperature), measurement.Temperature);
            }, cancellationToken);
        }

        private Task Handle(HddUsageMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.HddUsage.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter(nameof(measurement.Hdd), measurement.Hdd)
                    .AddParameter(nameof(measurement.Hostname), measurement.Hostname)
                    .AddParameter(nameof(measurement.Available), measurement.Available)
                    .AddParameter(nameof(measurement.Used), measurement.Used)
                    .AddParameter(nameof(measurement.Total), measurement.Total);
            }, cancellationToken);
        }

        private Task Handle(HeartbeatMeasurement measurement, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private Task Handle(LoadMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.Load.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Hostname), measurement.Hostname)
                    .AddParameter(nameof(measurement.LastOneMinute), measurement.LastOneMinute)
                    .AddParameter(nameof(measurement.LastFiveMinutes), measurement.LastFiveMinutes)
                    .AddParameter(nameof(measurement.LastFifteenMinutes), measurement.LastFifteenMinutes);
            }, cancellationToken);
        }

        private Task Handle(PingMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.Ping.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Source), measurement.Source)
                    .AddParameter(nameof(measurement.Target), measurement.Target)
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter("StatusAsText", $"{measurement.Status}")
                    .AddParameter(nameof(measurement.Status), (int)measurement.Status)
                    .AddParameter(nameof(measurement.Timeout), measurement.Timeout.TotalSeconds)
                    .AddParameter(nameof(measurement.RoundtripTime), measurement.RoundtripTime.TotalSeconds);
            }, cancellationToken);
        }

        private Task Handle(TcpPortStateMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.TcpPortState.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Source), measurement.Source)
                    .AddParameter(nameof(measurement.Target), measurement.Target)
                    .AddParameter(nameof(measurement.Port), measurement.Port)
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter("StatusAsText", $"{measurement.Status}")
                    .AddParameter(nameof(measurement.Status), (int)measurement.Status)
                    .AddParameter(nameof(measurement.After), measurement.After.TotalSeconds)
                    .AddParameter(nameof(measurement.Timeout), measurement.Timeout.TotalSeconds);
            }, cancellationToken);
        }

        private Task Handle(NsResolveMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.NsResolve.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Source), measurement.Source)
                    .AddParameter(nameof(measurement.Target), measurement.Target)
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter("StatusAsText", $"{measurement.Status}")
                    .AddParameter(nameof(measurement.Status), (int)measurement.Status)
                    .AddParameter(nameof(measurement.After), measurement.After.TotalSeconds)
                    .AddParameter(nameof(measurement.Timeout), measurement.Timeout.TotalSeconds);
            }, cancellationToken);
        }

        private Task Handle(Dht22Measurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.Dht22.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Alias), measurement.Alias)
                    .AddParameter(nameof(measurement.Hostname), measurement.Hostname)
                    .AddParameter(nameof(measurement.IsLastReadSuccessful), measurement.IsLastReadSuccessful)
                    .AddParameter(nameof(measurement.LastReadAge), measurement.LastReadAge)
                    .AddParameter(nameof(measurement.LastRelativeHumidity), measurement.LastRelativeHumidity)
                    .AddParameter(nameof(measurement.LastTemperature), measurement.LastTemperature);
            }, cancellationToken);
        }

        private Task Handle(StateMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.State.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Metric), measurement.Metric)
                    .AddParameter(nameof(measurement.Status), measurement.Status)
                    .AddParameter(nameof(measurement.StatusText), measurement.StatusText)
                    .AddParameter(nameof(measurement.Comment), measurement.Comment);
            }, cancellationToken);
        }

        private Task Handle(RationalNumberMeasurement measurement, CancellationToken cancellationToken)
        {
            return Insert(command =>
            {
                command
                    .WithCommandText(HistorianConfiguration.Npgsql.RationalNumber.InsertQuery)
                    .AddParameter(nameof(measurement.Timestamp), measurement.Timestamp.ToLocalTime())
                    .AddParameter(nameof(measurement.Metric), measurement.Metric)
                    .AddParameter(nameof(measurement.Category), measurement.Category)
                    .AddParameter(nameof(measurement.Value), measurement.Value);
            }, cancellationToken);
        }

        private Task Handle(UptimeMeasurement measurement, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task Insert(Action<NpgsqlCommand> configure, CancellationToken cancellationToken)
        {
            try
            {
                using var connection = new NpgsqlConnection(HistorianConfiguration.Npgsql.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                using var command = connection.CreateCommand();
                configure(command);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

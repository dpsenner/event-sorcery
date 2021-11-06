using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Application;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;

namespace EventSorcery.Components.Measuring
{
    internal class MeasurementRequestGenerator : ISingletonComponent,
        INotificationHandler<SensorMeasurement>,
        INotificationHandler<OutboundMeasurement>,
        IMeasurementTimingService,
        INotificationHandler<ApplicationStartCompleted>,
        INotificationHandler<ApplicationShutdownRequested>,
        INotificationHandler<ApplicationShutdownCompleted>,
        INotificationHandler<ConnectionEstablished>,
        INotificationHandler<ConnectionLost>
    {
        protected IMediator Mediator { get; }

        protected Configuration.Generic Generic { get; }

        protected CancellationTokenSource CancellationTokenSource { get; set; }

        protected bool IsConnected { get; set; }

        protected IDictionary<ISensorScanRateItem, Stopwatch> LastMeasurement { get; }

        protected IDictionary<ISensorScanRateItem, Func<IEnumerable<ISensorScanRateItem>, CancellationToken, Task>> IsDueCallback { get; }


        public MeasurementRequestGenerator(IMediator mediator, Configuration.Generic generic)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Generic = generic ?? throw new ArgumentNullException(nameof(generic));
            LastMeasurement = new Dictionary<ISensorScanRateItem, Stopwatch>();
            IsDueCallback = new Dictionary<ISensorScanRateItem, Func<IEnumerable<ISensorScanRateItem>, CancellationToken, Task>>();
        }

        public Task Handle(ApplicationStartCompleted notification, CancellationToken cancellationToken)
        {
            CancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Main(CancellationTokenSource.Token));
            return Task.CompletedTask;
        }

        public Task Handle(ConnectionEstablished notification, CancellationToken cancellationToken)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task Handle(ConnectionLost notification, CancellationToken cancellationToken)
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownRequested notification, CancellationToken cancellationToken)
        {
            CancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownCompleted notification, CancellationToken cancellationToken)
        {
            // dispose cancellation token
            CancellationTokenSource.Dispose();
            return Task.CompletedTask;
        }

        public Task Handle(SensorMeasurement notification, CancellationToken cancellationToken)
        {
            // discard publish if not connected
            if (!IsConnected)
            {
                Console.WriteLine($"Not connected, discarding sensor measurement of sensor '{notification.Sensor}' ..");
                return Task.CompletedTask;
            }

            return Mediator.Publish(new PublishRequest()
            {
                Topic = $"{Generic.TopicPrefix}/{notification.Sensor}",
                Payload = Encoding.UTF8.GetBytes(notification.Value),
                Qos = Generic.Measurements.Qos,
                Retain = false,
            }, cancellationToken);
        }

        public Task Handle(OutboundMeasurement notification, CancellationToken cancellationToken)
        {
            // discard publish if not connected
            if (!IsConnected)
            {
                Console.WriteLine($"Not connected, discarding outbound measurement named '{notification.Name}' ..");
                return Task.CompletedTask;
            }

            return Mediator.Publish(new PublishAsJsonRequest()
            {
                Topic = $"event/measurement/{notification.Name}",
                Payload = notification.Item,
                Qos = Generic.Measurements.Qos,
                Retain = false,
            }, cancellationToken);
        }

        public void Register<T>(T item, Func<T, CancellationToken, Task> isDueCallback)
            where T : ISensorScanRateItem
        {
            LastMeasurement[item] = Stopwatch.StartNew();
            IsDueCallback[item] = (IEnumerable<ISensorScanRateItem> items, CancellationToken cancellationToken) =>
            {
                // if this doesn't work, it is a bug
                var itemsAsT = items.Where(t => t is T).Cast<T>().ToList();
                var itemAsT = itemsAsT.FirstOrDefault();
                return isDueCallback(itemAsT, cancellationToken);
            };
        }

        public void Register<T>(IEnumerable<T> items, Func<IEnumerable<T>, CancellationToken, Task> isDueCallback)
            where T : ISensorScanRateItem
        {
            Func<IEnumerable<ISensorScanRateItem>, CancellationToken, Task> callback = (IEnumerable<ISensorScanRateItem> items, CancellationToken cancellationToken) =>
            {
                // if this doesn't work, it is a bug
                var itemsAsT = items.Where(t => t is T).Cast<T>().ToList();
                return isDueCallback(itemsAsT, cancellationToken);
            };
            foreach (var item in items)
            {
                LastMeasurement[item] = Stopwatch.StartNew();
                IsDueCallback[item] = callback;
            }
        }

        public bool IsDue<T>(T item)
            where T : ISensorScanRateItem
        {
            if (!LastMeasurement.ContainsKey(item))
            {
                throw new InvalidOperationException("Sensor configuration item has not been registered earlier");
            }

            var stopwatch = LastMeasurement[item];
            return stopwatch.Elapsed >= item.ScanRate;
        }

        public void ResetDue<T>(T item)
            where T : ISensorScanRateItem
        {
            if (!LastMeasurement.ContainsKey(item))
            {
                throw new InvalidOperationException("Sensor configuration item has not been registered earlier");
            }

            LastMeasurement[item].Restart();
        }

        public void ResetDue<T>(IEnumerable<T> items)
            where T : ISensorScanRateItem
        {
            foreach (var item in items)
            {
                ResetDue(item);
            }
        }

        private Task OnIsDue(IEnumerable<ISensorScanRateItem> items, CancellationToken cancellationToken)
        {
            // inform any due items
            var callbacks = items
                .Where(t => IsDueCallback.ContainsKey(t))
                .Select(t => IsDueCallback[t])
                .Distinct();
            return Task.WhenAll(callbacks.Select(t => t(items, cancellationToken)));
        }

        private async Task Main(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (IsConnected)
                    {
                        // collect due items
                        var dueItems = LastMeasurement
                            .Where(t => IsDue(t.Key))
                            .Select(t => t.Key)
                            .ToList();
                        if (dueItems.Any())
                        {
                            await OnIsDue(dueItems, cancellationToken);
                        }
                        else
                        {
                            // no items due
                        }
                    }
                    else
                    {
                        // do not request measurements while disconnected
                        // we have to delay in this situation at least for some
                        // time to avoid hot looping
                        Console.WriteLine($"Not connected, delaying new measurements for one second ..");
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }

                    // delay and wait for next cycle
                    var nextDueTimes = LastMeasurement
                        .Select(t => t.Key.ScanRate - t.Value.Elapsed)
                        .Distinct()
                        .OrderBy(t => t)
                        .ToList();
                    if (nextDueTimes.Count > 0)
                    {
                        var minDueTime = nextDueTimes.First();

                        // this delay is a smart guess observing that a Task.Delay() won't
                        // be accurate enough to accomodate a delay shorter than this interval.
                        // better avoid unnecessary delays if the smallest delay is shorter than this
                        // upon reaching this interval it is better to skip the delay and straight
                        // check if any item is due
                        var smallestPossibleDelay = TimeSpan.Zero;
                        if (minDueTime < smallestPossibleDelay)
                        {
                            // another item is already due, skip delay
                        }
                        else
                        {
                            await Task.Delay(minDueTime, cancellationToken);
                        }
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Measurement request generator caught exception and resumes operation: {ex}");
                }
            }
        }
    }
}

using MediatR;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;
using System.Diagnostics;
using System.Collections.Generic;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Load : ASensor
    {
        protected IMediator Mediator { get; }

        protected LoadConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        private ILoadCollector LoadCollector { get; set; }

        public Load(IMediator mediator, LoadConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            if (!Configuration.Enable)
            {
                return;
            }

            MeasurementTimingService.Register(Configuration, OnIsDue);

            // if on windows, start collector
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    LoadCollector = new LinuxLoadCollector();
                    break;
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Win32NT:
                    LoadCollector = new WindowsLoadCollector();
                    break;
                case PlatformID.MacOSX:
                // not supported
                default:
                    break;
            }

            LoadCollector?.Start();
        }

        protected override void OnApplicationShutdownRequested()
        {
            base.OnApplicationShutdownRequested();

            // shutdown collector
            LoadCollector?.Stop();
        }

        protected async Task OnIsDue(LoadConfiguration configuration, CancellationToken cancellationToken)
        {
            if (LoadCollector == null)
            {
                // not supported
                MeasurementTimingService.ResetDue(configuration);
                return;
            }

            var loadAverage = await LoadCollector.GetLoadAverage(cancellationToken);
            if (loadAverage == null)
            {
                // N/A
                MeasurementTimingService.ResetDue(configuration);
                return;
            }

            await Mediator.Publish(new OutboundMeasurement()
            {
                Name = "load",
                Item = new LoadMeasurement()
                {
                    Timestamp = DateTime.UtcNow,
                    Hostname = System.Net.Dns.GetHostName(),
                    LastOneMinute = loadAverage.OneMinute,
                    LastFiveMinutes = loadAverage.FiveMinutes,
                    LastFifteenMinutes = loadAverage.FifteenMinutes,
                },
            }, cancellationToken);

            MeasurementTimingService.ResetDue(configuration);
        }

        private interface ILoadCollector
        {
            Task<LoadAverage> GetLoadAverage(CancellationToken cancellationToken);

            void Start();

            void Stop();
        }

        private class LinuxLoadCollector : ILoadCollector
        {
            public async Task<LoadAverage> GetLoadAverage(CancellationToken cancellationToken)
            {
                if (!File.Exists("/proc/loadavg"))
                {
                    return null;
                }

                var rawString = await File.ReadAllTextAsync("/proc/loadavg", Encoding.UTF8, cancellationToken);
                var loadSplit = rawString.Split(" ").Take(3).Select(x => Convert.ToDouble(x, CultureInfo.InvariantCulture)).ToList();
                return new LoadAverage()
                {
                    OneMinute = loadSplit[0],
                    FiveMinutes = loadSplit[1],
                    FifteenMinutes = loadSplit[2],
                };
            }

            public void Start()
            {
                // void
            }

            public void Stop()
            {
                // void
            }
        }

        private class WindowsLoadCollector : ILoadCollector
        {
            protected CancellationTokenSource CancellationTokenSource { get; set; }

            protected LoadAverage State { get; set; }

            protected object StateLock { get; } = new object();

            public Task<LoadAverage> GetLoadAverage(CancellationToken cancellationToken)
            {
                lock (StateLock)
                {
                    Console.WriteLine($"{DateTime.Now} - {State}");
                    // return Task.FromResult(State);
                    return Task.FromResult(default(LoadAverage));
                }
            }

            public void Start()
            {
                // void
                CancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => Main(CancellationTokenSource.Token));
            }

            public void Stop()
            {
                // void
                CancellationTokenSource.Cancel();
            }

            private class ProcessorTime
            {
                public DateTime When { get; set; }


                public double Percentage { get; set; }
            }

            private async Task Main(CancellationToken cancellationToken)
            {
                var last1 = new List<ProcessorTime>();
                var last5 = new List<ProcessorTime>();
                var last15 = new List<ProcessorTime>();

                using var performanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                while (!cancellationToken.IsCancellationRequested)
                {
                    // remove oldest, if older than 15 minutes
                    last1.RemoveAll(t => (DateTime.UtcNow - t.When) > TimeSpan.FromMinutes(1));
                    last5.RemoveAll(t => (DateTime.UtcNow - t.When) > TimeSpan.FromMinutes(5));
                    last15.RemoveAll(t => (DateTime.UtcNow - t.When) > TimeSpan.FromMinutes(15));

                    // add current
                    var current = new ProcessorTime()
                    {
                        When = DateTime.UtcNow,
                        Percentage = performanceCounter.NextValue() / 100 * Environment.ProcessorCount,
                    };
                    last15.Add(current);
                    last5.Add(current);
                    last1.Add(current);

                    // update state
                    if (last1.Count > 2)
                    {
                        lock (StateLock)
                        {
                            State = new LoadAverage()
                            {
                                OneMinute = LoadAverageFrom(last1),
                                FiveMinutes = LoadAverageFrom(last5),
                                FifteenMinutes = LoadAverageFrom(last15),
                            };
                        }
                    }

                    // delay and wait for next cycle
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        // ignore
                    }
                }

                CancellationTokenSource.Dispose();
            }

            private double LoadAverageFrom(List<ProcessorTime> items)
            {
                return items.Average(t => t.Percentage);
            }
        }

        private class LoadAverage
        {
            public double OneMinute { get; set; }

            public double FiveMinutes { get; set; }

            public double FifteenMinutes { get; set; }

            public override string ToString()
            {
                return $"{OneMinute.ToString("0.00", CultureInfo.InvariantCulture)} {FiveMinutes.ToString("0.00", CultureInfo.InvariantCulture)} {FifteenMinutes.ToString("0.00", CultureInfo.InvariantCulture)}";
            }
        }
    }
}

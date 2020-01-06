using MediatR;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Application;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;
using EventSorcery.Infrastructure.DependencyInjection;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class Dht22Sensor : ASensor
    {
        protected IMediator Mediator { get; }

        protected Dht22SensorConfiguration Configuration { get; }

        private readonly Dictionary<int, IDht22Reader> DeviceReaders = new Dictionary<int, IDht22Reader>();

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public Dht22Sensor(IMediator mediator, Dht22SensorConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            foreach (var item in Configuration.Items.Where(t => t.Enable))
            {
                var gpioPin = item.GpioPin;

                // Console.WriteLine($"Initialize gpio reader to read dht22 sensor gpio pin {gpioPin} ..");
                DeviceReaders.Add(gpioPin, new Dht22ReaderV1(gpioPin, PinNumberingScheme.Board));
            }

            MeasurementTimingService.Register(Configuration.Items.Where(t => t.Enable), OnIsDue);
        }

        protected override void OnApplicationShutdownCompleted()
        {
            base.OnApplicationShutdownCompleted();

            foreach (var item in DeviceReaders.Values)
            {
                item.Dispose();
            }

            DeviceReaders.Clear();
        }

        private async Task OnIsDue(IEnumerable<Dht22SensorConfiguration.Dht22ConfigurationItem> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                var reader = DeviceReaders[item.GpioPin];
                var alias = item.Alias;

                try
                {
                    reader.ReadData();
                    MeasurementTimingService.ResetDue(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read dht22 sensor from gpio pin {item.GpioPin} with exception: {ex}");
                }

                if (reader.LastReadAge.HasValue)
                {
                    double lastTemperature = reader.LastTemperature;
                    double lastHumidity = reader.LastHumidity;
                    TimeSpan lastReadAge = reader.LastReadAge.Value;
                    bool isLastReadSuccessful = reader.IsLastReadSuccessful;
                    await Mediator.Publish(new OutboundMeasurement()
                    {
                        Name = "dht22",
                        Item = new Dht22Measurement()
                        {
                            Timestamp = DateTime.UtcNow,
                            Hostname = System.Net.Dns.GetHostName(),
                            Alias = alias,
                            IsLastReadSuccessful = isLastReadSuccessful,
                            LastTemperature = lastTemperature,
                            LastRelativeHumidity = lastHumidity,
                            LastReadAge = lastReadAge,
                        },
                    }, cancellationToken);
                }
                else
                {
                    // do not publish if read was unsuccessful
                    // Console.WriteLine($"Failed to read dht22 sensor from gpio pin {item.GpioPin} ..");
                }
            }
        }

        private interface IDht22Reader : IDisposable
        {
            double LastTemperature { get; }

            double LastHumidity { get; }

            TimeSpan? LastReadAge { get; }

            bool IsLastReadSuccessful { get; }

            void ReadData();
        }

        private class Dht22ReaderV1 : IDht22Reader
        {
            /// <summary>
            /// GPIO pin
            /// </summary>
            protected readonly int _pin;

            /// <summary>
            /// <see cref="GpioController"/> related with the <see cref="_pin"/>.
            /// </summary>
            protected readonly GpioController _controller;

            // wait about 1 ms
            private readonly Stopwatch _stopwatch = new Stopwatch();
            private readonly Stopwatch _lastReadAge = new Stopwatch();
            private readonly Stopwatch _readDataTiming = new Stopwatch();
            private bool _hasBeenReadOnce = false;
            private List<(PinEventTypes, TimeSpan)> _register = new List<(PinEventTypes, TimeSpan)>();

            /// <summary>
            /// Gets a boolean flag that indicates whether the last read was succesful or not.
            /// </summary>
            public bool IsLastReadSuccessful { get; private set; }

            /// <summary>
            /// Gets a time span that indicates the age of the last measurement. Note that this returns default(TimeSpan?)
            /// if no measurement has been made so far.
            /// </summary>
            public TimeSpan? LastReadAge { get { return _hasBeenReadOnce ? _lastReadAge.Elapsed as TimeSpan? : null; } }

            /// <summary>
            /// Get the last read temperature
            /// </summary>
            /// <remarks>
            /// If last read was not successfull, it returns double.NaN
            /// </remarks>
            public double LastTemperature { get; private set; }

            /// <summary>
            /// Get the last read of relative humidity in percentage
            /// </summary>
            /// <remarks>
            /// If last read was not successfull, it returns double.NaN
            /// </remarks>
            public double LastHumidity { get; private set; }

            /// <summary>
            /// Create a DHT sensor
            /// </summary>
            /// <param name="pin">The pin number (GPIO number)</param>
            /// <param name="pinNumberingScheme">The GPIO pin numbering scheme</param>
            public Dht22ReaderV1(int pin, PinNumberingScheme pinNumberingScheme)
            {
                _controller = new GpioController(pinNumberingScheme);
                _pin = pin;
            }

            /// <summary>
            /// Read through One-Wire
            /// </summary>
            public void ReadData()
            {
                if (!_controller.IsPinOpen(_pin))
                {
                    //Console.WriteLine($"Attempting to open gpio pin {_pin} ..");
                    _controller.OpenPin(_pin);

                    // delay 1s to make sure DHT is stable
                    DelayExactly(TimeSpan.FromSeconds(1), true);
                    //Console.WriteLine($"Opened gpio pin {_pin} ..");
                }
                
                var oneMicrosecond = TimeSpan.FromMilliseconds(0.001);

                // change pin mode to output
                SetPinMode(PinMode.Output);

                // keep data line HIGH
                //Write(PinValue.High);
                //DelayExactly(TimeSpan.FromMilliseconds(100), false);

                // send trigger signal
                // wait at least 18 milliseconds
                // here wait for 18 milliseconds will cause sensor initialization to fail
                // therefore we make it safe 20 milliseconds
                Write(PinValue.Low);
                DelayExactly(oneMicrosecond * 1000 * 20, false);

                // pull up data line and set the pin mode to input
                // the DHT should react and respond with a falling flank
                Write(PinValue.High);
                SetPinMode(PinMode.Input);

                // wait for the dht communication to happen in a very tight
                // loop to collect as much measurements as possible with a
                // high resolution
                var readTimeout = 5 * 8 * oneMicrosecond * 150;

                // reset internal state
                _register.Clear();
                _readDataTiming.Restart();
                while (true)
                {
                    var elapsed = _readDataTiming.Elapsed;
                    if (_readDataTiming.Elapsed > readTimeout)
                    {
                        // escape the polling read
                        break;
                    }

                    var pinValue = Read();
                    if (_register.Count > 0)
                    {
                        if (_register[_register.Count-1].Item1 != pinValue)
                        {
                            _register.Add((pinValue, elapsed));
                        }
                    }
                    else
                    {
                        _register.Add((pinValue, elapsed));
                    }
                }

                // filter every captured flank to only contain the falling flanks
                // and the time we observed it
                List<TimeSpan> fallingFlanks = new List<TimeSpan>();
                for (int i=0; i < _register.Count-1; i++)
                {
                    var item = _register[i];
                    var next = _register[i+1];
                    if (item.Item1 == PinEventTypes.Rising && next.Item1 == PinEventTypes.Falling)
                    {
                        var elapsed = next.Item2 - item.Item2;
                        fallingFlanks.Add(elapsed);
                    }
                }

                // if there are not enough flanks, we can abort right away
                if (fallingFlanks.Count < 4 * 8)
                {
                    IsLastReadSuccessful = false;
                    return;
                }

                // transform falling flank durations into bits
                List<bool> bits = new List<bool>();
                foreach (var item in fallingFlanks.TakeLast(5 * 8))
                {
                    var microseconds = (long)(item.TotalMilliseconds * 1000);
                    if (microseconds < 35)
                    {
                        bits.Add(false);
                    }
                    else if (microseconds > 55)
                    {
                        bits.Add(true);
                    }
                }

                // if there are not exactly 40 bits we can abort right away
                // the protocol enforces exactly 5 bytes
                if (bits.Count != 5 * 8)
                {
                    IsLastReadSuccessful = false;
                    return;
                }

                // transform bits to an array of bytes
                var _readBuff = ToByteArray(bits.ToArray());
                if (_readBuff.Length != 5)
                {
                    IsLastReadSuccessful = false;
                    return;
                }
                
                // verify data was actually read
                if (_readBuff.Select(t => (int)t).Sum() == 0)
                {
                    IsLastReadSuccessful = false;
                    return;
                }

                // verify checksum
                if ((_readBuff[4] != ((_readBuff[0] + _readBuff[1] + _readBuff[2] + _readBuff[3]) & 0xFF)))
                {
                    IsLastReadSuccessful = false;
                    return;
                }

                var humidity = GetHumidity(_readBuff);
                if (humidity < 0 || humidity > 100)
                {
                    // data makes no sense
                    IsLastReadSuccessful = false;
                    return;
                }

                LastHumidity = humidity;
                LastTemperature = GetTemperature(_readBuff);
                _lastReadAge.Restart();
                IsLastReadSuccessful = true;
                _hasBeenReadOnce = true;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _controller?.Dispose();
            }

            private byte[] ToByteArray(bool[] bools)
            {
                int bytes = bools.Length / 8;
                if ((bools.Length % 8) != 0) bytes++;
                byte[] arr2 = new byte[bytes];
                for (int i = 0; i < bools.Length; i++)
                {
                    var byteIndex = i / 8;
                    arr2[byteIndex] <<= 1;
                    if (bools[i])
                    {
                        arr2[byteIndex] += 1;
                    }
                }

                return arr2;
            }

            private void OnPinChangedEvent(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
            {
                /*var old = _currentPinValue;
                var nu = pinValueChangedEventArgs.ChangeType;
                Console.WriteLine($"{_readDataTiming.Elapsed} - Pin {pinValueChangedEventArgs.PinNumber}, flank changes {old} => {nu}");
                _currentPinValue = nu;
                */
                _register.Add((pinValueChangedEventArgs.ChangeType, _readDataTiming.Elapsed));
            }

            private void SetPinMode(PinMode pinMode)
            {
                // Console.WriteLine($"{_readDataTiming.Elapsed} - Pin {_pin}, mode is now {pinMode}");
                _controller.SetPinMode(_pin, pinMode);
            }

            private void Write(PinValue value)
            {
                // Console.WriteLine($"{_readDataTiming.Elapsed} - Pin {_pin} write {value}");
                _controller.Write(_pin, value);
            }

            private PinEventTypes Read()
            {
                var value = _controller.Read(_pin);
                if (value == PinValue.High)
                {
                    return PinEventTypes.Rising;
                }
                else if (value == PinValue.Low)
                {
                    return PinEventTypes.Falling;
                }

                return PinEventTypes.None;
            }
            
            private static double GetHumidity(byte[] readBuff)
            {
                var value = ((readBuff[0] << 8) + readBuff[1]) * 0.1;
                return Math.Round(value, 1);
            }
            
            private static double GetTemperature(byte[] readBuff)
            {
                var temp = (((readBuff[2] & 0x7F) << 8) + readBuff[3]) * 0.1;

                // if MSB = 1 we have negative temperature
                if ((readBuff[2] & 0x80) == 0x80)
                {
                    temp *= -1;
                }

                return Math.Round(temp, 1);
            }
            
            private void DelayExactly(TimeSpan delay, bool allowThreadYield)
            {
                _stopwatch.Restart();
                long start = Stopwatch.GetTimestamp();
                long microseconds = (long)(delay.TotalMilliseconds / 1000);
                ulong minimumTicks = (ulong)(microseconds * Stopwatch.Frequency / 1_000_000);

                if (!allowThreadYield)
                {
                    do
                    {
                        Thread.SpinWait(1);
                    }
                    while (_stopwatch.Elapsed < delay);
                }
                else
                {
                    SpinWait spinWait = new SpinWait();
                    do
                    {
                        spinWait.SpinOnce();
                    }
                    while (_stopwatch.Elapsed < delay);
                }
            }
        }
    }
}

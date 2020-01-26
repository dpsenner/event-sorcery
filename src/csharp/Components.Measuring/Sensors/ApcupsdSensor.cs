using MediatR;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class ApcupsdSensor : ASensor
    {
        protected IMediator Mediator { get; }

        protected ApcupsdSensorConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public ApcupsdSensor(IMediator mediator, ApcupsdSensorConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            if (Configuration.Enable)
            {
                MeasurementTimingService.Register(Configuration, OnIsDue);
            }
        }

        private async Task OnIsDue(ApcupsdSensorConfiguration configuration, CancellationToken cancellationToken)
        {
            if (!configuration.Enable)
            {
                return;
            }
            
            MeasurementTimingService.ResetDue(configuration);
            var upsBatteryMeasurement = await GetUpsBatteryMeasurement(configuration, cancellationToken);
            await Mediator.Publish(new OutboundMeasurement()
            {
                Name = "ups-battery",
                Item = upsBatteryMeasurement,
            }, cancellationToken);
        }

        private async Task<UpsBatteryMeasurement> GetUpsBatteryMeasurement(ApcupsdSensorConfiguration configuration, CancellationToken cancellationToken)
        {
            using (var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "apcaccess",
                Arguments = "-u",
                StandardOutputEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            }))
            {
                var result = new UpsBatteryMeasurement()
                {
                    Timestamp = DateTime.UtcNow,
                    Alias = configuration.Alias,
                    Hostname = System.Net.Dns.GetHostName(),
                };
                while (!process.HasExited)
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
                        var match = Regex.Match(line, "^(.+): (.+)");
                        if (!match.Success)
                        {
                            // skip
                        }
                        else
                        {
                            var topic = match.Groups[1].Value.Trim();
                            var value = match.Groups[2].Value.Trim();

                            switch (topic)
                            {
                                case "DATE":
                                {
                                    try
                                    {
                                        if (DateTime.TryParseExact(value.Substring(0, 19), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                                        {
                                            result.Age = DateTime.Now - timestamp;
                                        }
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        // ignore
                                    }
                                    catch (FormatException)
                                    {
                                        // ignore
                                    }
                                }
                                break;
                                case "MANDATE":
                                {

                                    try
                                    {
                                        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var manufacturingDate))
                                        {
                                            result.ManufacturingDate = manufacturingDate;
                                        }
                                    }
                                    catch (FormatException)
                                    {
                                        // ignore
                                    }
                                }
                                break;
                                case "MODEL":
                                {
                                    result.Model = value;
                                }
                                break;
                                case "BATTV":
                                {
                                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.CurrentBatteryVoltage = valueParsed;
                                    }
                                }
                                break;
                                case "NOMBATTV":
                                {
                                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.NominativeBatteryVoltage = valueParsed;
                                    }
                                }
                                break;
                                case "BCHARGE":
                                {
                                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.BatteryCharge = valueParsed;
                                    }
                                }
                                break;
                                case "MBATTCHG":
                                {
                                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.MinBatteryCharge = valueParsed;
                                    }
                                }
                                break;
                                case "TIMELEFT":
                                {
                                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.TimeLeft = TimeSpan.FromMinutes(valueParsed);
                                    }
                                }
                                break;
                                case "MINTIMEL":
                                {
                                    if (double.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.MinTimeLeft = TimeSpan.FromMinutes(valueParsed);
                                    }
                                }
                                break;
                                case "CUMONBATT":
                                {
                                    if (double.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueParsed))
                                    {
                                        result.CumulativeOnBattery = TimeSpan.FromSeconds(valueParsed);
                                    }
                                }
                                break;
                                case "STATUS":
                                {
                                    /*
                                    CAL TRIM BOOST ONLINE ONBATT OVERLOAD LOWBATT REPLACEBATT NOBATT SLAVE SLAVEDOWN
                                    or
                                    COMMLOST
                                    or
                                    SHUTTING DOWN
                                    */
                                    result.StatusText = value;
                                    if (value.Contains("ONLINE"))
                                    {
                                        result.IsOnline = true;
                                    }

                                    if (value.Contains("ONBATT"))
                                    {
                                        result.IsOnBattery = true;
                                    }

                                    if (value.Contains("OVERLOAD"))
                                    {
                                        result.IsOverload = true;
                                    }

                                    if (value.Contains("LOWBATT"))
                                    {
                                        result.IsOnLowBattery = true;
                                    }

                                    if (value.Contains("REPLACEBATT"))
                                    {
                                        result.IsBatteryReplacementRequested = true;
                                    }

                                    if (value.Contains("NOBATT"))
                                    {
                                        result.IsBatteryMissing = true;
                                    }

                                    if (value.Contains("COMMLOST"))
                                    {
                                        result.IsCommunicationLost = true;
                                    }

                                    if (value.Contains("SHUTTING DOWN"))
                                    {
                                        result.IsShuttingDown = true;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(result.StatusText))
                {
                    return result;
                }

                return null;
            }
        }
    }
}

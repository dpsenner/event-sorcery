using System;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class UpsBatteryMeasurement : IMeasurement
    {
        public DateTime Timestamp { get; set; }

        public TimeSpan? Age { get; set; }

        public string Hostname { get; set; }

        public string Model { get; set; }

        public string Alias { get; set; }

        public string StatusText { get; set; }
        
        public bool IsOnline { get; set; }

        public bool IsOnBattery { get; set; }

        public bool IsOnLowBattery { get; set; }

        public bool IsCommunicationLost { get; set; }

        public bool IsShuttingDown { get; set; }

        public bool IsOverload { get; set; }

        public bool IsBatteryReplacementRequested { get; set; }

        public bool IsBatteryMissing { get; set; }

        public double? BatteryCharge { get; set; }

        public TimeSpan? TimeLeft { get; set; }

        public double? MinBatteryCharge { get; set; }

        public TimeSpan? MinTimeLeft { get; set; }

        public TimeSpan? CumulativeOnBattery { get; set; }

        public double? CurrentBatteryVoltage { get; set; }

        public double? NominativeBatteryVoltage { get; set; }

        public DateTime? ManufacturingDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Measuring.Measurements
{
    public class GenericJsonMeasurement : IMeasurement
    {
        public string Topic { get; set; }

        public string QueryString { get; set; }

        public string Payload { get; set; }
    }
}

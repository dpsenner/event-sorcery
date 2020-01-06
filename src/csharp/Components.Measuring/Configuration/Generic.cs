using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Measuring.Configuration
{
    internal class Generic
    {
        public string TopicPrefix { get; set; }

        public MeasurementsConfiguration Measurements { get; set; } = new MeasurementsConfiguration();
    }
}

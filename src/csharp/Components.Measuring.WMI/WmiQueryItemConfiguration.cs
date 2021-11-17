using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Measuring.Sensors.WMI
{
    internal class WmiQueryItemConfiguration
    {
        public string Domain { get; set; }

        public string Hostname { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Namespace { get; set; }

        public string Query { get; set; }

        public string Topic { get; set; }
    }
}

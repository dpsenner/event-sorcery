using System.Collections.Generic;

namespace EventSorcery.Components.Historian.Configuration
{
    internal class GenericJsonConfigurationItem
    {
        public List<string> Topics { get; set; }

        public string QueryString { get; set; }
    }
}

using System.Collections.Generic;

namespace EventSorcery.Components.Historian.Configuration
{
    internal class GenericJsonConfiguration
    {
        public bool Enable { get; set; } = false;

        public List<GenericJsonConfigurationItem> Items { get; set; } = new List<GenericJsonConfigurationItem>();
    }
}

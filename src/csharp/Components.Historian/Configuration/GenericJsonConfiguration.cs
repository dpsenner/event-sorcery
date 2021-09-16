using System.Collections.Generic;

namespace EventSorcery.Components.Historian.Configuration
{
    internal class GenericJsonConfiguration
    {
        public List<GenericJsonConfigurationItem> Items { get; set; } = new List<GenericJsonConfigurationItem>();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Infrastructure.Configuration
{
    public interface IConfigurationPathProvider
    {
        string Path { get; }
    }
}

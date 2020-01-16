using System;

namespace EventSorcery.Events.Measuring
{
    /// <summary>
    /// Published on application startup and contains the application arguments
    /// given on the command line.
    /// </summary>
    public interface IMeasurement
    {
        DateTime Timestamp { get; set; }
    }
}

using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Application
{
    /// <summary>
    /// Published on application startup and contains the application arguments
    /// given on the command line.
    /// </summary>
    public class ApplicationStartRequested : INotification
    {
    }
}

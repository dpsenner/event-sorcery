using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Application
{
    /// <summary>
    /// Published when every component has handled <see cref="ApplicationStartRequested"/>.
    /// </summary>
    public class ApplicationStartCompleted : INotification
    {
    }
}

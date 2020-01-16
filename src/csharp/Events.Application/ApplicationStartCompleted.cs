using MediatR;

namespace EventSorcery.Events.Application
{
    /// <summary>
    /// Published when every component has handled <see cref="ApplicationStartRequested"/>.
    /// </summary>
    public class ApplicationStartCompleted : INotification
    {
    }
}

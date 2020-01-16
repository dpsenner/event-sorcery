using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Application;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal abstract class ASensor : ISingletonComponent,
        INotificationHandler<ApplicationStartRequested>,
        INotificationHandler<ApplicationShutdownRequested>,
        INotificationHandler<ApplicationShutdownCompleted>
    {
        public Task Handle(ApplicationStartRequested notification, CancellationToken cancellationToken)
        {
            OnApplicationStartRequested();
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownRequested notification, CancellationToken cancellationToken)
        {
            OnApplicationShutdownRequested();
            return Task.CompletedTask;
        }

        public Task Handle(ApplicationShutdownCompleted notification, CancellationToken cancellationToken)
        {
            OnApplicationShutdownCompleted();
            return Task.CompletedTask;
        }

        protected virtual void OnApplicationStartRequested()
        {
        }

        protected virtual void OnApplicationShutdownRequested()
        {
        }

        protected virtual void OnApplicationShutdownCompleted()
        {
        }
    }
}

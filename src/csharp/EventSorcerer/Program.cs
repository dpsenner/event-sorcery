using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using EventSorcery.Events.Application;
using EventSorcery.Components.Historian;
using EventSorcery.Components.Measuring;
using EventSorcery.Infrastructure.Configuration;
using EventSorcery.Infrastructure.Mqtt;

namespace EventSorcery.EventSorcerer
{
    public class Program
    {
        public static int Main(string[] arguments)
        {
            // parse command line arguments
            var application = new CommandLineApplication();
            var helpOption = application.HelpOption("-? |-h |--help");
            var cleanSessionOption = application.Option("--clean-session", "Instructs the program to connect to the mqtt broker with a clean session.", CommandOptionType.NoValue);
            var configurationPathOption = application.Option("-c|--configuration-path", "The path where this application looks for configuration files.", CommandOptionType.SingleValue);
            application.OnExecute(() =>
            {
                var configurationPath = Directory.GetCurrentDirectory();
                if (helpOption.HasValue())
                {
                    application.ShowHelp();
                    return Task.FromResult(0);
                }

                if (configurationPathOption.HasValue())
                {
                    configurationPath = configurationPathOption.Value();
                }

                bool? cleanSession = null;
                if (cleanSessionOption.HasValue())
                {
                    cleanSession = true;
                }

                return ApplicationMain(configurationPath, cleanSession, arguments);
            });

            return application.Execute(arguments);
        }

        private static async Task<int> ApplicationMain(string configurationPath, bool? cleanSession, string[] arguments)
        {
            try
            {
                var serviceCollection = new ServiceCollection()
                    .AddSingleton<CustomMediator>()
                    .AddTransient<IMediator>(sp => sp.GetRequiredService<CustomMediator>())
                    .AddConfigurationPath(configurationPath)
                    .AddConfiguration(arguments)
                    .AddMqttInfrastructure(cleanSession)
                    .AddMeasuringComponents()
                    .AddNpgslComponents();
                using (var serviceProvider = serviceCollection.BuildServiceProvider())
                {
                    var mediator = serviceProvider.GetRequiredService<IMediator>();
                    await mediator.Publish(new ApplicationStartRequested());
                    await mediator.Publish(new ApplicationStartCompleted());

                    await CancelKeyPressed();

                    Console.WriteLine("Shutting down ..");
                    await mediator.Publish(new ApplicationShutdownRequested());
                    await mediator.Publish(new ApplicationShutdownCompleted());

                    // give the mediator a chance to process all remaining async events
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                    Console.WriteLine("Bye!");

                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("The application crashed, that's unfortunate. This may give a clue:");
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static Task CancelKeyPressed()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("SIGINT received ..");
                eventArgs.Cancel = true;
                taskCompletionSource.SetResult(true);
            };
            return taskCompletionSource.Task;
        }

        private class CustomMediator : Mediator
        {
            public CustomMediator(IServiceProvider serviceProvider) : base(serviceProvider) { }

            protected override Task PublishCore(IEnumerable<NotificationHandlerExecutor> allHandlers, INotification notification, CancellationToken cancellationToken)
            {
                return Task.WhenAll(allHandlers.Select(h => h.HandlerCallback(notification, cancellationToken)));
        }
    }
}
}
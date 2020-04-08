using MediatR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Events.Measuring;
using EventSorcery.Events.Measuring.Measurements;

namespace EventSorcery.Components.Measuring.Sensors
{
    internal class ApplicationSensor : ASensor
    {
        protected IMediator Mediator { get; }

        protected ApplicationSensorConfiguration Configuration { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        public ApplicationSensor(IMediator mediator, ApplicationSensorConfiguration configuration, IMeasurementTimingService measurementTimingService)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            MeasurementTimingService.Register(Configuration.Items.Where(t => t.Enable), OnIsDue);
        }

        private async Task OnIsDue(IEnumerable<ApplicationSensorConfiguration.PathConfiguration> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = item.Command,
                    Arguments = item.Arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                };
                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    await StartAndWaitForExitAsync(process, cancellationToken);
                }
            }
            
            MeasurementTimingService.ResetDue(items);
        }

        private Task StartAndWaitForExitAsync(Process process, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, AssemblyLoadEventArgs) => taskCompletionSource.TrySetResult(true);
            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(taskCompletionSource.SetCanceled);
            }

            process.Start();
            return taskCompletionSource.Task;
        }
    }
}

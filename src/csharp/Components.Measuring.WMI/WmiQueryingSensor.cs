using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventSorcery.Components.Measuring;
using EventSorcery.Events.Application;
using EventSorcery.Events.Mqtt;
using EventSorcery.Infrastructure.DependencyInjection;
using MediatR;
using ORMi;

namespace EventSorcery.Components.Measuring.Sensors.WMI
{
    internal class WmiQueryingSensor : ASensor
    {
        protected IMediator Mediator { get; }

        protected IMeasurementTimingService MeasurementTimingService { get; }

        protected WmiQueryingSensorConfiguration Configuration { get; }

        public WmiQueryingSensor(IMediator mediator, IMeasurementTimingService measurementTimingService, WmiQueryingSensorConfiguration configuration)
        {
            Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            MeasurementTimingService = measurementTimingService ?? throw new ArgumentNullException(nameof(measurementTimingService));
        }

        protected override void OnApplicationStartRequested()
        {
            base.OnApplicationStartRequested();

            if (Configuration.Enable)
            {
                MeasurementTimingService.Register(Configuration, OnIsDue);
            }
        }

        private async Task OnIsDue(WmiQueryingSensorConfiguration configuration, CancellationToken cancellationToken)
        {
            if (!configuration.Enable)
            {
                return;
            }

            MeasurementTimingService.ResetDue(configuration);

            foreach (var configurationItem in configuration.Items)
            {
                await ProcessItem(configurationItem, cancellationToken);
            }
        }

        private async Task ProcessItem(WmiQueryItemConfiguration item, CancellationToken cancellationToken)
        {
            var wmiHelper = new WMIHelper(item.Namespace, item.Hostname, item.Username, item.Password);
            var cimInstances = wmiHelper.Query(item.Query);

            var payload = new Payload()
            {
                Domain = item.Domain,
                Hostname = item.Hostname,
                Namespace = item.Namespace,
                Query = item.Query,
            };

            var properties = GetPropertiesFromQuery(item.Query);
            foreach (var cimInstance in cimInstances)
            {
                var instance = new Dictionary<string,object>();
                foreach (var property in cimInstance)
                {
                    var propertyName = property.Key;
                    if (IsMatchingProperty(properties, propertyName))
                    {
                        instance[propertyName] = property.Value;
                    }
                }

                payload.Instances.Add(instance);
            }

            await Mediator.Publish(new PublishAsJsonRequest()
            {
                Topic = item.Topic,
                Retain = false,
                Qos = QualityOfService.AtMostOnce,
                Payload = payload,
            }, cancellationToken);
        }

        private List<string> GetPropertiesFromQuery(string query)
        {
            var match = Regex.Match(query, "SELECT(.*)FROM", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new InvalidOperationException($"Could not parse properties from WMI query '{query}'");
            }

            var fields = match.Groups[1].Value;
            var fieldsSplit = fields.Split(",");
            return fieldsSplit.Select(field => field.Trim()).ToList();
        }

        private bool IsMatchingProperty(List<string> properties, string propertyName)
        {
            if (properties.Count == 1 && string.Equals("*", properties[0].Trim()))
            {
                return true;
            }

            if (properties.Any(t => t.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        protected class Payload
        {
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;

            public string Domain { get; set; }

            public string Hostname { get; set; }

            public string Namespace { get; set; }

            public string Query { get; set; }

            public List<Dictionary<string,object>> Instances { get; set; } = new List<Dictionary<string,object>>();
        }
    }
}

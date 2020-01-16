using EventSorcery.Events.Mqtt;
using MQTTnet.Protocol;
using System;

namespace EventSorcery.Infrastructure.Mqtt.Internals
{
    internal static class ConversionExtensions
    {
        public static QualityOfService ToQualityOfService(this MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    return QualityOfService.AtMostOnce;
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    return QualityOfService.AtLeastOnce;
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    return QualityOfService.ExactlyOnce;
                default:
                    throw new NotImplementedException();
            }
        }
        public static MqttQualityOfServiceLevel ToQualityOfService(this QualityOfService qualityOfServiceLevel)
        {
            switch (qualityOfServiceLevel)
            {
                case QualityOfService.AtMostOnce:
                    return MqttQualityOfServiceLevel.AtMostOnce;
                case QualityOfService.AtLeastOnce:
                    return MqttQualityOfServiceLevel.AtLeastOnce;
                case QualityOfService.ExactlyOnce:
                    return MqttQualityOfServiceLevel.ExactlyOnce;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

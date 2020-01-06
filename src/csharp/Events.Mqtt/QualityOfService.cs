using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Events.Mqtt
{
    public enum QualityOfService
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2,
    }
}

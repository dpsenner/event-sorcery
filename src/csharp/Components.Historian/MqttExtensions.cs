using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Historian
{
    internal static class MqttExtensions
    {
        public static bool IsTopicMatch(this string topicPattern, string topic)
        {
            var patternFragments = topicPattern.Split('/');
            var topicFragments = topic.Split('/');

            for (int i = 0; i < patternFragments.Length; i++)
            {
                string patternFragment = patternFragments[i];
                if (patternFragment == "+")
                {
                    // single level wildcard, accept
                }
                else if (patternFragment == "#")
                {
                    // multi level wildcard, accept
                    return true;
                }
                else
                {
                    // match both
                    if (i < topicFragments.Length)
                    {
                        var topicFragment = topicFragments[i];
                        if (!string.Equals(patternFragment, topicFragment))
                        {
                            // no match
                            return false;
                        }
                    }
                    else
                    {
                        // topic has not enough fragments
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

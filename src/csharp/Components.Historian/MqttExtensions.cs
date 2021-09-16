using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Historian
{
    internal static class MqttExtensions
    {
        public static bool IsTopicMatch(this string topicPattern, string topic)
        {
            var multiWildcard = "#";
            var wildcard = "+";
            var patternFragments = topicPattern.Split('/');
            var topicFragments = topic.Split('/');

            for (int i = 0; i < patternFragments.Length; i++)
            {
                string patternFragment = patternFragments[i];
                if (multiWildcard.Equals(patternFragment))
                {
                    // multi level wildcard, accept
                    return true;
                }
                else if (wildcard.Equals(patternFragment))
                {
                    // single level wildcard, accept
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

                    // if at the last element of pattern and still items left in topic
                    // this is no match; there are more fragments in the topic than in
                    // the pattern
                    if (i + 1 == patternFragments.Length)
                    {
                        if (topicFragments.Length > patternFragments.Length)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}

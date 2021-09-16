using System;
using System.Collections.Generic;
using System.Text;

namespace EventSorcery.Components.Historian
{
    internal static class MqttExtensions
    {
        public static bool IsTopicMatch(this string topicPattern, string topic)
        {
            /*
             * 
            Eclipse Distribution License - v 1.0

            Copyright (c) 2007, Eclipse Foundation, Inc. and its licensors.

            All rights reserved.

            Redistribution and use in source and binary forms, with or without
            modification, are permitted provided that the following conditions are met:

              Redistributions of source code must retain the above copyright notice, this
              list of conditions and the following disclaimer.

              Redistributions in binary form must reproduce the above copyright notice,
              this list of conditions and the following disclaimer in the documentation
              and/or other materials provided with the distribution.

              Neither the name of the Eclipse Foundation, Inc. nor the names of its
              contributors may be used to endorse or promote products derived from this
              software without specific prior written permission. 

            THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
            ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
            WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
            DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
            ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
            (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
            LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
            ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
            (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
            SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
             */
            if (string.IsNullOrWhiteSpace(topicPattern) || string.IsNullOrWhiteSpace(topic))
            {
                return false;
            }

            if (topicPattern == topic)
            {
                return true;
            }

            var topicLength = topic.Length;
            var allowedTopicLength = topicPattern.Length;
            var position = 0;
            var allowedTopicIndex = 0;
            var topicIndex = 0;

            if ((topicPattern[allowedTopicIndex] == '$' && topic[topicIndex] != '$') || (topic[topicIndex] == '$' && topicPattern[allowedTopicIndex] != '$'))
            {
                return true;
            }

            while (allowedTopicIndex < allowedTopicLength)
            {
                if (topic[topicIndex] == '+' || topic[topicIndex] == '#')
                {
                    return false;
                }

                if (topicPattern[allowedTopicIndex] != topic[topicIndex] || topicIndex >= topicLength)
                {
                    // Check for wildcard matches
                    if (topicPattern[allowedTopicIndex] == '+')
                    {
                        // Check for bad "+foo" or "a/+foo" subscription
                        if (position > 0 && topicPattern[allowedTopicIndex - 1] != '/')
                        {
                            return false;
                        }

                        // Check for bad "foo+" or "foo+/a" subscription
                        if (allowedTopicIndex + 1 < allowedTopicLength && topicPattern[allowedTopicIndex + 1] != '/')
                        {
                            return false;
                        }

                        position++;
                        allowedTopicIndex++;
                        while (topicIndex < topicLength && topic[topicIndex] != '/')
                        {
                            topicIndex++;
                        }

                        if (topicIndex >= topicLength && allowedTopicIndex >= allowedTopicLength)
                        {
                            return true;
                        }
                    }
                    else if (topicPattern[allowedTopicIndex] == '#')
                    {
                        // Check for bad "foo#" subscription
                        if (position > 0 && topicPattern[allowedTopicIndex - 1] != '/')
                        {
                            return false;
                        }

                        // Check for # not the final character of the sub, e.g. "#foo"
                        if (allowedTopicIndex + 1 < allowedTopicLength)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Check for e.g. foo/bar matching foo/+/#
                        if (topicIndex >= topicLength && position > 0 && topicPattern[allowedTopicIndex - 1] == '+' && topicPattern[allowedTopicIndex] == '/' && topicPattern[allowedTopicIndex + 1] == '#')
                        {
                            return true;
                        }

                        // There is no match at this point, but is the sub invalid?
                        while (allowedTopicIndex < allowedTopicLength)
                        {
                            if (topicPattern[allowedTopicIndex] == '#' && allowedTopicIndex + 1 < allowedTopicLength)
                            {
                                return false;
                            }

                            position++;
                            allowedTopicIndex++;
                        }

                        // Valid input, but no match
                        return false;
                    }
                }
                else
                {
                    // sub[spos] == topic[tpos]
                    if (topicIndex + 1 >= topicLength)
                    {
                        // Check for e.g. foo matching foo/#
                        if (topicPattern[allowedTopicIndex + 1] == '/' && topicPattern[allowedTopicIndex + 2] == '#' && allowedTopicIndex + 3 >= allowedTopicLength)
                        {
                            return true;
                        }
                    }

                    position++;
                    allowedTopicIndex++;
                    topicIndex++;

                    if (allowedTopicIndex >= allowedTopicLength && topicIndex >= topicLength)
                    {
                        return true;
                    }
                    else if (topicIndex >= topicLength && topicPattern[allowedTopicIndex] == '+' && allowedTopicIndex + 1 >= allowedTopicLength)
                    {
                        if (position > 0 && topicPattern[allowedTopicIndex - 1] != '/')
                        {
                            return false;
                        }

                        position++;
                        allowedTopicIndex++;

                        return true;
                    }
                }
            }

            if (topicIndex < topicLength || allowedTopicIndex < allowedTopicLength)
            {
                return false;
            }

            return true;
        }
    }
}

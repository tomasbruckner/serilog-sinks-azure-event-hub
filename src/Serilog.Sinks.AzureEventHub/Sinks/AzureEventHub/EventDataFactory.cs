// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;
using System.Text;
using Azure.Messaging.EventHubs;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Builds the <see cref="EventData"/> that the sinks publish to Event Hubs from a Serilog
    /// <see cref="LogEvent"/>. Shared by the batching and non-batching sinks so both produce
    /// identically shaped events.
    /// </summary>
    static class EventDataFactory
    {
        const string TypePropertyName = "Type";
        const string LevelPropertyName = "Level";

        public static EventData CreateEventData(LogEvent logEvent, ITextFormatter formatter, bool includeProperties)
        {
            byte[] body;
            using (var render = new StringWriter())
            {
                formatter.Format(logEvent, render);
                body = Encoding.UTF8.GetBytes(render.ToString());
            }

            var eventData = new EventData(body);
            eventData.Properties.Add(TypePropertyName, "SerilogEvent");
            eventData.Properties.Add(LevelPropertyName, logEvent.Level.ToString());

            if (includeProperties)
            {
                foreach (var property in logEvent.Properties)
                {
                    // Never let a log event property clobber the reserved Type/Level properties.
                    if (eventData.Properties.ContainsKey(property.Key))
                        continue;

                    eventData.Properties[property.Key] = RenderPropertyValue(property.Value);
                }
            }

            return eventData;
        }

        static object RenderPropertyValue(LogEventPropertyValue value)
        {
            // Pass scalars through as their underlying value so consumers keep the original type
            // where Event Hubs supports it; render structured/sequence/dictionary values to a string.
            if (value is ScalarValue scalar)
                return scalar.Value;

            using (var writer = new StringWriter())
            {
                value.Render(writer);
                return writer.ToString();
            }
        }
    }
}

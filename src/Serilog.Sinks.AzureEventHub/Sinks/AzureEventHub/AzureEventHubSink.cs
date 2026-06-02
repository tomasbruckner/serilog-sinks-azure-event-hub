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

using System;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Writes log events to an Azure Event Hub.
    /// </summary>
    public class AzureEventHubSink : ILogEventSink, IDisposable
    {
        readonly EventHubProducerClient _eventHubClient;
        readonly ITextFormatter _formatter;
        readonly bool _shouldIncludeProperties;
        readonly bool _ownsClient;

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
        /// <param name="shouldIncludeProperties">Whether the log event's properties are added to the EventData.</param>
        public AzureEventHubSink(
            EventHubProducerClient eventHubClient,
            ITextFormatter formatter,
            bool shouldIncludeProperties = false)
            : this(eventHubClient, formatter, shouldIncludeProperties, ownsClient: false)
        {
        }

        // ownsClient is true only when the library created the client (the connectionString
        // overloads); a caller-supplied client is left for the caller to dispose.
        internal AzureEventHubSink(
            EventHubProducerClient eventHubClient,
            ITextFormatter formatter,
            bool shouldIncludeProperties,
            bool ownsClient)
        {
            _eventHubClient = eventHubClient;
            _formatter = formatter;
            _shouldIncludeProperties = shouldIncludeProperties;
            _ownsClient = ownsClient;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            var eventHubData = EventDataFactory.CreateEventData(logEvent, _formatter, _shouldIncludeProperties);

            //Unfortunately no support for async in Serilog yet
            //https://github.com/serilog/serilog/issues/134
            _eventHubClient.SendAsync(new[] { eventHubData }, new SendEventOptions { PartitionKey = Guid.NewGuid().ToString() }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes the underlying Event Hub client, but only when this sink created it
        /// (i.e. it was configured from a connection string rather than a caller-supplied client).
        /// </summary>
        public void Dispose()
        {
            if (_ownsClient)
                _eventHubClient.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
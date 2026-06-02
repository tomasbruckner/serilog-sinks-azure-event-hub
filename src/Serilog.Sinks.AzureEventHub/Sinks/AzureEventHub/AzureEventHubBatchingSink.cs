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
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Writes log events to an Azure Event Hub in batches. Batching is driven by
    /// Serilog's built-in periodic batching infrastructure via <see cref="IBatchedLogEventSink"/>.
    /// </summary>
    public class AzureEventHubBatchingSink : IBatchedLogEventSink, IDisposable
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
        public AzureEventHubBatchingSink(
            EventHubProducerClient eventHubClient,
            ITextFormatter formatter,
            bool shouldIncludeProperties = false)
            : this(eventHubClient, formatter, shouldIncludeProperties, ownsClient: false)
        {
        }

        // ownsClient is true only when the library created the client (the connectionString
        // overloads); a caller-supplied client is left for the caller to dispose.
        internal AzureEventHubBatchingSink(
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
        /// Emit a batch of log events to the Event Hub.
        /// </summary>
        /// <param name="batch">The events to emit.</param>
        /// <remarks>
        /// All events in the batch share a single random partition key so they are kept
        /// together on the same Event Hub partition. Events are accumulated into
        /// size-constrained <see cref="EventDataBatch"/> instances; if the events do not
        /// fit into a single request they are split across several sends. When a batch is
        /// split, delivery is at-least-once: if a later send fails after an earlier one
        /// succeeded, the batching infrastructure retries the whole batch, so the already
        /// delivered events may be sent again. Consumers that require exactly-once should
        /// de-duplicate downstream.
        /// </remarks>
        public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
        {
            var batchOptions = new CreateBatchOptions { PartitionKey = Guid.NewGuid().ToString() };
            var eventBatch = await _eventHubClient.CreateBatchAsync(batchOptions).ConfigureAwait(false);

            try
            {
                foreach (var logEvent in batch)
                {
                    var eventData = EventDataFactory.CreateEventData(logEvent, _formatter, _shouldIncludeProperties);

                    if (eventBatch.TryAdd(eventData))
                        continue;

                    // The current batch is full. Flush it and start a fresh one (same
                    // partition key) before retrying the event that didn't fit.
                    if (eventBatch.Count > 0)
                    {
                        await _eventHubClient.SendAsync(eventBatch).ConfigureAwait(false);
                        eventBatch.Dispose();
                        eventBatch = await _eventHubClient.CreateBatchAsync(batchOptions).ConfigureAwait(false);
                    }

                    if (!eventBatch.TryAdd(eventData))
                    {
                        // A single event too large for an empty batch is dropped rather than
                        // stalling the whole batch; report it through Serilog's self-log.
                        SelfLog.WriteLine(
                            "Azure Event Hub sink discarded a '{0}' log event at {1:o} that exceeds the maximum batch size.",
                            logEvent.Level,
                            logEvent.Timestamp);
                    }
                }

                if (eventBatch.Count > 0)
                    await _eventHubClient.SendAsync(eventBatch).ConfigureAwait(false);
            }
            finally
            {
                eventBatch.Dispose();
            }
        }

        /// <summary>
        /// Allows the sink to perform periodic work when no events are buffered. No-op for this sink.
        /// </summary>
        public Task OnEmptyBatchAsync() => Task.CompletedTask;

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

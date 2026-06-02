using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Moq;
using Serilog.Events;
using Xunit;

namespace Serilog.Sinks.AzureEventHub.Tests
{
    public class AzureEventHubBatchingSinkTests
    {
        [Fact]
        public async Task EmitBatchAsync_SendsAllEventsInOneBatchWithASharedPartitionKey()
        {
            var added = new List<EventData>();
            var partitionKeys = new List<string>();
            var sendCount = 0;

            var client = CreateClient(
                // Accept every event into the batch (effectively unlimited size).
                tryAdd: (store, ev) => { added.Add(ev); return true; },
                partitionKeys: partitionKeys,
                onSend: () => sendCount++);

            var sink = new AzureEventHubBatchingSink(client.Object, TestLogEvents.Formatter);

            await sink.EmitBatchAsync(new[]
            {
                TestLogEvents.Create(LogEventLevel.Information, "first"),
                TestLogEvents.Create(LogEventLevel.Warning, "second")
            });

            Assert.Equal(1, sendCount);
            Assert.Equal(2, added.Count);
            Assert.Contains("first", added[0].EventBody.ToString());
            Assert.Equal("SerilogEvent", added[1].Properties["Type"]);
            Assert.Equal("Warning", added[1].Properties["Level"]);

            var key = Assert.Single(partitionKeys.Distinct());
            Assert.True(Guid.TryParse(key, out _));
        }

        [Fact]
        public async Task EmitBatchAsync_SplitsAcrossMultipleSendsWhenABatchFillsUp()
        {
            const int capacityPerBatch = 2;

            var added = new List<EventData>();
            var partitionKeys = new List<string>();
            var sendCount = 0;

            var client = CreateClient(
                // Each individual batch only accepts `capacityPerBatch` events.
                tryAdd: (store, ev) =>
                {
                    if (store.Count >= capacityPerBatch)
                        return false;
                    added.Add(ev);
                    return true;
                },
                partitionKeys: partitionKeys,
                onSend: () => sendCount++);

            var sink = new AzureEventHubBatchingSink(client.Object, TestLogEvents.Formatter);

            await sink.EmitBatchAsync(new[]
            {
                TestLogEvents.Create(LogEventLevel.Information, "a"),
                TestLogEvents.Create(LogEventLevel.Information, "b"),
                TestLogEvents.Create(LogEventLevel.Information, "c")
            });

            Assert.Equal(3, added.Count);                 // every event delivered
            Assert.Equal(2, sendCount);                   // 2 + 1 across two sends
            Assert.All(partitionKeys, k => Assert.Equal(partitionKeys[0], k)); // same key for the whole batch
        }

        /// <summary>
        /// Builds a mocked <see cref="EventHubProducerClient"/> whose CreateBatchAsync returns a
        /// fresh model-factory batch (with its own backing store) on every call, and whose
        /// SendAsync is observed via <paramref name="onSend"/>.
        /// </summary>
        static Mock<EventHubProducerClient> CreateClient(
            Func<List<EventData>, EventData, bool> tryAdd,
            List<string> partitionKeys,
            Action onSend)
        {
            var client = new Mock<EventHubProducerClient>();

            client
                .Setup(c => c.CreateBatchAsync(
                    It.IsAny<CreateBatchOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CreateBatchOptions options, CancellationToken _) =>
                {
                    partitionKeys.Add(options.PartitionKey);
                    var store = new List<EventData>();
                    var batch = EventHubsModelFactory.EventDataBatch(
                        batchSizeBytes: long.MaxValue,
                        batchEventStore: store,
                        batchOptions: options,
                        tryAddCallback: ev => tryAdd(store, ev));
                    return new ValueTask<EventDataBatch>(batch);
                });

            client
                .Setup(c => c.SendAsync(
                    It.IsAny<EventDataBatch>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback(() => onSend());

            return client;
        }
    }
}

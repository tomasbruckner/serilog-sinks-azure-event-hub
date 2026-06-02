using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Serilog;
using Xunit;

namespace Serilog.Sinks.AzureEventHub.IntegrationTests
{
    /// <summary>
    /// End-to-end tests that log through the sink into a real (emulated) Event Hub and read the
    /// event back, verifying the body and the <c>Type</c>/<c>Level</c> properties survive the round trip.
    /// </summary>
    public class EventHubRoundTripTests : IClassFixture<EventHubsEmulatorFixture>
    {
        readonly EventHubsEmulatorFixture _emulator;

        public EventHubRoundTripTests(EventHubsEmulatorFixture emulator) => _emulator = emulator;

        [Fact]
        public async Task NonBatchingSink_DeliversEventToEventHub()
        {
            var marker = Guid.NewGuid().ToString("N");

            using (var log = new LoggerConfiguration()
                       .WriteTo.AzureEventHub(_emulator.ConnectionString, EventHubsEmulatorFixture.EventHubName,
                           outputTemplate: "{Message}")
                       .CreateLogger())
            {
                log.Information("non-batched {Marker}", marker);
            }

            var received = await ReadEventContainingAsync(marker, TimeSpan.FromSeconds(60));

            Assert.Equal("SerilogEvent", received.Properties["Type"]);
            Assert.Equal("Information", received.Properties["Level"]);
        }

        [Fact]
        public async Task BatchingSink_DeliversEventToEventHub()
        {
            var marker = Guid.NewGuid().ToString("N");

            using (var log = new LoggerConfiguration()
                       .WriteTo.AzureEventHub(_emulator.ConnectionString, EventHubsEmulatorFixture.EventHubName,
                           outputTemplate: "{Message}",
                           writeInBatches: true,
                           period: TimeSpan.FromMilliseconds(200))
                       .CreateLogger())
            {
                log.Warning("batched {Marker}", marker);
            } // disposing the logger flushes the pending batch

            var received = await ReadEventContainingAsync(marker, TimeSpan.FromSeconds(60));

            Assert.Equal("SerilogEvent", received.Properties["Type"]);
            Assert.Equal("Warning", received.Properties["Level"]);
        }

        /// <summary>
        /// Reads the hub from the earliest offset until an event whose body contains
        /// <paramref name="marker"/> is found, so concurrent/leftover events don't cause false matches.
        /// </summary>
        async Task<EventData> ReadEventContainingAsync(string marker, TimeSpan timeout)
        {
            await using var consumer = new EventHubConsumerClient(
                EventHubConsumerClient.DefaultConsumerGroupName,
                _emulator.ConnectionString,
                EventHubsEmulatorFixture.EventHubName);

            using var cts = new CancellationTokenSource(timeout);

            try
            {
                await foreach (var partitionEvent in consumer.ReadEventsAsync(startReadingAtEarliestEvent: true, cancellationToken: cts.Token))
                {
                    var data = partitionEvent.Data;
                    if (data != null && data.EventBody.ToString().Contains(marker))
                        return data;
                }
            }
            catch (OperationCanceledException)
            {
            }

            throw new TimeoutException($"No event containing marker '{marker}' arrived within {timeout.TotalSeconds:N0}s.");
        }
    }
}

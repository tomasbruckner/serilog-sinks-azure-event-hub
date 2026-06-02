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
    public class AzureEventHubSinkTests
    {
        [Fact]
        public void Emit_SendsOneEventWithFormattedBodyAndProperties()
        {
            List<EventData> captured = null;
            SendEventOptions capturedOptions = null;

            var client = new Mock<EventHubProducerClient>();
            client
                .Setup(c => c.SendAsync(
                    It.IsAny<IEnumerable<EventData>>(),
                    It.IsAny<SendEventOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<IEnumerable<EventData>, SendEventOptions, CancellationToken>(
                    (events, options, _) =>
                    {
                        captured = events.ToList();
                        capturedOptions = options;
                    });

            var sink = new AzureEventHubSink(client.Object, TestLogEvents.Formatter);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "hello world"));

            var eventData = Assert.Single(captured);
            Assert.Contains("hello world", eventData.EventBody.ToString());
            Assert.Equal("SerilogEvent", eventData.Properties["Type"]);
            Assert.Equal("Information", eventData.Properties["Level"]);
        }

        [Fact]
        public void Emit_UsesARandomGuidPartitionKeyPerEvent()
        {
            var partitionKeys = new List<string>();

            var client = new Mock<EventHubProducerClient>();
            client
                .Setup(c => c.SendAsync(
                    It.IsAny<IEnumerable<EventData>>(),
                    It.IsAny<SendEventOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<IEnumerable<EventData>, SendEventOptions, CancellationToken>(
                    (_, options, _) => partitionKeys.Add(options.PartitionKey));

            var sink = new AzureEventHubSink(client.Object, TestLogEvents.Formatter);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "one"));
            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "two"));

            Assert.All(partitionKeys, key => Assert.True(Guid.TryParse(key, out _)));
            Assert.Equal(2, partitionKeys.Distinct().Count());
        }
    }
}

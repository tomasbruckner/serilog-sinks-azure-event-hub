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

        [Fact]
        public void Emit_WithShouldIncludeProperties_IncludesLogEventPropertiesOnEventData()
        {
            var client = CaptureClient(out var captured);
            var sink = new AzureEventHubSink(client, TestLogEvents.Formatter, shouldIncludeProperties: true);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "hi", TestLogEvents.Property("UserId", 42)));

            var eventData = Assert.Single(captured);
            Assert.Equal(42, eventData.Properties["UserId"]);
            // reserved properties are still present
            Assert.Equal("SerilogEvent", eventData.Properties["Type"]);
            Assert.Equal("Information", eventData.Properties["Level"]);
        }

        [Fact]
        public void Emit_WithShouldIncludeProperties_RendersUnsupportedScalarTypeAsString()
        {
            var client = CaptureClient(out var captured);
            var sink = new AzureEventHubSink(client, TestLogEvents.Formatter, shouldIncludeProperties: true);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "hi", TestLogEvents.Property("Status", Status.Active)));

            var eventData = Assert.Single(captured);
            // Enums (and other types Event Hubs can't serialize as AMQP properties) are rendered
            // to a string so SendAsync never throws a SerializationException.
            Assert.Equal("Active", eventData.Properties["Status"]);
            Assert.IsType<string>(eventData.Properties["Status"]);
        }

        [Fact]
        public void Emit_WithShouldIncludeProperties_PassesSupportedScalarTypesThrough()
        {
            var client = CaptureClient(out var captured);
            var sink = new AzureEventHubSink(client, TestLogEvents.Formatter, shouldIncludeProperties: true);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "hi", TestLogEvents.Property("UserId", 42)));

            var eventData = Assert.Single(captured);
            // int is natively supported, so it must keep its CLR type rather than become a string.
            Assert.Equal(42, eventData.Properties["UserId"]);
            Assert.IsType<int>(eventData.Properties["UserId"]);
        }

        enum Status
        {
            Active
        }

        [Fact]
        public void Emit_ByDefault_DoesNotIncludeLogEventProperties()
        {
            var client = CaptureClient(out var captured);
            var sink = new AzureEventHubSink(client, TestLogEvents.Formatter);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Information, "hi", TestLogEvents.Property("UserId", 42)));

            var eventData = Assert.Single(captured);
            Assert.False(eventData.Properties.ContainsKey("UserId"));
        }

        [Fact]
        public void Emit_WithShouldIncludeProperties_DoesNotOverwriteReservedProperties()
        {
            var client = CaptureClient(out var captured);
            var sink = new AzureEventHubSink(client, TestLogEvents.Formatter, shouldIncludeProperties: true);

            sink.Emit(TestLogEvents.Create(LogEventLevel.Warning, "hi", TestLogEvents.Property("Level", "bogus")));

            var eventData = Assert.Single(captured);
            // the sink's own Level must win over a same-named log event property
            Assert.Equal("Warning", eventData.Properties["Level"]);
        }

        // A mock client whose SendAsync records the events it is given into `captured`.
        static EventHubProducerClient CaptureClient(out List<EventData> captured)
        {
            var sent = new List<EventData>();
            captured = sent;

            var client = new Mock<EventHubProducerClient>();
            client
                .Setup(c => c.SendAsync(
                    It.IsAny<IEnumerable<EventData>>(),
                    It.IsAny<SendEventOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<IEnumerable<EventData>, SendEventOptions, CancellationToken>(
                    (events, _, _) => sent.AddRange(events));

            return client.Object;
        }
    }
}

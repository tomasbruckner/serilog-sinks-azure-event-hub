using System;
using Azure.Messaging.EventHubs.Producer;
using Moq;
using Serilog.Formatting;
using Xunit;

namespace Serilog.Sinks.AzureEventHub.Tests
{
    public class LoggerConfigurationAzureEventHubExtensionsTests
    {
        [Fact]
        public void AzureEventHub_WithNullFormatter_ThrowsArgumentNullException()
        {
            var client = new Mock<EventHubProducerClient>().Object;
            var configuration = new LoggerConfiguration();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                configuration.WriteTo.AzureEventHub((ITextFormatter)null, client));

            Assert.Equal("formatter", ex.ParamName);
        }

        [Fact]
        public void AuditAzureEventHub_WithNullFormatter_ThrowsArgumentNullException()
        {
            var client = new Mock<EventHubProducerClient>().Object;
            var configuration = new LoggerConfiguration();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                configuration.AuditTo.AzureEventHub((ITextFormatter)null, client));

            Assert.Equal("formatter", ex.ParamName);
        }

        [Fact]
        public void AzureEventHub_WithNullConnectionString_ThrowsArgumentNullException()
        {
            var configuration = new LoggerConfiguration();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                configuration.WriteTo.AzureEventHub((string)null, "hub"));

            Assert.Equal("connectionString", ex.ParamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void AzureEventHub_WithEmptyConnectionString_ThrowsArgumentException(string connectionString)
        {
            var configuration = new LoggerConfiguration();

            // An empty/whitespace (non-null) value is an ArgumentException, not ArgumentNullException.
            var ex = Assert.Throws<ArgumentException>(() =>
                configuration.WriteTo.AzureEventHub(connectionString, "hub"));

            Assert.Equal("connectionString", ex.ParamName);
        }

        [Fact]
        public void AzureEventHub_WithEmptyEventHubName_ThrowsArgumentException()
        {
            var configuration = new LoggerConfiguration();

            var ex = Assert.Throws<ArgumentException>(() =>
                configuration.WriteTo.AzureEventHub("Endpoint=sb://test/;SharedAccessKeyName=k;SharedAccessKey=v", " "));

            Assert.Equal("eventHubName", ex.ParamName);
        }
    }
}

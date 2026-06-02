using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Testcontainers.EventHubs;
using Xunit;

namespace Serilog.Sinks.AzureEventHub.IntegrationTests
{
    /// <summary>
    /// Starts the Azure Event Hubs emulator (plus its Azurite dependency) once and shares the
    /// resulting connection string across every test in the class. Requires a running Docker engine.
    /// </summary>
    public sealed class EventHubsEmulatorFixture : IAsyncLifetime
    {
        public const string EventHubName = "logs";

        // Testcontainers 4.12 marks the parameterless EventHubsBuilder() obsolete in favour of
        // passing an explicit image. In practice the image-parameter overload does not apply the
        // module's emulator readiness wait strategy, so the container never reports ready and start
        // times out. Keep the parameterless constructor (which waits correctly) until the module
        // exposes a supported way to override the image.
#pragma warning disable CS0618 // Type or member is obsolete
        readonly EventHubsContainer _container = new EventHubsBuilder()
#pragma warning restore CS0618
            .WithAcceptLicenseAgreement(true)
            .WithConfigurationBuilder(EventHubsServiceConfiguration.Create()
                .WithEntity(EventHubName, 1, EventHubConsumerClient.DefaultConsumerGroupName))
            .Build();

        public string ConnectionString { get; private set; }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
        }

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();
    }
}

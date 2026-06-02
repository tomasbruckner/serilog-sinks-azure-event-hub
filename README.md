# Serilog.Sinks.AzureEventHub

[![CI](https://github.com/tomasbruckner/serilog-sinks-azure-event-hub/actions/workflows/ci.yml/badge.svg)](https://github.com/tomasbruckner/serilog-sinks-azure-event-hub/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/TomasBruckner.Serilog.Sinks.AzureEventHub.svg)](https://www.nuget.org/packages/TomasBruckner.Serilog.Sinks.AzureEventHub/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A [Serilog](https://serilog.net) sink that writes log events to [Azure Event Hubs](https://learn.microsoft.com/azure/event-hubs/).

Each log event is rendered to text (using a Serilog output template or a custom formatter), sent as the body of an Event Hub `EventData`, and tagged with two application properties:

| Property | Value |
| --- | --- |
| `Type` | `SerilogEvent` |
| `Level` | the event's log level (e.g. `Information`, `Error`) |

Enable `shouldIncludeProperties` to also emit the log event's own structured properties (see [Including log event properties](#including-log-event-properties)).

## Install

```shell
dotnet add package TomasBruckner.Serilog.Sinks.AzureEventHub
```

Built against **Serilog 4** and the modern **`Azure.Messaging.EventHubs`** SDK. The package targets `netstandard2.0`, so it can be consumed from modern .NET (8/10) as well as .NET Framework 4.6.1+/4.8.

## Getting started

The simplest configuration takes an Event Hub connection string and the hub name:

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureEventHub(
        connectionString: "Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...",
        eventHubName: "logs")
    .CreateLogger();

Log.Information("Hello from {Machine}", Environment.MachineName);

Log.CloseAndFlush();
```

### Using an existing `EventHubProducerClient`

Pass your own client when you need full control over how it is built — for example to authenticate with Microsoft Entra ID instead of a connection string:

```csharp
using Azure.Identity;
using Azure.Messaging.EventHubs.Producer;
using Serilog;

var client = new EventHubProducerClient(
    fullyQualifiedNamespace: "my-namespace.servicebus.windows.net",
    eventHubName: "logs",
    credential: new DefaultAzureCredential());

Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureEventHub(client)
    .CreateLogger();
```

> `DefaultAzureCredential` comes from the [`Azure.Identity`](https://www.nuget.org/packages/Azure.Identity) package.

## Batching

By default the sink sends each event individually and synchronously. For higher throughput, enable batching with `writeInBatches: true`; events are then buffered and flushed periodically:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureEventHub(
        connectionString: connectionString,
        eventHubName: "logs",
        writeInBatches: true,
        period: TimeSpan.FromSeconds(2),   // how often to flush
        batchPostingLimit: 50)             // max events per flush
    .CreateLogger();
```

**Partition keys:** the batching sink assigns one random partition key per *batch*, while the default (non-batching) sink assigns one per *event*. Batching therefore changes how events are distributed across partitions, which is why it is opt-in.

## Audit logging

Use `AuditTo` instead of `WriteTo` when delivery failures must propagate to the caller (audit sinks are always synchronous and never batch):

```csharp
Log.Logger = new LoggerConfiguration()
    .AuditTo.AzureEventHub(connectionString, eventHubName: "audit")
    .CreateLogger();
```

## Including log event properties

By default only `Type` and `Level` are attached. Set `shouldIncludeProperties: true` to also add each of the log event's structured properties as an Event Hub application property:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureEventHub(connectionString, "logs", shouldIncludeProperties: true)
    .CreateLogger();

Log.Information("Order {OrderId} shipped to {Region}", 1234, "EU");
// EventData.Properties -> Type=SerilogEvent, Level=Information, OrderId=1234, Region=EU
```

Scalar values are passed through with their original type where Event Hubs supports it; structured/collection values are rendered to a string. The reserved `Type` and `Level` properties are never overwritten by a same-named log event property. The option is available on both `WriteTo` and `AuditTo`.

## Custom formatting

Provide an output template, or supply any Serilog `ITextFormatter` — for example to emit JSON with [`Serilog.Formatting.Compact`](https://www.nuget.org/packages/Serilog.Formatting.Compact):

```csharp
using Serilog.Formatting.Compact;

// Output template
Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureEventHub(connectionString, "logs",
        outputTemplate: "{Timestamp:o} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Custom formatter (JSON)
Log.Logger = new LoggerConfiguration()
    .WriteTo.AzureEventHub(new CompactJsonFormatter(), connectionString, "logs")
    .CreateLogger();
```

## Configuration reference

`WriteTo.AzureEventHub(...)` accepts:

| Parameter | Default | Description |
| --- | --- | --- |
| `connectionString` / `eventHubName` | — | Connect by connection string + hub name… |
| `eventHubClient` | — | …or pass a pre-built `EventHubProducerClient` instead. |
| `outputTemplate` | `"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}"` | Template used to render each event. |
| `formatter` | — | A custom `ITextFormatter` (alternative to `outputTemplate`). |
| `formatProvider` | `null` | Culture used when rendering. |
| `restrictedToMinimumLevel` | `Verbose` | Minimum level handled by the sink. |
| `writeInBatches` | `false` | Buffer and flush events periodically. |
| `period` | `2s` | Flush interval (batching only). |
| `batchPostingLimit` | `50` | Max events per flush (batching only). |
| `shouldIncludeProperties` | `false` | Add the log event's properties as Event Hub event data properties. |

`AuditTo.AzureEventHub(...)` accepts the same parameters except the batching ones (`writeInBatches`, `period`, `batchPostingLimit`).

## Building from source

Requires the **.NET SDK 10** (the solution uses the `.slnx` format and the test projects target `net10.0`). The library itself targets `netstandard2.0`.

```shell
dotnet build -c Release
dotnet test  -c Release
```

The integration tests spin up the [Azure Event Hubs emulator](https://learn.microsoft.com/azure/event-hubs/test-locally-with-event-hub-emulator) via [Testcontainers](https://dotnet.testcontainers.org/), so a **running Docker engine** is required. To run only the fast unit tests (no Docker):

```shell
dotnet test ./test/Serilog.Sinks.AzureEventHub.Tests/Serilog.Sinks.AzureEventHub.Tests.csproj
```

## License

Licensed under the [MIT License](LICENSE).

This is a community fork of [`serilog-contrib/serilog-sinks-azureeventhub`](https://github.com/serilog-contrib/serilog-sinks-azureeventhub),
which is licensed under Apache-2.0. Upstream-derived portions remain under
Apache-2.0; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for attribution
and a full copy of that license. The assembly and `Serilog` namespace remain
`Serilog.Sinks.AzureEventHub`; only the NuGet package id differs.

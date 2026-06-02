# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A [Serilog](https://serilog.net) sink that writes log events to Azure Event Hubs. The library lives in `src/Serilog.Sinks.AzureEventHub` and is published as the `Serilog.Sinks.AzureEventHub` NuGet package. Tests live in `test/`: `Serilog.Sinks.AzureEventHub.Tests` (fast unit tests) and `Serilog.Sinks.AzureEventHub.IntegrationTests` (round-trip against the Event Hubs emulator).

## Build & test

```powershell
dotnet build -c Release                  # builds the .slnx solution
dotnet test  -c Release                  # runs unit + integration tests
dotnet test .\test\Serilog.Sinks.AzureEventHub.Tests\Serilog.Sinks.AzureEventHub.Tests.csproj   # unit only (no Docker)
# single test: dotnet test --filter "FullyQualifiedName~AzureEventHubBatchingSinkTests"
```

- **Toolchain floor: .NET SDK 10** (and VS 2022 17.13+ / Rider 2024.3+). The solution is the XML `.slnx` format, which `dotnet` can only build with SDK 9.0.200+; tests target `net10.0`. The library itself is unaffected (see below).
- **Integration tests require a running Docker engine** — they start the Azure Event Hubs emulator (plus Azurite) via Testcontainers. Without Docker, run the unit-test project only.
- `TreatWarningsAsErrors` is `true` **on the library** (not the test projects), so any compiler warning there (including missing XML doc comments — `GenerateDocumentationFile` is on) fails the build. Keep public members documented.
- The library is strong-named with `assets/Serilog.snk` (`SignAssembly`). Don't remove signing. Test projects are unsigned and reference the library via `ProjectReference`, testing only its public API.
- Single library target: `netstandard2.0` (consumable from modern .NET *and* .NET Framework 4.6.1+/4.8) — this is the consumer-facing floor and is independent of the SDK 10 *build* floor above. Default C# language version for this TFM is 7.3 — avoid C# 8+ syntax (`using` declarations, switch expressions) in the library unless you set `<LangVersion>`. Test projects set `<LangVersion>latest</LangVersion>`.
- Unit tests mock `EventHubProducerClient` with Moq; the batching tests build a real `EventDataBatch` via `EventHubsModelFactory.EventDataBatch(...)` so the `CreateBatchAsync`/`TryAdd`/overflow path is exercised without Docker. Integration tests log through the sink into the emulator and read the event back via `EventHubConsumerClient`.

## Architecture

The public surface is two static extension-method classes in the `Serilog` namespace (note `RootNamespace` is `Serilog`, not the folder name), each overloaded to accept either a pre-built `EventHubProducerClient` or a `connectionString` + `eventHubName` pair, and either an output template string or a custom `ITextFormatter`:

- **`LoggerConfigurationAzureEventHubExtensions`** → `WriteTo.AzureEventHub(...)` for normal logging.
- **`AuditLoggerConfigurationAzureEventHubExtensions`** → `AuditTo.AzureEventHub(...)` for audit logging.

These pick one of two sink implementations under `Sinks/AzureEventHub/`:

- **`AzureEventHubSink`** (`ILogEventSink`) — the default. Sends each event individually and **blocks** on the async send via `.GetAwaiter().GetResult()`, because Serilog's `Emit` has no async contract (see the comment referencing serilog/serilog#134). Audit logging *always* uses this sink (synchronous = failures propagate to the caller).
- **`AzureEventHubBatchingSink`** (`Serilog.Core.IBatchedLogEventSink`) — used only when `writeInBatches: true` on the `WriteTo` path. The extension wraps it with Serilog 4's built-in batching via `loggerConfiguration.Sink(batchedSink, batchingOptions, …)`; `BatchingOptions.BatchSizeLimit`/`BufferingTimeLimit` come from the `batchPostingLimit`/`period` parameters. Events are packed into a size-constrained `EventDataBatch` (`CreateBatchAsync` → `TryAdd` → `SendAsync`); oversized batches are split across multiple sends.

Partition key behavior differs between the two and is intentional: the batching sink assigns one random `Guid` partition key per *batch*; the non-batching sink assigns one per *event*. This is why `writeInBatches` is opt-in rather than the default — it changes event distribution across partitions.

Both sinks do the same per-event transform: render the `LogEvent` through the `ITextFormatter` to a string, UTF-8 encode it as the `EventData` body, and attach two properties — `Type = "SerilogEvent"` and `Level = <level>`.

## Dependencies / SDK note

Built on the modern **`Azure.Messaging.EventHubs`** SDK (`EventHubProducerClient`) and **Serilog 4.x**. Two prior migrations to keep in mind when reading old code or PRs: the messaging layer moved off the legacy `Microsoft.Azure.EventHubs` SDK, and batching moved off the separate `Serilog.Sinks.PeriodicBatching` package onto Serilog 4 core's `IBatchedLogEventSink`. Ignore both older APIs when making changes.

## Versioning / release

- Package version comes from `<VersionPrefix>` in the library `.csproj`. (The standalone `.nuspec` is legacy — `dotnet pack` builds from the csproj metadata, not that file — but keep its dependency list in sync to avoid confusion.)
- CI is **GitHub Actions** (`.github/workflows/ci.yml`, .NET SDK `10.0.x`): builds and runs tests on every push/PR to `main`/`dev` (the runner's Docker engine backs the integration tests). **Publishing happens only on push to `main`** — it packs, pushes to NuGet, and cuts a GitHub release. Requires the `NUGET_API_KEY` repo secret. The default working branch is `dev`.
- `Build.ps1` is the old AppVeyor pack script, left in place for local packaging; it is no longer wired to CI.

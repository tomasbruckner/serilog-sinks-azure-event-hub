# Changelog

All notable changes in this fork are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project aims to follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This fork diverged from
[`serilog-contrib/serilog-sinks-azureeventhub`](https://github.com/serilog-contrib/serilog-sinks-azureeventhub)
at version 6.0.x. Everything below is new since the fork.

## [7.0.0] - Unreleased

### Changed
- **Serilog 2.5 → 4.x.** Batching now uses Serilog 4 core's `IBatchedLogEventSink`; the
  `AzureEventHubBatchingSink` is wired up through `LoggerSinkConfiguration.Sink(batchedSink, BatchingOptions, …)`
  instead of inheriting from the deprecated `PeriodicBatchingSink` base class.
- **`Azure.Messaging.EventHubs` 5.4.1 → 5.12.2.** Batched events are now published via a size-constrained
  `EventDataBatch` (`CreateBatchAsync` → `TryAdd` → `SendAsync`); batches that exceed the maximum request
  size are automatically split across multiple sends instead of failing.
- **Target framework narrowed to `netstandard2.0` only.** Still consumable from modern .NET (8/10) and
  .NET Framework 4.6.1+/4.8.
- Package version bumped to **7.0.0** (requires Serilog 4 — a breaking change for consumers).
- **Relicensed this fork to the MIT License.** Upstream-derived code remains under Apache-2.0;
  attribution and a full copy of that license are retained in `THIRD-PARTY-NOTICES.md`.
- **Renamed the NuGet package to `TomasBruckner.Serilog.Sinks.AzureEventHub`.** The assembly and the
  `Serilog` namespace are unchanged, so existing code keeps compiling — only the package id differs.

### Removed
- `net461` target framework (out of support).
- `Serilog.Sinks.PeriodicBatching` dependency (batching is part of Serilog 4 core).
- AppVeyor configuration (`appveyor.yml`) and the legacy `CHANGES.md`.

### Added
- **`shouldIncludeProperties` option** on `WriteTo`/`AuditTo` (default `false`): emit the log event's
  structured properties as Event Hub event data properties (scalars keep their type, other values are
  rendered to a string; the reserved `Type`/`Level` are never overwritten). Addresses the long-standing
  upstream requests in issues #9 and #31 and PRs #18/#28/#32.
- **GitHub Actions CI** (`.github/workflows/ci.yml`, .NET SDK 10): builds and tests every push/PR to
  `main`/`dev`; packs, pushes to NuGet, and cuts a GitHub release on pushes to `main`.
- **Unit tests** (xUnit + Moq) covering both sinks, including event formatting, the `Type`/`Level`
  properties, partition-key behaviour, and batch-overflow splitting.
- **Integration tests** (Testcontainers) that round-trip a log event through the Azure Event Hubs
  emulator and read it back.
- Migrated the solution to the **`.slnx`** format; test projects target `net10.0`.
- Project documentation: `README.md` and `CLAUDE.md`.
- **Source Link + `snupkg` symbol package**, and the `README.md` is now packed into the NuGet package.
- **Central Package Management** (`Directory.Packages.props`) and a `global.json` pinning the .NET 10 SDK
  floor; shared test settings moved to `test/Directory.Build.props`.
- **Community health files**: `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`, `CODEOWNERS`,
  issue/PR templates, plus `.editorconfig`, `.gitattributes`, and Dependabot configuration.

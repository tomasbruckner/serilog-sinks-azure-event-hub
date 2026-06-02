# Contributing

Thanks for your interest in contributing! This is a community fork of the
Serilog Azure Event Hubs sink, published as
`TomasBruckner.Serilog.Sinks.AzureEventHub`.

## Prerequisites

- **.NET SDK 10** or newer. The solution uses the `.slnx` format, which `dotnet`
  can only build with SDK 9.0.200+; the test projects target `net10.0`.
- **Docker** — required only to run the integration tests, which start the Azure
  Event Hubs emulator (plus Azurite) via Testcontainers.

## Build & test

```shell
dotnet build -c Release
dotnet test  -c Release            # unit + integration tests (Docker required)

# Unit tests only (no Docker):
dotnet test ./test/Serilog.Sinks.AzureEventHub.Tests/Serilog.Sinks.AzureEventHub.Tests.csproj
```

## Workflow

- The default working branch is **`dev`**. Branch off `dev` and open a pull
  request **into `dev`** — please don't push directly to `dev` or `main`.
- Releases are published from `main`.
- Keep pull requests focused: one logical change per PR.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/)
  (e.g. `feat:`, `fix:`, `chore:`, `docs:`).

## Coding guidelines

- The library targets **`netstandard2.0`** so it stays consumable from modern
  .NET and .NET Framework 4.6.1+/4.8. The default C# language version for this
  target is 7.3 — avoid C# 8+ syntax (e.g. `using` declarations, switch
  expressions) in `src/`.
- `TreatWarningsAsErrors` is **on** for the library and XML documentation is
  generated, so every public member must have XML doc comments or the build
  fails.
- The library is strong-named; don't change or remove the signing setup.
- Add or update tests for any behavior change. Unit tests use xUnit + Moq;
  integration tests use Testcontainers.
- Update `CHANGELOG.md` for any user-facing change.

## Reporting bugs & requesting features

Use the GitHub issue templates. For security issues, **do not** open a public
issue — see [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions are licensed under the
[MIT License](LICENSE). Note that portions of this project derive from the
upstream Apache-2.0 project; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

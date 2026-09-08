# SystemAbstractions

Operating system abstractions for improved service testability. Interfaces are public, implementations are internal, and
services are registered through `IServiceCollection` extensions.

## Packages

| Package | Contents |
| --- | --- |
| [Bitbound.SystemAbstractions](https://www.nuget.org/packages/Bitbound.SystemAbstractions) | `IFileSystem`, `IFileAccessPermissions`, `IProcessManager`, `ISystemEnvironment`, and the Windows-only `IRegistryManager`, plus the `Result`/`Result<T>` primitives and the `AddSystemAbstractions()` registrations. |
| [Bitbound.SystemAbstractions.TestUtilities](https://www.nuget.org/packages/Bitbound.SystemAbstractions.TestUtilities) | `FakeFileSystem`, `FakeRegistry`, platform-specific xUnit facts and theories, and an xUnit-backed `ILogger`. Types are public. |

## Layout

```
Bitbound.SystemAbstractions              the abstractions and their implementations
Bitbound.SystemAbstractions.TestUtilities the fakes and xUnit helpers
Tests/Bitbound.SystemAbstractions.Tests  tests for both, including the fakes
```

Libraries target `net8.0` and `net10.0`. The test project targets `net10.0` and runs on Microsoft's testing platform, so
it is executed with `dotnet run` rather than `dotnet test`.

## Build and test

```
dotnet build SystemAbstractions.slnx
dotnet run --project Tests/Bitbound.SystemAbstractions.Tests/Bitbound.SystemAbstractions.Tests.csproj
```

A single test class or method:

```
dotnet run --project Tests/Bitbound.SystemAbstractions.Tests/Bitbound.SystemAbstractions.Tests.csproj --no-build -- `
  -filter /Bitbound.SystemAbstractions.Tests/Bitbound.SystemAbstractions.Tests.Registry/FakeRegistryTests
```

Filter paths are `/assembly/namespace/class/method`, and the namespace segment is separate from (and usually repeats) the
assembly name.

## Publishing

`.github/workflows/publish.yml` builds, tests, and packs both libraries. It authenticates to NuGet with OIDC via
`NuGet/login`, and pushes only on a manual (`workflow_dispatch`) run against `main`.

# Bitbound.SystemAbstractions

Operating system abstractions for .NET services: file system, processes, system environment, and the Windows registry.
Interfaces are public, implementations are internal, and everything is registered through
`ServiceCollectionExtensions`.

## Quick Start

Install the package.

```
dotnet add package Bitbound.SystemAbstractions
```

Register the abstractions on your `IServiceCollection`.

```csharp
using Bitbound.SystemAbstractions;

var services = new ServiceCollection();
services.AddSystemAbstractions();
```

Resolve and use them through the interfaces.

```csharp
public class BackupService(IFileSystem fileSystem, IProcessManager processes)
{
  public async Task RunAsync()
  {
    if (fileSystem.FileExists("/data/state.json"))
    {
      var result = await processes.GetProcessOutput("dotnet", "--version");
      if (result.IsSuccess)
      {
        Console.WriteLine("dotnet version: " + result.Value);
      }
    }
  }
}
```

The companion test package provides in-memory fakes and xUnit helpers.

```
dotnet add package Bitbound.SystemAbstractions.TestUtilities
```

```csharp
using Bitbound.SystemAbstractions.FileSystem;

var fileSystem = new FakeFileSystem();
fileSystem.AddFile("/data/config.json", """{"port": 5000}""");

var services = new ServiceCollection();
services.AddSystemAbstractions();
services.AddSingleton<IFileSystem>(fileSystem);

// Inject into your service and assert against the fakes.
```

`AddSystemAbstractions()` registers:

| Service | Registration | Notes |
| --- | --- | --- |
| `IFileSystem` | `AddFileSystem()` | Files, directories, drives, streams, zip extraction, `which`/`where.exe` path resolution. |
| `IFileAccessPermissions` | `AddFileSystem()` | Windows ACLs by `WellKnownSidType`, or `UnixFileMode` on Linux/macOS. |
| `IProcessManager` | `AddProcesses()` | Start, inspect, and wait on processes. `IProcess` wraps `System.Diagnostics.Process`. |
| `ISystemEnvironment` | `AddSystemEnvironment()` | Platform, RID, process id, startup paths, well-known directories. |
| `IRegistryManager` | `AddRegistry()` | Windows only. Throws `PlatformNotSupportedException` elsewhere, and `AddSystemAbstractions()` skips it. |

The registry abstractions use their own `RegistryHive`, `RegistryView`, and `RegistryValueKind` enums, so the contracts
are platform-neutral and can be faked on any operating system.

## Returning failures instead of throwing

`IFileSystem.ResolveFilePath` and `IProcessManager.GetProcessOutput` return `Result`/`Result<T>` from
`Bitbound.SystemAbstractions.Primitives`, which carry `IsSuccess`, `Reason`, and the `Exception` that produced the
failure.

```csharp
var resolved = await fileSystem.ResolveFilePath("dotnet");
if (!resolved.IsSuccess)
{
  logger.LogWarning("dotnet is not on the path: {Reason}", resolved.Reason);
}
```

## Testing

Use the companion [Bitbound.SystemAbstractions.TestUtilities](https://www.nuget.org/packages/Bitbound.SystemAbstractions.TestUtilities)
package, which provides `FakeFileSystem` and `FakeRegistry`.

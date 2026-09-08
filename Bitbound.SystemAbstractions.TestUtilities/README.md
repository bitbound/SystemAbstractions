# Bitbound.SystemAbstractions.TestUtilities

In-memory fakes and xUnit helpers for testing services that depend on
[Bitbound.SystemAbstractions](https://www.nuget.org/packages/Bitbound.SystemAbstractions). Unlike the main package, the
types here are public so tests can build and inspect them directly.

## Install

```
dotnet add package Bitbound.SystemAbstractions.TestUtilities
```

## FakeFileSystem

`FakeFileSystem` implements `IFileSystem` and `IFileAccessPermissions` entirely in memory. It mirrors the real
implementation's behavior, including parent-directory creation, `FileMode`/`FileAccess`/`FileShare` rules on streams,
zip extraction, and the exception types and messages the BCL throws.

```csharp
var fileSystem = new FakeFileSystem();
fileSystem.AddFile("/data/config.json", """{"port": 5000}""");
fileSystem.AddDrive("/mnt/data", name: "data", totalSize: 1_000_000, totalFreeSpace: 400_000);
fileSystem.SetResolvedFilePath("dotnet", "/usr/share/dotnet/dotnet");

Assert.True(fileSystem.FileExists("/data/config.json"));
```

Seeding and inspection helpers:

| Member | Purpose |
| --- | --- |
| `AddFile(path, content, ...)` | Seed a file from a string or `byte[]`, creating parent directories. |
| `AddDirectory(path, ...)` | Seed a directory and its parents. |
| `AddDrive(rootPath, ...)` | Seed an entry returned by `GetDrives()`. |
| `SetResolvedFilePath(fileName, path)` | Program the answer to `ResolveFilePath(fileName)`. |
| `SetVersionInfoSource(path, sourcePath)` | Back `GetFileVersionInfo(path)` with a real file's version info. |

The constructor takes `directorySeparator` (`/` by default, `\` for Windows-style paths) and `isCaseSensitive`
(`false` by default, matching Windows).

## FakeRegistry

`FakeRegistry` implements `IRegistryManager` against a memory-backed tree, and applies the same value-kind inference and
error semantics as the Windows registry, so code that reads and writes keys is testable on any operating system.

```csharp
var registry = new FakeRegistry();
registry.SetValue(@"HKEY_CURRENT_USER\Software\MyApp", "InstallDir", @"C:\app");

registry.CurrentUser.OpenSubKey(@"Software\MyApp")!.GetValue("InstallDir");
```

Seeding and inspection helpers: `CreateKey(path)`, `SetValue(keyPath, name, value[, kind])`, `KeyExists(path)`,
`GetValueNames(path)`, `ValueExists(keyPath, name)`, and `Reset()`. The constructor takes `isCaseSensitive`
(`false` by default, matching Windows). Both `HKEY_CURRENT_USER` and `HKCU` style prefixes resolve to the same hive, as
do the other five hives.

Swap the fakes in over the real registrations:

```csharp
var services = new ServiceCollection();
services.AddSystemAbstractions();
services.AddSingleton<IFileSystem>(fileSystem);
services.AddSingleton<IFileAccessPermissions>(fileSystem);
services.AddSingleton<IRegistryManager>(registry);
```

## Platform-specific facts and theories

Skip attributes for tests that only work on one operating system. They set `Skip` at construction time when the current
operating system does not match, so the test reports as skipped rather than failed.

`WindowsOnlyFactAttribute`, `WindowsOnlyTheoryAttribute`, `LinuxOnlyFactAttribute`, `LinuxOnlyTheoryAttribute`,
`MacOnlyFactAttribute`, `MacOnlyTheoryAttribute`.

Session-shaped variants gate on the desktop session rather than the operating system alone:
`InteractiveWindowsFactAttribute` (interactive Windows session, not a CI runner), `WaylandOnlyFactAttribute`
(`WAYLAND_DISPLAY` or `XDG_SESSION_TYPE=wayland`), and `MacKeychainIntegrationFactAttribute` (interactive macOS session,
not a CI runner).

```csharp
[WindowsOnlyFact]
public void Set_WhenAclIsApplied_UpdatesTheFile()
{
  // ...
}
```

## Logging

`XunitLogger`, `XunitLogger<T>`, and `XunitLoggerProvider` implement `Microsoft.Extensions.Logging` on top of xUnit's
`ITestOutputHelper`, so `ILogger` output from the services under test lands in the test's output.

```csharp
services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(testOutputHelper));
```

`XunitLogger<T>` uses `typeof(T).Name` as the category. `BeginScope` nests, and each entry lists its open scopes from
innermost to outermost.

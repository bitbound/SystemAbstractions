using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

namespace Bitbound.SystemAbstractions.Tests;

public class SystemEnvironmentTests
{
  private readonly ISystemEnvironment _environment = new ServiceCollection()
    .AddSystemEnvironment()
    .BuildServiceProvider()
    .GetRequiredService<ISystemEnvironment>();

  [Fact]
  public void GetCommonApplicationDataDirectory_WhenCalled_ReturnsAnExistingDirectory()
  {
    Assert.True(Directory.Exists(_environment.GetCommonApplicationDataDirectory()));
  }

  [Fact]
  public void GetProfileDirectory_WhenCalled_ReturnsAnExistingDirectory()
  {
    Assert.True(Directory.Exists(_environment.GetProfileDirectory()));
  }

  [Fact]
  public void IsDebug_WhenBuiltWithTheTestProject_ReportsTheBuildConfiguration()
  {
#if DEBUG
    Assert.True(_environment.IsDebug);
#else
    Assert.False(_environment.IsDebug);
#endif
  }

  [Fact]
  public void PlatformGuards_WhenQueried_ReportExactlyOnePlatform()
  {
    var platformCount = (_environment.IsWindows() ? 1 : 0)
      + (_environment.IsLinux() ? 1 : 0)
      + (_environment.IsMacOS() ? 1 : 0);

    Assert.Equal(1, platformCount);
  }

  [Fact]
  public void Platform_WhenReadOnThisHost_MatchesTheOperatingSystemChecks()
  {
    if (OperatingSystem.IsWindows())
    {
      Assert.Equal(SystemPlatform.Windows, _environment.Platform);
    }
    else if (OperatingSystem.IsLinux())
    {
      Assert.Equal(SystemPlatform.Linux, _environment.Platform);
    }
    else if (OperatingSystem.IsMacOS())
    {
      Assert.Equal(SystemPlatform.MacOs, _environment.Platform);
    }
    else
    {
      Assert.Equal(SystemPlatform.Unknown, _environment.Platform);
    }
  }

  [Fact]
  public void ProcessValues_WhenRead_MatchTheRunningProcess()
  {
    Assert.Equal(Environment.ProcessId, _environment.ProcessId);
    Assert.Equal(Environment.ProcessorCount, _environment.ProcessorCount);
    Assert.Equal(Environment.CurrentManagedThreadId, _environment.CurrentThreadId);
    Assert.Equal(Environment.Is64BitOperatingSystem, _environment.Is64Bit);
  }

  [Fact]
  public void Runtime_WhenReadOnThisHost_MapsTheRuntimeIdentifier()
  {
    var expected = RuntimeInformation.RuntimeIdentifier switch
    {
      "win-x64" => RuntimeId.WinX64,
      "win-x86" => RuntimeId.WinX86,
      "win-arm64" => RuntimeId.WinArm64,
      "linux-x64" => RuntimeId.LinuxX64,
      "linux-arm64" => RuntimeId.LinuxArm64,
      "osx-x64" => RuntimeId.MacOsX64,
      "osx-arm64" => RuntimeId.MacOsArm64,
      _ => RuntimeId.Unknown
    };

    Assert.Equal(expected, _environment.Runtime);
  }

  [Fact]
  public void StartupPaths_WhenRead_PointAtTheRunningExecutable()
  {
    Assert.Equal(AppContext.BaseDirectory, _environment.SelfExtractDir);
    Assert.Equal(Environment.ProcessPath, _environment.StartupExePath);
    Assert.Equal(Path.GetDirectoryName(Environment.ProcessPath), _environment.StartupDirectory);
  }
}

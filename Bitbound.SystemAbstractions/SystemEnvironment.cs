using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Bitbound.SystemAbstractions;

/// <summary>
/// The operating system a process is running on.
/// </summary>
public enum SystemPlatform
{
  Unknown,
  Windows,
  Linux,
  MacOs,
  MacCatalyst,
  Android,
  Ios,
  Browser
}

/// <summary>
/// A .NET runtime identifier, narrowed to the identifiers these abstractions are exercised against.
/// </summary>
public enum RuntimeId
{
  Unknown,
  WinX86,
  WinX64,
  WinArm64,
  LinuxX64,
  LinuxArm64,
  MacOsX64,
  MacOsArm64
}

/// <summary>
/// Provides access to values from the operating system and current process.
/// </summary>
public interface ISystemEnvironment
{
  int CurrentThreadId { get; }
  bool Is64Bit { get; }
  bool IsDebug { get; }
  SystemPlatform Platform { get; }
  int ProcessId { get; }
  int ProcessorCount { get; }
  RuntimeId Runtime { get; }
  string SelfExtractDir { get; }
  string StartupDirectory { get; }
  string StartupExePath { get; }

  string GetCommonApplicationDataDirectory();

  string GetProfileDirectory();

  [SupportedOSPlatformGuard("linux")]
  bool IsLinux();

  [SupportedOSPlatformGuard("macos")]
  bool IsMacOS();

  [SupportedOSPlatformGuard("windows")]
  bool IsWindows();
}

/// <summary>
/// Default <see cref="ISystemEnvironment"/> backed by <see cref="Environment"/>,
/// <see cref="OperatingSystem"/>, and <see cref="RuntimeInformation"/>.
/// Register it with <see cref="ServiceCollectionExtensions.AddSystemEnvironment(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
internal class SystemEnvironment : ISystemEnvironment
{
  /// <inheritdoc/>
  public int CurrentThreadId => Environment.CurrentManagedThreadId;

  /// <inheritdoc/>
  public bool Is64Bit => Environment.Is64BitOperatingSystem;

  /// <inheritdoc/>
  public bool IsDebug
  {
    get
    {
#if DEBUG
      return true;
#else
      return false;
#endif
    }
  }

  /// <inheritdoc/>
  public SystemPlatform Platform
  {
    get
    {
      if (OperatingSystem.IsWindows())
      {
        return SystemPlatform.Windows;
      }

      if (OperatingSystem.IsLinux())
      {
        return SystemPlatform.Linux;
      }

      if (OperatingSystem.IsMacOS())
      {
        return SystemPlatform.MacOs;
      }

      if (OperatingSystem.IsMacCatalyst())
      {
        return SystemPlatform.MacCatalyst;
      }

      if (OperatingSystem.IsAndroid())
      {
        return SystemPlatform.Android;
      }

      if (OperatingSystem.IsIOS())
      {
        return SystemPlatform.Ios;
      }

      if (OperatingSystem.IsBrowser())
      {
        return SystemPlatform.Browser;
      }

      return SystemPlatform.Unknown;
    }
  }

  /// <inheritdoc/>
  public int ProcessId => Environment.ProcessId;

  /// <inheritdoc/>
  public int ProcessorCount => Environment.ProcessorCount;

  /// <inheritdoc/>
  public RuntimeId Runtime
  {
    get
    {
      var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;

      // Rid strings can carry a libc qualifier (ex. "linux-musl-x64"). Strip it before matching.
      var normalizedIdentifier = runtimeIdentifier.Contains("musl", StringComparison.OrdinalIgnoreCase)
        ? runtimeIdentifier.Replace("-musl", string.Empty, StringComparison.OrdinalIgnoreCase)
        : runtimeIdentifier;

      return normalizedIdentifier switch
      {
        "win-x86" => RuntimeId.WinX86,
        "win-x64" => RuntimeId.WinX64,
        "win-arm64" => RuntimeId.WinArm64,
        "linux-x64" => RuntimeId.LinuxX64,
        "linux-arm64" => RuntimeId.LinuxArm64,
        "osx-x64" => RuntimeId.MacOsX64,
        "osx-arm64" => RuntimeId.MacOsArm64,
        _ => RuntimeId.Unknown
      };
    }
  }

  /// <inheritdoc/>
  public string SelfExtractDir => AppDomain.CurrentDomain.BaseDirectory;

  /// <inheritdoc/>
  public string StartupDirectory =>
    Path.GetDirectoryName(StartupExePath) ??
    throw new DirectoryNotFoundException("Unable to determine startup directory.");

  /// <inheritdoc/>
  public string StartupExePath { get; } = Environment.ProcessPath ?? Environment.GetCommandLineArgs().First();

  /// <inheritdoc/>
  public string GetCommonApplicationDataDirectory()
  {
    return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
  }

  /// <inheritdoc/>
  public string GetProfileDirectory()
  {
    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
  }

  /// <inheritdoc/>
  [SupportedOSPlatformGuard("linux")]
  public bool IsLinux() => OperatingSystem.IsLinux();

  /// <inheritdoc/>
  [SupportedOSPlatformGuard("macos")]
  public bool IsMacOS() => OperatingSystem.IsMacOS();

  /// <inheritdoc/>
  [SupportedOSPlatformGuard("windows")]
  public bool IsWindows() => OperatingSystem.IsWindows();
}

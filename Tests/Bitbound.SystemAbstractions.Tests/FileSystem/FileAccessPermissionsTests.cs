using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

using Bitbound.SystemAbstractions.FileSystem;
using Bitbound.SystemAbstractions.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Bitbound.SystemAbstractions.Tests.FileSystem;

[SupportedOSPlatform("windows")]
public class FileAccessPermissionsWindowsTests : IDisposable
{
  private readonly string _filePath = Path.Combine(
    Path.GetTempPath(),
    "bitbound-permissions-" + Guid.NewGuid().ToString("N")[..12] + ".txt");
  private readonly IFileAccessPermissions _permissions = new ServiceCollection()
    .AddFileSystem()
    .BuildServiceProvider()
    .GetRequiredService<IFileAccessPermissions>();

  public FileAccessPermissionsWindowsTests()
  {
    File.WriteAllText(_filePath, "protected content");
  }

  public void Dispose()
  {
    if (File.Exists(_filePath))
    {
      File.Delete(_filePath);
    }
  }

  [WindowsOnlyFact]
  public void Set_WhenAnAllowedSidIsRequested_AddsAFullControlRuleForIt()
  {
    _permissions.Set(
      _filePath,
      includeCurrentUser: false,
      isProtected: true,
      preserveInheritance: false,
      null,
      WellKnownSidType.WorldSid);

    AssertContainsFullControlFor(new SecurityIdentifier(WellKnownSidType.WorldSid, null));
  }

  [WindowsOnlyFact]
  public void Set_WhenCurrentUserIsIncluded_GrantsFullControlAndKeepsTheFileReadable()
  {
    _permissions.Set(
      _filePath,
      includeCurrentUser: true,
      isProtected: true,
      preserveInheritance: false,
      owner: null,
      WellKnownSidType.WorldSid);

    Assert.Equal("protected content", File.ReadAllText(_filePath));
    AssertContainsFullControlFor(WindowsIdentity.GetCurrent().User!);
  }

  [WindowsOnlyFact]
  public void Set_WhenFileDoesNotExist_ThrowsFileNotFound()
  {
    var missing = Path.Combine(Path.GetTempPath(), "bitbound-missing-" + Guid.NewGuid().ToString("N")[..12]);

    Assert.Throws<FileNotFoundException>(
      () => _permissions.Set(
        missing,
        includeCurrentUser: true,
        isProtected: false,
        preserveInheritance: true,
        null));
  }

  [WindowsOnlyFact]
  public void Set_WhenInheritanceIsNotPreserved_DropsTheInheritedRules()
  {
    _permissions.Set(
      _filePath,
      includeCurrentUser: false,
      isProtected: true,
      preserveInheritance: false,
      null,
      WellKnownSidType.WorldSid);

    var rules = new FileInfo(_filePath)
      .GetAccessControl()
      .GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier));

    Assert.NotEmpty(rules);
    Assert.All(
      rules.Cast<FileSystemAccessRule>(),
      rule => Assert.False(rule.IsInherited));
  }

  [WindowsOnlyFact]
  public void Set_WhenNoOwnerIsRequested_LeavesTheOwnerUnchanged()
  {
    var before = new FileInfo(_filePath).GetAccessControl().GetOwner(typeof(SecurityIdentifier));

    _permissions.Set(
      _filePath,
      includeCurrentUser: true,
      isProtected: false,
      preserveInheritance: true,
      null);

    Assert.Equal(
      before,
      new FileInfo(_filePath).GetAccessControl().GetOwner(typeof(SecurityIdentifier)));
  }

  private void AssertContainsFullControlFor(SecurityIdentifier identity)
  {
    var rules = new FileInfo(_filePath)
      .GetAccessControl()
      .GetAccessRules(true, true, typeof(SecurityIdentifier))
      .Cast<FileSystemAccessRule>();

    Assert.Contains(
      rules,
      rule => rule.IdentityReference.Value == identity.Value
        && (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl
        && rule.AccessControlType == AccessControlType.Allow);
  }
}

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public class FileAccessPermissionsUnixTests : IDisposable
{
  private readonly string _filePath = Path.Combine(
    Path.GetTempPath(),
    "bitbound-unix-permissions-" + Guid.NewGuid().ToString("N")[..12] + ".txt");
  private readonly IFileAccessPermissions _permissions = new ServiceCollection()
    .AddFileSystem()
    .BuildServiceProvider()
    .GetRequiredService<IFileAccessPermissions>();

  public FileAccessPermissionsUnixTests()
  {
    File.WriteAllText(_filePath, "unix content");
  }

  public void Dispose()
  {
    if (File.Exists(_filePath))
    {
      File.Delete(_filePath);
    }
  }

  [LinuxOnlyFact]
  public void Set_WhenFileDoesNotExist_ThrowsFileNotFound()
  {
    var missing = Path.Combine(Path.GetTempPath(), "bitbound-missing-" + Guid.NewGuid().ToString("N")[..12]);

    Assert.Throws<FileNotFoundException>(() => _permissions.Set(missing, UnixFileMode.UserRead));
  }

  [LinuxOnlyFact]
  public void Set_WhenUnixModeIsApplied_IsReadBackFromTheFile()
  {
    _permissions.Set(_filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

    Assert.Equal(
      UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
      File.GetUnixFileMode(_filePath));
  }
}

public class FileAccessPermissionsPlatformMismatchTests
{
  private readonly IFileAccessPermissions _permissions = new ServiceCollection()
    .AddFileSystem()
    .BuildServiceProvider()
    .GetRequiredService<IFileAccessPermissions>();

  [WindowsOnlyFact]
  public void Set_WhenUnixModeIsRequestedOnWindows_ThrowsPlatformNotSupportedException()
  {
#pragma warning disable CA1416 // The Unix overload is being called on purpose to prove its runtime guard.
    Assert.Throws<PlatformNotSupportedException>(
      () => _permissions.Set("C:\\does-not-matter.txt", UnixFileMode.UserRead));
#pragma warning restore CA1416
  }

  [LinuxOnlyFact]
  public void Set_WhenWindowsSidIsRequestedOnLinux_ThrowsPlatformNotSupportedException()
  {
#pragma warning disable CA1416 // The Windows overload is being called on purpose to prove its runtime guard.
    Assert.Throws<PlatformNotSupportedException>(
      () => _permissions.Set(
        "/tmp/does-not-matter.txt",
        includeCurrentUser: false,
        isProtected: false,
        preserveInheritance: true,
        WellKnownSidType.WorldSid));
#pragma warning restore CA1416
  }
}

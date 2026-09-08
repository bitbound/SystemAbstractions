using System.Runtime.Versioning;

using Bitbound.SystemAbstractions.TestUtilities;
using Bitbound.SystemAbstractions.TestUtilities.Registry;
using Bitbound.SystemAbstractions.Windows.Registry;
using Microsoft.Extensions.DependencyInjection;

using Win32Registry = Microsoft.Win32.Registry;
using Win32RegistryKey = Microsoft.Win32.RegistryKey;

namespace Bitbound.SystemAbstractions.Tests.Registry;

/// <summary>
/// Characterizes the real Windows registry through <see cref="IRegistryManager"/> and checks that
/// <see cref="FakeRegistry"/> agrees with it. Everything is written under one throwaway key in HKCU and removed when
/// each test finishes, so no elevation is needed and nothing is left behind.
/// </summary>
public class RegistryManagerTests : IDisposable
{
  private const string ProbeKeyName = "Bitbound.SystemAbstractions.Tests";

  private readonly FakeRegistry _fakeRegistry = new();
  private readonly IRegistryManager _registryManager;

  public RegistryManagerTests()
  {
    var services = new ServiceCollection();
    services.AddSystemAbstractions();

    _registryManager = services
      .BuildServiceProvider()
      .GetRequiredService<IRegistryManager>();

    CleanProbeTree();
  }

  [WindowsOnlyFact]
  public void CreateSubKey_WhenCalledTwice_ReturnsTheSameKey()
  {
    using var parent = CreateProbeKey("Idempotent");
    using var first = parent.CreateSubKey("Child");
    using var second = parent.OpenSubKey("Child");

    Assert.Equal(first.Name, second!.Name);
  }

  [WindowsOnlyFact]
  public void CreateSubKey_WhenCalledUnderCurrentUser_WritesRealKeyAndValue()
  {
    using var key = CreateProbeKey("RoundTrip");
    key.SetValue("Answer", 42);

    Assert.Equal($@"HKEY_CURRENT_USER\Software\{ProbeKeyName}\RoundTrip", key.Name);
    Assert.Equal(42, key.GetValue("Answer"));
    Assert.Equal(RegistryValueKind.DWord, key.GetValueKind("Answer"));
  }

  [WindowsOnlyFact]
  public void CreateSubKey_WhenParentHandleIsReadOnly_ThrowsUnauthorizedAccessException()
  {
    using var parent = CreateProbeKey("ReadOnly");
    using var readOnlyParent = _registryManager.CurrentUser.OpenSubKey($@"Software\{ProbeKeyName}\ReadOnly");

    Assert.NotNull(readOnlyParent);
    Assert.Throws<UnauthorizedAccessException>(() => readOnlyParent!.CreateSubKey("Blocked"));
  }

  [WindowsOnlyFact]
  public void CurrentUser_WhenResolvedFromContainer_HasRealRootName()
  {
    using var currentUser = _registryManager.CurrentUser;

    Assert.Equal("HKEY_CURRENT_USER", currentUser.Name);
  }

  [WindowsOnlyFact]
  public void DeleteSubKeyTree_WhenSubKeyExists_RemovesItInBoth()
  {
    using var realParent = CreateProbeKey("TreeDelete");
    using var realChild = realParent.CreateSubKey(@"Tree\Nested");

    using var fakeParent = _fakeRegistry.CreateKey(@"HKCU\TreeDelete");
    using var fakeChild = fakeParent.CreateSubKey(@"Tree\Nested");

    realParent.DeleteSubKeyTree("Tree");
    fakeParent.DeleteSubKeyTree("Tree");

    Assert.Null(realParent.OpenSubKey("Tree"));
    Assert.Null(fakeParent.OpenSubKey("Tree"));
  }

  [WindowsOnlyFact]
  public void DeleteSubKey_WhenSubKeyHasChildren_ThrowsTheSameMessageInBoth()
  {
    using var realParent = CreateProbeKey("NestedDelete");
    using var realChild = realParent.CreateSubKey(@"Tree\Nested");

    using var fakeParent = _fakeRegistry.CreateKey(@"HKCU\NestedDelete");
    using var fakeChild = fakeParent.CreateSubKey(@"Tree\Nested");

    var realException = Assert.Throws<InvalidOperationException>(() => realParent.DeleteSubKey("Tree"));
    var fakeException = Assert.Throws<InvalidOperationException>(() => fakeParent.DeleteSubKey("Tree"));

    Assert.Equal(realException.Message, fakeException.Message);
  }

  [WindowsOnlyFact]
  public void DeleteSubKey_WhenSubKeyMissing_ThrowsTheSameMessageInBoth()
  {
    using var realKey = CreateProbeKey("DeleteMissingSubKey");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\DeleteMissingSubKey");

    var realException = Assert.Throws<ArgumentException>(() => realKey.DeleteSubKey("Absent"));
    var fakeException = Assert.Throws<ArgumentException>(() => fakeKey.DeleteSubKey("Absent"));

    Assert.Equal(realException.Message, fakeException.Message);
  }

  [WindowsOnlyFact]
  public void DeleteValue_WhenValueMissing_ThrowsTheSameMessageInBoth()
  {
    using var realKey = CreateProbeKey("DeleteMissingValue");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\DeleteMissingValue");

    var realException = Assert.Throws<ArgumentException>(() => realKey.DeleteValue("Absent"));
    var fakeException = Assert.Throws<ArgumentException>(() => fakeKey.DeleteValue("Absent"));

    Assert.Equal(realException.Message, fakeException.Message);
  }

  public void Dispose()
  {
    CleanProbeTree();
    GC.SuppressFinalize(this);
  }

  [WindowsOnlyFact]
  public void GetSubKeyNames_WhenChildrenExist_ReturnsChildNamesInBoth()
  {
    using var realKey = CreateProbeKey("ChildNames");
    using var realChild = realKey.CreateSubKey("Alpha");

    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\ChildNames");
    using var fakeChild = fakeKey.CreateSubKey("Alpha");

    Assert.Equal(["Alpha"], realKey.GetSubKeyNames());
    Assert.Equal(realKey.GetSubKeyNames(), fakeKey.GetSubKeyNames());
  }

  [WindowsOnlyFact]
  public void GetValueKind_WhenValueMissing_ThrowsTheSameExceptionInBoth()
  {
    using var realKey = CreateProbeKey("MissingKind");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\MissingKind");

    var realException = Assert.Throws<IOException>(() => realKey.GetValueKind("Absent"));
    var fakeException = Assert.Throws<IOException>(() => fakeKey.GetValueKind("Absent"));

    Assert.Equal(realException.Message, fakeException.Message);
  }

  [WindowsOnlyFact]
  public void GetValue_WhenValueMissing_ReturnsNullInBoth()
  {
    using var realKey = CreateProbeKey("MissingValue");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\MissingValue");

    Assert.Null(realKey.GetValue("Absent"));
    Assert.Null(fakeKey.GetValue("Absent"));
    Assert.Equal("fallback", realKey.GetValue("Absent", "fallback"));
    Assert.Equal("fallback", fakeKey.GetValue("Absent", "fallback"));
  }

  [WindowsOnlyFact]
  public void LocalMachine_WhenResolvedFromContainer_HasRealRootName()
  {
    using var localMachine = _registryManager.LocalMachine;

    Assert.Equal("HKEY_LOCAL_MACHINE", localMachine.Name);
  }

  [WindowsOnlyFact]
  public void Names_WhenWrittenWithDifferentCase_ReadBackWithMatchingNamesInBoth()
  {
    using var realKey = CreateProbeKey("Casing");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\Casing");

    AssertCasingBehavior(realKey);
    AssertCasingBehavior(fakeKey);

    return;

    static void AssertCasingBehavior(IRegistryKey key)
    {
      key.SetValue("MiXeDVaLuE", 1);

      Assert.Equal(1, key.GetValue("mixedvalue"));
      Assert.Equal(["MiXeDVaLuE"], key.GetValueNames());
    }
  }

  [WindowsOnlyFact]
  public void OpenBaseKey_WhenHiveIsUsers_DoesNotThrow()
  {
    using var users = _registryManager.OpenBaseKey(RegistryHive.Users, RegistryView.Default);

    Assert.Equal("HKEY_USERS", users.Name);
    Assert.Equal(RegistryView.Default, users.View);
  }

  [WindowsOnlyFact]
  public void OpenSubKey_WhenSubKeyMissing_ReturnsNullInBoth()
  {
    using var realKey = CreateProbeKey("MissingSubKey");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\MissingSubKey");

    Assert.Null(realKey.OpenSubKey("Absent"));
    Assert.Null(fakeKey.OpenSubKey("Absent"));
  }

  [WindowsOnlyFact]
  public void SetValue_WhenByteArrayIsWritten_RealRegistryAndFakeAgree()
  {
    using var realKey = CreateProbeKey("Bytes");
    realKey.SetValue("Blob", new byte[] { 4, 8, 15 });

    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\Bytes");
    fakeKey.SetValue("Blob", new byte[] { 4, 8, 15 });

    Assert.Equal(RegistryValueKind.Binary, realKey.GetValueKind("Blob"));
    Assert.Equal(new byte[] { 4, 8, 15 }, realKey.GetValue("Blob"));
    Assert.Equal(realKey.GetValue("Blob"), fakeKey.GetValue("Blob"));
  }

  [WindowsOnlyFact]
  public void SetValue_WhenExpandStringIsWritten_RealRegistryAndFakeBothExpand()
  {
    using var realKey = CreateProbeKey("Expand");
    realKey.SetValue("Expanded", @"%PROGRAMFILES%\Bitbound", RegistryValueKind.ExpandString);

    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\Expand");
    fakeKey.SetValue("Expanded", @"%PROGRAMFILES%\Bitbound", RegistryValueKind.ExpandString);

    Assert.Equal(realKey.GetValue("Expanded"), fakeKey.GetValue("Expanded"));
    Assert.DoesNotContain("%PROGRAMFILES%", Assert.IsType<string>(realKey.GetValue("Expanded")));
  }

  [WindowsOnlyFact]
  public void SetValue_WhenStringArrayIsWritten_RealRegistryAndFakeAgree()
  {
    using var realKey = CreateProbeKey("Multi");
    realKey.SetValue("List", new[] { "one", "two" });

    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\Multi");
    fakeKey.SetValue("List", new[] { "one", "two" });

    Assert.Equal(RegistryValueKind.MultiString, realKey.GetValueKind("List"));
    Assert.Equal(new[] { "one", "two" }, realKey.GetValue("List"));
    Assert.Equal(realKey.GetValue("List"), fakeKey.GetValue("List"));
  }

  [WindowsOnlyFact]
  public void SetValue_WhenUnsupportedArrayTypeIsUsed_ThrowsTheSameMessageInBoth()
  {
    using var realKey = CreateProbeKey("BadArray");
    using var fakeKey = _fakeRegistry.CreateKey(@"HKCU\BadArray");

    var realException = Assert.Throws<ArgumentException>(() => realKey.SetValue("Ints", new[] { 1, 2 }));
    var fakeException = Assert.Throws<ArgumentException>(() => fakeKey.SetValue("Ints", new[] { 1, 2 }));

    Assert.Equal(realException.Message, fakeException.Message);
  }

  [WindowsOnlyTheory]
  [InlineData("StringValue", "hello", RegistryValueKind.String)]
  [InlineData("IntValue", 42, RegistryValueKind.DWord)]
  [InlineData("LongValue", 42L, RegistryValueKind.String)]
  [InlineData("BoolValue", true, RegistryValueKind.String)]
  [InlineData("DoubleValue", 42d, RegistryValueKind.String)]
  public void SetValue_WhenValueIsWritten_RealRegistryAndFakeAgree(
    string valueName,
    object value,
    RegistryValueKind expectedKind)
  {
    using var realKey = CreateProbeKey($"Kinds_{valueName}");
    realKey.SetValue(valueName, value);

    using var fakeKey = _fakeRegistry.CreateKey($@"HKCU\Kinds_{valueName}");
    fakeKey.SetValue(valueName, value);

    Assert.Equal(expectedKind, realKey.GetValueKind(valueName));
    Assert.Equal(realKey.GetValueKind(valueName), fakeKey.GetValueKind(valueName));
    Assert.Equal(realKey.GetValue(valueName), fakeKey.GetValue(valueName));
  }

  private static void CleanProbeTree()
  {
    if (!OperatingSystem.IsWindows())
    {
      return;
    }

    using var software = OpenSoftwareKey();
    software?.DeleteSubKeyTree(ProbeKeyName, throwOnMissingSubKey: false);
  }

  [SupportedOSPlatform("windows")]
  private static Win32RegistryKey? OpenSoftwareKey()
  {
    return Win32Registry.CurrentUser.OpenSubKey("Software", writable: true);
  }

  private IRegistryKey CreateProbeKey(string leafName)
  {
    using var currentUser = _registryManager.CurrentUser;
    using var software = currentUser.OpenSubKey("Software", writable: true)
      ?? throw new InvalidOperationException("HKCU\\Software could not be opened for writing.");
    using var probeRoot = software.CreateSubKey(ProbeKeyName);

    return probeRoot.CreateSubKey(leafName);
  }
}

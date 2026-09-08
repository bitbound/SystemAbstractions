using Bitbound.SystemAbstractions.TestUtilities.Registry;
using Bitbound.SystemAbstractions.Windows.Registry;

namespace Bitbound.SystemAbstractions.Tests.Registry;

public class FakeRegistryTests
{
  private readonly FakeRegistry _registry = new();

  [Fact]
  public void CaseSensitiveRegistry_WhenNamesDifferOnlyByCase_TreatsThemAsSeparateValues()
  {
    var registry = new FakeRegistry(isCaseSensitive: true);
    using var key = registry.CreateKey(@"HKCU\Software\Bitbound\CaseSensitive");
    key.SetValue("Value", 1);
    key.SetValue("value", 2);

    Assert.Equal(1, key.GetValue("Value"));
    Assert.Equal(2, key.GetValue("value"));
    Assert.Equal(2, key.ValueCount);
  }

  [Fact]
  public void CreateKey_WhenIntermediateKeysMissing_CreatesWholeChain()
  {
    using var leaf = _registry.CreateKey(@"HKCU\Software\Bitbound\Deep\Deeper\Leaf");

    Assert.True(_registry.KeyExists(@"HKCU\Software\Bitbound\Deep\Deeper"));
    Assert.Equal(@"HKEY_CURRENT_USER\Software\Bitbound\Deep\Deeper\Leaf", leaf.Name);
  }

  [Fact]
  public void CreateKey_WhenPathHasNoKnownHive_ThrowsArgumentException()
  {
    var exception = Assert.Throws<ArgumentException>(() => _registry.CreateKey(@"SOFTWARE\Bitbound"));

    Assert.Contains("does not start with a known registry hive", exception.Message);
  }

  [Fact]
  public void CreateKey_WhenPathUsesHiveAlias_StoresKeyUnderMatchingHiveRoot()
  {
    using var key = _registry.CreateKey(@"HKLM\SOFTWARE\Bitbound\Thing");

    Assert.Equal(@"HKEY_LOCAL_MACHINE\SOFTWARE\Bitbound\Thing", key.Name);
    Assert.True(_registry.KeyExists(@"HKEY_LOCAL_MACHINE\SOFTWARE\Bitbound\Thing"));
  }

  [Fact]
  public void CreateSubKey_WhenNameIsNull_ThrowsArgumentNullException()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\NullName");

    Assert.Throws<ArgumentNullException>(() => key.CreateSubKey(null!));
  }

  [Fact]
  public void CreateSubKey_WhenWritableArgumentIsFalse_ReturnsReadOnlyHandle()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\CreateReadOnly");
    using var child = key.CreateSubKey("Child", writable: false);

    Assert.Throws<UnauthorizedAccessException>(() => child.SetValue("Nope", 1));
  }

  [Fact]
  public void CurrentUser_WhenResolved_HasRealRootName()
  {
    using var currentUser = _registry.CurrentUser;

    Assert.Equal("HKEY_CURRENT_USER", currentUser.Name);
  }

  [Fact]
  public void DeleteSubKeyTree_WhenSubKeyExists_RemovesTheWholeTree()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Tree\Nested\Deeper");
    using var root = _registry.CurrentUser.OpenSubKey(@"Software\Bitbound", writable: true)!;

    root.DeleteSubKeyTree("Tree");

    Assert.False(_registry.KeyExists(@"HKCU\Software\Bitbound\Tree"));
    Assert.False(_registry.KeyExists(@"HKCU\Software\Bitbound\Tree\Nested"));
  }

  [Fact]
  public void DeleteSubKeyTree_WhenSubKeyMissing_ThrowsArgumentExceptionLikeWindowsDoes()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\TreeMissing");

    Assert.Throws<ArgumentException>(() => key.DeleteSubKeyTree("Absent"));
  }

  [Fact]
  public void DeleteSubKey_WhenSubKeyHasChildren_ThrowsInvalidOperationExceptionLikeWindowsDoes()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteTree\Parent");
    using var parent = _registry.CurrentUser.OpenSubKey(@"Software\Bitbound\DeleteTree", writable: true)!;
    using var child = parent.CreateSubKey(@"Tree\Nested");

    var exception = Assert.Throws<InvalidOperationException>(() => parent.DeleteSubKey("Tree"));

    Assert.Equal(
      "Registry key has subkeys and recursive removes are not supported by this method.",
      exception.Message);
  }

  [Fact]
  public void DeleteSubKey_WhenSubKeyIsEmpty_RemovesTheSubKey()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteLeaf");
    using var parent = _registry.CurrentUser.OpenSubKey(@"Software\Bitbound\DeleteLeaf", writable: true)!;
    using var leaf = parent.CreateSubKey("Leaf");

    parent.DeleteSubKey("Leaf");

    Assert.Null(parent.OpenSubKey("Leaf"));
  }

  [Fact]
  public void DeleteSubKey_WhenSubKeyMissingAndThrowDisabled_DoesNothing()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteMissing");

    key.DeleteSubKey("Absent", throwOnMissingSubKey: false);

    Assert.Empty(key.GetSubKeyNames());
  }

  [Fact]
  public void DeleteSubKey_WhenSubKeyMissing_ThrowsArgumentExceptionLikeWindowsDoes()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteMissing");

    var exception = Assert.Throws<ArgumentException>(() => key.DeleteSubKey("Absent"));

    Assert.Equal("Cannot delete a subkey tree because the subkey does not exist.", exception.Message);
  }

  [Fact]
  public void DeleteValue_WhenValueExists_RemovesTheValue()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteValue");
    key.SetValue("RemoveMe", 1);

    key.DeleteValue("RemoveMe");

    Assert.Null(key.GetValue("RemoveMe"));
  }

  [Fact]
  public void DeleteValue_WhenValueMissingAndThrowDisabled_DoesNothing()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteValue");

    key.DeleteValue("Absent", throwOnMissingValue: false);

    Assert.Empty(key.GetValueNames());
  }

  [Fact]
  public void DeleteValue_WhenValueMissing_ThrowsArgumentExceptionLikeWindowsDoes()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\DeleteValue");

    var exception = Assert.Throws<ArgumentException>(() => key.DeleteValue("Absent"));

    Assert.Equal("No value exists with that name.", exception.Message);
  }

  [Fact]
  public void Dispose_WhenCalledTwice_DoesNotThrow()
  {
    var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Dispose");

    key.Dispose();
    key.Dispose();

    Assert.True(key.IsDisposed);
  }

  [Fact]
  public void Dispose_WhenHandleDisposed_KeepsTheKeyItself()
  {
    var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Survivor");
    key.SetValue("Kept", 1);
    key.Dispose();

    using var reopened = _registry.CurrentUser.OpenSubKey(@"Software\Bitbound\Survivor");

    Assert.NotNull(reopened);
    Assert.Equal(1, reopened!.GetValue("Kept"));
  }

  [Fact]
  public void Dispose_WhenHandleUsedAfterwards_ThrowsObjectDisposedExceptionLikeWindowsDoes()
  {
    var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Disposed");
    var keyName = key.Name;
    key.Dispose();

    var exception = Assert.Throws<ObjectDisposedException>(() => key.GetValue("Anything"));

    Assert.Equal(keyName, exception.ObjectName);
    Assert.StartsWith("Cannot access a closed registry key.", exception.Message);
  }

  [Fact]
  public void GetSubKeyNames_WhenChildrenExist_ReturnsChildNamesOnly()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Children");
    using var first = key.CreateSubKey("First");
    using var second = key.CreateSubKey("Second");

    Assert.Equal(["First", "Second"], key.GetSubKeyNames());
    Assert.Equal(2, key.SubKeyCount);
  }

  [Fact]
  public void GetValueKind_WhenValueMissing_ThrowsIOExceptionLikeWindowsDoes()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\KindMissing");

    var exception = Assert.Throws<IOException>(() => key.GetValueKind("Absent"));

    Assert.Equal("The specified registry key does not exist.", exception.Message);
  }

  [Fact]
  public void GetValueNames_WhenValuesExist_ReturnsEveryName()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Names");
    key.SetValue("Alpha", 1);
    key.SetValue("Beta", "two");

    Assert.Equal(["Alpha", "Beta"], key.GetValueNames());
    Assert.Equal(["Alpha", "Beta"], _registry.GetValueNames(@"HKCU\Software\Bitbound\Names"));
  }

  [Fact]
  public void GetValue_WhenKindIsExpandString_ExpandsEnvironmentVariables()
  {
    var expected = Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES%\Bitbound");
    _registry.SetValue(
      @"HKCU\Software\Bitbound\Expand",
      "ExpandValue",
      @"%PROGRAMFILES%\Bitbound",
      RegistryValueKind.ExpandString);
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Expand");

    Assert.Equal(RegistryValueKind.ExpandString, key.GetValueKind("ExpandValue"));
    Assert.Equal(expected, key.GetValue("ExpandValue"));
  }

  [Fact]
  public void GetValue_WhenValueMissingAndDefaultSupplied_ReturnsDefault()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Missing");

    Assert.Equal("fallback", key.GetValue("Absent", "fallback"));
  }

  [Fact]
  public void GetValue_WhenValueMissing_ReturnsNull()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Missing");

    Assert.Null(key.GetValue("Absent"));
  }

  [Fact]
  public void LocalMachine_WhenResolved_HasRealRootName()
  {
    using var localMachine = _registry.LocalMachine;

    Assert.Equal("HKEY_LOCAL_MACHINE", localMachine.Name);
  }

  [Fact]
  public void Names_WhenWrittenWithDifferentCase_ReadBackWithMatchingNames()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Case");
    key.SetValue("MiXeDVaLuE", 1);

    Assert.Equal(1, key.GetValue("mixedvalue"));
    Assert.Equal(1, key.GetValue("MIXEDVALUE"));
    Assert.Equal(["MiXeDVaLuE"], key.GetValueNames());
  }

  [Fact]
  public void OpenBaseKey_WhenCalledForEachKnownHive_ReturnsMatchingRootName()
  {
    var expected = new Dictionary<RegistryHive, string>
    {
      [RegistryHive.ClassesRoot] = "HKEY_CLASSES_ROOT",
      [RegistryHive.CurrentConfig] = "HKEY_CURRENT_CONFIG",
      [RegistryHive.CurrentUser] = "HKEY_CURRENT_USER",
      [RegistryHive.LocalMachine] = "HKEY_LOCAL_MACHINE",
      [RegistryHive.PerformanceData] = "HKEY_PERFORMANCE_DATA",
      [RegistryHive.Users] = "HKEY_USERS"
    };

    foreach (var (hive, rootName) in expected)
    {
      using var baseKey = _registry.OpenBaseKey(hive, RegistryView.Default);

      Assert.Equal(rootName, baseKey.Name);
    }
  }

  [Fact]
  public void OpenBaseKey_WhenOpened_ReturnsReadOnlyHandleLikeTheRealApi()
  {
    using var baseKey = _registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

    Assert.Equal(RegistryView.Registry64, baseKey.View);
    Assert.Throws<UnauthorizedAccessException>(() => baseKey.SetValue("Blocked", 1));
  }

  [Fact]
  public void OpenSubKey_WhenParentHandleReadOnlyAndWritableRequested_ReturnsWritableHandle()
  {
    using var writable = _registry.CreateKey(@"HKCU\Software\Bitbound\Writable\Child");
    using var child = _registry.CurrentUser.OpenSubKey(@"Software\Bitbound\Writable\Child", writable: true);

    Assert.NotNull(child);
    child!.SetValue("Allowed", 1);

    Assert.Equal(1, child.GetValue("Allowed"));
  }

  [Fact]
  public void OpenSubKey_WhenSubKeyMissing_ReturnsNull()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\NoChildren");

    Assert.Null(key.OpenSubKey("Absent"));
  }

  [Fact]
  public void Reset_WhenCalled_EmptyEveryHive()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Reset", "Value", 1);
    _registry.SetValue(@"HKLM\SOFTWARE\Bitbound\Reset", "Value", 1);

    _registry.Reset();

    Assert.False(_registry.KeyExists(@"HKCU\Software\Bitbound"));
    Assert.False(_registry.KeyExists(@"HKLM\SOFTWARE\Bitbound"));
  }

  [Fact]
  public void SetValue_WhenHandleReadOnly_ThrowsUnauthorizedAccessException()
  {
    using var writable = _registry.CreateKey(@"HKCU\Software\Bitbound\ReadOnly");
    using var readOnly = _registry.CurrentUser.OpenSubKey(@"Software\Bitbound\ReadOnly");

    Assert.NotNull(readOnly);
    Assert.Throws<UnauthorizedAccessException>(() => readOnly!.SetValue("Nope", 1));
    Assert.Throws<UnauthorizedAccessException>(() => readOnly!.CreateSubKey("Nope"));
  }

  [Fact]
  public void SetValue_WhenKindIsExplicitAndValueDoesNotMatch_StoresStringRepresentation()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Kinds", "IntAsString", 42, RegistryValueKind.String);
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Kinds");

    Assert.Equal(RegistryValueKind.String, key.GetValueKind("IntAsString"));
    Assert.Equal("42", key.GetValue("IntAsString"));
  }

  [Fact]
  public void SetValue_WhenKindIsExplicitQWord_ReadsBackInt64()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Kinds", "QWordValue", 42L, RegistryValueKind.QWord);
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Kinds");

    Assert.Equal(RegistryValueKind.QWord, key.GetValueKind("QWordValue"));
    Assert.Equal(42L, Assert.IsType<long>(key.GetValue("QWordValue")));
  }

  [Fact]
  public void SetValue_WhenValueIsByteArray_StoresBinary()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Kinds", "BinaryValue", new byte[] { 1, 2, 3 });
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Kinds");

    Assert.Equal(RegistryValueKind.Binary, key.GetValueKind("BinaryValue"));
    Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<byte[]>(key.GetValue("BinaryValue")));
  }

  [Fact]
  public void SetValue_WhenValueIsInt_StoresDWordAndReadsBackInt()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Kinds", "DWordValue", 42);
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Kinds");

    Assert.Equal(RegistryValueKind.DWord, key.GetValueKind("DWordValue"));
    Assert.Equal(42, key.GetValue("DWordValue"));
  }

  [Fact]
  public void SetValue_WhenValueIsNull_ThrowsArgumentNullException()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\NullValue");

    Assert.Throws<ArgumentNullException>(() => key.SetValue("Nope", null!));
  }

  [Fact]
  public void SetValue_WhenValueIsStringArray_StoresMultiString()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Kinds", "MultiValue", new[] { "a", "b" });
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Kinds");

    Assert.Equal(RegistryValueKind.MultiString, key.GetValueKind("MultiValue"));
    Assert.Equal(new[] { "a", "b" }, Assert.IsType<string[]>(key.GetValue("MultiValue")));
  }

  [Fact]
  public void SetValue_WhenValueIsString_StoresString()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Kinds", "StringValue", "hello");
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Kinds");

    Assert.Equal(RegistryValueKind.String, key.GetValueKind("StringValue"));
    Assert.Equal("hello", key.GetValue("StringValue"));
  }

  [Fact]
  public void SetValue_WhenValueIsUnsupportedArray_ThrowsArgumentException()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\BadArray");

    var exception = Assert.Throws<ArgumentException>(() => key.SetValue("Ints", new[] { 1, 2 }));

    Assert.Contains("Only Byte[] and String[] are supported", exception.Message);
  }

  [Theory]
  [InlineData("LongValue", 42L, RegistryValueKind.String)]
  [InlineData("BoolValue", true, RegistryValueKind.String)]
  [InlineData("DoubleValue", 42d, RegistryValueKind.String)]
  public void SetValue_WhenValueTypeIsNotSpecialCased_StoresStringLikeWindowsDoes(
    string valueName,
    object value,
    RegistryValueKind expectedKind)
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Inferred", valueName, value);
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Inferred");

    Assert.Equal(expectedKind, key.GetValueKind(valueName));
    Assert.Equal(value.ToString(), key.GetValue(valueName));
  }

  [Fact]
  public void SubKeyCountAndValueCount_WhenChildrenAndValuesAdded_CountThem()
  {
    using var key = _registry.CreateKey(@"HKCU\Software\Bitbound\Counts");
    using var child = key.CreateSubKey("Child");
    key.SetValue("One", 1);
    key.SetValue("Two", 2);

    Assert.Equal(1, key.SubKeyCount);
    Assert.Equal(2, key.ValueCount);
  }

  [Fact]
  public void ValueExists_WhenValuePresentAndAbsent_ReportsCorrectly()
  {
    _registry.SetValue(@"HKCU\Software\Bitbound\Exists", "Present", 1);

    Assert.True(_registry.ValueExists(@"HKCU\Software\Bitbound\Exists", "Present"));
    Assert.False(_registry.ValueExists(@"HKCU\Software\Bitbound\Exists", "Absent"));
    Assert.False(_registry.ValueExists(@"HKCU\Software\Bitbound\Nope", "Present"));
  }

  [Fact]
  public void View_WhenChildOpenedFromBaseKey_InheritsParentView()
  {
    _registry.CreateKey(@"HKLM\SOFTWARE\Bitbound\ViewTest\Child").Dispose();
    using var baseKey = _registry.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
    using var child = baseKey.OpenSubKey(@"SOFTWARE\Bitbound\ViewTest\Child");

    Assert.NotNull(child);
    Assert.Equal(RegistryView.Registry32, child!.View);
  }
}

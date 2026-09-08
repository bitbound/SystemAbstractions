using System.Runtime.Versioning;
using Microsoft.Win32;

using Win32Registry = Microsoft.Win32.Registry;

namespace Bitbound.SystemAbstractions.Windows.Registry;

/// <summary>
/// A single key in the Windows registry. The contract is platform-neutral so it can be faked in tests.
/// Use <see cref="IRegistryManager"/> to obtain instances.
/// </summary>
public interface IRegistryKey : IDisposable
{
  string Name { get; }
  int SubKeyCount { get; }
  int ValueCount { get; }
  RegistryView View { get; }

  IRegistryKey CreateSubKey(string subkey);
  IRegistryKey CreateSubKey(string subkey, bool writable);

  void DeleteSubKey(string subKey);
  void DeleteSubKey(string subKey, bool throwOnMissingSubKey);
  void DeleteSubKeyTree(string subKey);
  void DeleteSubKeyTree(string subKey, bool throwOnMissingSubKey);
  void DeleteValue(string name);
  void DeleteValue(string name, bool throwOnMissingValue);

  string[] GetSubKeyNames();

  object? GetValue(string name);
  object? GetValue(string name, object? defaultValue);

  RegistryValueKind GetValueKind(string valueName);

  string[] GetValueNames();

  IRegistryKey? OpenSubKey(string name, bool writable = false);

  void SetValue(string name, object value);
  void SetValue(string name, object value, RegistryValueKind kind);
}

/// <summary>
/// Provides access to the Windows registry.
/// </summary>
public interface IRegistryManager
{
  IRegistryKey CurrentUser { get; }
  IRegistryKey LocalMachine { get; }

  IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view);
}

[SupportedOSPlatform("windows")]
internal sealed class RegistryKeyImpl(RegistryKey key) : IRegistryKey
{
  private readonly RegistryKey _key = key;

  public string Name => _key.Name;

  public int SubKeyCount => _key.SubKeyCount;

  public int ValueCount => _key.ValueCount;

  public RegistryView View => RegistryConversions.ToAbstractionView(_key.View);

  public static IRegistryKey From(RegistryKey key)
  {
    return new RegistryKeyImpl(key);
  }

  public IRegistryKey CreateSubKey(string subkey)
  {
    return new RegistryKeyImpl(OpenCreatedSubKey(_key.CreateSubKey(subkey), subkey));
  }

  public IRegistryKey CreateSubKey(string subkey, bool writable)
  {
    return new RegistryKeyImpl(OpenCreatedSubKey(_key.CreateSubKey(subkey, writable), subkey));
  }

  public void DeleteSubKey(string subKey)
  {
    _key.DeleteSubKey(subKey);
  }

  public void DeleteSubKey(string subKey, bool throwOnMissingSubKey)
  {
    _key.DeleteSubKey(subKey, throwOnMissingSubKey);
  }

  public void DeleteSubKeyTree(string subKey)
  {
    _key.DeleteSubKeyTree(subKey);
  }

  public void DeleteSubKeyTree(string subKey, bool throwOnMissingSubKey)
  {
    _key.DeleteSubKeyTree(subKey, throwOnMissingSubKey);
  }

  public void DeleteValue(string name)
  {
    _key.DeleteValue(name);
  }

  public void DeleteValue(string name, bool throwOnMissingValue)
  {
    _key.DeleteValue(name, throwOnMissingValue);
  }

  public void Dispose()
  {
    _key.Dispose();
  }

  public string[] GetSubKeyNames() => _key.GetSubKeyNames();

  public object? GetValue(string name) => _key.GetValue(name);

  public object? GetValue(string name, object? defaultValue) => _key.GetValue(name, defaultValue);

  public RegistryValueKind GetValueKind(string valueName)
  {
    return RegistryConversions.ToAbstractionValueKind(_key.GetValueKind(valueName));
  }

  public string[] GetValueNames() => _key.GetValueNames();

  public IRegistryKey? OpenSubKey(string name, bool writable = false)
  {
    return _key.OpenSubKey(name, writable) is { } key ? From(key) : null;
  }

  public void SetValue(string name, object value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _key.SetValue(name, value);
  }

  public void SetValue(string name, object value, RegistryValueKind kind)
  {
    ArgumentNullException.ThrowIfNull(value);
    _key.SetValue(name, value, RegistryConversions.ToWin32ValueKind(kind));
  }

  private static RegistryKey OpenCreatedSubKey(RegistryKey? subKey, string subkey)
  {
    return subKey ?? throw new InvalidOperationException(
      $"Unable to create or open sub key '{subkey}'. The parent key is likely opened as read-only.");
  }
}

[SupportedOSPlatform("windows")]
internal sealed class RegistryManager : IRegistryManager
{
  private readonly Lazy<IRegistryKey> _currentUser = new(() => RegistryKeyImpl.From(Win32Registry.CurrentUser));
  private readonly Lazy<IRegistryKey> _localMachine = new(() => RegistryKeyImpl.From(Win32Registry.LocalMachine));

  public IRegistryKey CurrentUser => _currentUser.Value;

  public IRegistryKey LocalMachine => _localMachine.Value;

  public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
  {
    return RegistryKeyImpl.From(
      RegistryKey.OpenBaseKey(RegistryConversions.ToWin32Hive(hive), RegistryConversions.ToWin32View(view)));
  }
}

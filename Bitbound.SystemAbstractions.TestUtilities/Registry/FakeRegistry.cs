using Bitbound.SystemAbstractions.Windows.Registry;

namespace Bitbound.SystemAbstractions.TestUtilities.Registry;

/// <summary>
/// A memory-backed <see cref="IRegistryManager"/>. Every hive exists up front and starts empty, so tests never touch
/// the real registry and never need to be elevated.
/// </summary>
/// <remarks>
/// Key and value names are case-insensitive by default, like the real registry. Values are stored with the same kind
/// inference rules <c>RegistryKey.SetValue</c> uses, so reads round-trip the way they would against Windows.
/// </remarks>
public sealed class FakeRegistry : IRegistryManager
{
  private static readonly Dictionary<string, RegistryHive> _hiveAliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["HKCR"] = RegistryHive.ClassesRoot,
    ["HKEY_CLASSES_ROOT"] = RegistryHive.ClassesRoot,
    ["HKCC"] = RegistryHive.CurrentConfig,
    ["HKEY_CURRENT_CONFIG"] = RegistryHive.CurrentConfig,
    ["HKCU"] = RegistryHive.CurrentUser,
    ["HKEY_CURRENT_USER"] = RegistryHive.CurrentUser,
    ["HKLM"] = RegistryHive.LocalMachine,
    ["HKEY_LOCAL_MACHINE"] = RegistryHive.LocalMachine,
    ["HKPD"] = RegistryHive.PerformanceData,
    ["HKEY_PERFORMANCE_DATA"] = RegistryHive.PerformanceData,
    ["HKU"] = RegistryHive.Users,
    ["HKEY_USERS"] = RegistryHive.Users
  };

  private readonly Dictionary<RegistryHive, FakeRegistryNode> _hives;
  private readonly StringComparer _nameComparer;

  /// <summary>
  /// Initializes a new instance of the <see cref="FakeRegistry"/> class with case-insensitive names.
  /// </summary>
  public FakeRegistry()
    : this(isCaseSensitive: false)
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="FakeRegistry"/> class.
  /// </summary>
  /// <param name="isCaseSensitive">Whether key and value names distinguish case. The real registry does not.</param>
  public FakeRegistry(bool isCaseSensitive)
  {
    _nameComparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    _hives = CreateHives(_nameComparer);
  }

  /// <inheritdoc/>
  public IRegistryKey CurrentUser => OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

  /// <inheritdoc/>
  public IRegistryKey LocalMachine => OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);

  internal object SyncRoot { get; } = new();

  /// <summary>
  /// Creates (or opens) the key at <paramref name="path"/> and returns a writable handle to it.
  /// Paths start with a hive, either by name (<c>HKEY_LOCAL_MACHINE\SOFTWARE\Foo</c>) or by alias (<c>HKLM\SOFTWARE\Foo</c>).
  /// </summary>
  /// <param name="path">The full key path, including the hive.</param>
  /// <returns>A writable handle to the key.</returns>
  public FakeRegistryKey CreateKey(string path)
  {
    ArgumentNullException.ThrowIfNull(path);

    var (hive, relativePath) = SplitHivePath(path);

    lock (SyncRoot)
    {
      var node = GetHiveRoot(hive).CreateSubKey(relativePath);
      return new FakeRegistryKey(this, node, writable: true, RegistryView.Default);
    }
  }

  /// <summary>
  /// Gets the names of the values stored directly on the key at <paramref name="path"/>.
  /// </summary>
  /// <param name="path">The full key path, including the hive.</param>
  /// <returns>The value names, or an empty array when the key does not exist.</returns>
  public string[] GetValueNames(string path)
  {
    ArgumentNullException.ThrowIfNull(path);

    var (hive, relativePath) = SplitHivePath(path);

    lock (SyncRoot)
    {
      return GetHiveRoot(hive).FindSubKey(relativePath) is { } node
        ? [.. node.ValueNames]
        : [];
    }
  }

  /// <summary>
  /// Gets whether the key at <paramref name="path"/> exists.
  /// </summary>
  /// <param name="path">The full key path, including the hive.</param>
  /// <returns><see langword="true"/> when the key exists; otherwise <see langword="false"/>.</returns>
  public bool KeyExists(string path)
  {
    ArgumentNullException.ThrowIfNull(path);

    var (hive, relativePath) = SplitHivePath(path);

    lock (SyncRoot)
    {
      return GetHiveRoot(hive).FindSubKey(relativePath) is not null;
    }
  }

  /// <inheritdoc/>
  public IRegistryKey OpenBaseKey(RegistryHive hive, RegistryView view)
  {
    lock (SyncRoot)
    {
      return new FakeRegistryKey(this, GetHiveRoot(hive), writable: false, view);
    }
  }

  /// <summary>
  /// Deletes everything stored in every hive.
  /// </summary>
  public void Reset()
  {
    lock (SyncRoot)
    {
      _hives.Clear();

      foreach (var hive in CreateHives(_nameComparer))
      {
        _hives[hive.Key] = hive.Value;
      }
    }
  }

  /// <summary>
  /// Writes a value to the key at <paramref name="keyPath"/>, creating the key if it does not exist.
  /// </summary>
  /// <param name="keyPath">The full key path, including the hive.</param>
  /// <param name="valueName">The name of the value to write.</param>
  /// <param name="value">The value to write.</param>
  public void SetValue(string keyPath, string valueName, object value)
  {
    using var key = CreateKey(keyPath);
    key.SetValue(valueName, value);
  }

  /// <summary>
  /// Writes a value to the key at <paramref name="keyPath"/> with an explicit kind, creating the key if it does not exist.
  /// </summary>
  /// <param name="keyPath">The full key path, including the hive.</param>
  /// <param name="valueName">The name of the value to write.</param>
  /// <param name="value">The value to write.</param>
  /// <param name="kind">The registry value kind to store.</param>
  public void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind)
  {
    using var key = CreateKey(keyPath);
    key.SetValue(valueName, value, kind);
  }

  /// <summary>
  /// Gets whether the value exists on the key at <paramref name="keyPath"/>.
  /// </summary>
  /// <param name="keyPath">The full key path, including the hive.</param>
  /// <param name="valueName">The name of the value to look for.</param>
  /// <returns><see langword="true"/> when the value exists; otherwise <see langword="false"/>.</returns>
  public bool ValueExists(string keyPath, string valueName)
  {
    ArgumentNullException.ThrowIfNull(keyPath);
    ArgumentNullException.ThrowIfNull(valueName);

    var (hive, relativePath) = SplitHivePath(keyPath);

    lock (SyncRoot)
    {
      return GetHiveRoot(hive).FindSubKey(relativePath)?.FindValue(valueName) is not null;
    }
  }

  private static Dictionary<RegistryHive, FakeRegistryNode> CreateHives(StringComparer nameComparer)
  {
    return new Dictionary<RegistryHive, FakeRegistryNode>
    {
      [RegistryHive.ClassesRoot] = new FakeRegistryNode("HKEY_CLASSES_ROOT", nameComparer),
      [RegistryHive.CurrentConfig] = new FakeRegistryNode("HKEY_CURRENT_CONFIG", nameComparer),
      [RegistryHive.CurrentUser] = new FakeRegistryNode("HKEY_CURRENT_USER", nameComparer),
      [RegistryHive.LocalMachine] = new FakeRegistryNode("HKEY_LOCAL_MACHINE", nameComparer),
      [RegistryHive.PerformanceData] = new FakeRegistryNode("HKEY_PERFORMANCE_DATA", nameComparer),
      [RegistryHive.Users] = new FakeRegistryNode("HKEY_USERS", nameComparer)
    };
  }

  private static (RegistryHive Hive, string RelativePath) SplitHivePath(string path)
  {
    var segments = FakeRegistryNode.SplitPath(path);

    if (segments.Length == 0 || !_hiveAliases.TryGetValue(segments[0], out var hive))
    {
      throw new ArgumentException(
        $"The path '{path}' does not start with a known registry hive. " +
        "Use a full name like HKEY_LOCAL_MACHINE or an alias like HKLM.",
        nameof(path));
    }

    return (hive, string.Join('\\', segments[1..]));
  }

  private FakeRegistryNode GetHiveRoot(RegistryHive hive)
  {
    return _hives.TryGetValue(hive, out var root)
      ? root
      : throw new ArgumentOutOfRangeException(nameof(hive), hive, "Unknown registry hive.");
  }
}

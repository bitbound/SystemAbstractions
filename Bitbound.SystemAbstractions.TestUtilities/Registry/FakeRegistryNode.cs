using Bitbound.SystemAbstractions.Windows.Registry;

namespace Bitbound.SystemAbstractions.TestUtilities.Registry;

internal sealed class FakeRegistryValue(object value, RegistryValueKind kind)
{
  public RegistryValueKind Kind { get; } = kind;

  public object Value { get; } = value;
}

/// <summary>
/// Shared, mutable storage for one registry key. <see cref="FakeRegistryKey"/> handles are thin, disposable
/// views onto a node, mirroring how <c>RegistryKey</c> handles behave.
/// </summary>
internal sealed class FakeRegistryNode(string name, StringComparer nameComparer)
{
  private readonly SortedDictionary<string, FakeRegistryNode> _subKeys = new(nameComparer);
  private readonly SortedDictionary<string, FakeRegistryValue> _values = new(nameComparer);

  public string Name { get; } = name;
  public int SubKeyCount => _subKeys.Count;
  public IEnumerable<string> SubKeyNames => _subKeys.Keys;
  public int ValueCount => _values.Count;
  public IEnumerable<string> ValueNames => _values.Keys;

  public static string CombinePath(string parentPath, string relativePath)
  {
    return $"{parentPath}\\{relativePath.Trim('\\')}";
  }

  public static string[] SplitPath(string path)
  {
    return path
      .Trim('\\')
      .Split(['\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }

  public FakeRegistryNode CreateSubKey(string path)
  {
    var current = this;

    foreach (var segment in SplitPath(path))
    {
      if (!current._subKeys.TryGetValue(segment, out var child))
      {
        child = new FakeRegistryNode(CombinePath(current.Name, segment), nameComparer);
        current._subKeys[segment] = child;
      }

      current = child;
    }

    return current;
  }

  public bool DeleteSubKey(string name)
  {
    return _subKeys.Remove(name);
  }

  public bool DeleteValue(string name)
  {
    return _values.Remove(name);
  }

  public FakeRegistryNode? FindSubKey(string path)
  {
    var current = this;

    foreach (var segment in SplitPath(path))
  {
      if (!current._subKeys.TryGetValue(segment, out var child))
      {
        return null;
      }

      current = child;
    }

    return current;
  }

  public FakeRegistryValue? FindValue(string name)
  {
    return _values.TryGetValue(name, out var value) ? value : null;
  }

  public bool HasSubKeys()
  {
    return _subKeys.Count > 0;
  }

  public void SetValue(string name, FakeRegistryValue value)
  {
    _values[name] = value;
  }
}

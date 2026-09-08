using Bitbound.SystemAbstractions.Windows.Registry;

namespace Bitbound.SystemAbstractions.TestUtilities.Registry;

/// <summary>
/// A disposable, in-memory handle onto a <see cref="FakeRegistry"/> key. Behaves like a <c>RegistryKey</c> handle:
/// reads work from any handle, writes need one that was created or opened as writable, and every member throws
/// <see cref="ObjectDisposedException"/> after the handle is disposed.
/// </summary>
public sealed class FakeRegistryKey : IRegistryKey
{
  private readonly FakeRegistryNode _node;
  private readonly FakeRegistry _registry;
  private readonly RegistryView _view;

  internal FakeRegistryKey(FakeRegistry registry, FakeRegistryNode node, bool writable, RegistryView view)
  {
    _registry = registry;
    _node = node;
    _view = view;
    IsWritable = writable;
  }

  /// <summary>
  /// Gets whether this handle has been disposed.
  /// </summary>
  public bool IsDisposed { get; private set; }

  /// <summary>
  /// Gets whether this handle was created or opened for writing. Mirrors the <c>writable</c> argument that produced it.
  /// </summary>
  public bool IsWritable { get; }

  /// <inheritdoc/>
  public string Name => ExecuteRead(() => _node.Name);

  /// <inheritdoc/>
  public int SubKeyCount => ExecuteRead(() => _node.SubKeyCount);

  /// <inheritdoc/>
  public int ValueCount => ExecuteRead(() => _node.ValueCount);

  /// <inheritdoc/>
  public RegistryView View => ExecuteRead(() => _view);

  /// <inheritdoc/>
  public IRegistryKey CreateSubKey(string subkey)
  {
    return CreateSubKey(subkey, writable: true);
  }

  /// <inheritdoc/>
  public IRegistryKey CreateSubKey(string subkey, bool writable)
  {
    ArgumentNullException.ThrowIfNull(subkey);

    return ExecuteWrite(() => new FakeRegistryKey(_registry, _node.CreateSubKey(subkey), writable, _view));
  }

  /// <inheritdoc/>
  public void DeleteSubKey(string subKey)
  {
    DeleteSubKey(subKey, throwOnMissingSubKey: true);
  }

  /// <inheritdoc/>
  public void DeleteSubKey(string subKey, bool throwOnMissingSubKey)
  {
    DeleteSubKeyInternal(subKey, throwOnMissingSubKey, recursive: false);
  }

  /// <inheritdoc/>
  public void DeleteSubKeyTree(string subKey)
  {
    DeleteSubKeyTree(subKey, throwOnMissingSubKey: true);
  }

  /// <inheritdoc/>
  public void DeleteSubKeyTree(string subKey, bool throwOnMissingSubKey)
  {
    DeleteSubKeyInternal(subKey, throwOnMissingSubKey, recursive: true);
  }

  /// <inheritdoc/>
  public void DeleteValue(string name)
  {
    DeleteValue(name, throwOnMissingValue: true);
  }

  /// <inheritdoc/>
  public void DeleteValue(string name, bool throwOnMissingValue)
  {
    ExecuteWrite(() =>
    {
      if (_node.FindValue(name) is null && throwOnMissingValue)
      {
        // The real registry throws without a parameter name, so the message matches it verbatim.
        throw new ArgumentException("No value exists with that name.");
      }

      _node.DeleteValue(name);
    });
  }

  /// <inheritdoc/>
  public void Dispose()
  {
    // Disposing a handle is idempotent and never removes the key itself, same as the real registry.
    IsDisposed = true;
  }

  /// <inheritdoc/>
  public string[] GetSubKeyNames()
  {
    return ExecuteRead<string[]>(() => [.. _node.SubKeyNames]);
  }

  /// <inheritdoc/>
  public object? GetValue(string name)
  {
    return ExecuteRead(() => ReadValueOrDefault(name, null));
  }

  /// <inheritdoc/>
  public object? GetValue(string name, object? defaultValue)
  {
    return ExecuteRead(() => ReadValueOrDefault(name, defaultValue));
  }

  /// <inheritdoc/>
  public RegistryValueKind GetValueKind(string valueName)
  {
    return ExecuteRead(() => _node.FindValue(valueName) is { } value
      ? value.Kind
      : throw new IOException("The specified registry key does not exist."));
  }

  /// <inheritdoc/>
  public string[] GetValueNames()
  {
    return ExecuteRead<string[]>(() => [.. _node.ValueNames]);
  }

  /// <inheritdoc/>
  public IRegistryKey? OpenSubKey(string name, bool writable = false)
  {
    return ExecuteRead(() => _node.FindSubKey(name) is { } child
      ? new FakeRegistryKey(_registry, child, writable, _view)
      : null);
  }

  /// <inheritdoc/>
  public void SetValue(string name, object value)
  {
    SetValueInternal(name, value, requestedKind: null);
  }

  /// <inheritdoc/>
  public void SetValue(string name, object value, RegistryValueKind kind)
  {
    SetValueInternal(name, value, kind);
  }

  private static void ThrowIfMissingSubKey(bool throwOnMissingSubKey)
  {
    if (throwOnMissingSubKey)
    {
      // The real registry throws without a parameter name, so the message matches it verbatim.
      throw new ArgumentException("Cannot delete a subkey tree because the subkey does not exist.");
    }
  }

  private void DeleteSubKeyInternal(string subKey, bool throwOnMissingSubKey, bool recursive)
  {
    ArgumentNullException.ThrowIfNull(subKey);

    ExecuteWrite(() =>
    {
      var segments = FakeRegistryNode.SplitPath(subKey);
      var parent = _node;

      for (var i = 0; i < segments.Length - 1; i++)
      {
        if (parent.FindSubKey(segments[i]) is not { } intermediate)
        {
          ThrowIfMissingSubKey(throwOnMissingSubKey);
          return;
        }

        parent = intermediate;
      }

      var name = segments.Length > 0 ? segments[^1] : string.Empty;
      if (name.Length == 0 || parent.FindSubKey(name) is null)
      {
        ThrowIfMissingSubKey(throwOnMissingSubKey);
        return;
      }

      if (!recursive && parent.FindSubKey(name)!.HasSubKeys())
      {
        throw new InvalidOperationException(
          "Registry key has subkeys and recursive removes are not supported by this method.");
      }

      parent.DeleteSubKey(name);
    });
  }

  private T ExecuteRead<T>(Func<T> action)
  {
    ThrowIfDisposed();

    lock (_registry.SyncRoot)
    {
      return action();
    }
  }

  private T ExecuteWrite<T>(Func<T> action)
  {
    ThrowIfDisposed();
    ThrowIfNotWritable();

    lock (_registry.SyncRoot)
    {
      return action();
    }
  }

  private void ExecuteWrite(Action action)
  {
    ExecuteWrite<object?>(() =>
    {
      action();
      return null;
    });
  }

  private object? ReadValueOrDefault(string name, object? defaultValue)
  {
    return _node.FindValue(name) is { } value ? FakeRegistryValueFactory.ReadValue(value) : defaultValue;
  }

  private void SetValueInternal(string name, object value, RegistryValueKind? requestedKind)
  {
    ExecuteWrite(() => _node.SetValue(name, FakeRegistryValueFactory.Create(value, requestedKind)));
  }

  private void ThrowIfDisposed()
  {
    if (IsDisposed)
    {
      throw new ObjectDisposedException(_node.Name, "Cannot access a closed registry key.");
    }
  }

  private void ThrowIfNotWritable()
  {
    if (!IsWritable)
    {
      throw new UnauthorizedAccessException("Cannot write to the registry key.");
    }
  }
}

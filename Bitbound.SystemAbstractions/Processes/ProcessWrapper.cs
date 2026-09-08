using System.Diagnostics;
using System.Runtime.Versioning;

namespace Bitbound.SystemAbstractions.Processes;

/// <summary>
/// <para>
///   An abstraction over <see cref="Process"/>. Use <see cref="ProcessWrapper"/> for the real thing.
/// </para>
/// <para>
///   Add members as needed.
/// </para>
/// </summary>
public interface IProcess : IDisposable
{
  event EventHandler<IProcess>? Exited;

  int BasePriority { get; }
  bool EnableRaisingEvents { get; set; }
  int ExitCode { get; }
  DateTime ExitTime { get; }
  string? FilePath { get; }
  nint Handle { get; }
  int HandleCount { get; }
  bool HasExited { get; }
  int Id { get; }
  string MachineName { get; }
  string ProcessName { get; }

  [SupportedOSPlatform("windows")]
  [SupportedOSPlatform("linux")]
  nint ProcessorAffinity { get; set; }

  bool Responding { get; }
  int SessionId { get; }
  StreamWriter StandardInput { get; }
  StreamReader StandardOutput { get; }
  ProcessStartInfo StartInfo { get; set; }

  void Kill();
  void Kill(bool entireProcessTree);
  Task WaitForExitAsync(CancellationToken cancellationToken);
}

internal sealed class ProcessWrapper(Process process) : IProcess
{
  private readonly object _exitedSyncRoot = new();
  private readonly Process _process = process;

  private bool _disposed;
  private bool _isSubscribedToProcessExited;

  public event EventHandler<IProcess>? Exited
  {
    add
    {
      if (value is null)
      {
        return;
      }

      lock (_exitedSyncRoot)
      {
        ExitedPrivate += value;

        if (_isSubscribedToProcessExited)
        {
          return;
        }

        _process.Exited += HandleProcessExited;
        _isSubscribedToProcessExited = true;
      }
    }
    remove
    {
      if (value is null)
      {
        return;
      }

      lock (_exitedSyncRoot)
      {
        ExitedPrivate -= value;

        if (ExitedPrivate is not null || !_isSubscribedToProcessExited)
        {
          return;
        }

        _process.Exited -= HandleProcessExited;
        _isSubscribedToProcessExited = false;
      }
    }
  }

  private event EventHandler<IProcess>? ExitedPrivate;

  public int BasePriority => _process.BasePriority;

  public bool EnableRaisingEvents
  {
    get => _process.EnableRaisingEvents;
    set => _process.EnableRaisingEvents = value;
  }

  public int ExitCode => _process.ExitCode;

  public DateTime ExitTime => _process.ExitTime;

  public string? FilePath
  {
    get
    {
      try
      {
        return _process.MainModule?.FileName;
      }
      catch
      {
        return null;
      }
    }
  }

  public nint Handle => _process.Handle;

  public int HandleCount => _process.HandleCount;

  public bool HasExited => _process.HasExited;

  public int Id => _process.Id;

  public string MachineName => _process.MachineName;

  public string ProcessName => _process.ProcessName;

  [SupportedOSPlatform("windows")]
  [SupportedOSPlatform("linux")]
  public nint ProcessorAffinity
  {
    get => _process.ProcessorAffinity;
    set => _process.ProcessorAffinity = value;
  }

  public bool Responding => _process.Responding;

  public int SessionId => _process.SessionId;

  public StreamWriter StandardInput => _process.StandardInput;

  public StreamReader StandardOutput => _process.StandardOutput;

  public ProcessStartInfo StartInfo
  {
    get => _process.StartInfo;
    set => _process.StartInfo = value;
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    lock (_exitedSyncRoot)
    {
      if (_isSubscribedToProcessExited)
      {
        _process.Exited -= HandleProcessExited;
        _isSubscribedToProcessExited = false;
      }

      ExitedPrivate = null;
    }

    _process.Dispose();
    _disposed = true;
    GC.SuppressFinalize(this);
  }

  public void Kill()
  {
    _process.Kill();
  }

  public void Kill(bool entireProcessTree)
  {
    _process.Kill(entireProcessTree);
  }

  public Task WaitForExitAsync(CancellationToken cancellationToken)
  {
    return _process.WaitForExitAsync(cancellationToken);
  }

  private void HandleProcessExited(object? sender, EventArgs args)
  {
    EventHandler<IProcess>? exitedHandlers;

    lock (_exitedSyncRoot)
    {
      exitedHandlers = ExitedPrivate;
    }

    exitedHandlers?.Invoke(this, this);
  }
}

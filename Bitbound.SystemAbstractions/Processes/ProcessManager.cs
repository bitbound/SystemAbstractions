using System.Diagnostics;
using Bitbound.SystemAbstractions.Primitives;

namespace Bitbound.SystemAbstractions.Processes;

/// <summary>
/// Creates, inspects, and waits on operating system processes.
/// </summary>
public interface IProcessManager
{
  IProcess GetCurrentProcess();
  int GetCurrentSessionId();
  IProcess GetProcessById(int processId);
  IProcess[] GetProcesses();
  IProcess[] GetProcessesByName(string processName);

  /// <summary>
  /// Runs a command and returns its standard output.
  /// </summary>
  /// <param name="command">The executable to run.</param>
  /// <param name="arguments">The arguments to pass to the executable.</param>
  /// <param name="timeoutMs">How long to wait for the command to exit, in milliseconds.</param>
  /// <returns>The command's standard output, or a failure describing why the command did not produce output.</returns>
  Task<Result<string>> GetProcessOutput(string command, string arguments, int timeoutMs = 10_000);

  IProcess? LaunchUri(Uri uri);
  IProcess Start(string fileName);
  IProcess Start(string fileName, string arguments);
  IProcess? Start(string fileName, string arguments, bool useShellExec);
  IProcess? Start(ProcessStartInfo startInfo);

  /// <summary>
  /// Starts a process and waits for it to exit.
  /// </summary>
  /// <param name="startInfo">The start information for the process.</param>
  /// <param name="timeout">How long to wait for the process to exit.</param>
  /// <returns>The process exit code.</returns>
  /// <exception cref="ProcessStatusException">Thrown when the process exits with a non-zero exit code.</exception>
  Task<int> StartAndWaitForExit(ProcessStartInfo startInfo, TimeSpan timeout);

  Task<int> StartAndWaitForExit(string fileName, string arguments, bool useShellExec, TimeSpan timeout);
  Task<int> StartAndWaitForExit(string fileName, string arguments, bool useShellExec, CancellationToken cancellationToken);
}

internal sealed class ProcessManager : IProcessManager
{
  private int? _currentSessionId;

  public IProcess GetCurrentProcess()
  {
    return new ProcessWrapper(Process.GetCurrentProcess());
  }

  public int GetCurrentSessionId()
  {
    return _currentSessionId ??= Process.GetCurrentProcess().SessionId;
  }

  public IProcess GetProcessById(int processId)
  {
    return new ProcessWrapper(Process.GetProcessById(processId));
  }

  public IProcess[] GetProcesses()
  {
    return [.. Process.GetProcesses().Select(p => new ProcessWrapper(p))];
  }

  public IProcess[] GetProcessesByName(string processName)
  {
    return [.. Process.GetProcessesByName(processName).Select(p => new ProcessWrapper(p))];
  }

  public async Task<Result<string>> GetProcessOutput(string command, string arguments, int timeoutMs = 10_000)
  {
    try
    {
      var startInfo = new ProcessStartInfo(command, arguments)
      {
        WindowStyle = ProcessWindowStyle.Hidden,
        UseShellExecute = false,
        RedirectStandardOutput = true
      };

      using var process = Process.Start(startInfo);

      if (process is null)
      {
        return Result.Fail<string>("Process failed to start.");
      }

      using var cts = new CancellationTokenSource(timeoutMs);
      var outputTask = process.StandardOutput.ReadToEndAsync();

      await process.WaitForExitAsync(cts.Token);

      var output = await outputTask;
      return Result.Ok(output);
    }
    catch (OperationCanceledException)
    {
      return Result.Fail<string>(
        $"Timed out while waiting for command to finish.  Command: {command}.  Arguments: {arguments}");
    }
    catch (Exception ex)
    {
      return Result.Fail<string>(ex);
    }
  }

  public IProcess? LaunchUri(Uri uri)
  {
    ArgumentNullException.ThrowIfNull(uri);

    var startInfo = new ProcessStartInfo
    {
      FileName = $"{uri}",
      UseShellExecute = true
    };
    var process = Process.Start(startInfo);
    return process is not null ? new ProcessWrapper(process) : null;
  }

  public IProcess Start(string fileName)
  {
    var process = Process.Start(fileName);
    ArgumentNullException.ThrowIfNull(process);
    return new ProcessWrapper(process);
  }

  public IProcess Start(string fileName, string arguments)
  {
    var process = Process.Start(fileName, arguments);
    ArgumentNullException.ThrowIfNull(process);
    return new ProcessWrapper(process);
  }

  public IProcess? Start(string fileName, string arguments, bool useShellExec)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = fileName,
      Arguments = arguments,
      UseShellExecute = useShellExec
    };
    var process = Process.Start(startInfo);
    return process is not null ? new ProcessWrapper(process) : null;
  }

  public IProcess? Start(ProcessStartInfo startInfo)
  {
    ArgumentNullException.ThrowIfNull(startInfo);

    var process = Process.Start(startInfo);
    return process is not null ? new ProcessWrapper(process) : null;
  }

  public async Task<int> StartAndWaitForExit(ProcessStartInfo startInfo, TimeSpan timeout)
  {
    using var process = Process.Start(startInfo);
    ArgumentNullException.ThrowIfNull(process);

    using var cts = new CancellationTokenSource(timeout);
    await process.WaitForExitAsync(cts.Token);

    if (process.ExitCode != 0)
    {
      throw new ProcessStatusException(process.ExitCode);
    }

    return process.ExitCode;
  }

  public async Task<int> StartAndWaitForExit(string fileName, string arguments, bool useShellExec, TimeSpan timeout)
  {
    using var cts = new CancellationTokenSource(timeout);
    return await StartAndWaitForExit(fileName, arguments, useShellExec, cts.Token);
  }

  public async Task<int> StartAndWaitForExit(
    string fileName,
    string arguments,
    bool useShellExec,
    CancellationToken cancellationToken)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = fileName,
      Arguments = arguments,
      UseShellExecute = useShellExec
    };

    using var process = Process.Start(startInfo);
    ArgumentNullException.ThrowIfNull(process);

    await process.WaitForExitAsync(cancellationToken);
    return process.ExitCode;
  }
}

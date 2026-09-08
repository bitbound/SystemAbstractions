using System.Diagnostics;

using Bitbound.SystemAbstractions.Processes;
using Microsoft.Extensions.DependencyInjection;

namespace Bitbound.SystemAbstractions.Tests.Processes;

public class ProcessWrapperTests
{
  private readonly IProcessManager _manager = new ServiceCollection()
    .AddProcesses()
    .BuildServiceProvider()
    .GetRequiredService<IProcessManager>();

  [Fact]
  public void Dispose_WhenCalledTwice_DoesNotThrow()
  {
    using var process = _manager.Start(Shell("exit 0"))!;

    process.Dispose();
    process.Dispose();
  }

  [Fact]
  public void EnableRaisingEvents_WhenSetToTrue_IsReadBackAsTrue()
  {
    var process = _manager.GetCurrentProcess();

    process.EnableRaisingEvents = true;

    Assert.True(process.EnableRaisingEvents);
    process.EnableRaisingEvents = false;
  }

  [Fact]
  public async Task Exited_WhenProcessFinishes_FiresWithTheWrapperAndItsExitCode()
  {
    // The child lingers briefly so the subscription below always happens before it exits.
    using var process = _manager.Start(LingerThenExit(7))!;
    process.EnableRaisingEvents = true;

    using var completion = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
      TestContext.Current.CancellationToken,
      completion.Token);

    var exited = new TaskCompletionSource<IProcess>(TaskCreationOptions.RunContinuationsAsynchronously);
    using var registration = linked.Token.Register(() => exited.TrySetCanceled());

    void Handler(object? sender, IProcess exitedProcess) => exited.TrySetResult(exitedProcess);

    process.Exited += Handler;

    var result = await exited.Task;
    process.Exited -= Handler;

    Assert.Same(process, result);
    Assert.Equal(7, result.ExitCode);
  }

  [Fact]
  public void GetCurrentProcess_WhenPropertiesAreRead_ReportsTheRunningProcess()
  {
    var process = _manager.GetCurrentProcess();

    Assert.Equal(Environment.ProcessId, process.Id);
    // .NET reports "." rather than the host name for a process running on the local machine.
    Assert.False(string.IsNullOrEmpty(process.MachineName));
    Assert.False(string.IsNullOrEmpty(process.ProcessName));
    Assert.True(process.Handle != 0);
    Assert.True(process.HandleCount > 0);
    Assert.True(process.BasePriority > 0);
    Assert.True(process.SessionId >= 0);
    Assert.True(
      string.Equals(Environment.ProcessPath, process.FilePath, StringComparison.OrdinalIgnoreCase),
      $"expected {Environment.ProcessPath}, got {process.FilePath}");
  }

  [Fact]
  public async Task HasExited_WhenTheProcessFinishes_ReportsTrue()
  {
    using var process = _manager.Start(Shell("exit 0"))!;

    await process.WaitForExitAsync(TestContext.Current.CancellationToken);

    Assert.True(process.HasExited);
    Assert.Equal(0, process.ExitCode);
    Assert.True(process.ExitTime <= DateTime.Now);
  }

  [Fact]
  public async Task KillAndDispose_WhenTheProcessAlreadyExited_DoesNotThrow()
  {
    var process = _manager.Start(Shell("exit 0"))!;
    await process.WaitForExitAsync(TestContext.Current.CancellationToken);

    process.KillAndDispose();
    process.KillAndDispose();
  }

  [Fact]
  public void KillAndDispose_WhenTheProcessIsStillRunning_KillsAndDisposesIt()
  {
    var process = _manager.Start(SleepForAWhile())!;

    Assert.True(process.Id > 0);

    process.KillAndDispose();

    Assert.ThrowsAny<Exception>(() => _ = process.HasExited);
  }

  [Fact]
  public async Task Kill_WhenTheProcessIsStillRunning_StopsIt()
  {
    using var process = _manager.Start(SleepForAWhile())!;

    process.Kill(entireProcessTree: true);
    await process.WaitForExitAsync(TestContext.Current.CancellationToken);

    Assert.True(process.HasExited);
  }

  [Fact]
  public void ProcessStatusException_WhenConstructedWithAStatusCode_ReportsItInTheMessage()
  {
    var exception = new ProcessStatusException(42);

    Assert.Equal(42, exception.StatusCode);
    Assert.Equal("Process exited with status code 42", exception.Message);
  }

  [Fact]
  public async Task StandardInput_WhenWrittenTo_IsReadByTheProcess()
  {
    var startInfo = OperatingSystem.IsWindows()
      ? new ProcessStartInfo("findstr", ".")
      : new ProcessStartInfo("/bin/cat", string.Empty);

    startInfo.UseShellExecute = false;
    startInfo.RedirectStandardInput = true;
    startInfo.RedirectStandardOutput = true;

    using var process = _manager.Start(startInfo)!;

    await process.StandardInput.WriteAsync("piped through stdin");
    process.StandardInput.Close();

    var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
    await process.WaitForExitAsync(TestContext.Current.CancellationToken);

    Assert.Contains("piped through stdin", output);
  }

  [Fact]
  public void StartInfo_WhenRead_BackReportsTheStartInfoThatWasUsed()
  {
    var startInfo = Shell("exit 0");

    using var process = _manager.Start(startInfo)!;

    Assert.Same(startInfo, process.StartInfo);
  }

  [Fact]
  public async Task Start_WhenProcessWritesToStandardOutput_ReadersSeeTheLine()
  {
    var startInfo = Shell("echo hello from stdout");
    startInfo.RedirectStandardOutput = true;

    using var process = _manager.Start(startInfo)!;
    var output = process.StandardOutput.ReadToEnd();
    await process.WaitForExitAsync(TestContext.Current.CancellationToken);

    Assert.Contains("hello from stdout", output);
  }

  private static ProcessStartInfo LingerThenExit(int exitCode)
  {
    return OperatingSystem.IsWindows()
      ? Shell($"ping -n 3 127.0.0.1 > nul & exit {exitCode}")
      : Shell($"sleep 3; exit {exitCode}");
  }

  private static ProcessStartInfo Shell(string command)
  {
    return OperatingSystem.IsWindows()
      ? new ProcessStartInfo("cmd.exe", $"/c {command}") { UseShellExecute = false }
      : new ProcessStartInfo("/bin/sh", $"-c \"{command}\"") { UseShellExecute = false };
  }

  private static ProcessStartInfo SleepForAWhile()
  {
    return OperatingSystem.IsWindows()
      ? new ProcessStartInfo("cmd.exe", "/c ping -n 30 127.0.0.1 > nul") { UseShellExecute = false }
      : new ProcessStartInfo("/bin/sh", "-c \"sleep 30\"") { UseShellExecute = false };
  }
}

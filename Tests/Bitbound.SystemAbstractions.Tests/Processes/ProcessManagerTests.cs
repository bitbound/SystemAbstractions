using System.Diagnostics;

using Bitbound.SystemAbstractions.Processes;
using Microsoft.Extensions.DependencyInjection;

namespace Bitbound.SystemAbstractions.Tests.Processes;

public class ProcessManagerTests
{
  private readonly IProcessManager _manager = new ServiceCollection()
    .AddProcesses()
    .BuildServiceProvider()
    .GetRequiredService<IProcessManager>();

  [Fact]
  public void GetCurrentProcess_WhenCalled_ReportsTheCurrentProcessId()
  {
    var process = _manager.GetCurrentProcess();

    Assert.Equal(Environment.ProcessId, process.Id);
  }

  [Fact]
  public void GetCurrentSessionId_WhenCalledTwice_ReturnsTheSameValue()
  {
    Assert.Equal(_manager.GetCurrentSessionId(), _manager.GetCurrentSessionId());
  }

  [Fact]
  public void GetProcessById_WhenIdIsTheCurrentProcess_ReturnsAWrapperForIt()
  {
    var process = _manager.GetProcessById(Environment.ProcessId);

    Assert.Equal(Environment.ProcessId, process.Id);
  }

  [Fact]
  public void GetProcessesByName_WhenNothingMatchesTheName_ReturnsNoProcesses()
  {
    Assert.Empty(_manager.GetProcessesByName("bitbound-no-such-process-9f3a2b"));
  }

  [Fact]
  public void GetProcesses_WhenQueried_IncludesTheCurrentProcess()
  {
    Assert.Contains(_manager.GetProcesses(), process => process.Id == Environment.ProcessId);
  }

  [Fact]
  public async Task GetProcessOutput_WhenCommandCannotBeFound_FailsWithTheThrownException()
  {
    var result = await _manager.GetProcessOutput("bitbound-no-such-command-9f3a2b", string.Empty);

    Assert.False(result.IsSuccess);
    Assert.True(result.HadException);
    Assert.NotNull(result.Exception);
  }

  [Fact]
  public async Task GetProcessOutput_WhenCommandOutlivesTheTimeout_FailsWithATimeoutReason()
  {
    var (command, arguments) = OperatingSystem.IsWindows()
      ? ("cmd.exe", "/c ping -n 20 127.0.0.1")
      : ("/bin/sleep", "20");

    var result = await _manager.GetProcessOutput(command, arguments, timeoutMs: 500);

    Assert.False(result.IsSuccess);
    Assert.Contains("Timed out", result.Reason);
  }

  [Fact]
  public async Task GetProcessOutput_WhenCommandWritesNothing_ReturnsOkWithEmptyOutput()
  {
    var (command, arguments) = OperatingSystem.IsWindows()
      ? ("cmd.exe", "/c exit 0")
      : ("/bin/sh", "-c \"exit 0\"");

    var result = await _manager.GetProcessOutput(command, arguments);

    Assert.True(result.IsSuccess, result.Reason);
    Assert.Equal(string.Empty, result.Value);
  }

  [Fact]
  public async Task GetProcessOutput_WhenCommandWritesToStdout_ReturnsOkWithThatOutput()
  {
    var (command, arguments) = Echo("hello from the process manager");

    var result = await _manager.GetProcessOutput(command, arguments);

    Assert.True(result.IsSuccess, result.Reason);
    Assert.Contains("hello from the process manager", result.Value);
  }

  [Fact]
  public void LaunchUri_WhenUriIsNull_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => _manager.LaunchUri(null!));
  }

  [Fact]
  public async Task StartAndWaitForExit_WhenCancellationTokenIsUsed_ReturnsTheExitCode()
  {
    var (fileName, arguments) = OperatingSystem.IsWindows()
      ? ("cmd.exe", "/c exit 0")
      : ("/bin/sh", "-c \"exit 0\"");

    var exitCode = await _manager.StartAndWaitForExit(
      fileName,
      arguments,
      useShellExec: false,
      TestContext.Current.CancellationToken);

    Assert.Equal(0, exitCode);
  }

  [Fact]
  public async Task StartAndWaitForExit_WhenCommandCannotBeFound_Throws()
  {
    await Assert.ThrowsAnyAsync<Exception>(
      () => _manager.StartAndWaitForExit(
        "bitbound-no-such-command-9f3a2b",
        string.Empty,
        useShellExec: false,
        TimeSpan.FromSeconds(5)));
  }

  [Fact]
  public async Task StartAndWaitForExit_WhenCommandFails_ThrowsProcessStatusExceptionWithTheExitCode()
  {
    var exception = await Assert.ThrowsAsync<ProcessStatusException>(
      () => _manager.StartAndWaitForExit(Shell("exit 3"), TimeSpan.FromSeconds(30)));

    Assert.Equal(3, exception.StatusCode);
    Assert.Contains("3", exception.Message);
  }

  [Fact]
  public async Task StartAndWaitForExit_WhenCommandSucceeds_ReturnsZero()
  {
    var exitCode = await _manager.StartAndWaitForExit(Shell("exit 0"), TimeSpan.FromSeconds(30));

    Assert.Equal(0, exitCode);
  }

  [Fact]
  public void Start_WhenStartInfoIsNull_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => _manager.Start((ProcessStartInfo)null!));
  }

  private static (string Command, string Arguments) Echo(string message)
  {
    return OperatingSystem.IsWindows()
      ? ("cmd.exe", $"/c echo {message}")
      : ("/bin/echo", message);
  }

  private static ProcessStartInfo Shell(string command)
  {
    return OperatingSystem.IsWindows()
      ? new ProcessStartInfo("cmd.exe", $"/c {command}") { UseShellExecute = false }
      : new ProcessStartInfo("/bin/sh", $"-c \"{command}\"") { UseShellExecute = false };
  }
}

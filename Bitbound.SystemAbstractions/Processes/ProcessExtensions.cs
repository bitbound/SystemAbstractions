namespace Bitbound.SystemAbstractions.Processes;

/// <summary>
/// Convenience helpers for <see cref="IProcess"/>.
/// </summary>
public static class ProcessExtensions
{
  /// <summary>
  /// Kills the process and disposes it, ignoring failures from either step.
  /// </summary>
  /// <param name="process">The process to tear down.</param>
  public static void KillAndDispose(this IProcess process)
  {
    ArgumentNullException.ThrowIfNull(process);

    try
    {
      process.Kill();
    }
    catch
    {
      // The process may already be gone.
    }

    try
    {
      process.Dispose();
    }
    catch
    {
      // Disposal failures during teardown are not actionable.
    }
  }
}

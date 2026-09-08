namespace Bitbound.SystemAbstractions.Processes;

/// <summary>
/// Thrown when a process exits with a non-zero status code.
/// </summary>
/// <param name="statusCode">The exit code the process returned.</param>
public class ProcessStatusException(int statusCode) : Exception
{

  /// <inheritdoc/>
  public override string Message => $"Process exited with status code {StatusCode}";

  /// <summary>
  /// Gets the exit code the process returned.
  /// </summary>
  public int StatusCode { get; } = statusCode;
}

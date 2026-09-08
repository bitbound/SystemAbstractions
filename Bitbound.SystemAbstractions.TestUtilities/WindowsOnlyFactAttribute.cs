using Xunit;

namespace Bitbound.SystemAbstractions.TestUtilities;

/// <summary>
/// Xunit attribute to skip tests when not running on Windows.
/// </summary>
public class WindowsOnlyFactAttribute : FactAttribute
{
  public WindowsOnlyFactAttribute()
  {
    if (!OperatingSystem.IsWindows())
    {
      Skip = "Test only runs on Windows";
    }
  }
}
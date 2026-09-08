using Xunit;

namespace Bitbound.SystemAbstractions.TestUtilities;

/// <summary>
/// Xunit attribute to skip tests when not running on macOS.
/// </summary>
public class MacOnlyFactAttribute : FactAttribute
{
  public MacOnlyFactAttribute()
  {
    if (!OperatingSystem.IsMacOS())
    {
      Skip = "Test only runs on macOS";
    }
  }
}
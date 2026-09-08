using Bitbound.SystemAbstractions.TestUtilities;

namespace Bitbound.SystemAbstractions.Tests.TestUtilities;

public class PlatformAttributeTests
{
  [Fact]
  public void InteractiveWindowsFactAttribute_WhenNotRunningOnWindows_CarriesTheSkipReason()
  {
    WithEnvironmentVariable("CI", null, () =>
    {
      WithEnvironmentVariable("GITHUB_ACTIONS", null, () =>
      {
        WithEnvironmentVariable("TF_BUILD", null, () =>
        {
          var attribute = new InteractiveWindowsFactAttribute();

          AssertSkipsOnOtherPlatforms(
            attribute.Skip,
            OperatingSystem.IsWindows() && Environment.UserInteractive,
            "Test only runs on interactive Windows sessions");
        });
      });
    });
  }

  [Fact]
  public void InteractiveWindowsFactAttribute_WhenTheCIVariableIsSet_CarriesTheSkipReason()
  {
    WithEnvironmentVariable("CI", "true", () =>
    {
      var attribute = new InteractiveWindowsFactAttribute();

      Assert.Equal("Test only runs on interactive Windows sessions", attribute.Skip);
    });
  }

  [Fact]
  public void LinuxOnlyFactAttribute_WhenNotRunningOnLinux_CarriesTheSkipReason()
  {
    var attribute = new LinuxOnlyFactAttribute();

    AssertSkipsOnOtherPlatforms(attribute.Skip, OperatingSystem.IsLinux(), "Test only runs on Linux");
  }

  [Fact]
  public void LinuxOnlyTheoryAttribute_WhenNotRunningOnLinux_CarriesTheSkipReason()
  {
    var attribute = new LinuxOnlyTheoryAttribute();

    AssertSkipsOnOtherPlatforms(attribute.Skip, OperatingSystem.IsLinux(), "Test only runs on Linux");
  }

  [Fact]
  public void MacKeychainIntegrationFactAttribute_WhenNotRunningOnMacOS_DoesNotSkip()
  {
    if (OperatingSystem.IsMacOS())
    {
      return;
    }

    var attribute = new MacKeychainIntegrationFactAttribute();

    Assert.Null(attribute.Skip);
  }

  [Fact]
  public void MacKeychainIntegrationFactAttribute_WhenOnMacOSInCI_CarriesTheSkipReason()
  {
    WithEnvironmentVariable("CI", "true", () =>
    {
      var attribute = new MacKeychainIntegrationFactAttribute();

      if (OperatingSystem.IsMacOS())
      {
        Assert.Equal("Test requires interactive macOS Keychain access", attribute.Skip);
      }
      else
      {
        Assert.Null(attribute.Skip);
      }
    });
  }

  [Fact]
  public void MacOnlyFactAttribute_WhenNotRunningOnMacOS_CarriesTheSkipReason()
  {
    var attribute = new MacOnlyFactAttribute();

    AssertSkipsOnOtherPlatforms(attribute.Skip, OperatingSystem.IsMacOS(), "Test only runs on macOS");
  }

  [Fact]
  public void MacOnlyTheoryAttribute_WhenNotRunningOnMacOS_CarriesTheSkipReason()
  {
    var attribute = new MacOnlyTheoryAttribute();

    AssertSkipsOnOtherPlatforms(attribute.Skip, OperatingSystem.IsMacOS(), "Test only runs on MacOS");
  }

  [Fact]
  public void WaylandOnlyFactAttribute_WhenTheDisplayIsWayland_DoesNotSkip()
  {
    WithEnvironmentVariable("WAYLAND_DISPLAY", "wayland-0", () =>
    {
      var attribute = new WaylandOnlyFactAttribute();

      Assert.Null(attribute.Skip);
    });
  }

  [Fact]
  public void WaylandOnlyFactAttribute_WhenTheSessionTypeIsWayland_DoesNotSkip()
  {
    WithEnvironmentVariable("WAYLAND_DISPLAY", null, () =>
    {
      WithEnvironmentVariable("XDG_SESSION_TYPE", "wayland", () =>
      {
        var attribute = new WaylandOnlyFactAttribute();

        Assert.Null(attribute.Skip);
      });
    });
  }

  [Fact]
  public void WaylandOnlyFactAttribute_WhenTheSessionTypeIsX11_CarriesTheSkipReason()
  {
    WithEnvironmentVariable("WAYLAND_DISPLAY", null, () =>
    {
      WithEnvironmentVariable("XDG_SESSION_TYPE", "x11", () =>
      {
        var attribute = new WaylandOnlyFactAttribute();

        Assert.Equal("Test only runs on Wayland desktop sessions", attribute.Skip);
      });
    });
  }

  [Fact]
  public void WindowsOnlyFactAttribute_WhenNotRunningOnWindows_CarriesTheSkipReason()
  {
    var attribute = new WindowsOnlyFactAttribute();

    AssertSkipsOnOtherPlatforms(attribute.Skip, OperatingSystem.IsWindows(), "Test only runs on Windows");
  }

  [Fact]
  public void WindowsOnlyTheoryAttribute_WhenNotRunningOnWindows_CarriesTheSkipReason()
  {
    var attribute = new WindowsOnlyTheoryAttribute();

    AssertSkipsOnOtherPlatforms(attribute.Skip, OperatingSystem.IsWindows(), "Test only runs on Windows");
  }

  private static void AssertSkipsOnOtherPlatforms(string? skipReason, bool isCurrentPlatform, string expectedReason)
  {
    if (isCurrentPlatform)
    {
      Assert.Null(skipReason);
      return;
    }

    Assert.Equal(expectedReason, skipReason);
  }

  private static void WithEnvironmentVariable(string name, string? value, Action assertion)
  {
    var original = Environment.GetEnvironmentVariable(name);

    try
    {
      Environment.SetEnvironmentVariable(name, value);
      assertion();
    }
    finally
    {
      Environment.SetEnvironmentVariable(name, original);
    }
  }
}

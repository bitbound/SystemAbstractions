using System.Runtime.Versioning;

using Win32RegistryHive = Microsoft.Win32.RegistryHive;
using Win32RegistryValueKind = Microsoft.Win32.RegistryValueKind;
using Win32RegistryView = Microsoft.Win32.RegistryView;

namespace Bitbound.SystemAbstractions.Windows.Registry;

[SupportedOSPlatform("windows")]
internal static class RegistryConversions
{
  public static RegistryValueKind ToAbstractionValueKind(Win32RegistryValueKind valueKind)
  {
    return valueKind switch
    {
      Win32RegistryValueKind.Unknown => RegistryValueKind.Unknown,
      Win32RegistryValueKind.String => RegistryValueKind.String,
      Win32RegistryValueKind.ExpandString => RegistryValueKind.ExpandString,
      Win32RegistryValueKind.Binary => RegistryValueKind.Binary,
      Win32RegistryValueKind.DWord => RegistryValueKind.DWord,
      Win32RegistryValueKind.MultiString => RegistryValueKind.MultiString,
      Win32RegistryValueKind.QWord => RegistryValueKind.QWord,
      _ => throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, "Unknown registry value kind.")
    };
  }

  public static RegistryView ToAbstractionView(Win32RegistryView view)
  {
    return view switch
    {
      Win32RegistryView.Default => RegistryView.Default,
      Win32RegistryView.Registry64 => RegistryView.Registry64,
      Win32RegistryView.Registry32 => RegistryView.Registry32,
      _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unknown registry view.")
    };
  }

  public static Win32RegistryHive ToWin32Hive(RegistryHive hive)
  {
    return hive switch
    {
      RegistryHive.ClassesRoot => Win32RegistryHive.ClassesRoot,
      RegistryHive.CurrentConfig => Win32RegistryHive.CurrentConfig,
      RegistryHive.CurrentUser => Win32RegistryHive.CurrentUser,
      RegistryHive.LocalMachine => Win32RegistryHive.LocalMachine,
      RegistryHive.PerformanceData => Win32RegistryHive.PerformanceData,
      RegistryHive.Users => Win32RegistryHive.Users,
      _ => throw new ArgumentOutOfRangeException(nameof(hive), hive, "Unknown registry hive.")
    };
  }

  public static Win32RegistryValueKind ToWin32ValueKind(RegistryValueKind valueKind)
  {
    return valueKind switch
    {
      RegistryValueKind.Unknown => Win32RegistryValueKind.Unknown,
      RegistryValueKind.String => Win32RegistryValueKind.String,
      RegistryValueKind.ExpandString => Win32RegistryValueKind.ExpandString,
      RegistryValueKind.Binary => Win32RegistryValueKind.Binary,
      RegistryValueKind.DWord => Win32RegistryValueKind.DWord,
      RegistryValueKind.MultiString => Win32RegistryValueKind.MultiString,
      RegistryValueKind.QWord => Win32RegistryValueKind.QWord,
      _ => throw new ArgumentOutOfRangeException(nameof(valueKind), valueKind, "Unknown registry value kind.")
    };
  }

  public static Win32RegistryView ToWin32View(RegistryView view)
  {
    return view switch
    {
      RegistryView.Default => Win32RegistryView.Default,
      RegistryView.Registry64 => Win32RegistryView.Registry64,
      RegistryView.Registry32 => Win32RegistryView.Registry32,
      _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unknown registry view.")
    };
  }
}

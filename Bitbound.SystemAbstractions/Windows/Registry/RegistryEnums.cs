namespace Bitbound.SystemAbstractions.Windows.Registry;

/// <summary>
/// A predefined root key in the Windows registry. Values match <see cref="Microsoft.Win32.RegistryHive"/>.
/// </summary>
public enum RegistryHive
{
  ClassesRoot = unchecked(-2147483648),
  CurrentConfig = unchecked(-2147483647),
  CurrentUser = unchecked(-2147483646),
  LocalMachine = unchecked(-2147483644),
  PerformanceData = unchecked(-2147483643),
  Users = unchecked(-2147483642)
}

/// <summary>
/// Which registry view to use when the operating system is 64-bit. Values match <see cref="Microsoft.Win32.RegistryView"/>.
/// </summary>
public enum RegistryView
{
  Default = 0,
  Registry64 = 0x100,
  Registry32 = 0x200
}

/// <summary>
/// The data type stored with a registry value. Values match <see cref="Microsoft.Win32.RegistryValueKind"/>.
/// </summary>
public enum RegistryValueKind
{
  Unknown = 0,
  String = 1,
  ExpandString = 2,
  Binary = 3,
  DWord = 4,
  MultiString = 7,
  QWord = 11
}

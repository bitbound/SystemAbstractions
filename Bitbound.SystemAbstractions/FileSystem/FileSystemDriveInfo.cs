namespace Bitbound.SystemAbstractions.FileSystem;

/// <summary>
/// Represents a mountable volume. Mirrors the members of <see cref="DriveInfo"/> that are useful behind an abstraction.
/// </summary>
public interface IFileSystemDrive
{
  string DriveFormat { get; }
  DriveType DriveType { get; }
  bool IsReady { get; }
  string Name { get; }
  IFileSystemDirectory RootDirectory { get; }
  long TotalFreeSpace { get; }
  long TotalSize { get; }
  string VolumeLabel { get; }
}

internal sealed class FileSystemDriveInfo(DriveInfo driveInfo) : IFileSystemDrive
{
  public string DriveFormat => driveInfo.DriveFormat;

  public DriveType DriveType => driveInfo.DriveType;

  public bool IsReady => driveInfo.IsReady;

  public string Name => driveInfo.Name;

  public IFileSystemDirectory RootDirectory => new FileSystemDirectoryInfo(driveInfo.RootDirectory);

  public long TotalFreeSpace => driveInfo.TotalFreeSpace;

  public long TotalSize => driveInfo.TotalSize;

  public string VolumeLabel => driveInfo.VolumeLabel;
}

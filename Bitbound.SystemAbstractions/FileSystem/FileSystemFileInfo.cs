namespace Bitbound.SystemAbstractions.FileSystem;

/// <summary>
/// Represents a file. Mirrors the members of <see cref="FileInfo"/> that are useful behind an abstraction.
/// </summary>
public interface IFileSystemFile
{
  FileAttributes Attributes { get; }
  bool Exists { get; }
  string FullName { get; }
  DateTime LastWriteTime { get; }
  long Length { get; }
  string Name { get; }
}

internal sealed class FileSystemFileInfo(FileInfo fileInfo) : IFileSystemFile
{
  public FileAttributes Attributes => fileInfo.Attributes;

  public bool Exists => fileInfo.Exists;

  public string FullName => fileInfo.FullName;

  public DateTime LastWriteTime => fileInfo.LastWriteTime;

  public long Length => fileInfo.Length;

  public string Name => fileInfo.Name;
}

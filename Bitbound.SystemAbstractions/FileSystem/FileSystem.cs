using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Bitbound.SystemAbstractions.Primitives;
using Microsoft.Extensions.Logging;

namespace Bitbound.SystemAbstractions.FileSystem;

/// <summary>
/// An abstraction over the file system. Use <see cref="FileSystem"/> for the real thing,
/// or <c>FakeFileSystem</c> from Bitbound.SystemAbstractions.TestUtilities in tests.
/// </summary>
public interface IFileSystem
{
  Task AppendAllLinesAsync(string path, IEnumerable<string> lines);
  void CopyFile(string sourceFile, string destinationFile, bool overwrite);
  IFileSystemDirectory CreateDirectory(string directoryPath);
  Stream CreateFile(string filePath);
  Stream CreateFileStream(string filePath, FileMode mode);
  Stream CreateFileStream(string filePath, FileMode mode, FileAccess access, FileShare fileShare);
  void DeleteDirectory(string directoryPath, bool recursive);
  void DeleteFile(string filePath);
  bool DirectoryExists(string directoryPath);
  void ExtractZipArchive(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles);
  Task ExtractZipArchiveAsync(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles, CancellationToken cancellationToken = default);
  bool FileExists(string path);
  string[] GetDirectories(string path);
  IFileSystemDirectory GetDirectoryInfo(string directoryPath);
  IFileSystemDrive[] GetDrives();
  IFileSystemFile GetFileInfo(string filePath);
  string[] GetFiles(string path);
  string[] GetFiles(string path, string searchPattern);
  string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
  string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions);
  FileVersionInfo GetFileVersionInfo(string filePath);
  string JoinPaths(char separator, params string[] paths);
  void MoveDirectory(string sourceDirectory, string destinationDirectory);
  void MoveFile(string sourceFile, string destinationFile, bool overwrite);
  Stream OpenFileStream(string path, FileMode mode, FileAccess access);
  Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare fileShare);
  Task<byte[]> ReadAllBytesAsync(string path);
  Task<string[]> ReadAllLinesAsync(string path);
  string ReadAllText(string filePath);
  Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
  Task ReplaceLineInFile(string filePath, string matchPattern, string replaceLineWith, int maxMatches = -1);

  /// <summary>
  /// Resolves the absolute file path for the specified file name using <c>which</c> on Unix-based systems
  /// or <c>where.exe</c> on Windows.
  /// </summary>
  /// <param name="fileName">The name of the file to resolve. Cannot be null or empty.</param>
  Task<Result<string>> ResolveFilePath(string fileName);

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  void SetUnixFileMode(string filePath, UnixFileMode fileMode);

  Task WriteAllBytesAsync(string path, byte[] buffer, CancellationToken cancellationToken = default);
  Task WriteAllLines(string path, IEnumerable<string> lines);
  void WriteAllText(string filePath, string contents);
  Task WriteAllTextAsync(string path, string content);
}

internal sealed class FileSystem(ILogger<FileSystem> logger) : IFileSystem
{
  private const int ResolveFilePathTimeoutMs = 5_000;

  private readonly ILogger<FileSystem> _logger = logger;

  public Task AppendAllLinesAsync(string path, IEnumerable<string> lines)
  {
    return File.AppendAllLinesAsync(path, lines);
  }

  public void CopyFile(string sourceFile, string destinationFile, bool overwrite)
  {
    File.Copy(sourceFile, destinationFile, overwrite);
  }

  public IFileSystemDirectory CreateDirectory(string directoryPath)
  {
    return new FileSystemDirectoryInfo(Directory.CreateDirectory(directoryPath));
  }

  public Stream CreateFile(string filePath)
  {
    return File.Create(filePath);
  }

  public Stream CreateFileStream(string filePath, FileMode mode)
  {
    return new FileStream(filePath, mode);
  }

  public Stream CreateFileStream(string filePath, FileMode mode, FileAccess access, FileShare fileShare)
  {
    return new FileStream(filePath, mode, access, fileShare);
  }

  public void DeleteDirectory(string directoryPath, bool recursive)
  {
    Directory.Delete(directoryPath, recursive);
  }

  public void DeleteFile(string filePath)
  {
    File.Delete(filePath);
  }

  public bool DirectoryExists(string directoryPath)
  {
    return Directory.Exists(directoryPath);
  }

  public void ExtractZipArchive(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
  {
    System.IO.Compression.ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
  }

  public Task ExtractZipArchiveAsync(
    string sourceArchiveFileName,
    string destinationDirectoryName,
    bool overwriteFiles,
    CancellationToken cancellationToken = default)
  {
    // The async ZipFile overloads are not available on all target frameworks.
    return Task.Run(
      () => ExtractZipArchive(sourceArchiveFileName, destinationDirectoryName, overwriteFiles),
      cancellationToken);
  }

  public bool FileExists(string path)
  {
    return File.Exists(path);
  }

  public string[] GetDirectories(string path)
  {
    return Directory.GetDirectories(path);
  }

  public IFileSystemDirectory GetDirectoryInfo(string directoryPath)
  {
    return new FileSystemDirectoryInfo(new DirectoryInfo(directoryPath));
  }

  public IFileSystemDrive[] GetDrives()
  {
    return [.. DriveInfo.GetDrives().Select(x => new FileSystemDriveInfo(x))];
  }

  public IFileSystemFile GetFileInfo(string filePath)
  {
    return new FileSystemFileInfo(new FileInfo(filePath));
  }

  public string[] GetFiles(string path)
  {
    return Directory.GetFiles(path);
  }

  public string[] GetFiles(string path, string searchPattern)
  {
    return Directory.GetFiles(path, searchPattern);
  }

  public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
  {
    return Directory.GetFiles(path, searchPattern, searchOption);
  }

  public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
  {
    return Directory.GetFiles(path, searchPattern, enumerationOptions);
  }

  public FileVersionInfo GetFileVersionInfo(string filePath)
  {
    return FileVersionInfo.GetVersionInfo(filePath);
  }

  public string JoinPaths(char separator, params string[] paths)
  {
    ArgumentNullException.ThrowIfNull(paths);

    var builder = new StringBuilder();

    for (var i = 0; i < paths.Length; i++)
    {
      var path = paths[i];
      if (string.IsNullOrEmpty(path))
      {
        continue;
      }

      if (i == 0)
      {
        // Preserve the absolute root indicator on the first segment, but remove trailing separators.
        builder.Append(path.TrimEnd(separator));
        continue;
      }

      // Remove separators around each intermediate segment to avoid duplicates.
      var trimmed = path.Trim(separator);
      if (trimmed.Length == 0)
      {
        continue;
      }

      if (builder.Length > 0 && builder[^1] != separator)
      {
        builder.Append(separator);
      }

      builder.Append(trimmed);
    }

    return builder.ToString();
  }

  public void MoveDirectory(string sourceDirectory, string destinationDirectory)
  {
    Directory.Move(sourceDirectory, destinationDirectory);
  }

  public void MoveFile(string sourceFile, string destinationFile, bool overwrite)
  {
    File.Move(sourceFile, destinationFile, overwrite);
  }

  public Stream OpenFileStream(string path, FileMode mode, FileAccess access)
  {
    return File.Open(path, mode, access);
  }

  public Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare fileShare)
  {
    return File.Open(path, mode, access, fileShare);
  }

  public async Task<byte[]> ReadAllBytesAsync(string path)
  {
    return await File.ReadAllBytesAsync(path);
  }

  public async Task<string[]> ReadAllLinesAsync(string path)
  {
    return await File.ReadAllLinesAsync(path);
  }

  public string ReadAllText(string filePath)
  {
    return File.ReadAllText(filePath);
  }

  public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
  {
    return File.ReadAllTextAsync(path, cancellationToken);
  }

  public async Task ReplaceLineInFile(string filePath, string matchPattern, string replaceLineWith, int maxMatches = -1)
  {
    var lines = await File.ReadAllLinesAsync(filePath);
    var matchCount = 0;

    for (var i = 0; i < lines.Length; i++)
    {
      if (lines[i].Contains(matchPattern, StringComparison.OrdinalIgnoreCase))
      {
        lines[i] = replaceLineWith;
        matchCount++;
      }

      if (maxMatches > -1 && matchCount >= maxMatches)
      {
        break;
      }
    }

    await File.WriteAllLinesAsync(filePath, lines);
  }

  public async Task<Result<string>> ResolveFilePath(string fileName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

    try
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = OperatingSystem.IsWindows() ? "where.exe" : "which",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };
      startInfo.ArgumentList.Add(fileName);

      using var process = Process.Start(startInfo);
      if (process is null)
      {
        return Result
          .Fail<string>($"Failed to start process to resolve file path for file name '{fileName}'.")
          .Log(_logger);
      }

      using var cts = new CancellationTokenSource(ResolveFilePathTimeoutMs);
      var outputTask = process.StandardOutput.ReadToEndAsync();
      var errorOutputTask = process.StandardError.ReadToEndAsync();

      await process.WaitForExitAsync(cts.Token);

      var output = await outputTask;
      if (string.IsNullOrWhiteSpace(output))
      {
        var errorOutput = await errorOutputTask;
        return Result
          .Fail<string>($"File '{fileName}' not found. Error: {errorOutput}")
          .Log(_logger);
      }

      var firstItem = output
        .Trim()
        .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault();

      if (string.IsNullOrWhiteSpace(firstItem) || !FileExists(firstItem))
      {
        return Result
          .Fail<string>($"File '{fileName}' not found.")
          .Log(_logger);
      }

      return Result.Ok(firstItem);
    }
    catch (OperationCanceledException)
    {
      return Result
        .Fail<string>($"Timed out while resolving file path for file name '{fileName}'.")
        .Log(_logger);
    }
    catch (Exception ex)
    {
      return Result
        .Fail<string>(ex, $"Failed to resolve file path for file name '{fileName}'.")
        .Log(_logger);
    }
  }

  [SupportedOSPlatform("linux")]
  [SupportedOSPlatform("macos")]
  public void SetUnixFileMode(string filePath, UnixFileMode fileMode)
  {
    File.SetUnixFileMode(filePath, fileMode);
  }

  public Task WriteAllBytesAsync(string path, byte[] buffer, CancellationToken cancellationToken = default)
  {
    return File.WriteAllBytesAsync(path, buffer, cancellationToken);
  }

  public Task WriteAllLines(string path, IEnumerable<string> lines)
  {
    return File.WriteAllLinesAsync(path, lines);
  }

  public void WriteAllText(string filePath, string contents)
  {
    File.WriteAllText(filePath, contents);
  }

  public Task WriteAllTextAsync(string path, string content)
  {
    return File.WriteAllTextAsync(path, content);
  }
}

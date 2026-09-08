using System.Text;

using Bitbound.SystemAbstractions.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace Bitbound.SystemAbstractions.Tests.FileSystem;

public sealed class FileSystemTests : IDisposable
{
  private readonly IFileSystem _fileSystem = new ServiceCollection()
    .AddFileSystem()
    .BuildServiceProvider()
    .GetRequiredService<IFileSystem>();
  private readonly string _root = Path.Combine(
    Path.GetTempPath(),
    "bitbound-systemabstractions-" + Guid.NewGuid().ToString("N")[..12]);

  public FileSystemTests()
  {
    Directory.CreateDirectory(_root);
  }

  [Fact]
  public async Task AppendAllLinesAsync_WhenFileAlreadyExists_AddsToTheEnd()
  {
    var path = Path.Combine(_root, "append.txt");
    await _fileSystem.WriteAllLines(path, ["first"]);

    await _fileSystem.AppendAllLinesAsync(path, ["second", "third"]);

    Assert.Equal(["first", "second", "third"], await _fileSystem.ReadAllLinesAsync(path));
  }

  [Fact]
  public void CopyFile_WhenOverwriteIsFalse_CopiesContent()
  {
    var source = Path.Combine(_root, "source.txt");
    var destination = Path.Combine(_root, "destination.txt");
    _fileSystem.WriteAllText(source, "payload");

    _fileSystem.CopyFile(source, destination, overwrite: false);

    Assert.Equal("payload", _fileSystem.ReadAllText(destination));
  }

  [Fact]
  public void CreateDirectory_WhenPathIsNested_CreatesEverySegment()
  {
    var created = _fileSystem.CreateDirectory(Path.Combine(_root, "a", "b"));

    Assert.True(_fileSystem.DirectoryExists(Path.Combine(_root, "a", "b")));
    Assert.Equal("b", created.Name);
  }

  [Fact]
  public void CreateFileStream_WhenAccessIsWriteOnly_ThrowsOnRead()
  {
    var path = Path.Combine(_root, "write-only.txt");

    using var stream = _fileSystem.CreateFileStream(
      path,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None);

    Assert.False(stream.CanRead);
    Assert.True(stream.CanWrite);
  }

  [Fact]
  public void CreateFile_WhenFileIsCreated_CanBeWrittenAndRead()
  {
    var path = Path.Combine(_root, "stream.txt");

    using (var stream = _fileSystem.CreateFile(path))
    {
      using var writer = new StreamWriter(stream, Encoding.UTF8);
      writer.Write("streamed");
    }

    Assert.Equal("streamed", _fileSystem.ReadAllText(path));
  }

  [Fact]
  public void DeleteDirectory_WhenRecursive_RemovesTheTree()
  {
    var directory = Path.Combine(_root, "tree");
    _fileSystem.CreateDirectory(Path.Combine(directory, "deep"));
    _fileSystem.WriteAllText(Path.Combine(directory, "deep", "file.txt"), "content");

    _fileSystem.DeleteDirectory(directory, recursive: true);

    Assert.False(_fileSystem.DirectoryExists(directory));
  }

  [Fact]
  public void DeleteFile_WhenFileExists_RemovesIt()
  {
    var path = Path.Combine(_root, "gone.txt");
    _fileSystem.WriteAllText(path, "content");

    _fileSystem.DeleteFile(path);

    Assert.False(_fileSystem.FileExists(path));
  }

  public void Dispose()
  {
    if (Directory.Exists(_root))
    {
      Directory.Delete(_root, recursive: true);
    }
  }

  [Fact]
  public async Task ExtractZipArchiveAsync_WhenArchiveIsExtracted_WritesTheEntry()
  {
    var archivePath = Path.Combine(_root, "async-archive.zip");
    using (var archiveStream = File.Create(archivePath))
    {
      using var archive = new System.IO.Compression.ZipArchive(
        archiveStream,
        System.IO.Compression.ZipArchiveMode.Create);

      var entry = archive.CreateEntry("entry.txt");
      using var entryStream = entry.Open();
      using var writer = new StreamWriter(
        entryStream,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

      writer.Write("zipped async");
    }

    var destination = Path.Combine(_root, "async-extracted");
    await _fileSystem.ExtractZipArchiveAsync(
      archivePath,
      destination,
      overwriteFiles: false,
      TestContext.Current.CancellationToken);

    Assert.Equal("zipped async", _fileSystem.ReadAllText(Path.Combine(destination, "entry.txt")));
  }

  [Fact]
  public void ExtractZipArchive_WhenArchiveIsExtracted_WritesTheEntry()
  {
    var archivePath = Path.Combine(_root, "archive.zip");
    using (var archiveStream = File.Create(archivePath))
    {
      using var archive = new System.IO.Compression.ZipArchive(
        archiveStream,
        System.IO.Compression.ZipArchiveMode.Create);

      var entry = archive.CreateEntry("entry.txt");
      using var entryStream = entry.Open();
      using var writer = new StreamWriter(
        entryStream,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

      writer.Write("zipped");
    }

    var destination = Path.Combine(_root, "extracted");
    _fileSystem.ExtractZipArchive(archivePath, destination, overwriteFiles: false);

    Assert.Equal("zipped", _fileSystem.ReadAllText(Path.Combine(destination, "entry.txt")));
  }

  [Fact]
  public void FileExists_WhenFileIsMissing_ReturnsFalse()
  {
    Assert.False(_fileSystem.FileExists(Path.Combine(_root, "nope.txt")));
  }

  [Fact]
  public void GetDirectories_WhenChildrenExist_ReturnsTheChildDirectory()
  {
    _fileSystem.CreateDirectory(Path.Combine(_root, "child"));

    Assert.Contains(Path.Combine(_root, "child"), _fileSystem.GetDirectories(_root));
  }

  [Fact]
  public void GetDirectoryInfo_WhenDirectoryIsMissing_ReportsDoesNotExist()
  {
    Assert.False(_fileSystem.GetDirectoryInfo(Path.Combine(_root, "missing")).Exists);
  }

  [Fact]
  public void GetDrives_WhenQueried_ReturnsAtLeastOneDrive()
  {
    Assert.NotEmpty(_fileSystem.GetDrives());
  }

  [Fact]
  public void GetFileInfo_WhenFileExists_ReportsLengthAndName()
  {
    var path = Path.Combine(_root, "info.txt");
    _fileSystem.WriteAllText(path, "12345");

    var info = _fileSystem.GetFileInfo(path);

    Assert.True(info.Exists);
    Assert.Equal("info.txt", info.Name);
    Assert.Equal(Encoding.UTF8.GetByteCount("12345"), info.Length);
  }

  [Fact]
  public void GetFiles_WhenRecursing_IncludesNestedFiles()
  {
    _fileSystem.CreateDirectory(Path.Combine(_root, "nested"));
    _fileSystem.WriteAllText(Path.Combine(_root, "top.txt"), "top");
    _fileSystem.WriteAllText(Path.Combine(_root, "nested", "bottom.txt"), "bottom");

    var files = _fileSystem.GetFiles(_root, "*.txt", SearchOption.AllDirectories);

    Assert.Equal(2, files.Length);
  }

  [Fact]
  public void GetFiles_WhenSearchPatternGiven_ReturnsOnlyMatches()
  {
    _fileSystem.WriteAllText(Path.Combine(_root, "a.txt"), "a");
    _fileSystem.WriteAllText(Path.Combine(_root, "b.log"), "b");

    var files = _fileSystem.GetFiles(_root, "*.txt");

    Assert.Equal([Path.Combine(_root, "a.txt")], files);
  }

  [Fact]
  public void GetFileVersionInfo_WhenFileIsTheRunningAssembly_ReportsAVersion()
  {
    var path = Environment.ProcessPath!;

    var info = _fileSystem.GetFileVersionInfo(path);

    Assert.NotNull(info);
    Assert.NotNull(info.FileVersion);
  }

  [Fact]
  public void JoinPaths_WhenSegmentsGiven_JoinsWithTheSeparator()
  {
    Assert.Equal("/a/b/c.txt", _fileSystem.JoinPaths('/', "/a", "b", "c.txt"));
  }

  [Fact]
  public void MoveDirectory_WhenMoved_TakesChildrenAlong()
  {
    var source = Path.Combine(_root, "before");
    var destination = Path.Combine(_root, "after");
    _fileSystem.CreateDirectory(source);
    _fileSystem.WriteAllText(Path.Combine(source, "child.txt"), "child");

    _fileSystem.MoveDirectory(source, destination);

    Assert.False(_fileSystem.DirectoryExists(source));
    Assert.Equal("child", _fileSystem.ReadAllText(Path.Combine(destination, "child.txt")));
  }

  [Fact]
  public void MoveFile_WhenMoved_LeavesNoSourceBehind()
  {
    var source = Path.Combine(_root, "before.txt");
    var destination = Path.Combine(_root, "after.txt");
    _fileSystem.WriteAllText(source, "moved");

    _fileSystem.MoveFile(source, destination, overwrite: false);

    Assert.False(_fileSystem.FileExists(source));
    Assert.Equal("moved", _fileSystem.ReadAllText(destination));
  }

  [Fact]
  public void OpenFileStream_WhenFileExistsForRead_ReturnsReadableStream()
  {
    var path = Path.Combine(_root, "open.txt");
    _fileSystem.WriteAllText(path, "readable");

    using var stream = _fileSystem.OpenFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

    using var reader = new StreamReader(stream, Encoding.UTF8);
    Assert.Equal("readable", reader.ReadToEnd());
  }

  [Fact]
  public async Task ReplaceLineInFile_WhenPatternMatches_ReplacesTheMatchedLine()
  {
    var path = Path.Combine(_root, "config.txt");
    await _fileSystem.WriteAllLines(path, ["port=1", "keep=me"]);

    await _fileSystem.ReplaceLineInFile(path, "port", "port=8080");

    Assert.Equal(["port=8080", "keep=me"], await _fileSystem.ReadAllLinesAsync(path));
  }

  [Fact]
  public async Task ResolveFilePath_WhenCommandExists_ResolvesAnAbsolutePath()
  {
    var fileName = OperatingSystem.IsWindows() ? "where.exe" : "which";

    var result = await _fileSystem.ResolveFilePath(fileName);

    Assert.True(result.IsSuccess, result.Reason);
    Assert.True(Path.IsPathRooted(result.Value), result.Value);
  }

  [Fact]
  public async Task ResolveFilePath_WhenCommandIsMissing_Fails()
  {
    var result = await _fileSystem.ResolveFilePath("bitbound-does-not-exist-9f3a2b");

    Assert.False(result.IsSuccess);
    Assert.Contains("bitbound-does-not-exist-9f3a2b", result.Reason);
  }

  [Fact]
  public async Task WriteAllBytesAsync_WhenFileIsWritten_ReadsBackTheSameBytes()
  {
    var path = Path.Combine(_root, "bytes.bin");

    await _fileSystem.WriteAllBytesAsync(path, [4, 5, 6], TestContext.Current.CancellationToken);

    Assert.Equal([4, 5, 6], await _fileSystem.ReadAllBytesAsync(path));
  }

  [Fact]
  public async Task WriteAllLines_WhenLinesAreWritten_ReadsBackTheSameLines()
  {
    var path = Path.Combine(_root, "lines.txt");

    await _fileSystem.WriteAllLines(path, ["one", "two"]);

    Assert.Equal(["one", "two"], await _fileSystem.ReadAllLinesAsync(path));
  }

  [Fact]
  public async Task WriteAllTextAsync_WhenFileIsWritten_ReadsBackWithReadAllText()
  {
    var path = Path.Combine(_root, "written.txt");

    await _fileSystem.WriteAllTextAsync(path, "file system content");

    Assert.Equal("file system content", _fileSystem.ReadAllText(path));
    Assert.True(_fileSystem.FileExists(path));
  }

  [Fact]
  public async Task WriteAllText_WhenFileIsWritten_ReadsBackWithReadAllTextAsync()
  {
    var path = Path.Combine(_root, "sync.txt");

    _fileSystem.WriteAllText(path, "sync content");

    Assert.Equal(
      "sync content",
      await _fileSystem.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
  }
}

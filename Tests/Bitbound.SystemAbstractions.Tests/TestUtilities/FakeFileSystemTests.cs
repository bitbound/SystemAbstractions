using System.Text;

using Bitbound.SystemAbstractions.FileSystem;
using Bitbound.SystemAbstractions.TestUtilities.FileSystem;

namespace Bitbound.SystemAbstractions.Tests.TestUtilities;

public class FakeFileSystemTests
{
  private readonly FakeFileSystem _fileSystem = new();

  [Fact]
  public void AddDirectory_WhenAdded_ReportsThroughGetDirectoryInfo()
  {
    _fileSystem.AddDirectory("/data/reports");

    var info = _fileSystem.GetDirectoryInfo("/data/reports");

    Assert.True(info.Exists);
    Assert.Equal("reports", info.Name);
    Assert.Equal("/data/reports", info.FullName);
    Assert.Equal(FileAttributes.Directory, info.Attributes);
  }

  [Fact]
  public async Task AddFile_WhenGivenBytes_ReadsBackTheSameBytes()
  {
    _fileSystem.AddFile("/data/blob.bin", new byte[] { 1, 2, 3 });

    Assert.Equal(new byte[] { 1, 2, 3 }, await _fileSystem.ReadAllBytesAsync("/data/blob.bin"));
  }

  [Fact]
  public void AddFile_WhenGivenStringContent_ReadsBackTheSameText()
  {
    _fileSystem.AddFile("/data/hello.txt", "hello world");

    Assert.Equal("hello world", _fileSystem.ReadAllText("/data/hello.txt"));
    Assert.True(_fileSystem.FileExists("/data/hello.txt"));
  }

  [Fact]
  public void AddFile_WhenParentDirectoriesMissing_CreatesTheWholeChain()
  {
    _fileSystem.AddFile("/data/deep/deeper/file.txt", "content");

    Assert.True(_fileSystem.DirectoryExists("/data"));
    Assert.True(_fileSystem.DirectoryExists("/data/deep"));
    Assert.True(_fileSystem.DirectoryExists("/data/deep/deeper"));
  }

  [Fact]
  public async Task AppendAllLinesAsync_WhenFileAlreadyHasLines_AddsToTheEnd()
  {
    await _fileSystem.WriteAllLines("/data/lines.txt", ["one"]);
    await _fileSystem.AppendAllLinesAsync("/data/lines.txt", ["two", "three"]);

    Assert.Equal(["one", "two", "three"], await _fileSystem.ReadAllLinesAsync("/data/lines.txt"));
  }

  [Fact]
  public void CaseInsensitiveFilesystem_WhenLookupDiffersByCase_FindsSameFile()
  {
    _fileSystem.AddFile("/data/file.txt", "content");

    Assert.Equal("content", _fileSystem.ReadAllText("/data/FILE.TXT"));
  }

  [Fact]
  public void CaseSensitiveFilesystem_WhenFilesDifferOnlyByCase_KeepsThemApart()
  {
    var fileSystem = new FakeFileSystem(isCaseSensitive: true);
    fileSystem.AddFile("/data/file.txt", "lower");
    fileSystem.AddFile("/data/FILE.TXT", "upper");

    Assert.Equal("lower", fileSystem.ReadAllText("/data/file.txt"));
    Assert.Equal("upper", fileSystem.ReadAllText("/data/FILE.TXT"));
  }

  [Fact]
  public void CopyFile_WhenDestinationExistsAndOverwriteIsFalse_ThrowsIOException()
  {
    _fileSystem.AddFile("/data/source.txt", "new");
    _fileSystem.AddFile("/data/destination.txt", "old");

    Assert.Throws<IOException>(
      () => _fileSystem.CopyFile("/data/source.txt", "/data/destination.txt", overwrite: false));
  }

  [Fact]
  public void CopyFile_WhenDestinationExistsAndOverwriteIsTrue_ReplacesContent()
  {
    _fileSystem.AddFile("/data/source.txt", "new");
    _fileSystem.AddFile("/data/destination.txt", "old");

    _fileSystem.CopyFile("/data/source.txt", "/data/destination.txt", overwrite: true);

    Assert.Equal("new", _fileSystem.ReadAllText("/data/destination.txt"));
  }

  [Fact]
  public void CopyFile_WhenDestinationIsNew_CopiesContent()
  {
    _fileSystem.AddFile("/data/source.txt", "payload");

    _fileSystem.CopyFile("/data/source.txt", "/data/copy.txt", overwrite: false);

    Assert.Equal("payload", _fileSystem.ReadAllText("/data/copy.txt"));
    Assert.True(_fileSystem.FileExists("/data/source.txt"));
  }

  [Fact]
  public void CopyFile_WhenSourceMissing_ThrowsFileNotFoundException()
  {
    Assert.Throws<FileNotFoundException>(() => _fileSystem.CopyFile("/data/missing.txt", "/data/other.txt", false));
  }

  [Fact]
  public void CreateDirectory_WhenNestedPathGiven_CreatesEverySegment()
  {
    var created = _fileSystem.CreateDirectory("/data/a/b/c");

    Assert.Equal("c", created.Name);
    Assert.True(_fileSystem.DirectoryExists("/data/a/b"));
    Assert.True(_fileSystem.DirectoryExists("/data/a/b/c"));
  }

  [Fact]
  public void DeleteDirectory_WhenDirectoryHasChildrenAndNotRecursive_ThrowsIOException()
  {
    _fileSystem.AddFile("/data/full/child.txt", "content");

    Assert.Throws<IOException>(() => _fileSystem.DeleteDirectory("/data/full", recursive: false));
  }

  [Fact]
  public void DeleteDirectory_WhenDirectoryMissing_ThrowsDirectoryNotFoundException()
  {
    Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.DeleteDirectory("/data/missing", recursive: false));
  }

  [Fact]
  public void DeleteDirectory_WhenRecursive_RemovesWholeTree()
  {
    _fileSystem.AddFile("/data/tree/a/b/c.txt", "content");

    _fileSystem.DeleteDirectory("/data/tree", recursive: true);

    Assert.False(_fileSystem.DirectoryExists("/data/tree"));
    Assert.False(_fileSystem.FileExists("/data/tree/a/b/c.txt"));
  }

  [Fact]
  public void DeleteFile_WhenFileExists_RemovesIt()
  {
    _fileSystem.AddFile("/data/gone.txt", "content");

    _fileSystem.DeleteFile("/data/gone.txt");

    Assert.False(_fileSystem.FileExists("/data/gone.txt"));
  }

  [Fact]
  public void DirectoryExists_WhenDirectoryMissing_ReturnsFalse()
  {
    Assert.False(_fileSystem.DirectoryExists("/nope"));
  }

  [Fact]
  public async Task ExtractZipArchiveAsync_WhenArchiveHasEntries_WritesFiles()
  {
    _fileSystem.AddFile("/packages/tool.zip", BuildZip([("readme.txt", "hello")]));

    await _fileSystem.ExtractZipArchiveAsync(
      "/packages/tool.zip",
      "/extracted",
      overwriteFiles: false,
      TestContext.Current.CancellationToken);

    Assert.Equal("hello", _fileSystem.ReadAllText("/extracted/readme.txt"));
  }

  [Fact]
  public void ExtractZipArchive_WhenArchiveHasNestedEntries_WritesFilesAndDirectories()
  {
    _fileSystem.AddFile("/packages/tool.zip", BuildZip([("readme.txt", "hello"), ("docs/guide.txt", "guide")]));

    _fileSystem.ExtractZipArchive("/packages/tool.zip", "/extracted", overwriteFiles: false);

    Assert.Equal("hello", _fileSystem.ReadAllText("/extracted/readme.txt"));
    Assert.Equal("guide", _fileSystem.ReadAllText("/extracted/docs/guide.txt"));
    Assert.True(_fileSystem.DirectoryExists("/extracted/docs"));
  }

  [Fact]
  public void ExtractZipArchive_WhenFileExistsAndOverwriteIsFalse_ThrowsIOException()
  {
    _fileSystem.AddFile("/packages/tool.zip", BuildZip([("readme.txt", "new")]));
    _fileSystem.AddFile("/extracted/readme.txt", "old");

    Assert.Throws<IOException>(
      () => _fileSystem.ExtractZipArchive("/packages/tool.zip", "/extracted", overwriteFiles: false));
  }

  [Fact]
  public void FileExists_WhenFileMissing_ReturnsFalse()
  {
    Assert.False(_fileSystem.FileExists("/data/missing.txt"));
  }

  [Fact]
  public void GetDirectories_WhenChildrenExist_ReturnsChildDirectories()
  {
    _fileSystem.AddDirectory("/data/one");
    _fileSystem.AddDirectory("/data/two");
    _fileSystem.AddDirectory("/data/one/nested");

    Assert.Equal(["/data/one", "/data/two"], _fileSystem.GetDirectories("/data"));
  }

  [Fact]
  public void GetDirectoryInfo_WhenDirectoryMissing_ReportsDoesNotExist()
  {
    var info = _fileSystem.GetDirectoryInfo("/data/missing");

    Assert.False(info.Exists);
  }

  [Fact]
  public void GetDrives_WhenDriveAdded_ReturnsDriveWithValues()
  {
    _fileSystem.AddDrive("/mnt/data", name: "data", totalSize: 1000, totalFreeSpace: 400, volumeLabel: "scratch");

    var drive = Assert.Single(_fileSystem.GetDrives());

    Assert.Equal("data", drive.Name);
    Assert.Equal(1000, drive.TotalSize);
    Assert.Equal(400, drive.TotalFreeSpace);
    Assert.Equal("scratch", drive.VolumeLabel);
    Assert.True(drive.IsReady);
    Assert.Equal(DriveType.Fixed, drive.DriveType);
    Assert.Equal("/mnt/data", drive.RootDirectory.FullName);
  }

  [Fact]
  public void GetFileInfo_WhenFileExists_ReportsLengthAndName()
  {
    _fileSystem.AddFile("/data/report.csv", "a,b,c");

    var info = _fileSystem.GetFileInfo("/data/report.csv");

    Assert.True(info.Exists);
    Assert.Equal("report.csv", info.Name);
    Assert.Equal(Encoding.UTF8.GetByteCount("a,b,c"), info.Length);
  }

  [Fact]
  public void GetFileInfo_WhenFileMissing_ReportsDoesNotExist()
  {
    var info = _fileSystem.GetFileInfo("/data/missing.txt");

    Assert.False(info.Exists);
  }

  [Fact]
  public void GetFiles_WhenDirectoryMissing_ThrowsDirectoryNotFoundException()
  {
    Assert.Throws<DirectoryNotFoundException>(() => _fileSystem.GetFiles("/missing"));
  }

  [Fact]
  public void GetFiles_WhenEnumerationOptionsRecurses_IncludesNestedFiles()
  {
    _fileSystem.AddFile("/data/top.txt", "1");
    _fileSystem.AddFile("/data/nested/bottom.txt", "2");

    var files = _fileSystem.GetFiles(
      "/data",
      "*.txt",
      new EnumerationOptions { RecurseSubdirectories = true });

    Assert.Equal(["/data/nested/bottom.txt", "/data/top.txt"], files);
  }

  [Fact]
  public void GetFiles_WhenPatternGiven_ReturnsMatchingFilesOnly()
  {
    _fileSystem.AddFile("/data/a.txt", "1");
    _fileSystem.AddFile("/data/b.log", "2");
    _fileSystem.AddFile("/data/c.txt", "3");

    Assert.Equal(["/data/a.txt", "/data/c.txt"], _fileSystem.GetFiles("/data", "*.txt"));
    Assert.Equal(["/data/a.txt", "/data/b.log", "/data/c.txt"], _fileSystem.GetFiles("/data"));
  }

  [Fact]
  public void GetFiles_WhenSearchOptionIsAllDirectories_IncludesNestedFiles()
  {
    _fileSystem.AddFile("/data/top.txt", "1");
    _fileSystem.AddFile("/data/nested/bottom.txt", "2");

    Assert.Equal(["/data/top.txt"], _fileSystem.GetFiles("/data", "*.txt", SearchOption.TopDirectoryOnly));
    Assert.Equal(
      ["/data/nested/bottom.txt", "/data/top.txt"],
      _fileSystem.GetFiles("/data", "*.txt", SearchOption.AllDirectories));
  }

  [Fact]
  public void JoinPaths_WhenSegmentIsEmpty_SkipsIt()
  {
    Assert.Equal("/data/file.txt", _fileSystem.JoinPaths('/', "/data", "", "file.txt"));
  }

  [Fact]
  public void JoinPaths_WhenSegmentsHaveTrailingSeparators_ProducesSingleSeparators()
  {
    Assert.Equal("/data/reports/file.txt", _fileSystem.JoinPaths('/', "/data/", "reports/", "file.txt"));
  }

  [Fact]
  public void JoinPaths_WhenSeparatorIsBackslash_UsesThatSeparator()
  {
    Assert.Equal(@"C:\data\file.txt", _fileSystem.JoinPaths('\\', @"C:\", @"data", @"file.txt"));
  }

  [Fact]
  public void MoveDirectory_WhenDestinationExists_ThrowsIOException()
  {
    _fileSystem.AddDirectory("/data/before");
    _fileSystem.AddDirectory("/data/after");

    Assert.Throws<IOException>(() => _fileSystem.MoveDirectory("/data/before", "/data/after"));
  }

  [Fact]
  public void MoveDirectory_WhenMoved_TakesChildrenAlong()
  {
    _fileSystem.AddFile("/data/before/child.txt", "content");
    _fileSystem.AddDirectory("/data/before/nested");
    _fileSystem.AddFile("/data/before/nested/deep.txt", "deep");

    _fileSystem.MoveDirectory("/data/before", "/data/after");

    Assert.False(_fileSystem.DirectoryExists("/data/before"));
    Assert.Equal("content", _fileSystem.ReadAllText("/data/after/child.txt"));
    Assert.Equal("deep", _fileSystem.ReadAllText("/data/after/nested/deep.txt"));
  }

  [Fact]
  public void MoveFile_WhenMoved_LeavesNoSourceBehind()
  {
    _fileSystem.AddFile("/data/before.txt", "content");

    _fileSystem.MoveFile("/data/before.txt", "/data/after.txt", overwrite: false);

    Assert.False(_fileSystem.FileExists("/data/before.txt"));
    Assert.Equal("content", _fileSystem.ReadAllText("/data/after.txt"));
  }

  [Fact]
  public async Task ReplaceLineInFile_WhenMatchPatternAppearsMoreThanOnce_ReplacesEveryMatch()
  {
    await _fileSystem.WriteAllLines("/data/config.txt", ["port=1", "port=2"]);

    await _fileSystem.ReplaceLineInFile("/data/config.txt", "PORT", "port=9999");

    Assert.Equal(
      ["port=9999", "port=9999"],
      await _fileSystem.ReadAllLinesAsync("/data/config.txt"));
  }

  [Fact]
  public async Task ReplaceLineInFile_WhenMaxMatchesIsOne_ReplacesOnlyTheFirstMatch()
  {
    await _fileSystem.WriteAllLines("/data/config.txt", ["port=1", "port=2", "other=3"]);

    await _fileSystem.ReplaceLineInFile("/data/config.txt", "port", "port=9999", maxMatches: 1);

    Assert.Equal(["port=9999", "port=2", "other=3"], await _fileSystem.ReadAllLinesAsync("/data/config.txt"));
  }

  [Fact]
  public async Task ResolveFilePath_WhenPathWasNotRegistered_Fails()
  {
    var result = await _fileSystem.ResolveFilePath("missing-tool");

    Assert.False(result.IsSuccess);
    Assert.Contains("missing-tool", result.Reason);
  }

  [Fact]
  public async Task ResolveFilePath_WhenPathWasRegistered_ReturnsIt()
  {
    _fileSystem.SetResolvedFilePath("dotnet", "/usr/share/dotnet/dotnet");

    var result = await _fileSystem.ResolveFilePath("dotnet");

    Assert.True(result.IsSuccess);
    Assert.Equal("/usr/share/dotnet/dotnet", result.Value);
  }

  [Fact]
  public void WindowsFilesystem_WhenPathsUseBackslashes_WorksWithRegisteredFiles()
  {
    var fileSystem = new FakeFileSystem('\\');
    fileSystem.AddFile(@"C:\data\file.txt", "content");

    Assert.True(fileSystem.FileExists(@"C:\data\file.txt"));
    Assert.Equal("content", fileSystem.ReadAllText(@"C:\data\file.txt"));
  }

  [Fact]
  public async Task WriteAllBytesAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
  {
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    // The cancelled token is the input under test here, not the test's own cancellation token.
#pragma warning disable xUnit1051
    await Assert.ThrowsAnyAsync<OperationCanceledException>(
      () => _fileSystem.WriteAllBytesAsync("/data/file.bin", [1], cts.Token));
#pragma warning restore xUnit1051
  }

  [Fact]
  public async Task WriteAllBytesAsync_WhenWritten_ReadsBackWithReadAllBytesAsync()
  {
    await _fileSystem.WriteAllBytesAsync(
      "/data/written.bin",
      [9, 8, 7],
      TestContext.Current.CancellationToken);

    Assert.Equal(new byte[] { 9, 8, 7 }, await _fileSystem.ReadAllBytesAsync("/data/written.bin"));
  }

  [Fact]
  public async Task WriteAllLines_WhenWritten_ReadsBackTheSameLines()
  {
    await _fileSystem.WriteAllLines("/data/lines.txt", ["one", "two"]);

    Assert.Equal(["one", "two"], await _fileSystem.ReadAllLinesAsync("/data/lines.txt"));
  }

  [Fact]
  public async Task WriteAllTextAsync_WhenWritten_ReadsBackWithReadAllText()
  {
    await _fileSystem.WriteAllTextAsync("/data/written.txt", "written content");

    Assert.Equal("written content", _fileSystem.ReadAllText("/data/written.txt"));
  }

  private static byte[] BuildZip(IEnumerable<(string Path, string Content)> entries)
  {
    using var stream = new MemoryStream();
    using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create))
    {
      foreach (var (path, content) in entries)
      {
        var entry = archive.CreateEntry(path);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
      }
    }

    return stream.ToArray();
  }
}

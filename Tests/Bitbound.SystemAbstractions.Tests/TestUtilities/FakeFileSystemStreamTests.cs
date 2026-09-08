using System.Text;

using Bitbound.SystemAbstractions.FileSystem;
using Bitbound.SystemAbstractions.TestUtilities.FileSystem;

namespace Bitbound.SystemAbstractions.Tests.TestUtilities;

public class FakeFileSystemStreamTests
{
  private readonly FakeFileSystem _fileSystem = new();

  [Fact]
  public void CreateFileStream_WhenModeIsAppendAndAccessIsReadWrite_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(
      () => _fileSystem.CreateFileStream("/data/log.txt", FileMode.Append, FileAccess.ReadWrite, FileShare.None));
  }

  [Fact]
  public void CreateFileStream_WhenModeIsAppend_WritesAfterTheExistingContent()
  {
    _fileSystem.AddFile("/data/log.txt", "first ");

    using (var stream = _fileSystem.CreateFileStream("/data/log.txt", FileMode.Append))
    {
      Assert.Equal(6, stream.Position);

      var writer = new StreamWriter(stream, Encoding.UTF8);
      writer.Write("second");
      writer.Flush();
    }

    Assert.Equal("first second", _fileSystem.ReadAllText("/data/log.txt"));
  }

  [Fact]
  public async Task CreateFile_WhenFileAlreadyExists_TruncatesIt()
  {
    _fileSystem.AddFile("/data/report.txt", "old content that is longer");

    using (var stream = _fileSystem.CreateFile("/data/report.txt"))
    {
      stream.Write([1]);
    }

    Assert.Single(await _fileSystem.ReadAllBytesAsync("/data/report.txt"));
  }

  [Fact]
  public void CreateFile_WhenStreamIsWrittenAndFlushed_TheFileSystemSeesTheContent()
  {
    using (var stream = _fileSystem.CreateFile("/data/report.txt"))
    {
      using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
      writer.Write("written through a stream");
    }

    Assert.Equal("written through a stream", _fileSystem.ReadAllText("/data/report.txt"));
  }

  [Fact]
  public async Task DisposeAsync_WhenTheStreamIsDisposed_CommitsTheWrittenContent()
  {
    await using (var stream = _fileSystem.CreateFileStream(
      "/data/async.txt",
      FileMode.Create,
      FileAccess.Write,
      FileShare.None))
    {
      await stream.WriteAsync("async written"u8.ToArray(), TestContext.Current.CancellationToken);
    }

    Assert.Equal("async written", _fileSystem.ReadAllText("/data/async.txt"));
  }

  [Fact]
  public void Dispose_WhenTheStreamIsClosed_FurtherReadsThrowObjectDisposedException()
  {
    _fileSystem.AddFile("/data/closed.txt", "content");

    var stream = _fileSystem.OpenFileStream("/data/closed.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
    stream.Dispose();

    Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1]));
  }

  [Fact]
  public void OpenFileStream_WhenAccessIsRead_ReportsWriteAsUnsupported()
  {
    _fileSystem.AddFile("/data/readonly.txt", "content");

    using var stream = _fileSystem.OpenFileStream("/data/readonly.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

    Assert.True(stream.CanRead);
    Assert.False(stream.CanWrite);
    Assert.Throws<NotSupportedException>(() => stream.Write([1]));
  }

  [Fact]
  public void OpenFileStream_WhenAccessIsWrite_ReportsReadAsUnsupported()
  {
    using var stream = _fileSystem.CreateFileStream(
      "/data/writeonly.txt",
      FileMode.Create,
      FileAccess.Write,
      FileShare.None);

    Assert.True(stream.CanWrite);
    Assert.False(stream.CanRead);
    Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1]));
  }

  [Fact]
  public void OpenFileStream_WhenAnotherHandleHoldsShareNone_ThrowsIOException()
  {
    using var held = _fileSystem.OpenFileStream("/data/locked.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.None);

    Assert.Throws<IOException>(
      () => _fileSystem.OpenFileStream("/data/locked.txt", FileMode.Open, FileAccess.Read, FileShare.Read));
  }

  [Fact]
  public void OpenFileStream_WhenExistingHandleSharesReadOnly_WriterIsRejected()
  {
    _fileSystem.AddFile("/data/shared.txt", "shared content");

    using var reader = _fileSystem.OpenFileStream("/data/shared.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

    Assert.Throws<IOException>(
      () => _fileSystem.OpenFileStream("/data/shared.txt", FileMode.Open, FileAccess.Write, FileShare.Read));
  }

  [Fact]
  public void OpenFileStream_WhenExistingHandleSharesRead_SecondReaderIsAllowed()
  {
    _fileSystem.AddFile("/data/shared.txt", "shared content");

    using var first = _fileSystem.OpenFileStream("/data/shared.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
    using var second = _fileSystem.OpenFileStream("/data/shared.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

    using var reader = new StreamReader(second, Encoding.UTF8);
    Assert.Equal("shared content", reader.ReadToEnd());
    Assert.True(first.CanRead);
  }

  [Fact]
  public void OpenFileStream_WhenModeIsCreateNewAndFileExists_ThrowsIOException()
  {
    _fileSystem.AddFile("/data/exists.txt", "content");

    Assert.Throws<IOException>(
      () => _fileSystem.OpenFileStream("/data/exists.txt", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None));
  }

  [Fact]
  public void OpenFileStream_WhenModeIsOpenAndFileIsMissing_ThrowsFileNotFoundException()
  {
    Assert.Throws<FileNotFoundException>(
      () => _fileSystem.OpenFileStream("/data/missing.txt", FileMode.Open, FileAccess.Read, FileShare.Read));
  }

  [Fact]
  public void OpenFileStream_WhenModeIsOpenOrCreateAndFileIsMissing_CreatesIt()
  {
    using var stream = _fileSystem.OpenFileStream(
      "/data/created.txt",
      FileMode.OpenOrCreate,
      FileAccess.ReadWrite,
      FileShare.None);

    Assert.True(stream.CanWrite);
    Assert.True(_fileSystem.FileExists("/data/created.txt"));
  }

  [Fact]
  public void OpenFileStream_WhenModeIsTruncateWithoutWriteAccess_ThrowsArgumentException()
  {
    _fileSystem.AddFile("/data/exists.txt", "content");

    Assert.Throws<ArgumentException>(
      () => _fileSystem.OpenFileStream("/data/exists.txt", FileMode.Truncate, FileAccess.Read, FileShare.Read));
  }

  [Fact]
  public void OpenFileStream_WhenPathIsADirectory_ThrowsUnauthorizedAccessException()
  {
    _fileSystem.AddDirectory("/data/reports");

    Assert.Throws<UnauthorizedAccessException>(
      () => _fileSystem.OpenFileStream("/data/reports", FileMode.Open, FileAccess.Read, FileShare.Read));
  }

  [Fact]
  public void OpenFileStream_WhenThePreviousHandleWasClosed_ShareNoneOpensAgain()
  {
    using (var held = _fileSystem.OpenFileStream("/data/locked.txt", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
    {
      Assert.NotNull(held);
    }

    using var reopened = _fileSystem.OpenFileStream(
      "/data/locked.txt",
      FileMode.Open,
      FileAccess.ReadWrite,
      FileShare.None);

    Assert.True(reopened.CanRead);
  }

  [Fact]
  public void Seek_WhenMovedToTheStart_RereadsTheContent()
  {
    _fileSystem.AddFile("/data/seek.txt", "abc");

    using var stream = _fileSystem.OpenFileStream("/data/seek.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

    var first = new byte[1];
    Assert.Equal(1, stream.Read(first, 0, 1));
    stream.Seek(0, SeekOrigin.Begin);
    var again = new byte[1];
    Assert.Equal(1, stream.Read(again, 0, 1));

    Assert.Equal((byte)'a', first[0]);
    Assert.Equal((byte)'a', again[0]);
  }

  [Fact]
  public void SetLength_WhenTheStreamIsReadOnly_ThrowsNotSupportedException()
  {
    _fileSystem.AddFile("/data/readonly.txt", "content");

    using var stream = _fileSystem.OpenFileStream("/data/readonly.txt", FileMode.Open, FileAccess.Read, FileShare.Read);

    Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
  }
}

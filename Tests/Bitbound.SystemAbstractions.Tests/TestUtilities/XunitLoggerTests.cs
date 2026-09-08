using System.Text;

using Bitbound.SystemAbstractions.TestUtilities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bitbound.SystemAbstractions.Tests.TestUtilities;

public class XunitLoggerTests
{
  private readonly CapturingOutputHelper _output = new();

  [Fact]
  public void BeginScope_WhenActive_IsIncludedInSubsequentEntries()
  {
    var logger = new XunitLogger(_output, "MyCategory");

    using (logger.BeginScope("scope-one"))
    {
      logger.LogWarning("inside the scope");
    }

    logger.LogWarning("outside the scope");

    Assert.Contains("[MyCategory => scope-one]  inside the scope", _output.Lines[0]);
    Assert.DoesNotContain("scope-one", _output.Lines[1]);
  }

  [Fact]
  public void BeginScope_WhenNested_IsWrittenOutermostFirst()
  {
    var logger = new XunitLogger(_output, "MyCategory");

    using (logger.BeginScope("outer"))
    using (logger.BeginScope("inner"))
    {
      logger.LogInformation("nested");
    }

    Assert.Contains("inner => outer", _output.Lines.Single());
  }

  [Fact]
  public void CreateLogger_WhenGivenACategoryName_WritesThroughTheOutputHelper()
  {
    using var provider = new XunitLoggerProvider(_output);

    var logger = provider.CreateLogger("ProviderCategory");
    logger.LogDebug("from the provider");

    Assert.Contains("[ProviderCategory]  from the provider", _output.Lines.Single());
  }

  [Fact]
  public void Dispose_WhenTheProviderIsDisposed_DoesNotThrow()
  {
    var provider = new XunitLoggerProvider(_output);

    provider.Dispose();
  }

  [Fact]
  public void GenericLogger_WhenCreated_UsesTheTypeAsTheCategory()
  {
    var logger = new XunitLogger<XunitLoggerTests>(_output);

    logger.LogInformation("typed message");

    Assert.Contains($"[{nameof(XunitLoggerTests)}]  typed message", _output.Lines.Single());
  }

  [Fact]
  public void IsEnabled_WhenAnyLevelIsAskedFor_ReturnsTrue()
  {
    var logger = new XunitLogger(_output, "MyCategory");

    foreach (var level in Enum.GetValues<LogLevel>())
    {
      if (level != LogLevel.None)
      {
        Assert.True(logger.IsEnabled(level));
      }
    }
  }

  [Fact]
  public void Log_WhenCalled_WritesTheLevelCategoryAndMessage()
  {
    var logger = new XunitLogger(_output, "MyCategory");

    logger.LogInformation("hello {Value}", 42);

    var line = _output.Lines.Single();
    Assert.Contains("[Information]", line);
    Assert.Contains("[MyCategory]", line);
    Assert.Contains("hello 42", line);
    Assert.Contains($"[Process ID: {Environment.ProcessId}]", line);
  }

  [Fact]
  public void Log_WhenGivenAnException_WritesTheTypeMessageAndStackTrace()
  {
    var logger = new XunitLogger(_output, "MyCategory");
    var exception = new InvalidOperationException("outer", new IOException("inner"));

    logger.LogError(exception, "the operation failed");

    var line = _output.Output;
    Assert.Contains("[Error]", line);
    Assert.Contains("[InvalidOperationException]  outer", line);
    Assert.Contains("[IOException]  inner", line);
  }

  private sealed class CapturingOutputHelper : ITestOutputHelper
  {
    private readonly StringBuilder _builder = new();

    public List<string> Lines { get; } = [];

    public string Output => _builder.ToString();

    public void Write(string message)
    {
      _builder.Append(message);

      foreach (var line in message.Split('\n'))
      {
        var trimmed = line.TrimEnd('\r');
        if (!string.IsNullOrEmpty(trimmed))
        {
          Lines.Add(trimmed);
        }
      }
    }

    public void Write(string format, params object[] args)
    {
      Write(string.Format(format, args));
    }

    public void WriteLine(string message)
    {
      Write(message + Environment.NewLine);
    }

    public void WriteLine(string format, params object[] args)
    {
      WriteLine(string.Format(format, args));
    }
  }
}

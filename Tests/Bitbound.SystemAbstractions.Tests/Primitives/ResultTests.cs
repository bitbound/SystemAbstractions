using Bitbound.SystemAbstractions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bitbound.SystemAbstractions.Tests.Primitives;

public class ResultTests
{
  [Fact]
  public void Constructor_WhenSuccessfulWithAReason_AllowsIt()
  {
    var result = new Result(true, "ignored");

    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void Constructor_WhenUnsuccessfulWithoutAReason_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new Result(false));
  }

  [Fact]
  public void FailOfT_WhenGivenAReason_LeavesTheValueNull()
  {
    var result = Result.Fail<string>("no value for you");

    Assert.False(result.IsSuccess);
    Assert.Null(result.Value);
    Assert.Equal("no value for you", result.Reason);
  }

  [Fact]
  public void Fail_WhenGivenAnExceptionAndAReason_KeepsBoth()
  {
    var exception = new InvalidOperationException("the inner message");

    var result = Result.Fail(exception, "the outer reason");

    Assert.True(result.HadException);
    Assert.Equal("the outer reason", result.Reason);
    Assert.Same(exception, result.Exception);
  }

  [Fact]
  public void Fail_WhenGivenAnException_UsesTheMessageAsTheReason()
  {
    var exception = new InvalidOperationException("the inner message");

    var result = Result.Fail(exception);

    Assert.False(result.IsSuccess);
    Assert.True(result.HadException);
    Assert.Same(exception, result.Exception);
    Assert.Equal("the inner message", result.Reason);
  }

  [Fact]
  public void Fail_WhenGivenAReason_ReportsFailureWithThatReason()
  {
    var result = Result.Fail("it broke");

    Assert.False(result.IsSuccess);
    Assert.Equal("it broke", result.Reason);
    Assert.False(result.HadException);
  }

  [Fact]
  public void Log_WhenFailedWithAnException_LogsTheExceptionAndReturnsTheSameResult()
  {
    var logger = new RecordingLogger();
    var exception = new InvalidOperationException("it broke badly");

    var result = Result.Fail(exception);
    var returned = result.Log(logger);

    Assert.Same(result, returned);
    Assert.Same(exception, logger.LastException);
    Assert.Contains(logger.Messages, message => message.Contains("it broke badly", StringComparison.Ordinal));
  }

  [Fact]
  public void Log_WhenFailedWithoutAnException_LogsAnErrorAndReturnsTheSameResult()
  {
    var logger = new RecordingLogger();

    var result = Result.Fail("it broke");
    var returned = result.Log(logger);

    Assert.Same(result, returned);
    Assert.Contains(logger.Messages, message => message.Contains("it broke", StringComparison.Ordinal));
  }

  [Fact]
  public void Log_WhenLoggerIsNull_ThrowsArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => Result.Ok().Log(null!));
    Assert.Throws<ArgumentNullException>(() => Result.Ok(1).Log(null!));
  }

  [Fact]
  public void Log_WhenSuccessful_TracesAndReturnsTheSameResult()
  {
    var logger = new RecordingLogger();
    var result = Result.Ok();

    var returned = result.Log(logger);

    Assert.Same(result, returned);
    Assert.Equal("Operation succeeded.", logger.Messages.Single());
  }

  [Fact]
  public void Log_WhenUsingTheNullLogger_StillReturnsTheResult()
  {
    var result = Result.Ok("value");

    var returned = result.Log(NullLogger.Instance);

    Assert.Same(result, returned);
  }

  [Fact]
  public void OkOfT_WhenCreated_CarriesTheValue()
  {
    var result = Result.Ok("payload");

    Assert.True(result.IsSuccess);
    Assert.Equal("payload", result.Value);
    Assert.Equal(string.Empty, result.Reason);
  }

  [Fact]
  public void Ok_WhenCreated_ReportsSuccessWithNoReason()
  {
    var result = Result.Ok();

    Assert.True(result.IsSuccess);
    Assert.Equal(string.Empty, result.Reason);
    Assert.False(result.HadException);
  }

  [Fact]
  public void ToResultOfT_WhenFailedWithAnException_KeepsTheExceptionAndReason()
  {
    var exception = new IOException("disk went away");

    var result = Result.Fail<int>(exception, "copy failed").ToResult("substituted");

    Assert.False(result.IsSuccess);
    Assert.Equal("copy failed", result.Reason);
    Assert.Same(exception, result.Exception);
    Assert.Null(result.Value);
  }

  [Fact]
  public void ToResultOfT_WhenFailedWithoutAnException_SynthesizesOneFromTheReason()
  {
    var result = Result.Fail<int>("no reason object").ToResult("substituted");

    Assert.False(result.IsSuccess);
    Assert.True(result.HadException);
    Assert.Equal("no reason object", result.Reason);
  }

  [Fact]
  public void ToResult_WhenFailed_KeepsTheReasonAndException()
  {
    var exception = new IOException("disk went away");

    var result = Result.Fail<int>(exception, "copy failed").ToResult();

    Assert.False(result.IsSuccess);
    Assert.Equal("copy failed", result.Reason);
    Assert.Same(exception, result.Exception);
  }

  [Fact]
  public void ToResult_WhenSuccessful_DropsTheValueAndKeepsSuccess()
  {
    var result = Result.Ok(42).ToResult();

    Assert.True(result.IsSuccess);
  }

  private sealed class RecordingLogger : ILogger
  {
    public Exception? LastException { get; private set; }
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull
    {
      return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      Messages.Add(formatter(state, exception));
      LastException = exception;
    }
  }
}

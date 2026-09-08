using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Bitbound.SystemAbstractions.Primitives;

/// <summary>
/// Describes the success or failure of an operation that returns no value.
/// </summary>
public class Result
{
  /// <summary>
  /// Initializes a new instance of the <see cref="Result"/> class.
  /// </summary>
  /// <param name="isSuccess">Whether the operation succeeded.</param>
  /// <param name="reason">Why the operation failed. Required when <paramref name="isSuccess"/> is <see langword="false"/>.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="isSuccess"/> is <see langword="false"/> and <paramref name="reason"/> is empty.</exception>
  public Result(bool isSuccess, string reason = "")
  {
    if (!isSuccess && string.IsNullOrWhiteSpace(reason))
    {
      throw new ArgumentException("A reason or exception must be supplied for an unsuccessful result.", nameof(reason));
    }

    IsSuccess = isSuccess;
    Reason = reason;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result"/> class.
  /// </summary>
  /// <param name="isSuccess">Whether the operation succeeded.</param>
  /// <param name="exception">The exception that caused the failure, if any.</param>
  /// <param name="reason">Why the operation failed.</param>
  public Result(bool isSuccess, Exception? exception, string reason)
  {
    IsSuccess = isSuccess;
    Exception = exception;
    Reason = reason;
  }

  private Result(Exception exception)
  {
    IsSuccess = false;
    Reason = exception.Message;
    Exception = exception;
  }

  private Result(Exception exception, string reason)
  {
    IsSuccess = false;
    Reason = reason;
    Exception = exception;
  }

  /// <summary>
  /// Gets the exception that caused the failure, if any.
  /// </summary>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Gets a value indicating whether an exception is associated with this result.
  /// </summary>
  [MemberNotNullWhen(true, nameof(Exception))]
  public bool HadException => Exception is not null;

  /// <summary>
  /// Gets a value indicating whether the operation succeeded.
  /// </summary>
  [MemberNotNullWhen(false, nameof(Reason))]
  public bool IsSuccess { get; init; }

  /// <summary>
  /// Gets the reason the operation failed. Empty when the operation succeeded.
  /// </summary>
  public string Reason { get; init; } = string.Empty;

  /// <summary>
  /// Creates a failed result.
  /// </summary>
  /// <param name="reason">Why the operation failed.</param>
  /// <returns>A failed <see cref="Result"/>.</returns>
  public static Result Fail(string reason)
  {
    return new Result(false, reason);
  }

  /// <summary>
  /// Creates a failed result from an exception.
  /// </summary>
  /// <param name="exception">The exception that caused the failure.</param>
  /// <returns>A failed <see cref="Result"/>.</returns>
  public static Result Fail(Exception exception)
  {
    return new Result(exception);
  }

  /// <summary>
  /// Creates a failed result from an exception and an explicit reason.
  /// </summary>
  /// <param name="exception">The exception that caused the failure.</param>
  /// <param name="reason">Why the operation failed.</param>
  /// <returns>A failed <see cref="Result"/>.</returns>
  public static Result Fail(Exception exception, string reason)
  {
    return new Result(exception, reason);
  }

  /// <summary>
  /// Creates a failed result for an operation that returns a value.
  /// </summary>
  /// <typeparam name="T">The value type the operation would have returned.</typeparam>
  /// <param name="reason">Why the operation failed.</param>
  /// <returns>A failed <see cref="Result{T}"/>.</returns>
  public static Result<T> Fail<T>(string reason)
  {
    return new Result<T>(reason);
  }

  /// <summary>
  /// Creates a failed result for an operation that returns a value.
  /// </summary>
  /// <typeparam name="T">The value type the operation would have returned.</typeparam>
  /// <param name="exception">The exception that caused the failure.</param>
  /// <returns>A failed <see cref="Result{T}"/>.</returns>
  public static Result<T> Fail<T>(Exception exception)
  {
    return new Result<T>(exception);
  }

  /// <summary>
  /// Creates a failed result for an operation that returns a value.
  /// </summary>
  /// <typeparam name="T">The value type the operation would have returned.</typeparam>
  /// <param name="exception">The exception that caused the failure.</param>
  /// <param name="reason">Why the operation failed.</param>
  /// <returns>A failed <see cref="Result{T}"/>.</returns>
  public static Result<T> Fail<T>(Exception exception, string reason)
  {
    return new Result<T>(exception, reason);
  }

  /// <summary>
  /// Creates a successful result.
  /// </summary>
  /// <returns>A successful <see cref="Result"/>.</returns>
  public static Result Ok()
  {
    return new Result(true);
  }

  /// <summary>
  /// Creates a successful result carrying a value.
  /// </summary>
  /// <typeparam name="T">The value type.</typeparam>
  /// <param name="value">The value produced by the operation.</param>
  /// <returns>A successful <see cref="Result{T}"/>.</returns>
  public static Result<T> Ok<T>(T value)
  {
    return new Result<T>(value);
  }

  /// <summary>
  /// Writes the result to the supplied logger. Failures are logged as errors, successes as traces.
  /// </summary>
  /// <param name="logger">The logger to write to.</param>
  /// <returns>This result, so calls can be chained.</returns>
  public Result Log(ILogger logger)
  {
    ArgumentNullException.ThrowIfNull(logger);

    if (IsSuccess)
    {
      logger.LogTrace("Operation succeeded.");
      return this;
    }

    if (HadException)
    {
      logger.LogError(Exception, "Operation failed. Reason: {Reason}", Reason);
      return this;
    }

    logger.LogError("Operation failed. Reason: {Reason}", Reason);
    return this;
  }
}

/// <summary>
/// Describes the success or failure of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The value type the operation returns on success.</typeparam>
public class Result<T>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> class.
  /// </summary>
  /// <param name="value">The value produced by the operation, if any.</param>
  /// <param name="isSuccess">Whether the operation succeeded.</param>
  /// <param name="reason">Why the operation failed.</param>
  public Result(T? value, bool isSuccess, string reason)
  {
    Value = value;
    IsSuccess = isSuccess;
    Reason = reason;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> class representing success.
  /// </summary>
  /// <param name="value">The value produced by the operation.</param>
  public Result(T value)
  {
    IsSuccess = true;
    Value = value;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> class representing failure.
  /// </summary>
  /// <param name="exception">The exception that caused the failure.</param>
  public Result(Exception exception)
  {
    IsSuccess = false;
    Exception = exception;
    Reason = exception.Message;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> class representing failure.
  /// </summary>
  /// <param name="exception">The exception that caused the failure.</param>
  /// <param name="reason">Why the operation failed.</param>
  public Result(Exception exception, string reason)
  {
    IsSuccess = false;
    Exception = exception;
    Reason = reason;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="Result{T}"/> class representing failure.
  /// </summary>
  /// <param name="reason">Why the operation failed.</param>
  public Result(string reason)
  {
    IsSuccess = false;
    Reason = reason;
  }

  /// <summary>
  /// Gets the exception that caused the failure, if any.
  /// </summary>
  public Exception? Exception { get; init; }

  /// <summary>
  /// Gets a value indicating whether an exception is associated with this result.
  /// </summary>
  [MemberNotNullWhen(true, nameof(Exception))]
  public bool HadException => Exception is not null;

  /// <summary>
  /// Gets a value indicating whether the operation succeeded.
  /// </summary>
  [MemberNotNullWhen(true, nameof(Value))]
  [MemberNotNullWhen(false, nameof(Reason))]
  public bool IsSuccess { get; init; }

  /// <summary>
  /// Gets the reason the operation failed. Empty when the operation succeeded.
  /// </summary>
  public string Reason { get; init; } = string.Empty;

  /// <summary>
  /// Gets the value produced by the operation. Only meaningful when <see cref="IsSuccess"/> is <see langword="true"/>.
  /// </summary>
  public T? Value { get; init; }

  /// <summary>
  /// Writes the result to the supplied logger. Failures are logged as errors, successes as traces.
  /// </summary>
  /// <param name="logger">The logger to write to.</param>
  /// <returns>This result, so calls can be chained.</returns>
  public Result<T> Log(ILogger logger)
  {
    ArgumentNullException.ThrowIfNull(logger);

    if (IsSuccess)
    {
      logger.LogTrace("Operation succeeded.");
      return this;
    }

    if (HadException)
    {
      logger.LogError(Exception, "Operation failed. Reason: {Reason}", Reason);
      return this;
    }

    logger.LogError("Operation failed. Reason: {Reason}", Reason);
    return this;
  }

  /// <summary>
  /// Drops the value, keeping only the success state.
  /// </summary>
  /// <returns>A <see cref="Result"/> with the same success state as this instance.</returns>
  public Result ToResult()
  {
    return new Result(IsSuccess, Exception, Reason);
  }

  /// <summary>
  /// Replaces the value while keeping the success state.
  /// </summary>
  /// <typeparam name="TNewValue">The new value type.</typeparam>
  /// <param name="value">The value to carry on success.</param>
  /// <returns>A <see cref="Result{T}"/> of <typeparamref name="TNewValue"/>.</returns>
  public Result<TNewValue> ToResult<TNewValue>(TNewValue value)
  {
    if (IsSuccess)
    {
      return Result.Ok(value);
    }

    return Result.Fail<TNewValue>(Exception ?? new InvalidOperationException(Reason), Reason);
  }
}

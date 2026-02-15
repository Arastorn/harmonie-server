namespace Harmonie.Domain.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Provides a functional approach to error handling without exceptions.
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Success result cannot have an error");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

/// <summary>
/// Represents the result of an operation that returns a value on success.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        if (isSuccess && value == null)
            throw new InvalidOperationException("Success result must have a value");
        if (isSuccess && error != null)
            throw new InvalidOperationException("Success result cannot have an error");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    /// <summary>
    /// Maps the result value to a new type if successful
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess 
            ? Result<TNew>.Success(mapper(Value!))
            : Result<TNew>.Failure(Error!);
    }

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public Result<T> OnFailure(Action<string> action)
    {
        if (IsFailure) action(Error!);
        return this;
    }
}

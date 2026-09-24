namespace SchoolManagement.Shared;

/// <summary>
/// Represents the outcome of an operation without throwing exceptions for expected failure cases
/// (validation errors, "not found", "duplicate", "insufficient balance", etc.).
/// Every Application-layer use case returns a Result or Result&lt;T&gt; instead of throwing.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        Error = errors.Count > 0 ? errors[0] : null;
    }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(string error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToList());

    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Fail(error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result. Check IsSuccess first.");

    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors) : base(isSuccess, errors)
    {
        _value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<string>());
    public static Result<T> Fail(string error) => new(false, default, new[] { error });
    public static Result<T> Fail(IEnumerable<string> errors) => new(false, default, errors.ToList());
}

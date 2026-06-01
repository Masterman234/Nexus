namespace Nexus.SharedKernel;

/// <summary>
/// A pragmatic Result pattern to handle success/failure without exceptions.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && error != string.Empty)
            throw new InvalidOperationException();
        if (!isSuccess && error == string.Empty)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
}

public class Result<T>(bool isSuccess, T value, string error) : Result(isSuccess, error)
{
    private readonly T _value = value;

    public T Value => IsSuccess 
        ? _value 
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static new Result<T> Failure(string error) => new(false, default!, error);
}
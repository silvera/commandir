namespace Commandir.Core;

public sealed class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;
    
    private Result(T value)
    {
        _value = value;
    }

    private Result(string error)
    {
        _error = error;
    }

    public T Value => _value ?? throw new InvalidOperationException("Value is null");

    public bool HasError => _error != null;

    public Exception ToException() => new Exception(_error);

    public static Result<T> Ok(T value) => new Result<T>(value);
    public static Result<T> Error(string error) => new Result<T>(error);

}
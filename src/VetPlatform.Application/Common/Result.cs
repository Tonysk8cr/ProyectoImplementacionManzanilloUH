namespace VetPlatform.Application.Common;

public enum ErrorType { None, Validation, NotFound, Forbidden, Conflict, Unauthorized }

public class Result<T>
{
    public bool Success { get; }
    public bool Failure => !Success;
    public T? Value { get; }
    public IReadOnlyList<string> Errors { get; }
    public ErrorType ErrorType { get; }

    private Result(bool success, T? value, IReadOnlyList<string> errors, ErrorType type)
    { Success = success; Value = value; Errors = errors; ErrorType = type; }

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<string>(), ErrorType.None);
    public static Result<T> Fail(ErrorType type, params string[] errors) => new(false, default, errors, type);
    public static Result<T> NotFound(string error) => Fail(ErrorType.NotFound, error);
    public static Result<T> Forbidden(string error = "Acceso denegado.") => Fail(ErrorType.Forbidden, error);
    public static Result<T> Invalid(params string[] errors) => Fail(ErrorType.Validation, errors);
    public static Result<T> Conflict(string error) => Fail(ErrorType.Conflict, error);
}

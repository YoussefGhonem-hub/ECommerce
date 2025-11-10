namespace ECommerce.Application.Features.Common;

public class Result<T>
{
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = System.Array.Empty<string>();
    public System.Collections.Generic.Dictionary<string, string[]>? ValidationErrors { get; set; }
    public T? Data { get; set; }

    public static Result<T> Success(T data) => new Result<T> { Succeeded = true, Data = data };
    public static Result<T> Failure(params string[] errors) => new Result<T> { Succeeded = false, Errors = errors };
    public static Result<T> Validation(System.Collections.Generic.Dictionary<string, string[]> errors)
        => new Result<T> { Succeeded = false, ValidationErrors = errors };
}

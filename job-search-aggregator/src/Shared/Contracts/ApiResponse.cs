namespace JobSearchAggregator.Shared.Contracts;

/// <summary>
/// Envelope wrapping every API response body so clients (the Angular app)
/// get a consistent shape regardless of endpoint.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}

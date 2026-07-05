namespace FinanceApp.Application.DTOs.Common;

/// <summary>
/// Formato estándar de respuesta para TODOS los endpoints.
/// El frontend siempre recibirá esta estructura — nunca cambia.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public ApiError? Error { get; set; }

    // Métodos estáticos para crear respuestas fácilmente
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        Error = null
    };

    public static ApiResponse<T> Fail(string code, string message, List<ApiErrorDetail>? details = null) => new()
    {
        Success = false,
        Data = default,
        Message = null,
        Error = new ApiError
        {
            Code = code,
            Message = message,
            Details = details ?? new List<ApiErrorDetail>()
        }
    };
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ApiErrorDetail> Details { get; set; } = new();
}

public class ApiErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
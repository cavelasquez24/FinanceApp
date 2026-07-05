using System.Net;
using System.Text.Json;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Domain.Exceptions;

namespace FinanceApp.API.Middleware;

/// <summary>
/// Middleware que captura todas las excepciones no controladas
/// y las convierte en respuestas HTTP con el formato estándar.
/// Sin este middleware, las excepciones llegarían al cliente
/// como HTML o stack traces.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Pasa el request al siguiente middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            // Captura cualquier excepción no controlada
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            // Error de validación de negocio → 400
            DomainException domainEx => (
                StatusCode: HttpStatusCode.BadRequest,
                Response: ApiResponse<object>.Fail(domainEx.Code, domainEx.Message)
            ),

            // No autenticado o credenciales inválidas → 401
            UnauthorizedException unauthorizedEx => (
                StatusCode: HttpStatusCode.Unauthorized,
                Response: ApiResponse<object>.Fail(unauthorizedEx.Code, unauthorizedEx.Message)
            ),

            // Recurso no encontrado → 404
            NotFoundException notFoundEx => (
                StatusCode: HttpStatusCode.NotFound,
                Response: ApiResponse<object>.Fail("NOT_FOUND", notFoundEx.Message)
            ),

            // Cualquier otro error → 500
            _ => (
                StatusCode: HttpStatusCode.InternalServerError,
                Response: ApiResponse<object>.Fail(
                    "INTERNAL_ERROR",
                    "Ocurrió un error interno. Por favor intenta más tarde.")
            )
        };

        // Logueamos el error completo en el servidor pero nunca lo enviamos al cliente
        if (exception is not DomainException and not NotFoundException and not UnauthorizedException)
            _logger.LogError(exception, "Error no controlado: {Message}", exception.Message);

        context.Response.StatusCode = (int)response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response.Response, jsonOptions));
    }
}
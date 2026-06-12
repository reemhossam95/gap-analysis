using System.Net;
using System.Text.Json;
using ContentGapAnalyzer.Application.Common;
using FluentValidation;

namespace ContentGapAnalyzer.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request was cancelled by the client: {Method} {Path}", 
                context.Request.Method, context.Request.Path);
            
            context.Response.StatusCode = 499; // Client Closed Request
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<object> response;

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationEx.Errors
                    .Select(e => e.ErrorMessage)
                    .Distinct()
                    .ToList();
                response = ApiResponse<object>.Fail("Validation failed", errors);
                break;

            case KeyNotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = ApiResponse<object>.Fail(exception.Message);
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = ApiResponse<object>.Fail("Unauthorized access.");
                break;

            case InvalidOperationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = ApiResponse<object>.Fail(exception.Message);
                break;

            case HttpRequestException httpEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                response = ApiResponse<object>.Fail($"External service error: {httpEx.Message}");
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = ApiResponse<object>.Fail("An unexpected error occurred. Please try again later.");
                break;
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
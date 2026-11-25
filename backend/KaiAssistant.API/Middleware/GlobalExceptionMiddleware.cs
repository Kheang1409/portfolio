using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
namespace KaiAssistant.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;
    private readonly IReadOnlyList<IExceptionHandler> _handlers;
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env,
        IEnumerable<IExceptionHandler> handlers)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _handlers = handlers.OrderBy(h => h.Order).ToArray();
    }
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var includeDetails = _env.IsDevelopment();
        var problem = BuildProblemDetails(context, exception, includeDetails);
        context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }
    private ProblemDetails BuildProblemDetails(HttpContext context, Exception ex, bool includeDetails)
    {
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(ex)) ?? new UnhandledExceptionHandler();
        var problem = handler.CreateProblemDetails(ex, context, includeDetails);
        problem.Instance ??= context.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Extensions["correlationId"] = context.TraceIdentifier;
        problem.Extensions["errorCode"] = MapErrorCode(problem.Status ?? (int)HttpStatusCode.InternalServerError, ex);
        problem.Extensions["retryable"] = IsRetryable(problem.Status ?? (int)HttpStatusCode.InternalServerError, ex);
        return problem;
    }
    private static string MapErrorCode(int statusCode, Exception exception)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest when exception is ValidationException => "VALIDATION_FAILED",
            StatusCodes.Status400BadRequest => "BAD_REQUEST",
            StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
            StatusCodes.Status404NotFound => "NOT_FOUND",
            StatusCodes.Status408RequestTimeout => "REQUEST_TIMEOUT",
            StatusCodes.Status409Conflict => "CONFLICT",
            StatusCodes.Status429TooManyRequests => "RATE_LIMITED",
            StatusCodes.Status503ServiceUnavailable => "SERVICE_UNAVAILABLE",
            _ => "INTERNAL_ERROR"
        };
    }
    private static bool IsRetryable(int statusCode, Exception exception)
    {
        if (exception is TaskCanceledException)
        {
            return true;
        }
        return statusCode is StatusCodes.Status408RequestTimeout or
            StatusCodes.Status429TooManyRequests or
            StatusCodes.Status500InternalServerError or
            StatusCodes.Status502BadGateway or
            StatusCodes.Status503ServiceUnavailable or
            StatusCodes.Status504GatewayTimeout;
    }
}
public interface IExceptionHandler
{
    bool CanHandle(Exception exception);
    ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails);
    int Order { get; }
}
public class ArgumentExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => exception is ArgumentException;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails) =>
        new()
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Invalid argument",
            Detail = includeDetails ? exception.Message : null,
            Type = "https://httpstatuses.com/400"
        };
    public int Order => 1;
}
public class UnauthorizedAccessExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => exception is UnauthorizedAccessException;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails) =>
        new()
        {
            Status = (int)HttpStatusCode.Unauthorized,
            Title = "Unauthorized access",
            Detail = includeDetails ? exception.Message : null,
            Type = "https://httpstatuses.com/401"
        };
    public int Order => 2;
}
public class InvalidOperationExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => exception is InvalidOperationException;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails) =>
        new()
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Invalid operation",
            Detail = includeDetails ? exception.Message : null,
            Type = "https://httpstatuses.com/400"
        };
    public int Order => 3;
}
public class NotFoundExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => exception is KeyNotFoundException;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails) =>
        new()
        {
            Status = (int)HttpStatusCode.NotFound,
            Title = "Resource not found",
            Detail = includeDetails ? exception.Message : null,
            Type = "https://httpstatuses.com/404"
        };
    public int Order => 4;
}
public class ValidationExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => exception is ValidationException;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails)
    {
        var validationException = (ValidationException)exception;
        var problem = new ProblemDetails
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Validation failed",
            Detail = includeDetails ? validationException.Message : "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/400"
        };
        problem.Extensions["errors"] = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        return problem;
    }
    public int Order => 5;
}
public class RequestTimeoutExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => exception is TaskCanceledException;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails) =>
        new()
        {
            Status = (int)HttpStatusCode.RequestTimeout,
            Title = "Request cancelled or timed out",
            Detail = includeDetails ? exception.Message : null,
            Type = "https://httpstatuses.com/408"
        };
    public int Order => 6;
}
public class UnhandledExceptionHandler : IExceptionHandler
{
    public bool CanHandle(Exception exception) => true;
    public ProblemDetails CreateProblemDetails(Exception exception, HttpContext context, bool includeDetails)
    {
        var detail = includeDetails ? exception.ToString() : "An unexpected error occurred.";
        return new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Unexpected error",
            Detail = detail,
            Type = "https://httpstatuses.com/500"
        };
    }
    public int Order => int.MaxValue;
}
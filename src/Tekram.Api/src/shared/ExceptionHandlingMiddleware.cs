namespace Tekram.Api.src.shared;

using System.Net;
using System.Text.Json;
using FluentValidation;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DomainException ex)
        {
            await WriteProblemDetails(context, ex.StatusCode, ex.ErrorCode, ex.Message);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            context.Response.ContentType = "application/problem+json";
            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Validation Failed",
                status = 422,
                detail = "One or more validation errors occurred.",
                error = ErrorCodes.ValidationFailed,
                errors
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetails(context, 500, ErrorCodes.InternalError, "An internal error occurred.");
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string errorCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                422 => "Unprocessable Entity",
                429 => "Too Many Requests",
                _ => "Internal Server Error"
            },
            status = statusCode,
            detail,
            error = errorCode
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

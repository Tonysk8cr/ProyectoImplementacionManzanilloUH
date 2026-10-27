using Microsoft.AspNetCore.Mvc;
using VetPlatform.Domain.Exceptions;

namespace VetPlatform.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log) { _next = next; _log = log; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                ResourceNotFoundException => (StatusCodes.Status404NotFound, "NotFound"),
                VetPlatform.Domain.Exceptions.UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
                BusinessRuleViolationException => (StatusCodes.Status409Conflict, "BusinessRuleViolation"),
                DomainException => (StatusCodes.Status400BadRequest, "DomainError"),
                _ => (StatusCodes.Status500InternalServerError, "ServerError"),
            };
            if (status == 500) _log.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = status;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status, Title = title, Instance = ctx.Request.Path,
                Detail = status == 500 ? "Error interno del servidor." : ex.Message,
            }, options: null, contentType: "application/problem+json");
        }
    }
}

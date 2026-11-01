using Microsoft.AspNetCore.Mvc;
using VetPlatform.Application.Common;

namespace VetPlatform.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId => HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().UserId
        ?? throw new VetPlatform.Domain.Exceptions.UnauthorizedAccessException("Usuario no autenticado.");

    protected IActionResult ToActionResult<T>(Result<T> r, int successStatus = StatusCodes.Status200OK)
    {
        if (r.Success) return StatusCode(successStatus, r.Value);
        var status = r.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        var pd = new ProblemDetails { Status = status, Title = r.ErrorType.ToString(), Detail = string.Join(" ", r.Errors) };
        pd.Extensions["errors"] = r.Errors;
        return StatusCode(status, pd);
    }
}

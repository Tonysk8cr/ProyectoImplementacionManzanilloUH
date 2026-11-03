namespace VetPlatform.Domain.Exceptions;

/// <summary>Raised when an actor is not authorized for a resource (maps to HTTP 403).</summary>
public class UnauthorizedAccessException : DomainException
{
    public UnauthorizedAccessException(string message = "Acceso no autorizado.") : base(message) { }
}

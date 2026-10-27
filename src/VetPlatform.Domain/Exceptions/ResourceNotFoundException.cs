namespace VetPlatform.Domain.Exceptions;

public class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string resource, object id) : base($"{resource} '{id}' no encontrado.") { }
}

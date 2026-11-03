namespace VetPlatform.Domain.Exceptions;

public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message) : base(message) { }
}

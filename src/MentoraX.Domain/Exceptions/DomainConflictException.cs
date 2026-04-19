namespace MentoraX.Domain.Exceptions;

public sealed class DomainConflictException : DomainException
{
    public DomainConflictException(string message, string code = "domain_conflict")
        : base(code, message)
    {
    }
}
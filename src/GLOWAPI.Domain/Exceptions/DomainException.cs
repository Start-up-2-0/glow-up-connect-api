namespace GLOWAPI.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message, string code) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class LocalizacaoClienteInvalidaException : DomainException
{
    public const string ErrorCode = "LOCALIZACAO_CLIENTE_INVALIDA";

    public LocalizacaoClienteInvalidaException()
        : base("Nao foi possivel identificar a cidade a partir da localizacao informada.", ErrorCode)
    {
    }

    public LocalizacaoClienteInvalidaException(string message)
        : base(message, ErrorCode)
    {
    }
}

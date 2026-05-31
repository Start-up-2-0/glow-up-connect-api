namespace GLOWAPI.Domain.Exceptions.Negocios;

public class EnderecoOperacaoInvalidoException : DomainException
{
    public const string ErrorCode = "ENDERECO_OPERACAO_INVALIDO";

    public EnderecoOperacaoInvalidoException(string message)
        : base(message, ErrorCode)
    {
    }
}

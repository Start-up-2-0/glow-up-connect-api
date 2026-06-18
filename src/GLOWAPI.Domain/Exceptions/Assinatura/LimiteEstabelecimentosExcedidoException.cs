using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class LimiteEstabelecimentosExcedidoException : DomainException
{
    public const string ErrorCode = "LIMITE_ESTABELECIMENTOS_EXCEDIDO";

    public LimiteEstabelecimentosExcedidoException()
        : base(
            "Limite de unidades do plano atingido. Remova uma unidade ou faca upgrade.",
            ErrorCode)
    {
    }
}

using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public class TrocarPlanoAssinaturaRequestDto
{
    public int NovoPlanoId { get; set; }
    public GatewayPagamento? Gateway { get; set; }
    public MetodoPagamentoAssinatura MetodoPagamento { get; set; } = MetodoPagamentoAssinatura.Cartao;
}

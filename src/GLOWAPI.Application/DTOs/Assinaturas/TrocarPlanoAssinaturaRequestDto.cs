using GLOWAPI.Domain.Enums;
using GLOWAPI.Application.DTOs.Pagamentos;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public class TrocarPlanoAssinaturaRequestDto
{
    public int NovoPlanoId { get; set; }
    public GatewayPagamento? Gateway { get; set; }
    public PagamentoTransparenteMercadoPagoDto? Pagamento { get; set; }
}

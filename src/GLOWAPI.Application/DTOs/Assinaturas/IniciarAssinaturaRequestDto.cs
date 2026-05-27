using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public class IniciarAssinaturaRequestDto
{
    public int PlanoId { get; set; }
    public TipoAssinatura TipoAssinatura { get; set; }
    public int? EstabelecimentoId { get; set; }
    public CriarEstabelecimentoAssinaturaDto? Estabelecimento { get; set; }
    public int? ProfissionalAutonomoId { get; set; }
    public GatewayPagamento Gateway { get; set; } = GatewayPagamento.MercadoPago;
}

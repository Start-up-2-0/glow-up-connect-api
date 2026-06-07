using GLOWAPI.Domain.Enums;
using GLOWAPI.Application.DTOs.Pagamentos;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public class IniciarAssinaturaRequestDto
{
    public int PlanoId { get; set; }
    public TipoAssinatura TipoAssinatura { get; set; }
    public int? EstabelecimentoId { get; set; }
    public CriarEstabelecimentoAssinaturaDto? Estabelecimento { get; set; }
    public int? ProfissionalAutonomoId { get; set; }
    public CriarProfissionalAutonomoAssinaturaDto? ProfissionalAutonomo { get; set; }
    public GatewayPagamento Gateway { get; set; } = GatewayPagamento.MercadoPago;
    public int DiaVencimento { get; set; } = 10;
    public PagamentoTransparenteMercadoPagoDto? Pagamento { get; set; }
}

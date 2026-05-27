using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class GatewayPagamentoResolver : IGatewayPagamentoResolver
{
    private readonly IEnumerable<IGatewayPagamento> _gateways;

    public GatewayPagamentoResolver(IEnumerable<IGatewayPagamento> gateways)
    {
        _gateways = gateways;
    }

    public IGatewayPagamento Resolver(GatewayPagamento gateway)
    {
        var resolvedor = _gateways.FirstOrDefault(provedor => provedor.GatewaySuportado == gateway);
        if (resolvedor is null)
        {
            throw new InvalidOperationException($"Gateway de pagamento nao configurado: {gateway}.");
        }

        return resolvedor;
    }
}

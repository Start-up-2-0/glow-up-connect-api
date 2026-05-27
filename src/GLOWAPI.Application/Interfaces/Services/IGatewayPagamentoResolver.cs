using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IGatewayPagamentoResolver
{
    IGatewayPagamento Resolver(GatewayPagamento gateway);
}

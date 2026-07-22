using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaEncerramentoService
{
    Task EncerrarAsync(
        Assinatura assinatura,
        AssinaturaStatus statusFinal,
        string eventoHistorico,
        string observacao,
        Pagamento? pagamento = null,
        CancellationToken cancellationToken = default);

    Task ProcessarCancelamentosAgendadosAsync(
        DateTime dataReferenciaUtc,
        CancellationToken cancellationToken = default);
}

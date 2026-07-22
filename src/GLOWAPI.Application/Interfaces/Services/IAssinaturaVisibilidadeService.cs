using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaVisibilidadeService
{
    Task OcultarLojasVinculadasAsync(Assinatura assinatura, CancellationToken cancellationToken = default);

    Task ReexibirLojasVinculadasAsync(Assinatura assinatura, CancellationToken cancellationToken = default);
}

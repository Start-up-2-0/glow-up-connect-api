using GLOWAPI.Application.DTOs.Agenda;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAgendaNegocioService
{
    Task<IReadOnlyList<AgendaGeralResponseDto>> ListarAgendaGeralAsync(
        int estabelecimentoId,
        AgendaGeralFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgendaProfissionalResponseDto>> ListarAgendaProfissionalAsync(
        int estabelecimentoId,
        AgendaProfissionalFiltroDto filtro,
        CancellationToken cancellationToken = default);
}

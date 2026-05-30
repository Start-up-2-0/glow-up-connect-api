using GLOWAPI.Application.DTOs.Horarios;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IDisponibilidadeAgendaService
{
    Task<DisponibilidadeAgendaResponseDto> ConsultarPorEstabelecimentoAsync(
        int estabelecimentoId,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default);

    Task<DisponibilidadeAgendaResponseDto> ConsultarPublicoPorEstabelecimentoAsync(
        Guid publicGuid,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default);

    Task<DisponibilidadeAgendaResponseDto> ConsultarPublicoPorProfissionalAsync(
        Guid publicGuid,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default);

    Task<DisponibilidadeAgendaResponseDto> ConsultarPublicoPorProfissionalAutonomoAsync(
        Guid publicGuid,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default);
}

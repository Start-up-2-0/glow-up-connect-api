using GLOWAPI.Application.DTOs.Estabelecimentos;

namespace GLOWAPI.Application.DTOs.Dashboard;

public record DashboardClienteAgendamentoItemDto(
    int Id,
    int ServicoId,
    string ServicoNome,
    int ProfissionalId,
    string ProfissionalNome,
    DateTime Inicio,
    DateTime Fim,
    decimal Valor,
    string Status);

public record DashboardClienteAgendamentoDto(
    int Id,
    string Status,
    decimal ValorTotal,
    DateTime Inicio,
    DateTime Fim,
    Guid EstabelecimentoPublicGuid,
    string EstabelecimentoNome,
    string EstabelecimentoLogo,
    EnderecoResumoDto? Endereco,
    IReadOnlyList<DashboardClienteAgendamentoItemDto> Itens);

public record DashboardClienteLojaFrequenteDto(
    string Nome,
    int Visitas,
    Guid PublicGuid);

public record DashboardClienteProfissionalFrequenteDto(
    string Nome,
    int Visitas,
    string EstabelecimentoNome);

public record DashboardClienteServicoFrequenteDto(
    string Nome,
    int Vezes);

public record DashboardClienteUltimaVisitaDto(
    DateTime Data,
    string Servico,
    string Estabelecimento);

public record DashboardClienteRelacionamentoDto(
    DashboardClienteLojaFrequenteDto? LojaMaisFrequente,
    DashboardClienteProfissionalFrequenteDto? ProfissionalMaisFrequente,
    DashboardClienteServicoFrequenteDto? ServicoMaisContratado,
    DashboardClienteUltimaVisitaDto? UltimaVisita,
    DateTime? ClienteDesde,
    int TotalAtendimentos,
    decimal TotalGastoAcumulado,
    int? FrequenciaMediaDias);

public record DashboardClienteTimelineGrupoDto(
    string Id,
    string Label,
    IReadOnlyList<DashboardClienteAgendamentoDto> Items);

public record DashboardClienteResponseDto(
    decimal TotalGastoMes,
    decimal TotalGastoMesAnterior,
    int AtendimentosMes,
    int TotalAgendamentos,
    DashboardClienteAgendamentoDto? ProximoAgendamento,
    IReadOnlyList<DashboardClienteTimelineGrupoDto> HistoricoTimeline,
    DashboardClienteRelacionamentoDto Relacionamento);

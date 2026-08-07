using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.DTOs.Avaliacao;

namespace GLOWAPI.Application.DTOs.Dashboard;

public record DashboardNegocioReceitaDiaDto(string Data, decimal Total);

public record DashboardNegocioDistribuicaoServicoDto(string Nome, int Quantidade);

public record DashboardNegocioProfissionalDto(
    int Id,
    int ProfissionalId,
    string NomePublico,
    bool Ativo,
    bool PodeReceberAgendamento,
    decimal? NotaMedia,
    int AgendamentosHoje);

public record DashboardNegocioResponseDto(
    decimal TotalGanhoMes,
    decimal TotalGanhoMesAnterior,
    decimal TotalGanhoHoje,
    decimal TotalGanhoSemana,
    decimal TotalGanhoSemanaAnterior,
    int AgendamentosHoje,
    int AgendamentosOntem,
    int AgendamentosSemana,
    int CancelamentosHoje,
    int ClientesAtivos,
    int ServicosAtivos,
    IReadOnlyList<DashboardNegocioProfissionalDto> Profissionais,
    IReadOnlyList<AgendaGeralResponseDto> UltimosAtendimentos,
    IReadOnlyList<AgendaGeralResponseDto> ProximosAtendimentos,
    AvaliacaoResumoPublicoDto? AvaliacaoResumo,
    IReadOnlyList<DashboardNegocioReceitaDiaDto> ReceitaUltimos7Dias,
    IReadOnlyList<DashboardNegocioReceitaDiaDto> ReceitaUltimos30Dias,
    IReadOnlyList<DashboardNegocioDistribuicaoServicoDto> DistribuicaoServicos);

using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using AgendamentoEntity = GLOWAPI.Domain.Entities.Agendamento;

namespace GLOWAPI.Application.DTOs.Agendamento;

public record AgendamentoClienteResponseDto(
    int Id,
    string Status,
    decimal ValorTotal,
    int DuracaoTotalMinutos,
    DateTime Inicio,
    DateTime Fim,
    Guid EstabelecimentoPublicGuid,
    string EstabelecimentoNome,
    string EstabelecimentoLogo,
    EnderecoResumoDto? Endereco,
    string Observacao,
    string Origem,
    DateTime CreateAd,
    DateTime? CanceladoEm,
    IReadOnlyList<AgendamentoClienteItemResponseDto> Itens,
    string AvaliacaoStatus = "Indisponivel",
    AvaliacaoResumoClienteDto? AvaliacaoResumo = null)
{
    public static AgendamentoClienteResponseDto From(AgendamentoEntity agendamento) =>
        From(agendamento, "Indisponivel", null, incluirLogo: true);

    public static AgendamentoClienteResponseDto From(
        AgendamentoEntity agendamento,
        string avaliacaoStatus,
        AvaliacaoResumoClienteDto? avaliacaoResumo) =>
        From(agendamento, avaliacaoStatus, avaliacaoResumo, incluirLogo: true);

    public static AgendamentoClienteResponseDto From(
        AgendamentoEntity agendamento,
        string avaliacaoStatus,
        AvaliacaoResumoClienteDto? avaliacaoResumo,
        bool incluirLogo)
    {
        var itens = agendamento.Itens.OrderBy(item => item.Inicio).ToList();
        var estabelecimento = agendamento.Estabelecimento;
        EnderecoResumoDto? endereco = null;

        if (estabelecimento?.Endereco is not null)
        {
            var end = estabelecimento.Endereco;
            endereco = new EnderecoResumoDto(end.Logradouro, end.Bairro, end.Cidade, end.Estado);
        }

        return new AgendamentoClienteResponseDto(
            agendamento.Id,
            agendamento.Status.ToString(),
            agendamento.ValorTotal,
            AgendamentoHorarioHelper.ObterDuracaoTotalMinutos(agendamento),
            AgendamentoHorarioHelper.ObterInicio(agendamento),
            AgendamentoHorarioHelper.ObterFim(agendamento),
            estabelecimento?.PublicGuid ?? Guid.Empty,
            estabelecimento?.Nome ?? string.Empty,
            // Listagens omitem logo (base64) para evitar payloads de vários MB.
            incluirLogo ? estabelecimento?.Logo ?? string.Empty : string.Empty,
            endereco,
            agendamento.Observacao,
            agendamento.Origem.ToString(),
            agendamento.CreateAd,
            agendamento.CanceladoEm,
            itens.Select(AgendamentoClienteItemResponseDto.From).ToList(),
            avaliacaoStatus,
            avaliacaoResumo);
    }
}

public record AgendamentoClienteItemResponseDto(
    int Id,
    int ServicoId,
    string ServicoNome,
    int ProfissionalId,
    string ProfissionalNome,
    DateTime Inicio,
    DateTime Fim,
    decimal Valor,
    string Status)
{
    public static AgendamentoClienteItemResponseDto From(AgendamentoItem item) =>
        new(
            item.Id,
            item.ServicoId,
            item.Servico?.Nome ?? string.Empty,
            item.ProfissionalId,
            item.Profissional?.NomePublico ?? string.Empty,
            item.Inicio,
            item.Fim,
            item.Valor,
            item.Status.ToString());
}

public record AgendamentosClientePaginadoResponseDto(
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<AgendamentoClienteResponseDto> Itens);

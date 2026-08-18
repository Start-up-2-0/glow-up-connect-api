using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Agendamento;

public record ReceberAgendamentoRequestDto(
    FormaRecebimentoPresencial FormaRecebimento,
    decimal? Valor = null);

public record ReceberAgendamentoResponseDto(
    int AgendamentoId,
    string Status,
    int PagamentoId,
    int LancamentoCaixaId,
    decimal ValorRecebido,
    string FormaRecebimento,
    IReadOnlyList<int> LancamentosComissaoIds);

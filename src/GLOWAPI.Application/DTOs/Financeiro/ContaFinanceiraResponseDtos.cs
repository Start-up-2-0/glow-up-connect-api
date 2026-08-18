using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Financeiro;

public record ContaReceberResponseDto(
    int Id,
    int EstabelecimentoId,
    int? AgendamentoId,
    string Descricao,
    decimal Valor,
    DateTime Vencimento,
    string Status);

public record ContaPagarResponseDto(
    int Id,
    int EstabelecimentoId,
    string Fornecedor,
    string Categoria,
    string Descricao,
    decimal Valor,
    DateTime Vencimento,
    bool Recorrente,
    string Status);

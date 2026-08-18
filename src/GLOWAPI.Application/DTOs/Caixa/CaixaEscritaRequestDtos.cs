using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Caixa;

public record RegistrarAjusteCaixaRequestDto(
    SubtipoAjusteManual Subtipo,
    decimal Valor,
    string Descricao);

public record EstornarLancamentoCaixaRequestDto(string Motivo);

public record AbrirSessaoCaixaRequestDto(decimal SaldoInicial);

public record FecharSessaoCaixaRequestDto(decimal SaldoInformadoFechamento);

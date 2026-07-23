namespace GLOWAPI.Application.DTOs.Assinaturas;

public record PromocaoLancamentoStatusDto(
    bool Disponivel,
    int VagasRestantes,
    int DiasTrial,
    decimal PercentualDescontoMensalidade,
    int DiasAntecedenciaAlertaFatura,
    int DiasAntecedenciaGeracaoCobranca,
    int DiasToleranciaInadimplencia);

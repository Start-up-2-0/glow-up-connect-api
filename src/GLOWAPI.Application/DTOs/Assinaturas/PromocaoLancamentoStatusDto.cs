namespace GLOWAPI.Application.DTOs.Assinaturas;

public record PromocaoLancamentoStatusDto(
    bool Disponivel,
    int VagasRestantes,
    int DiasTrial,
    int DiasAntecedenciaAlertaFatura,
    int DiasAntecedenciaGeracaoCobranca,
    int DiasToleranciaInadimplencia);

namespace GLOWAPI.Application.DTOs.Assinaturas;

public record PromocaoLancamentoStatusDto(
    bool Disponivel,
    int VagasRestantes,
    int DiasTrial,
    int[] DiasVencimentoPermitidos,
    int DiasAntecedenciaAlertaFatura,
    int DiasAntecedenciaGeracaoCobranca);

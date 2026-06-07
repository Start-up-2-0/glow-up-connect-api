namespace GLOWAPI.Application.DTOs.Assinaturas;

public record CicloCobrancaDatasDto(
    DateTime Vencimento,
    DateTime Geracao,
    DateTime Alerta);

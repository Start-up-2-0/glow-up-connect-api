using GLOWAPI.Application.DTOs.Caixa;

namespace GLOWAPI.Application.Interfaces.Services;

public interface ISessaoCaixaNegocioService
{
    Task<SessaoCaixaResponseDto> ObterSessaoAtualAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<SessaoCaixaResponseDto> AbrirSessaoAsync(
        int estabelecimentoId,
        AbrirSessaoCaixaRequestDto request,
        CancellationToken cancellationToken = default);

    Task<SessaoCaixaResponseDto> FecharSessaoAsync(
        int estabelecimentoId,
        int sessaoId,
        FecharSessaoCaixaRequestDto request,
        CancellationToken cancellationToken = default);
}

public record SessaoCaixaResponseDto(
    int Id,
    int CaixaId,
    int UsuarioId,
    DateTime AbertoEm,
    DateTime? FechadoEm,
    decimal SaldoInicial,
    decimal? SaldoInformadoFechamento,
    decimal? Diferenca,
    string Status);

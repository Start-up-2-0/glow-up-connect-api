using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.DTOs.Mensageria;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IEstabelecimentoPerfilService
{
    Task<EstabelecimentoPerfilResponseDto> ObterAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<EstabelecimentoPerfilResponseDto> AtualizarAsync(
        int estabelecimentoId,
        AtualizarEstabelecimentoPerfilDto request,
        CancellationToken cancellationToken = default);

    Task<WhatsAppConfirmacaoInstrucoesDto> SolicitarConfirmacaoWhatsAppAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task AtualizarWhatsAppOptInAsync(
        int estabelecimentoId,
        bool optIn,
        CancellationToken cancellationToken = default);
}

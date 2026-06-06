using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConfirmacaoWhatsAppService
{
    Task<WhatsAppConfirmacaoInstrucoesDto> IniciarConfirmacaoAsync(
        Usuario usuario,
        CancellationToken cancellationToken = default);

    Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default);

    Task ConfirmarPorCodigoAsync(string telefone, string codigo, CancellationToken cancellationToken = default);

    Task<WhatsAppConfirmacaoInboundResultado> TentarConfirmarPorMensagemInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default);

    Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoRespostaInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default);

    Task ReenviarConfirmacaoAsync(string email, CancellationToken cancellationToken = default);
}

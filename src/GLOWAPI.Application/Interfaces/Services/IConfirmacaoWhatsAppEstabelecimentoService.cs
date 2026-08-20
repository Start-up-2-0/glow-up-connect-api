using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IConfirmacaoWhatsAppEstabelecimentoService
{
    Task<WhatsAppConfirmacaoInstrucoesDto> IniciarConfirmacaoAsync(
        Estabelecimento estabelecimento,
        IReadOnlyList<string> emailsDestino,
        CancellationToken cancellationToken = default);

    Task IniciarAposCriacaoAsync(
        Estabelecimento estabelecimento,
        Usuario? usuario,
        CancellationToken cancellationToken = default);

    Task ConfirmarPorCodigoAsync(
        int estabelecimentoId,
        string codigo,
        CancellationToken cancellationToken = default);

    Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<WhatsAppConfirmacaoInboundResultado> TentarConfirmarPorMensagemInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default);

    Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoRespostaInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default);
}

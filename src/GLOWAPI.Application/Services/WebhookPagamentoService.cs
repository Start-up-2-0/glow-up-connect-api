using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Pagamentos;

namespace GLOWAPI.Application.Services;

public class WebhookPagamentoService : IWebhookPagamentoService
{
    private readonly IWebhookPagamentoRepository _webhookPagamentoRepository;

    public WebhookPagamentoService(IWebhookPagamentoRepository webhookPagamentoRepository)
    {
        _webhookPagamentoRepository = webhookPagamentoRepository;
    }

    public async Task<WebhookPagamentoResponseDto> RegistrarAsync(
        RegistrarWebhookPagamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        Validar(request);

        var eventId = request.EventId.Trim();
        var webhookExistente = await _webhookPagamentoRepository.ObterPorEventoAsync(
            request.Gateway,
            eventId,
            cancellationToken);

        if (webhookExistente is not null)
        {
            return WebhookPagamentoResponseDto.From(webhookExistente, duplicado: true);
        }

        var webhook = new WebhookPagamento
        {
            Gateway = request.Gateway,
            EventId = eventId,
            EventType = request.EventType.Trim(),
            Payload = request.Payload.Trim(),
            Processado = false
        };

        await _webhookPagamentoRepository.AdicionarAsync(webhook, cancellationToken);
        await _webhookPagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        return WebhookPagamentoResponseDto.From(webhook, duplicado: false);
    }

    private static void Validar(RegistrarWebhookPagamentoRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            throw new WebhookPagamentoInvalidoException("EventId do webhook de pagamento e obrigatorio.");
        }

        if (request.EventId.Length > 150)
        {
            throw new WebhookPagamentoInvalidoException("EventId do webhook de pagamento deve ter no maximo 150 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            throw new WebhookPagamentoInvalidoException("EventType do webhook de pagamento e obrigatorio.");
        }

        if (request.EventType.Length > 120)
        {
            throw new WebhookPagamentoInvalidoException("EventType do webhook de pagamento deve ter no maximo 120 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            throw new WebhookPagamentoInvalidoException("Payload do webhook de pagamento e obrigatorio.");
        }
    }
}

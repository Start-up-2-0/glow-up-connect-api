namespace GLOWAPI.Application.Options;

public class WebhookPagamentoOptions
{
    public const string SectionName = "WebhookPagamento";
    public const string SecretHeaderName = "X-Glow-Webhook-Secret";

    /// <summary>
    /// Segredo exigido no header <see cref="SecretHeaderName"/> para POST /api/webhooks/pagamentos
    /// (entrada genérica). Em Production, se vazio, o endpoint fica desabilitado.
    /// Em Development/Testing, vazio permite o endpoint (facilita testes locais).
    /// </summary>
    public string RegistrarSecret { get; set; } = string.Empty;
}

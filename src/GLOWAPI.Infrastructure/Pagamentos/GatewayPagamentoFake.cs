using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Infrastructure.Pagamentos;

public class GatewayPagamentoFake : IGatewayPagamento
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GatewayPagamento GatewaySuportado { get; }

    public GatewayPagamentoFake(GatewayPagamento gatewaySuportado)
    {
        GatewaySuportado = gatewaySuportado;
    }

    public Task<CriarCobrancaGatewayResponse> CriarCobrancaAsync(
        CriarCobrancaGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Gateway != GatewaySuportado)
        {
            var payload = SerializarRequest(request);
            return Task.FromResult(CriarCobrancaGatewayResponse.Falha(
                payload,
                "{}",
                $"Gateway incompativel. Esperado {GatewaySuportado}, recebido {request.Gateway}."));
        }

        var gatewayPaymentId = $"{GatewaySuportado.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
        var requestPayload = SerializarRequest(request);
        var responsePayload = JsonSerializer.Serialize(new
        {
            id = gatewayPaymentId,
            status = "pending",
            reference = request.ReferenciaInterna
        }, JsonOptions);

        return Task.FromResult(new CriarCobrancaGatewayResponse(
            Sucesso: true,
            GatewayPaymentId: gatewayPaymentId,
            CheckoutUrl: $"https://checkout.fake.glowupconnect.local/{gatewayPaymentId}",
            QrCode: string.Empty,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload,
            MetodoPagamento: request.PagamentoTransparente?.PaymentMethodId ?? "fake"));
    }

    public Task<ConsultarPagamentoGatewayResponse> ConsultarPagamentoAsync(
        string gatewayPaymentId,
        CancellationToken cancellationToken = default)
    {
        var responsePayload = JsonSerializer.Serialize(new
        {
            id = gatewayPaymentId,
            status = "approved"
        }, JsonOptions);

        return Task.FromResult(new ConsultarPagamentoGatewayResponse(
            Sucesso: true,
            GatewayPaymentId: gatewayPaymentId,
            Status: "approved",
            ResponsePayload: responsePayload));
    }

    public Task<IReadOnlyList<string>> ListarPagamentosDaOrdemAsync(
        string ordemId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(
            string.IsNullOrWhiteSpace(ordemId) ? Array.Empty<string>() : new[] { ordemId });

    public Task<CriarAssinaturaRecorrenteGatewayResponse> CriarAssinaturaRecorrenteAsync(
        CriarAssinaturaRecorrenteGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Gateway != GatewaySuportado)
        {
            var payload = SerializarAssinaturaRequest(request);
            return Task.FromResult(CriarAssinaturaRecorrenteGatewayResponse.Falha(
                payload,
                "{}",
                $"Gateway incompativel. Esperado {GatewaySuportado}, recebido {request.Gateway}."));
        }

        var subscriptionId = $"{GatewaySuportado.ToString().ToLowerInvariant()}-sub-{Guid.NewGuid():N}";
        var requestPayload = SerializarAssinaturaRequest(request);
        var responsePayload = JsonSerializer.Serialize(new
        {
            id = subscriptionId,
            status = "authorized",
            payer_id = $"fake-customer-{Guid.NewGuid():N}"
        }, JsonOptions);

        return Task.FromResult(new CriarAssinaturaRecorrenteGatewayResponse(
            Sucesso: true,
            GatewaySubscriptionId: subscriptionId,
            GatewayCustomerId: $"fake-customer-{Guid.NewGuid():N}",
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload));
    }

    private static string SerializarRequest(CriarCobrancaGatewayRequest request) =>
        JsonSerializer.Serialize(new
        {
            gateway = request.Gateway.ToString(),
            reference = request.ReferenciaInterna,
            description = request.Descricao,
            amount = request.Valor,
            currency = request.Moeda,
            paymentMethod = request.PagamentoTransparente?.PaymentMethodId ?? "fake",
            payer = new
            {
                name = request.PagadorNome,
                email = request.PagadorEmail
            },
            expiresAt = request.ExpiraEm,
            metadata = request.Metadados
        }, JsonOptions);

    private static string SerializarAssinaturaRequest(CriarAssinaturaRecorrenteGatewayRequest request) =>
        JsonSerializer.Serialize(new
        {
            gateway = request.Gateway.ToString(),
            reference = request.ReferenciaInterna,
            description = request.Descricao,
            amount = request.Valor,
            currency = request.Moeda,
            diasTrial = request.DiasTrial,
            primeiraCobrancaEm = request.PrimeiraCobrancaEm,
            payer = new
            {
                name = request.PagadorNome,
                email = request.PagadorEmail
            },
            metadata = request.Metadados
        }, JsonOptions);
}

using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class AssinaturaHistoricoService : IAssinaturaHistoricoService
{
    private readonly IRepository<AssinaturaHistorico> _assinaturaHistoricoRepository;
    private readonly IRepository<PagamentoHistorico> _pagamentoHistoricoRepository;
    private readonly IRepository<AssinaturaRecorrenciaHistorico> _recorrenciaHistoricoRepository;

    public AssinaturaHistoricoService(
        IRepository<AssinaturaHistorico> assinaturaHistoricoRepository,
        IRepository<PagamentoHistorico> pagamentoHistoricoRepository,
        IRepository<AssinaturaRecorrenciaHistorico> recorrenciaHistoricoRepository)
    {
        _assinaturaHistoricoRepository = assinaturaHistoricoRepository;
        _pagamentoHistoricoRepository = pagamentoHistoricoRepository;
        _recorrenciaHistoricoRepository = recorrenciaHistoricoRepository;
    }

    public Task RegistrarAssinaturaAsync(
        Assinatura assinatura,
        string evento,
        AssinaturaStatus? statusAnterior,
        AssinaturaStatus statusNovo,
        Pagamento? pagamento = null,
        string observacao = "",
        string payloadJson = "",
        CancellationToken cancellationToken = default) =>
        _assinaturaHistoricoRepository.AdicionarAsync(new AssinaturaHistorico
        {
            Assinatura = assinatura,
            AssinaturaId = assinatura.Id,
            Pagamento = pagamento,
            PagamentoId = pagamento?.Id > 0 ? pagamento.Id : null,
            Evento = evento,
            StatusAnterior = statusAnterior,
            StatusNovo = statusNovo,
            PlanoId = assinatura.PlanoId,
            PlanoAlteracaoPendenteId = assinatura.PlanoAlteracaoPendenteId,
            Observacao = observacao,
            PayloadJson = payloadJson
        }, cancellationToken);

    public Task RegistrarPagamentoAsync(
        Pagamento pagamento,
        string evento,
        PagamentoStatus? statusAnterior,
        PagamentoStatus statusNovo,
        string observacao = "",
        string payloadJson = "",
        CancellationToken cancellationToken = default) =>
        _pagamentoHistoricoRepository.AdicionarAsync(new PagamentoHistorico
        {
            Pagamento = pagamento,
            PagamentoId = pagamento.Id,
            Assinatura = pagamento.Assinatura,
            AssinaturaId = pagamento.AssinaturaId,
            Evento = evento,
            StatusAnterior = statusAnterior,
            StatusNovo = statusNovo,
            Gateway = pagamento.Gateway,
            GatewayPaymentId = pagamento.GatewayPaymentId,
            MetodoPagamento = pagamento.MetodoPagamento,
            Valor = pagamento.Valor,
            Moeda = pagamento.Moeda,
            Observacao = observacao,
            PayloadJson = payloadJson
        }, cancellationToken);

    public Task RegistrarRecorrenciaAsync(
        Assinatura assinatura,
        string evento,
        string status,
        Pagamento? pagamento = null,
        DateTime? cicloInicio = null,
        DateTime? cicloFim = null,
        string observacao = "",
        string payloadJson = "",
        CancellationToken cancellationToken = default) =>
        _recorrenciaHistoricoRepository.AdicionarAsync(new AssinaturaRecorrenciaHistorico
        {
            Assinatura = assinatura,
            AssinaturaId = assinatura.Id,
            Pagamento = pagamento,
            PagamentoId = pagamento?.Id > 0 ? pagamento.Id : null,
            Evento = evento,
            Status = status,
            CicloInicio = cicloInicio,
            CicloFim = cicloFim,
            RenovacaoAutomatica = assinatura.RenovacaoAutomatica,
            Observacao = observacao,
            PayloadJson = payloadJson
        }, cancellationToken);
}

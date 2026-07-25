using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class MetaNegocioService : IMetaNegocioService
{
    private readonly IMetaRepository _metaRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IAgendamentoRepository _agendamentoRepository;

    public MetaNegocioService(
        IMetaRepository metaRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IAgendamentoRepository agendamentoRepository)
    {
        _metaRepository = metaRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _agendamentoRepository = agendamentoRepository;
    }

    public async Task<IReadOnlyList<MetaResponseDto>> ListarMetasAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var metas = await _metaRepository.ListarAtivasPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return metas.Select(MetaResponseDto.From).ToList();
    }

    public async Task<MetaResponseDto> CriarMetaAsync(
        int estabelecimentoId,
        CriarMetaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.MetaGerenciar,
            cancellationToken);

        ValidarMetaRequest(request.Nome, request.ValorMeta, request.PercentualComissao);

        var meta = new Meta
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = request.Nome.Trim(),
            TipoMeta = request.TipoMeta,
            ValorMeta = request.ValorMeta,
            PercentualComissao = request.PercentualComissao,
            Ativa = true,
            CreateAd = DateTime.UtcNow
        };

        await _metaRepository.AdicionarAsync(meta, cancellationToken);
        await _metaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.MetaCriada,
            nameof(Meta),
            meta.Id,
            request,
            cancellationToken);

        return MetaResponseDto.From(meta);
    }

    public async Task<MetaResponseDto> AtualizarMetaAsync(
        int estabelecimentoId,
        int metaId,
        AtualizarMetaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.MetaGerenciar,
            cancellationToken);

        ValidarMetaRequest(request.Nome, request.ValorMeta, request.PercentualComissao);

        var meta = await _metaRepository.ObterPorIdComTrackingAsync(metaId, cancellationToken);
        if (meta is null)
        {
            throw new MetaNegocioInvalidaException("Meta nao encontrada.");
        }

        if (meta.EstabelecimentoId != estabelecimentoId)
        {
            throw new MetaNegocioInvalidaException("Meta fora do estabelecimento.");
        }

        meta.Nome = request.Nome.Trim();
        meta.TipoMeta = request.TipoMeta;
        meta.ValorMeta = request.ValorMeta;
        meta.PercentualComissao = request.PercentualComissao;
        meta.Ativa = request.Ativa;
        meta.UpdatedAt = DateTime.UtcNow;

        _metaRepository.Atualizar(meta);
        await _metaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.MetaAlterada,
            nameof(Meta),
            meta.Id,
            request,
            cancellationToken);

        return MetaResponseDto.From(meta);
    }

    public async Task DesativarMetaAsync(
        int estabelecimentoId,
        int metaId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.MetaGerenciar,
            cancellationToken);

        var meta = await _metaRepository.ObterPorIdComTrackingAsync(metaId, cancellationToken);
        if (meta is null)
        {
            throw new MetaNegocioInvalidaException("Meta nao encontrada.");
        }

        if (meta.EstabelecimentoId != estabelecimentoId)
        {
            throw new MetaNegocioInvalidaException("Meta fora do estabelecimento.");
        }

        meta.Ativa = false;
        meta.UpdatedAt = DateTime.UtcNow;

        _metaRepository.Atualizar(meta);
        await _metaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.MetaDesativada,
            nameof(Meta),
            meta.Id,
            new { meta.Id },
            cancellationToken);
    }

    public async Task<IReadOnlyList<MetaProgressoProfissionalDto>> ListarProgressoAsync(
        int estabelecimentoId,
        int? metaId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var metas = metaId.HasValue
            ? new[] { await _metaRepository.ObterPorIdComTrackingAsync(metaId.Value, cancellationToken) }
                .Where(m => m is not null && m.EstabelecimentoId == estabelecimentoId && m.Ativa)
                .Cast<Meta>()
                .ToList()
            : (await _metaRepository.ListarAtivasPorEstabelecimentoAsync(estabelecimentoId, cancellationToken))
                .ToList();

        if (metas.Count == 0)
        {
            return Array.Empty<MetaProgressoProfissionalDto>();
        }

        var profissionais = await _profissionalEstabelecimentoRepository
            .ListarAtivosPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);

        if (profissionais.Count == 0)
        {
            return Array.Empty<MetaProgressoProfissionalDto>();
        }

        var resultado = new List<MetaProgressoProfissionalDto>();
        var agora = DateTime.UtcNow;
        var inicioMes = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);

        foreach (var meta in metas)
        {
            foreach (var vinculo in profissionais)
            {
                var agendamentos = await _agendamentoRepository
                    .ListarConcluidosPorProfissionalNoPeriodoAsync(
                        vinculo.ProfissionalId,
                        inicioMes,
                        fimMes,
                        cancellationToken);

                var itensConcluidos = agendamentos
                    .SelectMany(a => a.Itens)
                    .Where(i => i.ProfissionalId == vinculo.ProfissionalId
                        && i.Status == AgendamentoItemStatus.Concluido
                        && i.Inicio >= inicioMes
                        && i.Inicio < fimMes)
                    .ToList();

                var quantidadeRealizada = itensConcluidos.Count;
                var valorRealizado = itensConcluidos.Sum(i => i.Valor);

                decimal percentualProgresso;
                switch (meta.TipoMeta)
                {
                    case TipoMeta.Atendimentos:
                        percentualProgresso = CalcularPercentual(quantidadeRealizada, meta.ValorMeta);
                        break;
                    case TipoMeta.Faturamento:
                        percentualProgresso = CalcularPercentual(valorRealizado, meta.ValorMeta);
                        break;
                    case TipoMeta.Mista:
                        var percAtendimentos = CalcularPercentual(quantidadeRealizada, meta.ValorMeta);
                        var percFaturamento = CalcularPercentual(valorRealizado, meta.ValorMeta);
                        percentualProgresso = Math.Max(percAtendimentos, percFaturamento);
                        break;
                    default:
                        percentualProgresso = 0;
                        break;
                }

                resultado.Add(new MetaProgressoProfissionalDto(
                    vinculo.Id,
                    vinculo.ProfissionalId,
                    vinculo.Profissional?.NomePublico ?? $"Profissional #{vinculo.ProfissionalId}",
                    meta.Nome,
                    meta.TipoMeta.ToString(),
                    meta.ValorMeta,
                    meta.PercentualComissao,
                    quantidadeRealizada,
                    valorRealizado,
                    percentualProgresso,
                    percentualProgresso >= 100));
            }
        }

        return resultado;
    }

    private static void ValidarMetaRequest(string nome, decimal valorMeta, decimal percentualComissao)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new MetaNegocioInvalidaException("Nome da meta e obrigatorio.");
        }

        if (valorMeta <= 0)
        {
            throw new MetaNegocioInvalidaException("Valor da meta deve ser maior que zero.");
        }

        if (percentualComissao <= 0 || percentualComissao > 100)
        {
            throw new MetaNegocioInvalidaException("Percentual de comissao deve estar entre 0 e 100.");
        }
    }

    private static decimal CalcularPercentual(decimal realizado, decimal meta)
    {
        if (meta <= 0) return 0;
        return Math.Round(realizado / meta * 100, 2);
    }
}

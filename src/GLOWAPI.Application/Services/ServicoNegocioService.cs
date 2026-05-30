using GLOWAPI.Application.DTOs.Servicos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class ServicoNegocioService : IServicoNegocioService
{
    private readonly IServicoRepository _servicoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;

    public ServicoNegocioService(
        IServicoRepository servicoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IModulosAssinaturaService modulosAssinaturaService,
        IAuditoriaNegocioService auditoriaNegocioService)
    {
        _servicoRepository = servicoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _auditoriaNegocioService = auditoriaNegocioService;
    }

    public async Task<IReadOnlyList<ServicoResponseDto>> ListarAsync(
        int estabelecimentoId,
        ServicoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoVisualizar,
            cancellationToken);

        var servicos = await _servicoRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            filtro.Ativo,
            filtro.ProfissionalId,
            filtro.Nome,
            cancellationToken);

        return servicos.Select(ServicoResponseDto.From).ToList();
    }

    public async Task<ServicoResponseDto> CriarAsync(
        int estabelecimentoId,
        CriarServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoGerenciar,
            cancellationToken);

        ServicoValidador.ValidarServico(
            request.Nome,
            request.Descricao,
            request.PrecoBase,
            request.DuracaoMinutos);

        await ValidarLimiteServicosAsync(estabelecimentoId, cancellationToken);

        var servico = new Servico
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = request.Nome.Trim(),
            Descricao = request.Descricao?.Trim() ?? string.Empty,
            PrecoBase = request.PrecoBase,
            DuracaoMinutos = request.DuracaoMinutos,
            Ativo = true
        };

        await _servicoRepository.AdicionarAsync(servico, cancellationToken);
        await _servicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ServicoCriado,
            nameof(Servico),
            servico.Id,
            new
            {
                servico.Nome,
                servico.PrecoBase,
                servico.DuracaoMinutos,
                servico.Ativo
            },
            cancellationToken);

        return ServicoResponseDto.From(servico);
    }

    public async Task<ServicoResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int servicoId,
        AtualizarServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoGerenciar,
            cancellationToken);

        ServicoValidador.ValidarServico(
            request.Nome,
            request.Descricao,
            request.PrecoBase,
            request.DuracaoMinutos);

        var servico = await ObterServicoDoEstabelecimentoAsync(
            servicoId,
            estabelecimentoId,
            cancellationToken);

        var alteracaoAnterior = new
        {
            servico.Nome,
            servico.Descricao,
            servico.PrecoBase,
            servico.DuracaoMinutos
        };

        servico.Nome = request.Nome.Trim();
        servico.Descricao = request.Descricao?.Trim() ?? string.Empty;
        servico.PrecoBase = request.PrecoBase;
        servico.DuracaoMinutos = request.DuracaoMinutos;
        servico.UpdatedAt = DateTime.UtcNow;

        _servicoRepository.Atualizar(servico);
        await _servicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ServicoAlterado,
            nameof(Servico),
            servico.Id,
            new
            {
                anterior = alteracaoAnterior,
                atual = new
                {
                    servico.Nome,
                    servico.Descricao,
                    servico.PrecoBase,
                    servico.DuracaoMinutos
                }
            },
            cancellationToken);

        return ServicoResponseDto.From(servico);
    }

    public async Task<ServicoResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int servicoId,
        AtualizarStatusServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoGerenciar,
            cancellationToken);

        var servico = await ObterServicoDoEstabelecimentoAsync(
            servicoId,
            estabelecimentoId,
            cancellationToken);

        if (request.Ativo && !servico.Ativo)
        {
            await ValidarLimiteServicosAsync(estabelecimentoId, cancellationToken);
        }

        var statusAnterior = servico.Ativo;
        servico.Ativo = request.Ativo;
        servico.UpdatedAt = DateTime.UtcNow;

        _servicoRepository.Atualizar(servico);
        await _servicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ServicoStatusAlterado,
            nameof(Servico),
            servico.Id,
            new
            {
                statusAnterior,
                statusNovo = servico.Ativo,
                servico.Nome
            },
            cancellationToken);

        return ServicoResponseDto.From(servico);
    }

    public async Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorEstabelecimentoAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new NegocioNaoEncontradoException();
        }

        var servicos = await _servicoRepository.ListarPublicosPorEstabelecimentoAsync(
            estabelecimento.Id,
            cancellationToken);

        return servicos.Select(ServicoPublicoResponseDto.From).ToList();
    }

    public async Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorProfissionalAutonomoAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default)
    {
        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (profissional is null
            || !profissional.Ativo
            || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);
        if (vinculo?.EstabelecimentoId is null)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        var servicos = await _servicoRepository.ListarPublicosPorEstabelecimentoAsync(
            vinculo.EstabelecimentoId,
            cancellationToken);

        return servicos.Select(ServicoPublicoResponseDto.From).ToList();
    }

    private async Task<Servico> ObterServicoDoEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var servico = await _servicoRepository.ObterPorIdEEstabelecimentoAsync(
            servicoId,
            estabelecimentoId,
            ativo: null,
            cancellationToken);

        if (servico is null)
        {
            throw new ServicoNegocioNaoEncontradoException();
        }

        return servico;
    }

    private async Task ValidarLimiteServicosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var modulos = await _modulosAssinaturaService.ObterPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        var limiteServicos = modulos.Limites.Servicos;
        if (!limiteServicos.HasValue)
        {
            return;
        }

        var servicosAtivos = await _servicoRepository.ContarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        if (servicosAtivos >= limiteServicos.Value)
        {
            throw new LimiteServicosNegocioExcedidoException();
        }
    }
}

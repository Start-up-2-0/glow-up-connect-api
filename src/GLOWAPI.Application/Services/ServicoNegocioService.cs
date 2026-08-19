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
    private readonly IProfissionalEscopoAcessoService _profissionalEscopoAcessoService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IOnboardingPublicacaoService _onboardingPublicacaoService;
    private readonly IAvatarBase64Decoder _avatarBase64Decoder;
    private readonly IBase64ImageThumbnailer _thumbnailer;

    public ServicoNegocioService(
        IServicoRepository servicoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IProfissionalEscopoAcessoService profissionalEscopoAcessoService,
        IModulosAssinaturaService modulosAssinaturaService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IOnboardingPublicacaoService onboardingPublicacaoService,
        IAvatarBase64Decoder avatarBase64Decoder,
        IBase64ImageThumbnailer thumbnailer)
    {
        _servicoRepository = servicoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _profissionalEscopoAcessoService = profissionalEscopoAcessoService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _onboardingPublicacaoService = onboardingPublicacaoService;
        _avatarBase64Decoder = avatarBase64Decoder;
        _thumbnailer = thumbnailer;
    }

    public async Task<IReadOnlyList<ServicoResponseDto>> ListarAsync(
        int estabelecimentoId,
        ServicoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var autorizacao = await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoVisualizar,
            cancellationToken);

        var profissionalId = filtro.ProfissionalId;
        var apenasVinculados = filtro.ApenasVinculados ?? false;

        if (autorizacao.Role == EstablishmentUserRole.Profissional
            && !autorizacao.PossuiPermissao(PermissaoNegocio.ServicoGerenciar))
        {
            var escopo = await _profissionalEscopoAcessoService.ObterEscopoAsync(
                estabelecimentoId,
                cancellationToken);
            profissionalId = escopo.ProfissionalId;
            apenasVinculados = true;
        }

        var servicos = await _servicoRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            filtro.Ativo,
            profissionalId,
            filtro.Nome,
            apenasVinculados,
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

        var tipoServico = ServicoValidador.ValidarTipoServico(request.TipoServico);

        await ValidarLimiteServicosAsync(estabelecimentoId, cancellationToken);

        var servico = new Servico
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = request.Nome.Trim(),
            Descricao = request.Descricao?.Trim() ?? string.Empty,
            PrecoBase = request.PrecoBase,
            DuracaoMinutos = request.DuracaoMinutos,
            TipoServico = tipoServico,
            Imagem = ProcessarImagemInformada(request.Imagem, request.ImagemContentType),
            Ativo = true
        };

        await _servicoRepository.AdicionarAsync(servico, cancellationToken);
        await _servicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
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
                servico.TipoServico,
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

        var tipoServico = ServicoValidador.ValidarTipoServico(request.TipoServico ?? servico.TipoServico);

        var alteracaoAnterior = new
        {
            servico.Nome,
            servico.Descricao,
            servico.PrecoBase,
            servico.DuracaoMinutos,
            servico.TipoServico
        };

        servico.Nome = request.Nome.Trim();
        servico.Descricao = request.Descricao?.Trim() ?? string.Empty;
        servico.PrecoBase = request.PrecoBase;
        servico.DuracaoMinutos = request.DuracaoMinutos;
        servico.TipoServico = tipoServico;
        if (!string.IsNullOrWhiteSpace(request.Imagem))
        {
            servico.Imagem = ProcessarImagemInformada(request.Imagem, request.ImagemContentType);
        }

        servico.UpdatedAt = DateTime.UtcNow;

        _servicoRepository.Atualizar(servico);
        await _servicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
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
                    servico.DuracaoMinutos,
                    servico.TipoServico
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
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
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
        Guid? profissionalPublicGuid = null,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo || !estabelecimento.VisivelPublicamente)
        {
            throw new NegocioNaoEncontradoException();
        }

        int? profissionalId = null;
        if (profissionalPublicGuid.HasValue)
        {
            var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(
                profissionalPublicGuid.Value,
                cancellationToken);
            if (profissional is null || !profissional.Ativo)
            {
                throw new RecursoProfissionalNaoEncontradoException();
            }

            var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
                profissional.Id,
                estabelecimento.Id,
                cancellationToken);
            if (vinculo is null || !vinculo.Ativo || !vinculo.PodeReceberAgendamento)
            {
                throw new ProfissionalSemVinculoNegocioException();
            }

            profissionalId = profissional.Id;
        }

        var servicos = profissionalId.HasValue
            ? await _servicoRepository.ListarPorEstabelecimentoAsync(
                estabelecimento.Id,
                ativo: true,
                profissionalId,
                nome: null,
                apenasVinculados: false,
                cancellationToken)
            : await _servicoRepository.ListarPublicosPorEstabelecimentoAsync(
                estabelecimento.Id,
                cancellationToken);

        return servicos
            .Select(servico => ServicoPublicoResponseDto.From(servico, profissionalId))
            .ToList();
    }

    public Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorProfissionalAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default) =>
        ListarPublicosPorProfissionalInternoAsync(publicGuid, cancellationToken);

    public Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorProfissionalAutonomoAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default) =>
        ListarPublicosPorProfissionalInternoAsync(publicGuid, cancellationToken);

    private async Task<IReadOnlyList<ServicoPublicoResponseDto>> ListarPublicosPorProfissionalInternoAsync(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (profissional is null || !profissional.Ativo)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);
        if (vinculo?.EstabelecimentoId is null || !vinculo.Ativo || !vinculo.PodeReceberAgendamento)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        if (vinculo.Estabelecimento is null
            || !vinculo.Estabelecimento.Ativo
            || !vinculo.Estabelecimento.VisivelPublicamente)
        {
            throw new NegocioNaoEncontradoException();
        }

        var servicos = await _servicoRepository.ListarPorEstabelecimentoAsync(
            vinculo.EstabelecimentoId,
            ativo: true,
            profissional.Id,
            nome: null,
            apenasVinculados: false,
            cancellationToken);

        return servicos
            .Select(servico => ServicoPublicoResponseDto.From(servico, profissional.Id))
            .ToList();
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

    private string? ProcessarImagemInformada(string? imagem, string? imagemContentType) =>
        OperacaoPerfilValidation.ValidarImagemOpcional(
            imagem,
            imagemContentType,
            "Imagem do servico",
            _avatarBase64Decoder,
            _thumbnailer,
            message => new ServicoNegocioInvalidoException(message));
}

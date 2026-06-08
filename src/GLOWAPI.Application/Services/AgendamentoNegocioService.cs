using System.Text.Json;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class AgendamentoNegocioService : IAgendamentoNegocioService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<AgendamentoStatus> StatusAtivos =
    [
        AgendamentoStatus.PendenteConfirmacao,
        AgendamentoStatus.Confirmado,
        AgendamentoStatus.Remarcado,
        AgendamentoStatus.EmAtendimento
    ];

    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAgendamentoHistoricoRepository _agendamentoHistoricoRepository;
    private readonly IAgendamentoValidador _agendamentoValidador;
    private readonly IAgendamentoNotificacaoService _agendamentoNotificacaoService;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly ICurrentUserContext _currentUserContext;

    public AgendamentoNegocioService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAgendamentoRepository agendamentoRepository,
        IAgendamentoHistoricoRepository agendamentoHistoricoRepository,
        IAgendamentoValidador agendamentoValidador,
        IAgendamentoNotificacaoService agendamentoNotificacaoService,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        ICurrentUserContext currentUserContext)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _agendamentoRepository = agendamentoRepository;
        _agendamentoHistoricoRepository = agendamentoHistoricoRepository;
        _agendamentoValidador = agendamentoValidador;
        _agendamentoNotificacaoService = agendamentoNotificacaoService;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<ProfissionalPublicoResponseDto>> ListarProfissionaisPublicosPorLojaAsync(
        Guid publicGuidLoja,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await ObterEstabelecimentoPublicoAsync(publicGuidLoja, cancellationToken);
        var vinculos = await _profissionalEstabelecimentoRepository.ListarAtivosComAgendamentoPorEstabelecimentoAsync(
            estabelecimento.Id,
            cancellationToken);

        return vinculos
            .Where(vinculo => vinculo.Profissional is not null)
            .Select(vinculo => new ProfissionalPublicoResponseDto
            {
                PublicGuid = vinculo.Profissional!.PublicGuid,
                NomePublico = vinculo.Profissional.NomePublico
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ProfissionalVitrinePublicoResponseDto>> ListarProfissionaisVitrinePorLojaAsync(
        Guid publicGuidLoja,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await ObterEstabelecimentoPublicoAsync(publicGuidLoja, cancellationToken);
        var vinculos = await _profissionalEstabelecimentoRepository.ListarAtivosParaVitrinePorEstabelecimentoAsync(
            estabelecimento.Id,
            cancellationToken);

        return vinculos
            .Where(vinculo => vinculo.Profissional is not null)
            .Select(vinculo => new ProfissionalVitrinePublicoResponseDto
            {
                PublicGuid = vinculo.Profissional!.PublicGuid,
                NomePublico = vinculo.Profissional.NomePublico,
                Biografia = vinculo.Profissional.Biografia,
                Logo = vinculo.Profissional.Logo
            })
            .ToList();
    }

    public Task<AgendamentoCriadoResponseDto> CriarPublicoPorLojaAsync(
        Guid publicGuidLoja,
        CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return CriarPublicoInternoAsync(
            publicGuidLoja,
            request.ProfissionalPublicGuid,
            request,
            OrigemAgendamento.PublicoLoja,
            usuarioClienteId: null,
            cancellationToken);
    }

    public async Task<AgendamentoCriadoResponseDto> CriarPublicoPorProfissionalAsync(
        Guid publicGuidProfissional,
        CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuidProfissional, cancellationToken);
        if (profissional is null || !profissional.Ativo)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);
        if (vinculo?.Estabelecimento is null || !vinculo.Ativo || !vinculo.PodeReceberAgendamento)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        request.ProfissionalPublicGuid = publicGuidProfissional;

        return await CriarPublicoInternoAsync(
            vinculo.Estabelecimento.PublicGuid,
            publicGuidProfissional,
            request,
            OrigemAgendamento.PublicoProfissional,
            usuarioClienteId: null,
            cancellationToken);
    }

    public async Task<AgendamentoClienteResponseDto> CriarLogadoAsync(
        CriarAgendamentoLogadoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();

        var requestPublico = new CriarAgendamentoRequestDto
        {
            ProfissionalPublicGuid = request.ProfissionalPublicGuid,
            ServicoIds = request.ServicoIds,
            Data = request.Data,
            HorarioInicio = request.HorarioInicio,
            Observacao = request.Observacao
        };

        var criado = await CriarPublicoInternoAsync(
            request.EstabelecimentoPublicGuid,
            request.ProfissionalPublicGuid,
            requestPublico,
            OrigemAgendamento.Logado,
            userId,
            cancellationToken);

        var agendamento = await _agendamentoRepository.ObterPorIdEUsuarioClienteAsync(
            criado.Id,
            userId,
            cancellationToken);

        return AgendamentoClienteResponseDto.From(agendamento!);
    }

    public async Task<AgendamentosClientePaginadoResponseDto> ListarMeusAgendamentosAsync(
        AgendamentoClienteFiltroDto filtroDto,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var filtro = await MontarFiltroClienteAsync(userId, filtroDto, cancellationToken);

        var (agendamentos, total) = await _agendamentoRepository.ListarPorUsuarioClienteComFiltroAsync(
            filtro,
            cancellationToken);

        return new AgendamentosClientePaginadoResponseDto(
            total,
            filtro.Pagina,
            filtro.TamanhoPagina,
            agendamentos.Select(AgendamentoClienteResponseDto.From).ToList());
    }

    public async Task<AgendamentoClienteResponseDto> ObterMeuAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        var agendamento = await ObterMeuAgendamentoEntidadeAsync(agendamentoId, cancellationToken);
        return AgendamentoClienteResponseDto.From(agendamento);
    }

    public async Task<AgendamentoClienteResponseDto> CancelarMeuAgendamentoAsync(
        int agendamentoId,
        CancelarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new AgendamentoStatusInvalidoException("Motivo do cancelamento e obrigatorio.");
        }

        var agendamento = await ObterMeuAgendamentoEntidadeAsync(agendamentoId, cancellationToken);
        await ExecutarCancelamentoAsync(
            agendamento,
            request.Motivo.Trim(),
            registrarAuditoriaEstabelecimento: true,
            cancellationToken);

        return AgendamentoClienteResponseDto.From(agendamento);
    }

    public async Task<AgendamentoClienteResponseDto> RemarcarMeuAgendamentoAsync(
        int agendamentoId,
        RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new AgendamentoStatusInvalidoException("Motivo da remarcacao e obrigatorio.");
        }

        var agendamento = await ObterMeuAgendamentoEntidadeAsync(agendamentoId, cancellationToken);
        await ExecutarRemarcacaoAsync(
            agendamento,
            request,
            registrarAuditoriaEstabelecimento: true,
            cancellationToken);

        return AgendamentoClienteResponseDto.From(agendamento);
    }

    public async Task<AgendamentoCriadoResponseDto> ConfirmarAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaCriar,
            cancellationToken);

        var agendamento = await ObterAgendamentoEstabelecimentoAsync(agendamentoId, estabelecimentoId, cancellationToken);
        if (agendamento.Status is not (AgendamentoStatus.PendenteConfirmacao or AgendamentoStatus.Remarcado))
        {
            throw new AgendamentoStatusInvalidoException("Somente agendamentos pendentes ou remarcados podem ser confirmados.");
        }

        var statusAnterior = agendamento.Status;
        agendamento.Status = AgendamentoStatus.Confirmado;
        agendamento.UpdatedAt = DateTime.UtcNow;

        foreach (var item in agendamento.Itens)
        {
            item.Status = AgendamentoItemStatus.Confirmado;
            item.UpdatedAt = agendamento.UpdatedAt;
        }

        await RegistrarHistoricoAsync(
            agendamento,
            statusAnterior,
            agendamento.Status,
            motivo: null,
            cancellationToken);

        _agendamentoRepository.Atualizar(agendamento);
        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.AgendamentoConfirmado,
            nameof(Agendamento),
            agendamento.Id,
            new { agendamento.Status },
            cancellationToken);

        var contexto = ObterContextoNotificacao(agendamento);
        await _agendamentoNotificacaoService.AgendamentoConfirmadoAsync(
            agendamento,
            contexto.Estabelecimento,
            contexto.Profissional,
            cancellationToken);

        return AgendamentoCriadoResponseDto.From(agendamento);
    }

    public async Task<AgendamentoCriadoResponseDto> CancelarAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancelarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaCancelar,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new AgendamentoStatusInvalidoException("Motivo do cancelamento e obrigatorio.");
        }

        var agendamento = await ObterAgendamentoEstabelecimentoAsync(agendamentoId, estabelecimentoId, cancellationToken);
        await ExecutarCancelamentoAsync(
            agendamento,
            request.Motivo.Trim(),
            registrarAuditoriaEstabelecimento: true,
            cancellationToken);

        return AgendamentoCriadoResponseDto.From(agendamento);
    }

    public async Task<AgendamentoCriadoResponseDto> RemarcarAsync(
        int estabelecimentoId,
        int agendamentoId,
        RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaReagendar,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new AgendamentoStatusInvalidoException("Motivo da remarcacao e obrigatorio.");
        }

        var agendamento = await ObterAgendamentoEstabelecimentoAsync(agendamentoId, estabelecimentoId, cancellationToken);
        await ExecutarRemarcacaoAsync(
            agendamento,
            request,
            registrarAuditoriaEstabelecimento: true,
            cancellationToken);

        return AgendamentoCriadoResponseDto.From(agendamento);
    }

    public async Task<AgendamentoCriadoResponseDto> MarcarNaoCompareceuAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaCancelar,
            cancellationToken);

        var agendamento = await ObterAgendamentoEstabelecimentoAsync(agendamentoId, estabelecimentoId, cancellationToken);
        if (agendamento.Status != AgendamentoStatus.Confirmado)
        {
            throw new AgendamentoStatusInvalidoException("Somente agendamentos confirmados podem ser marcados como nao compareceu.");
        }

        var inicio = agendamento.Itens.Min(item => item.Inicio);
        if (inicio > DateTime.UtcNow)
        {
            throw new AgendamentoStatusInvalidoException("Agendamento ainda nao iniciou.");
        }

        var statusAnterior = agendamento.Status;
        agendamento.Status = AgendamentoStatus.NaoCompareceu;
        agendamento.UpdatedAt = DateTime.UtcNow;

        foreach (var item in agendamento.Itens)
        {
            item.Status = AgendamentoItemStatus.Cancelado;
            item.UpdatedAt = agendamento.UpdatedAt;
        }

        await RegistrarHistoricoAsync(
            agendamento,
            statusAnterior,
            agendamento.Status,
            motivo: "Cliente nao compareceu",
            cancellationToken);

        _agendamentoRepository.Atualizar(agendamento);
        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.AgendamentoNaoCompareceu,
            nameof(Agendamento),
            agendamento.Id,
            cancellationToken: cancellationToken);

        return AgendamentoCriadoResponseDto.From(agendamento);
    }

    public async Task<IReadOnlyList<AgendamentoHistoricoResponseDto>> ObterHistoricoAsync(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaVisualizarGeral,
            cancellationToken);

        _ = await ObterAgendamentoEstabelecimentoAsync(agendamentoId, estabelecimentoId, cancellationToken);

        var historico = await _agendamentoHistoricoRepository.ListarPorAgendamentoAsync(
            agendamentoId,
            cancellationToken);

        return historico.Select(AgendamentoHistoricoResponseDto.From).ToList();
    }

    private async Task<AgendamentoCriadoResponseDto> CriarPublicoInternoAsync(
        Guid publicGuidLoja,
        Guid? publicGuidProfissional,
        CriarAgendamentoRequestDto request,
        OrigemAgendamento origem,
        int? usuarioClienteId,
        CancellationToken cancellationToken)
    {
        var estabelecimento = await ObterEstabelecimentoPublicoAsync(publicGuidLoja, cancellationToken);
        var profissional = await ResolverProfissionalAsync(
            estabelecimento.Id,
            publicGuidProfissional,
            cancellationToken);

        var preparacao = await _agendamentoValidador.PrepararAsync(
            estabelecimento.Id,
            profissional.Id,
            request.ServicoIds,
            request.Data,
            request.HorarioInicio,
            origem,
            usuarioClienteId.HasValue ? null : request,
            usuarioClienteId,
            agendamentoIgnorarId: null,
            cancellationToken);

        await _agendamentoValidador.ValidarConflitoAsync(
            estabelecimento.Id,
            profissional.Id,
            preparacao.Inicio,
            preparacao.Fim,
            agendamentoIgnorarId: null,
            cancellationToken);

        var agendamento = new Agendamento
        {
            EstabelecimentoId = estabelecimento.Id,
            UsuarioClienteId = preparacao.UsuarioClienteId,
            ClienteNome = preparacao.UsuarioClienteId.HasValue ? null : preparacao.ClienteNome,
            ClienteEmail = preparacao.UsuarioClienteId.HasValue ? null : preparacao.ClienteEmail,
            ClienteTelefone = preparacao.UsuarioClienteId.HasValue ? null : preparacao.ClienteTelefone,
            Origem = origem,
            Status = AgendamentoStatus.PendenteConfirmacao,
            ValorTotal = preparacao.ValorTotal,
            Observacao = request.Observacao?.Trim() ?? string.Empty,
            CreateAd = DateTime.UtcNow
        };

        foreach (var itemPreparado in preparacao.Itens)
        {
            agendamento.Itens.Add(new AgendamentoItem
            {
                ServicoId = itemPreparado.Servico.Id,
                ProfissionalId = profissional.Id,
                Inicio = itemPreparado.Inicio,
                Fim = itemPreparado.Fim,
                Valor = itemPreparado.Valor,
                Status = AgendamentoItemStatus.Pendente,
                CreateAd = DateTime.UtcNow
            });
        }

        await _agendamentoRepository.AdicionarAsync(agendamento, cancellationToken);
        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        foreach (var item in agendamento.Itens)
        {
            item.Servico = preparacao.Itens.First(preparado => preparado.Servico.Id == item.ServicoId).Servico;
        }

        await RegistrarHistoricoAsync(
            agendamento,
            statusAnterior: agendamento.Status,
            statusNovo: agendamento.Status,
            motivo: "Agendamento criado",
            cancellationToken);

        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimento.Id,
            TipoAcaoAuditoriaNegocio.AgendamentoCriado,
            nameof(Agendamento),
            agendamento.Id,
            new
            {
                origem = origem.ToString(),
                profissionalId = profissional.Id,
                valorTotal = agendamento.ValorTotal,
                inicio = preparacao.Inicio
            },
            cancellationToken);

        await _agendamentoNotificacaoService.AgendamentoCriadoAsync(
            agendamento,
            estabelecimento,
            profissional,
            cancellationToken);

        return AgendamentoCriadoResponseDto.From(agendamento);
    }

    private async Task<Estabelecimento> ObterEstabelecimentoPublicoAsync(
        Guid publicGuidLoja,
        CancellationToken cancellationToken)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuidLoja, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new NegocioNaoEncontradoException();
        }

        return estabelecimento;
    }

    private async Task<Profissional> ResolverProfissionalAsync(
        int estabelecimentoId,
        Guid? publicGuidProfissional,
        CancellationToken cancellationToken)
    {
        if (!publicGuidProfissional.HasValue)
        {
            throw new AgendamentoServicosInvalidosException("Profissional e obrigatorio para o agendamento.");
        }

        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuidProfissional.Value, cancellationToken);
        if (profissional is null || !profissional.Ativo)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissional.Id,
            estabelecimentoId,
            cancellationToken);
        if (vinculo is null || !vinculo.Ativo || !vinculo.PodeReceberAgendamento)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        return profissional;
    }

    private async Task<Agendamento> ObterAgendamentoEstabelecimentoAsync(
        int agendamentoId,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoRepository.ObterPorIdEEstabelecimentoComItensAsync(
            agendamentoId,
            estabelecimentoId,
            cancellationToken);

        if (agendamento is null)
        {
            throw new AgendamentoNaoEncontradoException();
        }

        return agendamento;
    }

    private async Task RegistrarHistoricoAsync(
        Agendamento agendamento,
        AgendamentoStatus statusAnterior,
        AgendamentoStatus statusNovo,
        string? motivo,
        CancellationToken cancellationToken)
    {
        var historico = new AgendamentoHistorico
        {
            AgendamentoId = agendamento.Id,
            UsuarioExecutorId = _currentUserContext.UserId,
            StatusAnterior = statusAnterior,
            StatusNovo = statusNovo,
            Motivo = motivo,
            PayloadJson = JsonSerializer.Serialize(new
            {
                agendamento.ValorTotal,
                itens = agendamento.Itens.Select(item => new
                {
                    item.ServicoId,
                    item.ProfissionalId,
                    item.Inicio,
                    item.Fim,
                    item.Valor
                })
            }, JsonOptions),
            CriadoEm = DateTime.UtcNow
        };

        await _agendamentoHistoricoRepository.AdicionarAsync(historico, cancellationToken);
    }

    private static (Estabelecimento Estabelecimento, Profissional Profissional) ObterContextoNotificacao(Agendamento agendamento)
    {
        var profissional = agendamento.Itens.First().Profissional
            ?? throw new AgendamentoStatusInvalidoException("Profissional do agendamento nao encontrado.");
        var estabelecimento = agendamento.Estabelecimento
            ?? throw new AgendamentoStatusInvalidoException("Estabelecimento do agendamento nao encontrado.");

        return (estabelecimento, profissional);
    }

    private int ObterUserIdAutenticado()
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUserContext.UserId.Value;
    }

    private async Task<Agendamento> ObterMeuAgendamentoEntidadeAsync(
        int agendamentoId,
        CancellationToken cancellationToken)
    {
        var userId = ObterUserIdAutenticado();
        var agendamento = await _agendamentoRepository.ObterPorIdEUsuarioClienteAsync(
            agendamentoId,
            userId,
            cancellationToken);

        if (agendamento is null)
        {
            throw new AgendamentoNaoEncontradoException();
        }

        return agendamento;
    }

    private async Task<AgendamentoClienteFiltro> MontarFiltroClienteAsync(
        int userId,
        AgendamentoClienteFiltroDto filtroDto,
        CancellationToken cancellationToken)
    {
        int? estabelecimentoId = null;
        if (filtroDto.EstabelecimentoPublicGuid.HasValue)
        {
            var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(
                filtroDto.EstabelecimentoPublicGuid.Value,
                cancellationToken);
            if (estabelecimento is not null)
            {
                estabelecimentoId = estabelecimento.Id;
            }
        }

        var ordenacao = filtroDto.Ordenacao?.Trim().ToLowerInvariant();
        var ordenarPorProximos = ordenacao != "recentes";

        return new AgendamentoClienteFiltro(
            userId,
            filtroDto.Status,
            filtroDto.DataInicio,
            filtroDto.DataFim,
            estabelecimentoId,
            Math.Max(1, filtroDto.Pagina),
            Math.Clamp(filtroDto.TamanhoPagina, 1, 50),
            ordenarPorProximos);
    }

    private async Task ExecutarCancelamentoAsync(
        Agendamento agendamento,
        string motivo,
        bool registrarAuditoriaEstabelecimento,
        CancellationToken cancellationToken)
    {
        if (!StatusAtivos.Contains(agendamento.Status))
        {
            throw new AgendamentoStatusInvalidoException("Agendamento nao pode ser cancelado no status atual.");
        }

        var statusAnterior = agendamento.Status;
        agendamento.Status = AgendamentoStatus.Cancelado;
        agendamento.CanceladoEm = DateTime.UtcNow;
        agendamento.UpdatedAt = agendamento.CanceladoEm;

        foreach (var item in agendamento.Itens)
        {
            item.Status = AgendamentoItemStatus.Cancelado;
            item.UpdatedAt = agendamento.UpdatedAt;
        }

        await RegistrarHistoricoAsync(
            agendamento,
            statusAnterior,
            agendamento.Status,
            motivo,
            cancellationToken);

        _agendamentoRepository.Atualizar(agendamento);
        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        if (registrarAuditoriaEstabelecimento && agendamento.EstabelecimentoId.HasValue)
        {
            await _auditoriaNegocioService.RegistrarAsync(
                agendamento.EstabelecimentoId.Value,
                TipoAcaoAuditoriaNegocio.AgendamentoCancelado,
                nameof(Agendamento),
                agendamento.Id,
                new { motivo },
                cancellationToken);
        }

        var contexto = ObterContextoNotificacao(agendamento);
        await _agendamentoNotificacaoService.AgendamentoCanceladoAsync(
            agendamento,
            contexto.Estabelecimento,
            contexto.Profissional,
            motivo,
            cancellationToken);
    }

    private async Task ExecutarRemarcacaoAsync(
        Agendamento agendamento,
        RemarcarAgendamentoRequestDto request,
        bool registrarAuditoriaEstabelecimento,
        CancellationToken cancellationToken)
    {
        if (agendamento.Status is not (
            AgendamentoStatus.PendenteConfirmacao
            or AgendamentoStatus.Confirmado
            or AgendamentoStatus.Remarcado))
        {
            throw new AgendamentoStatusInvalidoException("Agendamento nao pode ser remarcado no status atual.");
        }

        if (!agendamento.EstabelecimentoId.HasValue)
        {
            throw new AgendamentoStatusInvalidoException("Estabelecimento do agendamento nao encontrado.");
        }

        var estabelecimentoId = agendamento.EstabelecimentoId.Value;
        var profissionalId = agendamento.Itens.First().ProfissionalId;
        var servicoIds = agendamento.Itens.OrderBy(item => item.Inicio).Select(item => item.ServicoId).ToArray();
        var dadosVisitante = agendamento.UsuarioClienteId.HasValue
            ? null
            : new CriarAgendamentoRequestDto
            {
                ClienteNome = agendamento.ClienteNome,
                ClienteEmail = agendamento.ClienteEmail,
                ClienteTelefone = agendamento.ClienteTelefone
            };

        var preparacao = await _agendamentoValidador.PrepararAsync(
            estabelecimentoId,
            profissionalId,
            servicoIds,
            request.Data,
            request.HorarioInicio,
            agendamento.Origem,
            dadosVisitante,
            agendamento.UsuarioClienteId,
            agendamentoIgnorarId: agendamento.Id,
            cancellationToken);

        var statusAnterior = agendamento.Status;
        agendamento.Status = AgendamentoStatus.Remarcado;
        agendamento.ValorTotal = preparacao.ValorTotal;
        agendamento.UpdatedAt = DateTime.UtcNow;

        _agendamentoRepository.Atualizar(agendamento);

        var itensExistentes = agendamento.Itens.OrderBy(item => item.Inicio).ToList();
        for (var index = 0; index < itensExistentes.Count; index++)
        {
            var itemExistente = itensExistentes[index];
            var itemPreparado = preparacao.Itens[index];
            itemExistente.Inicio = itemPreparado.Inicio;
            itemExistente.Fim = itemPreparado.Fim;
            itemExistente.Valor = itemPreparado.Valor;
            itemExistente.UpdatedAt = agendamento.UpdatedAt;
            itemExistente.Status = AgendamentoItemStatus.Pendente;
        }

        await RegistrarHistoricoAsync(
            agendamento,
            statusAnterior,
            agendamento.Status,
            request.Motivo.Trim(),
            cancellationToken);

        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        if (registrarAuditoriaEstabelecimento)
        {
            await _auditoriaNegocioService.RegistrarAsync(
                estabelecimentoId,
                TipoAcaoAuditoriaNegocio.AgendamentoRemarcado,
                nameof(Agendamento),
                agendamento.Id,
                new
                {
                    motivo = request.Motivo.Trim(),
                    inicio = preparacao.Inicio,
                    fim = preparacao.Fim
                },
                cancellationToken);
        }

        var contexto = ObterContextoNotificacao(agendamento);
        await _agendamentoNotificacaoService.AgendamentoRemarcadoAsync(
            agendamento,
            contexto.Estabelecimento,
            contexto.Profissional,
            request.Motivo.Trim(),
            cancellationToken);
    }
}

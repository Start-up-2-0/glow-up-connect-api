using System.Text.Json;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.Extensions.Options;

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
    private readonly IDisponibilidadeAgendaService _disponibilidadeAgendaService;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUsuarioService _usuarioService;
    private readonly IAgendamentoPropostaRemarcacaoRepository _propostaRemarcacaoRepository;
    private readonly AuthOptions _authOptions;

    public AgendamentoNegocioService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAgendamentoRepository agendamentoRepository,
        IAgendamentoHistoricoRepository agendamentoHistoricoRepository,
        IAgendamentoValidador agendamentoValidador,
        IAgendamentoNotificacaoService agendamentoNotificacaoService,
        IDisponibilidadeAgendaService disponibilidadeAgendaService,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        ICurrentUserContext currentUserContext,
        IUsuarioService usuarioService,
        IAgendamentoPropostaRemarcacaoRepository propostaRemarcacaoRepository,
        IOptions<AuthOptions> authOptions)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _agendamentoRepository = agendamentoRepository;
        _agendamentoHistoricoRepository = agendamentoHistoricoRepository;
        _agendamentoValidador = agendamentoValidador;
        _agendamentoNotificacaoService = agendamentoNotificacaoService;
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _currentUserContext = currentUserContext;
        _usuarioService = usuarioService;
        _propostaRemarcacaoRepository = propostaRemarcacaoRepository;
        _authOptions = authOptions.Value;
    }

    public async Task<AgendamentoContextoPublicoResponseDto> ObterContextoPublicoAsync(
        Guid publicGuidLoja,
        Guid profissionalPublicGuid,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await ObterEstabelecimentoPublicoAsync(publicGuidLoja, cancellationToken);
        var profissional = await ResolverProfissionalPorGuidAsync(
            estabelecimento.Id,
            profissionalPublicGuid,
            cancellationToken);

        EnderecoResumoDto? endereco = null;
        if (estabelecimento.Endereco is not null)
        {
            var end = estabelecimento.Endereco;
            endereco = new EnderecoResumoDto(end.Logradouro, end.Bairro, end.Cidade, end.Estado);
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissional.Id,
            estabelecimento.Id,
            cancellationToken);

        return new AgendamentoContextoPublicoResponseDto
        {
            Estabelecimento = new EstabelecimentoPublicoResponseDto(
                estabelecimento.PublicGuid,
                estabelecimento.Nome,
                estabelecimento.Logo,
                estabelecimento.Descricao,
                endereco,
                null),
            Profissional = new ProfissionalPublicoResponseDto
            {
                PublicGuid = profissional.PublicGuid,
                NomePublico = profissional.NomePublico
            },
            PodeReceberAgendamento = vinculo?.PodeReceberAgendamento == true
        };
    }

    public async Task<AgendamentoCriadoResponseDto> CriarPublicoComCadastroAsync(
        Guid publicGuidLoja,
        CriarAgendamentoComCadastroRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioService.CadastrarClienteAsync(request.Cadastro, cancellationToken);

        var agendamentoRequest = new CriarAgendamentoRequestDto
        {
            ProfissionalPublicGuid = request.ProfissionalPublicGuid,
            ServicoIds = request.ServicoIds,
            Data = request.Data,
            HorarioInicio = request.HorarioInicio,
            InicioSelecionado = request.InicioSelecionado,
            Observacao = request.Observacao
        };

        return await CriarPublicoInternoAsync(
            publicGuidLoja,
            request.ProfissionalPublicGuid,
            agendamentoRequest,
            OrigemAgendamento.CadastroPublico,
            usuario.Id,
            notificarCriacao: false,
            cancellationToken);
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
            cancellationToken: cancellationToken);
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
            cancellationToken: cancellationToken);
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
            InicioSelecionado = request.InicioSelecionado,
            Observacao = request.Observacao
        };

        var criado = await CriarPublicoInternoAsync(
            request.EstabelecimentoPublicGuid,
            request.ProfissionalPublicGuid,
            requestPublico,
            OrigemAgendamento.Logado,
            userId,
            cancellationToken: cancellationToken);

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

        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
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
        bool notificarCriacao = true,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await ObterEstabelecimentoPublicoAsync(publicGuidLoja, cancellationToken);
        var profissional = await ResolverProfissionalParaAgendamentoAsync(
            estabelecimento,
            publicGuidProfissional,
            request,
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
            inicioSelecionado: request.InicioSelecionado,
            cancellationToken);

        await _agendamentoValidador.ValidarConflitoAsync(
            estabelecimento.Id,
            profissional.Id,
            preparacao.Inicio,
            preparacao.Fim,
            agendamentoIgnorarId: null,
            cancellationToken);

        var manterContatoVisitante = !preparacao.UsuarioClienteId.HasValue || origem == OrigemAgendamento.CadastroPublico;

        var agendamento = new Agendamento
        {
            EstabelecimentoId = estabelecimento.Id,
            UsuarioClienteId = preparacao.UsuarioClienteId,
            ClienteNome = manterContatoVisitante ? preparacao.ClienteNome : null,
            ClienteEmail = manterContatoVisitante ? preparacao.ClienteEmail : null,
            ClienteTelefone = manterContatoVisitante ? preparacao.ClienteTelefone : null,
            Origem = origem,
            Status = AgendamentoStatus.PendenteConfirmacao,
            ValorTotal = preparacao.ValorTotal,
            Inicio = preparacao.Inicio,
            Fim = preparacao.Fim,
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
            motivo: origem == OrigemAgendamento.Logado ? "Agendamento interno criado" : "Agendamento criado",
            cancellationToken,
            payloadExtra: new
            {
                origem = origem.ToString(),
                usuarioClienteId = agendamento.UsuarioClienteId,
                estabelecimentoId = estabelecimento.Id,
                profissionalId = profissional.Id,
                servicoIds = request.ServicoIds,
                cliente = manterContatoVisitante
                    ? new
                    {
                        nome = preparacao.ClienteNome,
                        email = preparacao.ClienteEmail,
                        telefone = preparacao.ClienteTelefone
                    }
                    : null
            });

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

        if (notificarCriacao)
        {
            await _agendamentoNotificacaoService.AgendamentoCriadoAsync(
                agendamento,
                estabelecimento,
                profissional,
                cancellationToken);
        }

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

    private async Task<Profissional> ResolverProfissionalParaAgendamentoAsync(
        Estabelecimento estabelecimento,
        Guid? publicGuidProfissional,
        CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        if (publicGuidProfissional.HasValue)
        {
            return await ResolverProfissionalPorGuidAsync(
                estabelecimento.Id,
                publicGuidProfissional.Value,
                cancellationToken);
        }

        return await ResolverProfissionalSemPreferenciaAsync(
            estabelecimento,
            request.ServicoIds,
            request.Data,
            request.HorarioInicio,
            request.InicioSelecionado,
            cancellationToken);
    }

    private async Task<Profissional> ResolverProfissionalSemPreferenciaAsync(
        Estabelecimento estabelecimento,
        int[] servicoIds,
        DateOnly data,
        TimeOnly horarioInicio,
        DateTime? inicioSelecionado,
        CancellationToken cancellationToken)
    {
        if (servicoIds.Length == 0)
        {
            throw new AgendamentoServicosInvalidosException("Informe ao menos um servico.");
        }

        var inicioAlvo = AgendaDateTimeHelper.ResolverInicio(data, horarioInicio, inicioSelecionado);
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPublicoPorEstabelecimentoAsync(
            estabelecimento.PublicGuid,
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = data,
                DataFim = data,
                ServicoId = servicoIds[0],
                ServicoIds = servicoIds
            },
            cancellationToken);

        var slotsCompativeis = disponibilidade.Slots
            .Where(slot => AgendaDateTimeHelper.ExtrairWallClockUtc(slot.Inicio) == inicioAlvo)
            .OrderBy(slot => slot.ProfissionalId)
            .ToList();

        if (slotsCompativeis.Count == 0)
        {
            throw new HorarioIndisponivelException(
                "Horario indisponivel para os servicos selecionados.");
        }

        foreach (var slot in slotsCompativeis)
        {
            var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
                slot.ProfissionalId,
                estabelecimento.Id,
                cancellationToken);
            if (vinculo is null || !vinculo.Ativo || !vinculo.PodeReceberAgendamento)
            {
                continue;
            }

            var profissional = await _profissionalRepository.ObterPorIdAsync(slot.ProfissionalId, cancellationToken);
            if (profissional is not null && profissional.Ativo)
            {
                return profissional;
            }
        }

        throw new HorarioIndisponivelException(
            "Horario indisponivel para os servicos selecionados.");
    }

    private async Task<Profissional> ResolverProfissionalPorGuidAsync(
        int estabelecimentoId,
        Guid publicGuidProfissional,
        CancellationToken cancellationToken)
    {
        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuidProfissional, cancellationToken);
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
        CancellationToken cancellationToken,
        object? payloadExtra = null)
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
                extra = payloadExtra,
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

        if (filtroDto.DataInicio.HasValue && filtroDto.DataFim.HasValue && filtroDto.IntervaloPersonalizado)
        {
            AgendaPeriodoConsulta.ValidarIntervaloPersonalizado(
                filtroDto.DataInicio.Value,
                filtroDto.DataFim.Value);
        }

        return AgendamentoClienteFiltro.Criar(
            userId,
            filtroDto.Status,
            filtroDto.DataInicio,
            filtroDto.DataFim,
            estabelecimentoId,
            filtroDto.Pagina,
            filtroDto.TamanhoPagina,
            filtroDto.Ordenacao);
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
            inicioSelecionado: request.InicioSelecionado,
            cancellationToken);

        var statusAnterior = agendamento.Status;
        agendamento.Status = AgendamentoStatus.Remarcado;
        agendamento.ValorTotal = preparacao.ValorTotal;
        agendamento.Inicio = preparacao.Inicio;
        agendamento.Fim = preparacao.Fim;
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

    public async Task<PropostaRemarcacaoResponseDto> SugerirRemarcacaoAsync(
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
            throw new AgendamentoStatusInvalidoException("Motivo da sugestao e obrigatorio.");
        }

        var agendamento = await ObterAgendamentoEstabelecimentoAsync(agendamentoId, estabelecimentoId, cancellationToken);
        if (agendamento.Status is not (
            AgendamentoStatus.PendenteConfirmacao
            or AgendamentoStatus.Confirmado
            or AgendamentoStatus.Remarcado))
        {
            throw new AgendamentoStatusInvalidoException("Agendamento nao pode receber sugestao de remarcacao no status atual.");
        }

        await _propostaRemarcacaoRepository.ExpirarPendentesAnterioresAsync(agendamentoId, cancellationToken);

        var proposta = new AgendamentoPropostaRemarcacao
        {
            AgendamentoId = agendamentoId,
            DataSugerida = request.Data,
            HorarioInicioSugerido = request.HorarioInicio,
            Motivo = request.Motivo.Trim(),
            Status = PropostaRemarcacaoStatus.Pendente,
            TokenPublico = Guid.NewGuid(),
            ExpiraEm = DateTime.UtcNow.AddDays(3),
            UsuarioExecutorId = _currentUserContext.UserId,
            CriadoEm = DateTime.UtcNow
        };

        await _propostaRemarcacaoRepository.AdicionarAsync(proposta, cancellationToken);
        await _propostaRemarcacaoRepository.SalvarAlteracoesAsync(cancellationToken);

        await RegistrarHistoricoAsync(
            agendamento,
            agendamento.Status,
            agendamento.Status,
            "Proposta de remarcacao criada",
            cancellationToken,
            payloadExtra: new
            {
                evento = "PropostaCriada",
                propostaId = proposta.Id,
                dataSugerida = request.Data,
                horarioInicio = request.HorarioInicio,
                motivo = request.Motivo.Trim()
            });

        await _agendamentoHistoricoRepository.SalvarAlteracoesAsync(cancellationToken);

        var contexto = ObterContextoNotificacao(agendamento);
        var linkResposta = $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/agendamento/remarcacao/{proposta.TokenPublico}";
        await _agendamentoNotificacaoService.PropostaRemarcacaoEnviadaAsync(
            agendamento,
            contexto.Estabelecimento,
            contexto.Profissional,
            proposta,
            linkResposta,
            cancellationToken);

        await RegistrarHistoricoAsync(
            agendamento,
            agendamento.Status,
            agendamento.Status,
            "Notificacao de proposta enviada",
            cancellationToken,
            payloadExtra: new { evento = "PropostaNotificada", propostaId = proposta.Id, linkResposta });

        await _propostaRemarcacaoRepository.SalvarAlteracoesAsync(cancellationToken);

        return PropostaRemarcacaoResponseDto.From(proposta, agendamento, contexto.Profissional);
    }

    public async Task<PropostaRemarcacaoResponseDto> ObterPropostaRemarcacaoPorTokenAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default)
    {
        var proposta = await ObterPropostaPendentePorTokenAsync(tokenPublico, cancellationToken);
        var profissional = await _profissionalRepository.ObterPorIdAsync(
            proposta.Agendamento!.Itens.First().ProfissionalId,
            cancellationToken);

        return PropostaRemarcacaoResponseDto.From(proposta, proposta.Agendamento!, profissional);
    }

    public async Task<AgendamentoCriadoResponseDto> AceitarPropostaRemarcacaoPorTokenAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default)
    {
        var proposta = await ObterPropostaPendentePorTokenAsync(tokenPublico, cancellationToken);
        var agendamento = proposta.Agendamento!;
        await ExecutarAceitePropostaAsync(proposta, agendamento, cancellationToken);
        return AgendamentoCriadoResponseDto.From(agendamento);
    }

    public async Task RecusarPropostaRemarcacaoPorTokenAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken = default)
    {
        var proposta = await ObterPropostaPendentePorTokenAsync(tokenPublico, cancellationToken);
        await ExecutarRecusaPropostaAsync(proposta, proposta.Agendamento!, cancellationToken);
    }

    public async Task<AgendamentoClienteResponseDto> AceitarPropostaRemarcacaoLogadoAsync(
        int agendamentoId,
        int propostaId,
        CancellationToken cancellationToken = default)
    {
        var agendamento = await ObterMeuAgendamentoEntidadeAsync(agendamentoId, cancellationToken);
        var proposta = await _propostaRemarcacaoRepository.ObterPorIdEAgendamentoAsync(
            propostaId,
            agendamentoId,
            cancellationToken);
        if (proposta is null || proposta.Status != PropostaRemarcacaoStatus.Pendente || proposta.ExpiraEm <= DateTime.UtcNow)
        {
            throw new AgendamentoStatusInvalidoException("Proposta de remarcacao invalida ou expirada.");
        }

        await ExecutarAceitePropostaAsync(proposta, agendamento, cancellationToken);
        return AgendamentoClienteResponseDto.From(agendamento);
    }

    private async Task<AgendamentoPropostaRemarcacao> ObterPropostaPendentePorTokenAsync(
        Guid tokenPublico,
        CancellationToken cancellationToken)
    {
        var proposta = await _propostaRemarcacaoRepository.ObterPorTokenComAgendamentoAsync(tokenPublico, cancellationToken);
        if (proposta is null
            || proposta.Agendamento is null
            || proposta.Status != PropostaRemarcacaoStatus.Pendente
            || proposta.ExpiraEm <= DateTime.UtcNow)
        {
            throw new AgendamentoStatusInvalidoException("Proposta de remarcacao invalida ou expirada.");
        }

        return proposta;
    }

    private async Task ExecutarAceitePropostaAsync(
        AgendamentoPropostaRemarcacao proposta,
        Agendamento agendamento,
        CancellationToken cancellationToken)
    {
        var request = new RemarcarAgendamentoRequestDto
        {
            Data = proposta.DataSugerida,
            HorarioInicio = proposta.HorarioInicioSugerido,
            Motivo = proposta.Motivo
        };

        await ExecutarRemarcacaoAsync(
            agendamento,
            request,
            registrarAuditoriaEstabelecimento: true,
            cancellationToken);

        proposta.Status = PropostaRemarcacaoStatus.Aceita;
        proposta.RespondidoEm = DateTime.UtcNow;
        _propostaRemarcacaoRepository.Atualizar(proposta);

        await RegistrarHistoricoAsync(
            agendamento,
            agendamento.Status,
            agendamento.Status,
            "Proposta de remarcacao aceita",
            cancellationToken,
            payloadExtra: new { evento = "PropostaAceita", propostaId = proposta.Id });

        await _propostaRemarcacaoRepository.SalvarAlteracoesAsync(cancellationToken);

        var contexto = ObterContextoNotificacao(agendamento);
        await _agendamentoNotificacaoService.PropostaRemarcacaoRespondidaAsync(
            agendamento,
            contexto.Estabelecimento,
            contexto.Profissional,
            aceita: true,
            cancellationToken);
    }

    private async Task ExecutarRecusaPropostaAsync(
        AgendamentoPropostaRemarcacao proposta,
        Agendamento agendamento,
        CancellationToken cancellationToken)
    {
        proposta.Status = PropostaRemarcacaoStatus.Recusada;
        proposta.RespondidoEm = DateTime.UtcNow;
        _propostaRemarcacaoRepository.Atualizar(proposta);

        await RegistrarHistoricoAsync(
            agendamento,
            agendamento.Status,
            agendamento.Status,
            "Proposta de remarcacao recusada",
            cancellationToken,
            payloadExtra: new { evento = "PropostaRecusada", propostaId = proposta.Id });

        await _propostaRemarcacaoRepository.SalvarAlteracoesAsync(cancellationToken);

        var contexto = ObterContextoNotificacao(agendamento);
        await _agendamentoNotificacaoService.PropostaRemarcacaoRespondidaAsync(
            agendamento,
            contexto.Estabelecimento,
            contexto.Profissional,
            aceita: false,
            cancellationToken);
    }
}

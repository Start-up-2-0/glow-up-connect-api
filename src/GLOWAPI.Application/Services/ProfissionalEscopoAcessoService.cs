using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class ProfissionalEscopoAcessoService : IProfissionalEscopoAcessoService
{
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public ProfissionalEscopoAcessoService(
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        ICurrentUserContext currentUserContext)
    {
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<EscopoProfissionalResultado> ObterEscopoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUsuarioAutenticado();
        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorUsuarioAsync(
            userId,
            estabelecimentoId,
            cancellationToken);

        if (vinculo is null)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        return new EscopoProfissionalResultado(
            estabelecimentoId,
            userId,
            vinculo.ProfissionalId,
            vinculo.Id,
            vinculo.PodeReceberAgendamento);
    }

    public async Task<EscopoProfissionalResultado> AutorizarAgendamentoItemAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken = default)
    {
        var escopo = await ObterEscopoAsync(estabelecimentoId, cancellationToken);
        var item = await _agendamentoItemRepository.ObterPorIdComAgendamentoAsync(
            agendamentoItemId,
            cancellationToken);

        if (item is null || item.Agendamento is null)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        if (item.Agendamento.EstabelecimentoId != estabelecimentoId
            || item.ProfissionalId != escopo.ProfissionalId)
        {
            throw new RecursoForaEscopoProfissionalException();
        }

        return escopo;
    }

    public async Task<EscopoProfissionalResultado> AutorizarClienteAsync(
        int estabelecimentoId,
        int usuarioClienteId,
        CancellationToken cancellationToken = default)
    {
        var escopo = await ObterEscopoAsync(estabelecimentoId, cancellationToken);
        var clienteVinculado = await _agendamentoItemRepository.ExisteClienteVinculadoAoProfissionalAsync(
            estabelecimentoId,
            escopo.ProfissionalId,
            usuarioClienteId,
            cancellationToken);

        if (!clienteVinculado)
        {
            throw new RecursoForaEscopoProfissionalException();
        }

        return escopo;
    }

    private int ObterUsuarioAutenticado()
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUserContext.UserId.Value;
    }
}

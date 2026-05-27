using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class AssinaturaService : IAssinaturaService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly ICurrentUserContext _currentUser;

    public AssinaturaService(
        IAssinaturaRepository assinaturaRepository,
        IPlanoRepository planoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalRepository profissionalRepository,
        ICurrentUserContext currentUser)
    {
        _assinaturaRepository = assinaturaRepository;
        _planoRepository = planoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalRepository = profissionalRepository;
        _currentUser = currentUser;
    }

    public async Task<AssinaturaResponseDto> IniciarAsync(
        IniciarAssinaturaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var plano = await _planoRepository.ObterPorIdAsync(request.PlanoId, cancellationToken);
        if (plano is null || !plano.Ativo)
        {
            throw new PlanoNaoEncontradoException();
        }

        ValidarTitular(request);

        var assinatura = request.TipoAssinatura switch
        {
            TipoAssinatura.Estabelecimento => await CriarParaEstabelecimentoAsync(request, userId, cancellationToken),
            TipoAssinatura.ProfissionalAutonomo => await CriarParaProfissionalAutonomoAsync(request, userId, cancellationToken),
            _ => throw new AssinaturaTitularInvalidoException()
        };

        await _assinaturaRepository.AdicionarAsync(assinatura, cancellationToken);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        return AssinaturaResponseDto.From(assinatura);
    }

    private async Task<Assinatura> CriarParaEstabelecimentoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var estabelecimentoId = request.EstabelecimentoId!.Value;
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(estabelecimentoId, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(estabelecimentoId, userId, cancellationToken);
        if (vinculo is null)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        if (await _assinaturaRepository.ExisteAtivaOuPendentePorEstabelecimentoAsync(estabelecimentoId, cancellationToken))
        {
            throw new AssinaturaDuplicadaException();
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway);
        assinatura.EstabelecimentoId = estabelecimentoId;

        return assinatura;
    }

    private async Task<Assinatura> CriarParaProfissionalAutonomoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var profissionalId = request.ProfissionalAutonomoId!.Value;
        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        if (profissional.UsuarioId != userId)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        if (await _assinaturaRepository.ExisteAtivaOuPendentePorProfissionalAutonomoAsync(profissionalId, cancellationToken))
        {
            throw new AssinaturaDuplicadaException();
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway);
        assinatura.ProfissionalAutonomoId = profissionalId;

        return assinatura;
    }

    private static void ValidarTitular(IniciarAssinaturaRequestDto request)
    {
        var titularEstabelecimento = request.EstabelecimentoId.HasValue;
        var titularAutonomo = request.ProfissionalAutonomoId.HasValue;

        var titularValido = request.TipoAssinatura switch
        {
            TipoAssinatura.Estabelecimento => titularEstabelecimento && !titularAutonomo,
            TipoAssinatura.ProfissionalAutonomo => titularAutonomo && !titularEstabelecimento,
            _ => false
        };

        if (!titularValido)
        {
            throw new AssinaturaTitularInvalidoException();
        }
    }

    private static Assinatura CriarAssinaturaBase(int planoId, GatewayPagamento gateway) =>
        new()
        {
            PlanoId = planoId,
            Status = AssinaturaStatus.PendentePagamento,
            Inicio = DateTime.UtcNow,
            Gateway = gateway,
            RenovacaoAutomatica = true
        };

    private int ObterUserIdAutenticado()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUser.UserId.Value;
    }
}

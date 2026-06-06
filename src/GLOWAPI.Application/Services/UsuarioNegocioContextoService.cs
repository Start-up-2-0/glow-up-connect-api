using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class UsuarioNegocioContextoService : IUsuarioNegocioContextoService
{
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IMatrizPermissaoNegocioService _matrizPermissaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly ICurrentUserContext _currentUserContext;

    public UsuarioNegocioContextoService(
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IMatrizPermissaoNegocioService matrizPermissaoNegocioService,
        IModulosAssinaturaService modulosAssinaturaService,
        ICurrentUserContext currentUserContext)
    {
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _matrizPermissaoNegocioService = matrizPermissaoNegocioService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<EstabelecimentoAcessoResponseDto>> ListarEstabelecimentosAsync(
        CancellationToken cancellationToken = default)
    {
        GarantirAcessoNegocio();

        var usuarioId = ObterUsuarioAutenticado();
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(
            usuarioId,
            cancellationToken);

        var response = new List<EstabelecimentoAcessoResponseDto>(vinculos.Count);
        foreach (var vinculo in vinculos)
        {
            if (vinculo.Estabelecimento is null)
            {
                continue;
            }

            var possuiVinculoProfissional = await _profissionalEstabelecimentoRepository.ExisteAtivoPorUsuarioAsync(
                usuarioId,
                vinculo.EstabelecimentoId,
                cancellationToken);
            var permissoes = _matrizPermissaoNegocioService
                .ObterPermissoes(vinculo.RoleNoEstabelecimento, possuiVinculoProfissional)
                .Select(permissao => permissao.ToString())
                .OrderBy(permissao => permissao)
                .ToList();
            var modulos = await _modulosAssinaturaService.ObterPorEstabelecimentoAsync(
                vinculo.EstabelecimentoId,
                cancellationToken);

            response.Add(new EstabelecimentoAcessoResponseDto(
                vinculo.EstabelecimentoId,
                vinculo.Estabelecimento.PublicGuid,
                vinculo.Estabelecimento.Nome,
                vinculo.Estabelecimento.Logo,
                vinculo.RoleNoEstabelecimento.ToString(),
                possuiVinculoProfissional,
                permissoes,
                modulos.AssinaturaAtiva,
                modulos.AssinaturaId,
                modulos.PlanoId,
                modulos.PlanoNome,
                modulos.Modulos));
        }

        return response;
    }

    private void GarantirAcessoNegocio()
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        if (_currentUserContext.Role == UserRole.Cliente)
        {
            throw new ClienteSemAcessoNegocioException();
        }
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

using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class EstabelecimentoPerfilService : IEstabelecimentoPerfilService
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly ICurrentUserContext _currentUser;

    public EstabelecimentoPerfilService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        ICurrentUserContext currentUser)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _currentUser = currentUser;
    }

    public async Task<EstabelecimentoPerfilResponseDto> AtualizarAsync(
        int estabelecimentoId,
        AtualizarEstabelecimentoPerfilDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdComEnderecoAsync(estabelecimentoId, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(
            estabelecimentoId,
            userId,
            cancellationToken);

        if (vinculo is null)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        static Exception CriarExcecao(string mensagem) => new EstabelecimentoAssinaturaInvalidoException(mensagem);

        estabelecimento.Nome = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Nome, "Nome do estabelecimento", 150, CriarExcecao);
        estabelecimento.Logo = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Logo, "Logo do estabelecimento", 500, CriarExcecao);
        estabelecimento.Telefone = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Telefone, "Telefone do estabelecimento", 20, CriarExcecao);
        estabelecimento.Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Email, "Email do estabelecimento", 255, CriarExcecao);
        estabelecimento.UpdatedAt = DateTime.UtcNow;

        OperacaoPerfilValidation.AtualizarEndereco(
            estabelecimento.Endereco,
            endereco => estabelecimento.Endereco = endereco,
            request.Endereco,
            CriarExcecao);

        _estabelecimentoRepository.Atualizar(estabelecimento);
        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        return EstabelecimentoPerfilResponseDto.From(estabelecimento);
    }

    private int ObterUserIdAutenticado()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUser.UserId.Value;
    }
}

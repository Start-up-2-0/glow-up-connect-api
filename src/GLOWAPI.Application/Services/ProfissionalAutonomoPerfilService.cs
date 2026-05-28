using GLOWAPI.Application.DTOs.Profissionais;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class ProfissionalAutonomoPerfilService : IProfissionalAutonomoPerfilService
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly ICurrentUserContext _currentUser;

    public ProfissionalAutonomoPerfilService(
        IProfissionalRepository profissionalRepository,
        ICurrentUserContext currentUser)
    {
        _profissionalRepository = profissionalRepository;
        _currentUser = currentUser;
    }

    public async Task<ProfissionalAutonomoPerfilResponseDto> AtualizarAsync(
        int profissionalId,
        AtualizarProfissionalAutonomoPerfilDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var profissional = await _profissionalRepository.ObterPorIdComEnderecoAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        if (profissional.UsuarioId != userId)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        static Exception CriarExcecao(string mensagem) => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem);

        profissional.NomePublico = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.NomePublico, "Nome publico do profissional", 150, CriarExcecao);
        profissional.Logo = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Logo, "Logo do profissional", 500, CriarExcecao);
        profissional.Telefone = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Telefone, "Telefone do profissional", 20, CriarExcecao);
        profissional.Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Email, "Email do profissional", 255, CriarExcecao);
        profissional.UpdatedAt = DateTime.UtcNow;

        OperacaoPerfilValidation.AtualizarEndereco(
            profissional.Endereco,
            endereco => profissional.Endereco = endereco,
            request.Endereco,
            CriarExcecao);

        _profissionalRepository.Atualizar(profissional);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        return ProfissionalAutonomoPerfilResponseDto.From(profissional);
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

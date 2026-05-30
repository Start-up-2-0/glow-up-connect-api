using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;

namespace GLOWAPI.Application.Services;

public class EstabelecimentoPerfilService : IEstabelecimentoPerfilService
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public EstabelecimentoPerfilService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<EstabelecimentoPerfilResponseDto> AtualizarAsync(
        int estabelecimentoId,
        AtualizarEstabelecimentoPerfilDto request,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdComEnderecoAsync(estabelecimentoId, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.NegocioEditar,
            cancellationToken);

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
}

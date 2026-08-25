using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ComodidadePerfilServiceTests
{
    private readonly Mock<IComodidadeRepository> _comodidadeRepository = new();
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _vinculoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacao = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    [Fact]
    public async Task ListarCatalogoAsync_DeveRetornarCatalogoOrdenadoDoRepositorio()
    {
        _comodidadeRepository.Setup(repository => repository.ListarAtivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Comodidade { Id = 1, Nome = "Wi-Fi", Slug = "wi-fi", Icone = "wifi", Ordem = 1 },
                new Comodidade { Id = 2, Nome = "Café", Slug = "cafe", Icone = "coffee", Ordem = 2 }
            ]);

        var resultado = await CriarService().ListarCatalogoAsync();

        Assert.Collection(resultado,
            item => Assert.Equal("wi-fi", item.Slug),
            item => Assert.Equal("cafe", item.Slug));
    }

    [Fact]
    public async Task AtualizarEstabelecimentoAsync_DeveRejeitarIdsDuplicados()
    {
        ConfigurarAutorizacao(20, PermissaoNegocio.NegocioEditar);

        var acao = () => CriarService().AtualizarEstabelecimentoAsync(20, [1, 1]);

        var excecao = await Assert.ThrowsAsync<EstabelecimentoAssinaturaInvalidoException>(acao);
        Assert.Contains("duplicados", excecao.Message);
        _comodidadeRepository.Verify(
            repository => repository.SubstituirDoEstabelecimentoAsync(
                It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AtualizarProfissionalAutonomoAsync_DeveUsarEstabelecimentoTenant()
    {
        _currentUser.Setup(context => context.UserId).Returns(10);
        _profissionalRepository.Setup(repository => repository.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 30,
                UsuarioId = 10,
                Ativo = true,
                TipoProfissional = ProfessionalType.Autonomo
            });
        _vinculoRepository.Setup(repository => repository.ObterAtivoPorProfissionalAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento { ProfissionalId = 30, EstabelecimentoId = 20, Ativo = true });
        _comodidadeRepository.Setup(repository => repository.ListarAtivasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Comodidade { Id = 1, Nome = "Wi-Fi", Slug = "wi-fi", Icone = "wifi", Ordem = 1 }]);
        _comodidadeRepository.Setup(repository => repository.ListarPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Comodidade { Id = 1, Nome = "Wi-Fi", Slug = "wi-fi", Icone = "wifi", Ordem = 1 }]);

        var resultado = await CriarService().AtualizarProfissionalAutonomoAsync(30, [1]);

        Assert.Single(resultado);
        _comodidadeRepository.Verify(repository => repository.SubstituirDoEstabelecimentoAsync(
            20,
            It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 1 })),
            It.IsAny<CancellationToken>()));
    }

    private void ConfigurarAutorizacao(int estabelecimentoId, PermissaoNegocio permissao)
    {
        _autorizacao.Setup(service => service.AutorizarAsync(estabelecimentoId, permissao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                estabelecimentoId,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { permissao }));
    }

    private ComodidadePerfilService CriarService() => new(
        _comodidadeRepository.Object,
        _estabelecimentoRepository.Object,
        _profissionalRepository.Object,
        _vinculoRepository.Object,
        _autorizacao.Object,
        _currentUser.Object);
}

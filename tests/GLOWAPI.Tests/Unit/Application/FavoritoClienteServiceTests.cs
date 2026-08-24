using GLOWAPI.Application.DTOs.Favoritos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class FavoritoClienteServiceTests
{
    private readonly Mock<IFavoritoClienteRepository> _favoritos = new();
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentos = new();
    private readonly Mock<IProfissionalRepository> _profissionais = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _vinculos = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    public FavoritoClienteServiceTests() =>
        _currentUser.SetupGet(context => context.UserId).Returns(7);

    [Fact]
    public async Task AdicionarAsync_DeveSalvarLoja()
    {
        var guid = Guid.NewGuid();
        _estabelecimentos
            .Setup(repository => repository.ObterPorPublicGuidAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 10, PublicGuid = guid, Nome = "Studio", Ativo = true, VisivelPublicamente = true });
        FavoritoCliente? salvo = null;
        _favoritos
            .Setup(repository => repository.AdicionarAsync(It.IsAny<FavoritoCliente>(), It.IsAny<CancellationToken>()))
            .Callback<FavoritoCliente, CancellationToken>((favorito, _) => salvo = favorito)
            .Returns(Task.CompletedTask);

        var resultado = await CriarService().AdicionarAsync(new CriarFavoritoClienteRequestDto(guid, null));

        Assert.Equal("Loja", resultado.Tipo);
        Assert.NotNull(salvo);
        Assert.Equal(7, salvo.UsuarioClienteId);
        Assert.Null(salvo.ProfissionalId);
        _favoritos.Verify(repository => repository.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_DeveClassificarProfissionalAutonomo()
    {
        var lojaGuid = Guid.NewGuid();
        var profissionalGuid = Guid.NewGuid();
        _estabelecimentos
            .Setup(repository => repository.ObterPorPublicGuidAsync(lojaGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 10, PublicGuid = lojaGuid, Nome = "Espaco", Ativo = true, VisivelPublicamente = true });
        _profissionais
            .Setup(repository => repository.ObterPorPublicGuidAsync(profissionalGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional { Id = 20, PublicGuid = profissionalGuid, NomePublico = "Ana", TipoProfissional = ProfessionalType.Autonomo });
        _vinculos
            .Setup(repository => repository.ExisteAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultado = await CriarService().AdicionarAsync(new CriarFavoritoClienteRequestDto(lojaGuid, profissionalGuid));

        Assert.Equal("ProfissionalAutonomo", resultado.Tipo);
        Assert.Equal(profissionalGuid, resultado.ProfissionalPublicGuid);
    }

    [Fact]
    public async Task AdicionarAsync_DeveSerIdempotente()
    {
        var guid = Guid.NewGuid();
        var estabelecimento = new Estabelecimento { Id = 10, PublicGuid = guid, Nome = "Studio", Ativo = true, VisivelPublicamente = true };
        _estabelecimentos
            .Setup(repository => repository.ObterPorPublicGuidAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimento);
        _favoritos
            .Setup(repository => repository.ObterAsync(7, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FavoritoCliente { Id = 3, UsuarioClienteId = 7, EstabelecimentoId = 10, Estabelecimento = estabelecimento });

        var resultado = await CriarService().AdicionarAsync(new CriarFavoritoClienteRequestDto(guid, null));

        Assert.Equal(3, resultado.Id);
        _favoritos.Verify(repository => repository.AdicionarAsync(It.IsAny<FavoritoCliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private FavoritoClienteService CriarService() => new(
        _favoritos.Object,
        _estabelecimentos.Object,
        _profissionais.Object,
        _vinculos.Object,
        _currentUser.Object);
}

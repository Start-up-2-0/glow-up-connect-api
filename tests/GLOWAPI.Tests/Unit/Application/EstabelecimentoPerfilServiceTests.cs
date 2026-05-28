using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class EstabelecimentoPerfilServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IEstabelecimentoUsuarioRepository> _estabelecimentoUsuarioRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    public EstabelecimentoPerfilServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(10);
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarDadosEEndereco_QuandoUsuarioTemVinculo()
    {
        var estabelecimento = new Estabelecimento
        {
            Id = 20,
            Nome = "Antigo",
            Logo = "logo-antigo",
            Telefone = "111",
            Email = "antigo@email.com",
            Ativo = true,
            Endereco = new Endereco
            {
                Cidade = "Cidade antiga",
                Estado = "AA",
                Logradouro = "Local antigo",
                Cep = "Nao informado",
                Numero = "S/N",
                Bairro = "Nao informado"
            }
        };

        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimento);

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = 20,
                UsuarioId = 10,
                RoleNoEstabelecimento = EstablishmentUserRole.Owner,
                Ativo = true
            });

        var service = CreateService();

        var response = await service.AtualizarAsync(20, new AtualizarEstabelecimentoPerfilDto
        {
            Nome = " Studio Novo ",
            Logo = " https://cdn.test/novo.png ",
            Telefone = "11999999999",
            Email = "novo@email.com",
            Endereco = new()
            {
                Cidade = "Sao Paulo",
                Estado = "SP",
                Local = "Rua Nova"
            }
        });

        Assert.Equal("Studio Novo", response.Nome);
        Assert.Equal("https://cdn.test/novo.png", response.Logo);
        Assert.Equal("11999999999", response.Telefone);
        Assert.Equal("novo@email.com", response.Email);
        Assert.Equal("Sao Paulo", response.Endereco.Cidade);
        Assert.Equal("SP", response.Endereco.Estado);
        Assert.Equal("Rua Nova", response.Endereco.Local);
        Assert.NotNull(estabelecimento.UpdatedAt);
        Assert.NotNull(estabelecimento.Endereco!.UpdatedAt);

        _estabelecimentoRepository.Verify(r => r.Atualizar(estabelecimento), Times.Once);
        _estabelecimentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoLogoNaoForInformado()
    {
        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });

        _estabelecimentoUsuarioRepository
            .Setup(r => r.ObterAtivoAsync(20, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstabelecimentoUsuario { EstabelecimentoId = 20, UsuarioId = 10, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<EstabelecimentoAssinaturaInvalidoException>(() =>
            service.AtualizarAsync(20, new AtualizarEstabelecimentoPerfilDto
            {
                Nome = "Studio",
                Logo = " ",
                Telefone = "11999999999",
                Email = "studio@email.com",
                Endereco = new() { Cidade = "Sao Paulo", Estado = "SP", Local = "Rua Glow" }
            }));
    }

    private EstabelecimentoPerfilService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _estabelecimentoUsuarioRepository.Object,
            _currentUser.Object);
}

using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Negocios;
using GLOWAPI.Tests.Helpers;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class EstabelecimentoPerfilServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IEnderecoGeocodificacaoService> _enderecoGeocodificacaoService = new();
    private readonly Mock<IConfirmacaoWhatsAppEstabelecimentoService> _confirmacaoWhatsAppEstabelecimentoService = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    public EstabelecimentoPerfilServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(10);
        _usuarioRepository
            .Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 10, Email = "owner@email.com" });
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.NegocioEditar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.NegocioEditar }));

        _enderecoGeocodificacaoService
            .Setup(s => s.TentarGeocodificarAsync(It.IsAny<Endereco>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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

        var service = CreateService();

        var response = await service.AtualizarAsync(20, new AtualizarEstabelecimentoPerfilDto
        {
            Nome = " Studio Novo ",
            Logo = " https://cdn.test/novo.png ",
            Telefone = "11999999999",
            Email = "novo@email.com",
            Endereco = EnderecoOperacaoDtoBuilder.Criar(
                cidade: "Sao Paulo",
                logradouro: "Rua Nova",
                cep: "01310100",
                numero: "200",
                bairro: "Bela Vista")
        });

        Assert.Equal("Studio Novo", response.Nome);
        Assert.Equal("https://cdn.test/novo.png", response.Logo);
        Assert.Equal("11999999999", response.Telefone);
        Assert.Equal("novo@email.com", response.Email);
        Assert.Equal("Sao Paulo", response.Endereco.Cidade);
        Assert.Equal("SP", response.Endereco.Estado);
        Assert.Equal("Rua Nova", response.Endereco.Logradouro);
        Assert.NotNull(estabelecimento.UpdatedAt);
        Assert.NotNull(estabelecimento.Endereco!.UpdatedAt);

        _estabelecimentoRepository.Verify(r => r.Atualizar(estabelecimento), Times.Once);
        _estabelecimentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _enderecoGeocodificacaoService.Verify(s => s.TentarGeocodificarAsync(
            estabelecimento.Endereco,
            It.IsAny<CancellationToken>()), Times.Once);
        _autorizacaoNegocioService.Verify(s => s.AutorizarAsync(
            20,
            PermissaoNegocio.NegocioEditar,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoLogoNaoForInformado()
    {
        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });

        var service = CreateService();

        await Assert.ThrowsAsync<EstabelecimentoAssinaturaInvalidoException>(() =>
            service.AtualizarAsync(20, new AtualizarEstabelecimentoPerfilDto
            {
                Nome = "Studio",
                Logo = " ",
                Telefone = "11999999999",
                Email = "studio@email.com",
                Endereco = EnderecoOperacaoDtoBuilder.Criar(cidade: "Sao Paulo", logradouro: "Rua Glow")
            }));
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissaoEditarNegocio()
    {
        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 20, Ativo = true });
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.NegocioEditar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.AtualizarAsync(20, new AtualizarEstabelecimentoPerfilDto
            {
                Nome = "Studio",
                Logo = "logo",
                Telefone = "11999999999",
                Email = "studio@email.com",
                Endereco = EnderecoOperacaoDtoBuilder.Criar(cidade: "Sao Paulo", logradouro: "Rua Glow")
            }));

        _estabelecimentoRepository.Verify(r => r.Atualizar(It.IsAny<Estabelecimento>()), Times.Never);
    }

    private EstabelecimentoPerfilService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _autorizacaoNegocioService.Object,
            _enderecoGeocodificacaoService.Object,
            _confirmacaoWhatsAppEstabelecimentoService.Object,
            _usuarioRepository.Object,
            _currentUser.Object);
}

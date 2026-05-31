using GLOWAPI.Application.DTOs.Profissionais;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Tests.Helpers;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ProfissionalAutonomoPerfilServiceTests
{
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<IEnderecoGeocodificacaoService> _enderecoGeocodificacaoService = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();
    private readonly Mock<IConfirmacaoWhatsAppService> _confirmacaoWhatsAppService = new();
    private readonly Mock<IConfirmacaoWhatsAppEstabelecimentoService> _confirmacaoWhatsAppEstabelecimentoService = new();

    public ProfissionalAutonomoPerfilServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(10);
        _enderecoGeocodificacaoService
            .Setup(s => s.TentarGeocodificarAsync(It.IsAny<Endereco>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarDadosEEndereco_QuandoPerfilPertenceAoUsuario()
    {
        var profissional = new Profissional
        {
            Id = 30,
            UsuarioId = 10,
            NomePublico = "Antigo",
            TipoProfissional = ProfessionalType.Autonomo,
            Ativo = true
        };
        var estabelecimento = new Estabelecimento
        {
            Id = 40,
            Nome = "Antigo",
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

        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);

        _usuarioRepository
            .Setup(r => r.ObterPorIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 10, Telefone = "11977777777" });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorProfissionalAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 30,
                EstabelecimentoId = 40,
                Estabelecimento = estabelecimento,
                Ativo = true
            });

        var service = CreateService();

        var response = await service.AtualizarAsync(30, new AtualizarProfissionalAutonomoPerfilDto
        {
            NomePublico = " Maria Nova ",
            Logo = " https://cdn.test/maria.png ",
            Telefone = "11988888888",
            Email = "maria@email.com",
            Endereco = EnderecoOperacaoDtoBuilder.Criar(
                cidade: "Campinas",
                logradouro: "Rua das Palmeiras",
                numero: "12",
                bairro: "Centro")
        });

        Assert.Equal("Maria Nova", response.NomePublico);
        Assert.Equal("https://cdn.test/maria.png", response.Logo);
        Assert.Equal("11988888888", response.Telefone);
        Assert.Equal("maria@email.com", response.Email);
        Assert.Equal("Campinas", response.Endereco.Cidade);
        Assert.Equal("SP", response.Endereco.Estado);
        Assert.Equal("Rua das Palmeiras", response.Endereco.Logradouro);
        Assert.Equal("Maria Nova", estabelecimento.Nome);
        Assert.Equal("Campinas", estabelecimento.Endereco!.Cidade);

        _profissionalRepository.Verify(r => r.Atualizar(profissional), Times.Once);
        _estabelecimentoRepository.Verify(r => r.Atualizar(estabelecimento), Times.Once);
        _profissionalRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoPerfilNaoPertenceAoUsuario()
    {
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 30,
                UsuarioId = 99,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.AtualizarAsync(30, new AtualizarProfissionalAutonomoPerfilDto
            {
                NomePublico = "Maria",
                Logo = "https://cdn.test/maria.png",
                Telefone = "11988888888",
                Email = "maria@email.com",
                Endereco = EnderecoOperacaoDtoBuilder.Criar(cidade: "Campinas", logradouro: "Rua A")
            }));
    }

    private ProfissionalAutonomoPerfilService CreateService() =>
        new(
            _profissionalRepository.Object,
            _estabelecimentoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _usuarioRepository.Object,
            _currentUser.Object,
            _enderecoGeocodificacaoService.Object,
            _confirmacaoWhatsAppService.Object,
            _confirmacaoWhatsAppEstabelecimentoService.Object);
}

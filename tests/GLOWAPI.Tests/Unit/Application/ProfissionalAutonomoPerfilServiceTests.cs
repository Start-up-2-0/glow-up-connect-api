using GLOWAPI.Application.DTOs.Profissionais;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ProfissionalAutonomoPerfilServiceTests
{
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    public ProfissionalAutonomoPerfilServiceTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(10);
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

        _profissionalRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);

        var service = CreateService();

        var response = await service.AtualizarAsync(30, new AtualizarProfissionalAutonomoPerfilDto
        {
            NomePublico = " Maria Nova ",
            Logo = " https://cdn.test/maria.png ",
            Telefone = "11988888888",
            Email = "maria@email.com",
            Endereco = new()
            {
                Cidade = "Campinas",
                Estado = "SP",
                Local = "Sala 12"
            }
        });

        Assert.Equal("Maria Nova", response.NomePublico);
        Assert.Equal("https://cdn.test/maria.png", response.Logo);
        Assert.Equal("11988888888", response.Telefone);
        Assert.Equal("maria@email.com", response.Email);
        Assert.Equal("Campinas", response.Endereco.Cidade);
        Assert.Equal("SP", response.Endereco.Estado);
        Assert.Equal("Sala 12", response.Endereco.Local);

        _profissionalRepository.Verify(r => r.Atualizar(profissional), Times.Once);
        _profissionalRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoPerfilNaoPertenceAoUsuario()
    {
        _profissionalRepository
            .Setup(r => r.ObterPorIdComEnderecoAsync(30, It.IsAny<CancellationToken>()))
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
                Endereco = new() { Cidade = "Campinas", Estado = "SP", Local = "Sala 12" }
            }));
    }

    private ProfissionalAutonomoPerfilService CreateService() =>
        new(_profissionalRepository.Object, _currentUser.Object);
}

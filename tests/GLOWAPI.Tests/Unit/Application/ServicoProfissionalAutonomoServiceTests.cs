using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.DTOs.Servicos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ServicoProfissionalAutonomoServiceTests
{
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IServicoNegocioService> _servicoNegocioService = new();
    private readonly Mock<IProfissionalServicoNegocioService> _profissionalServicoNegocioService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    public ServicoProfissionalAutonomoServiceTests()
    {
        _currentUserContext.Setup(c => c.UserId).Returns(10);
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 40,
                UsuarioId = 10,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorProfissionalAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 40,
                EstabelecimentoId = 20,
                Ativo = true
            });
    }

    [Fact]
    public async Task CriarAsync_DeveAutoVincularProfissional()
    {
        _servicoNegocioService
            .Setup(s => s.CriarAsync(20, It.IsAny<CriarServicoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServicoResponseDto { Id = 30, Nome = "Corte", EstabelecimentoId = 20, Ativo = true });

        _servicoNegocioService
            .Setup(s => s.ListarAsync(
                20,
                It.Is<ServicoFiltroDto>(f => f.ProfissionalId == 40),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ServicoResponseDto
                {
                    Id = 30,
                    Nome = "Corte",
                    EstabelecimentoId = 20,
                    Ativo = true,
                    Profissionais =
                    [
                        new ServicoProfissionalResumoDto
                        {
                            ProfissionalId = 40,
                            Ativo = true,
                            Preco = 80,
                            DuracaoMinutos = 45
                        }
                    ]
                }
            ]);

        var service = CreateService();
        var response = await service.CriarAsync(
            40,
            new CriarServicoRequestDto
            {
                Nome = "Corte",
                PrecoBase = 80,
                DuracaoMinutos = 45
            });

        _profissionalServicoNegocioService.Verify(
            s => s.VincularAsync(
                20,
                40,
                30,
                It.IsAny<VincularServicoProfissionalRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(30, response.Id);
    }

    [Fact]
    public async Task ListarAsync_DeveLancarExcecao_QuandoUsuarioNaoEProprietario()
    {
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(40, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 40,
                UsuarioId = 99,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.ListarAsync(40, new ServicoFiltroDto()));
    }

    private ServicoProfissionalAutonomoService CreateService() =>
        new(
            _profissionalRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _servicoNegocioService.Object,
            _profissionalServicoNegocioService.Object,
            _currentUserContext.Object);
}

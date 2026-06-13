using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AuditoriaNegocioServiceTests
{
    private readonly Mock<IAuditoriaNegocioRepository> _auditoriaRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    [Fact]
    public async Task RegistrarAsync_DevePersistirAuditoriaComUsuarioAtualEPayload()
    {
        AuditoriaNegocio? auditoria = null;
        _currentUserContext
            .SetupGet(c => c.UserId)
            .Returns(99);
        _auditoriaRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<AuditoriaNegocio>(), It.IsAny<CancellationToken>()))
            .Callback<AuditoriaNegocio, CancellationToken>((entity, _) => auditoria = entity)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeRoleAlterada,
            nameof(EstabelecimentoUsuario),
            30,
            new
            {
                usuarioId = 40,
                roleAnterior = "Receptionist",
                roleNova = "Admin"
            });

        Assert.NotNull(auditoria);
        Assert.Equal(20, auditoria!.EstabelecimentoId);
        Assert.Equal(99, auditoria.UsuarioId);
        Assert.Equal(TipoAcaoAuditoriaNegocio.UsuarioEquipeRoleAlterada, auditoria.TipoAcao);
        Assert.Equal(nameof(EstabelecimentoUsuario), auditoria.Entidade);
        Assert.Equal(30, auditoria.EntidadeId);
        Assert.Contains("roleAnterior", auditoria.PayloadJson);
        Assert.Contains("roleNova", auditoria.PayloadJson);
        _auditoriaRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_DevePermitirUsuarioAnonimoQuandoContextoNaoTemUsuario()
    {
        AuditoriaNegocio? auditoria = null;
        _auditoriaRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<AuditoriaNegocio>(), It.IsAny<CancellationToken>()))
            .Callback<AuditoriaNegocio, CancellationToken>((entity, _) => auditoria = entity)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.RegistrarAsync(
            20,
            TipoAcaoAuditoriaNegocio.CaixaResumoConsultado,
            nameof(Caixa),
            50);

        Assert.NotNull(auditoria);
        Assert.Null(auditoria!.UsuarioId);
        Assert.Equal("{}", auditoria.PayloadJson);
    }

    private AuditoriaNegocioService CreateService() =>
        new(
            _auditoriaRepository.Object,
            _currentUserContext.Object);
}

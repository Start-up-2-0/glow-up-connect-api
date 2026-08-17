using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class CompactacaoImagensPersistidasServiceTests
{
    private static readonly string ImagemGrande = "data:image/jpeg;base64," + new string('A', 20_000);
    private const string ImagemCompactada = "data:image/jpeg;base64,compactada";

    [Fact]
    public async Task ProcessarLoteAsync_DeveAtualizarApenasQuandoNovoValorForMenor()
    {
        var repository = new Mock<ICompactacaoImagensRepository>();
        var thumbnailer = new Mock<IBase64ImageThumbnailer>();

        repository
            .Setup(r => r.ListarLoteUsuariosComAvatarAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImagemPersistidaRegistro(1, ImagemGrande)]);
        repository
            .Setup(r => r.ListarLoteEstabelecimentosComLogoAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ImagemPersistidaRegistro>());
        repository
            .Setup(r => r.ListarLoteProfissionaisComLogoAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ImagemPersistidaRegistro>());

        thumbnailer
            .Setup(t => t.ParaPersistencia(ImagemGrande, null, null))
            .Returns(ImagemCompactada);

        string? valorPersistido = null;
        repository
            .Setup(r => r.AtualizarAvatarUsuarioAsync(1, ImagemCompactada, It.IsAny<CancellationToken>()))
            .Callback<int, string, CancellationToken>((_, valor, _) => valorPersistido = valor)
            .Returns(Task.CompletedTask);

        var service = new CompactacaoImagensPersistidasService(
            repository.Object,
            thumbnailer.Object,
            NullLogger<CompactacaoImagensPersistidasService>.Instance);

        var cursor = new CompactacaoImagensCursor();
        var resultado = await service.ProcessarLoteAsync(cursor, 50);

        Assert.Equal(1, resultado.RegistrosProcessados);
        Assert.Equal(1, resultado.RegistrosAtualizados);
        Assert.True(resultado.BytesEconomizados > 0);
        Assert.Equal(ImagemCompactada, valorPersistido);
        Assert.Equal(1, cursor.Usuarios);
    }

    [Fact]
    public async Task ProcessarLoteAsync_DeveIgnorarQuandoCompactacaoNaoReduzTamanho()
    {
        var repository = new Mock<ICompactacaoImagensRepository>();
        var thumbnailer = new Mock<IBase64ImageThumbnailer>();

        repository
            .Setup(r => r.ListarLoteUsuariosComAvatarAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImagemPersistidaRegistro(2, ImagemGrande)]);
        repository
            .Setup(r => r.ListarLoteEstabelecimentosComLogoAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ImagemPersistidaRegistro>());
        repository
            .Setup(r => r.ListarLoteProfissionaisComLogoAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ImagemPersistidaRegistro>());

        thumbnailer
            .Setup(t => t.ParaPersistencia(ImagemGrande, null, null))
            .Returns(ImagemGrande);

        var service = new CompactacaoImagensPersistidasService(
            repository.Object,
            thumbnailer.Object,
            NullLogger<CompactacaoImagensPersistidasService>.Instance);

        var resultado = await service.ProcessarLoteAsync(new CompactacaoImagensCursor(), 50);

        Assert.Equal(1, resultado.RegistrosProcessados);
        Assert.Equal(0, resultado.RegistrosAtualizados);
        Assert.Equal(1, resultado.RegistrosIgnorados);
        repository.Verify(
            r => r.AtualizarAvatarUsuarioAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Application;

public class AvatarBase64DecoderTests
{
    private const string PngDataUri =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    private readonly AvatarBase64Decoder _decoder = new(Options.Create(new AvatarOptions()));

    [Fact]
    public void ValidarENormalizar_DeveRetornarDataUri_QuandoEntradaValida()
    {
        var resultado = _decoder.ValidarENormalizar(PngDataUri, null);

        Assert.StartsWith("data:image/png;base64,", resultado);
        Assert.Contains("iVBORw0KGgo", resultado);
    }

    [Fact]
    public void ValidarENormalizar_DeveLancarExcecao_QuandoExcedeMaxSizeBytes()
    {
        var decoder = new AvatarBase64Decoder(Options.Create(new AvatarOptions { MaxSizeBytes = 10 }));

        var ex = Assert.Throws<AvatarInvalidoException>(() =>
            decoder.ValidarENormalizar(PngDataUri, null));

        Assert.Contains("bytes decodificados", ex.Message);
    }

    [Fact]
    public void ValidarENormalizar_DeveLancarExcecao_QuandoBase64Invalido()
    {
        Assert.Throws<AvatarInvalidoException>(() =>
            _decoder.ValidarENormalizar("data:image/png;base64,!!!", null));
    }

    [Fact]
    public void ValidarENormalizar_DeveLancarExcecao_QuandoContentTypeAusente()
    {
        Assert.Throws<AvatarInvalidoException>(() =>
            _decoder.ValidarENormalizar("aGVsbG8=", null));
    }
}

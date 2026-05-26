using FluentAssertions;
using GLOWAPI.Infrastructure.Mensageria;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class EmailConteudoHtmlTests
{
    [Fact]
    public void TextoParaHtml_DeveEscaparHtmlEConverterQuebrasDeLinha()
    {
        var html = EmailConteudoHtml.TextoParaHtml("a < b\nlinha 2");

        html.Should().Be("a &lt; b<br/>linha 2");
    }

    [Fact]
    public void ConteudoParaHtml_DevePreservarHtmlCompleto()
    {
        var html = "<!doctype html><html><body><strong>Ok</strong></body></html>";

        EmailConteudoHtml.ConteudoParaHtml(html).Should().Be(html);
    }
}

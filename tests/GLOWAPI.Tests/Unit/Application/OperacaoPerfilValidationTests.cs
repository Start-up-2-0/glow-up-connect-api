using GLOWAPI.Application.DTOs.Operacoes;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Negocios;
using GLOWAPI.Tests.Helpers;

namespace GLOWAPI.Tests.Unit.Application;

public class OperacaoPerfilValidationTests
{
    [Fact]
    public void CriarEndereco_DeveNormalizarCepERetornarEnderecoCompleto()
    {
        var endereco = OperacaoPerfilValidation.CriarEndereco(
            EnderecoOperacaoDtoBuilder.Criar(cep: "13010-000"),
            mensagem => new EnderecoOperacaoInvalidoException(mensagem));

        Assert.Equal("13010000", endereco.Cep);
        Assert.Equal("Campinas", endereco.Cidade);
        Assert.Equal("SP", endereco.Estado);
        Assert.True(OperacaoPerfilValidation.EnderecoEstaCompletoParaGeocodificacao(endereco));
    }

    [Fact]
    public void CriarEndereco_DeveLancarExcecao_QuandoCepInvalido()
    {
        var dto = EnderecoOperacaoDtoBuilder.Criar(cep: "123");

        Assert.Throws<EnderecoOperacaoInvalidoException>(() =>
            OperacaoPerfilValidation.CriarEndereco(
                dto,
                mensagem => new EnderecoOperacaoInvalidoException(mensagem)));
    }

    [Fact]
    public void MontarEnderecoGeocodificacao_DeveIncluirPartesPrincipais()
    {
        var endereco = OperacaoPerfilValidation.CriarEndereco(
            EnderecoOperacaoDtoBuilder.Criar(),
            mensagem => new EnderecoOperacaoInvalidoException(mensagem));

        var formatado = OperacaoPerfilValidation.MontarEnderecoGeocodificacao(endereco);

        Assert.Contains("Rua das Flores, 100", formatado);
        Assert.Contains("Centro", formatado);
        Assert.Contains("Campinas", formatado);
        Assert.Contains("SP", formatado);
        Assert.Contains("13010-000", formatado);
        Assert.Contains("Brasil", formatado);
    }

    [Fact]
    public void AtualizarEndereco_DeveLimparCoordenadas_QuandoEnderecoMudar()
    {
        var endereco = new Endereco
        {
            Cep = "13010000",
            Logradouro = "Rua A",
            Numero = "10",
            Bairro = "Centro",
            Cidade = "Campinas",
            Estado = "SP",
            Latitude = -22.9056m,
            Longitude = -47.0608m,
            GeocodificadoEm = DateTime.UtcNow
        };

        OperacaoPerfilValidation.AtualizarEndereco(
            endereco,
            _ => { },
            EnderecoOperacaoDtoBuilder.Criar(logradouro: "Rua B"),
            mensagem => new EnderecoOperacaoInvalidoException(mensagem));

        Assert.Null(endereco.Latitude);
        Assert.Null(endereco.Longitude);
        Assert.Null(endereco.GeocodificadoEm);
        Assert.Equal("Rua B", endereco.Logradouro);
    }
}

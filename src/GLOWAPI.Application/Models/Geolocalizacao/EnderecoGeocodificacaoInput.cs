using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Models.Geolocalizacao;

public record EnderecoGeocodificacaoInput(
    string Cep,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Estado,
    string? Complemento)
{
    public static EnderecoGeocodificacaoInput FromEntity(Endereco endereco) =>
        new(
            endereco.Cep,
            endereco.Logradouro,
            endereco.Numero,
            endereco.Bairro,
            endereco.Cidade,
            endereco.Estado,
            string.IsNullOrWhiteSpace(endereco.Complemento) ? null : endereco.Complemento);

    public string MontarConsultaLivre() =>
        OperacaoPerfilValidation.MontarEnderecoGeocodificacao(ToEntity());

    private Endereco ToEntity() =>
        new()
        {
            Cep = Cep,
            Logradouro = Logradouro,
            Numero = Numero,
            Bairro = Bairro,
            Cidade = Cidade,
            Estado = Estado,
            Complemento = Complemento ?? string.Empty
        };
}

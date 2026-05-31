using GLOWAPI.Application.DTOs.Operacoes;

namespace GLOWAPI.Tests.Helpers;

public static class EnderecoOperacaoDtoBuilder
{
    public static EnderecoOperacaoDto Criar(
        string cidade = "Campinas",
        string estado = "SP",
        string logradouro = "Rua das Flores",
        string cep = "13010000",
        string numero = "100",
        string bairro = "Centro",
        string? complemento = null) =>
        new()
        {
            Cep = cep,
            Logradouro = logradouro,
            Numero = numero,
            Bairro = bairro,
            Cidade = cidade,
            Estado = estado,
            Complemento = complemento
        };
}

using GLOWAPI.Application.DTOs.Operacoes;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Services;

internal static class OperacaoPerfilValidation
{
    public static string ValidarTextoObrigatorio(
        string valor,
        string campo,
        int maximoCaracteres,
        Func<string, Exception> criarExcecao)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw criarExcecao($"{campo} e obrigatorio.");
        }

        var normalizado = valor.Trim();
        if (normalizado.Length > maximoCaracteres)
        {
            throw criarExcecao($"{campo} deve ter no maximo {maximoCaracteres} caracteres.");
        }

        return normalizado;
    }

    public static Endereco CriarEndereco(
        EnderecoOperacaoDto dto,
        Func<string, Exception> criarExcecao)
    {
        var cidade = ValidarTextoObrigatorio(dto.Cidade, "Cidade", 100, criarExcecao);
        var estado = ValidarTextoObrigatorio(dto.Estado, "Estado", 50, criarExcecao);
        var local = ValidarTextoObrigatorio(dto.Local, "Local", 200, criarExcecao);

        return new Endereco
        {
            Cidade = cidade,
            Estado = estado,
            Logradouro = local,
            Cep = "Nao informado",
            Numero = "S/N",
            Bairro = "Nao informado"
        };
    }

    public static void AtualizarEndereco(
        Endereco? enderecoAtual,
        Action<Endereco> atribuirNovoEndereco,
        EnderecoOperacaoDto dto,
        Func<string, Exception> criarExcecao)
    {
        var novoEndereco = CriarEndereco(dto, criarExcecao);
        var endereco = enderecoAtual;

        if (endereco is null)
        {
            atribuirNovoEndereco(novoEndereco);
            return;
        }

        endereco.Cidade = novoEndereco.Cidade;
        endereco.Estado = novoEndereco.Estado;
        endereco.Logradouro = novoEndereco.Logradouro;
        endereco.Cep = string.IsNullOrWhiteSpace(endereco.Cep) ? novoEndereco.Cep : endereco.Cep;
        endereco.Numero = string.IsNullOrWhiteSpace(endereco.Numero) ? novoEndereco.Numero : endereco.Numero;
        endereco.Bairro = string.IsNullOrWhiteSpace(endereco.Bairro) ? novoEndereco.Bairro : endereco.Bairro;
        endereco.UpdatedAt = DateTime.UtcNow;
    }
}

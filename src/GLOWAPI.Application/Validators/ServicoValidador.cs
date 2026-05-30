using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Validators;

public static class ServicoValidador
{
    public const int NomeMaxLength = 150;
    public const int DescricaoMaxLength = 500;
    public const int DuracaoMinimaMinutos = 1;
    public const int DuracaoMaximaMinutos = 480;

    public static void ValidarNome(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ServicoNegocioInvalidoException("Nome do servico e obrigatorio.");
        }

        if (nome.Trim().Length > NomeMaxLength)
        {
            throw new ServicoNegocioInvalidoException($"Nome do servico deve ter no maximo {NomeMaxLength} caracteres.");
        }
    }

    public static void ValidarDescricao(string? descricao)
    {
        if (descricao is not null && descricao.Length > DescricaoMaxLength)
        {
            throw new ServicoNegocioInvalidoException($"Descricao do servico deve ter no maximo {DescricaoMaxLength} caracteres.");
        }
    }

    public static void ValidarPreco(decimal preco)
    {
        if (preco < 0)
        {
            throw new ServicoNegocioInvalidoException("Preco do servico nao pode ser negativo.");
        }
    }

    public static void ValidarDuracao(int duracaoMinutos)
    {
        if (duracaoMinutos < DuracaoMinimaMinutos || duracaoMinutos > DuracaoMaximaMinutos)
        {
            throw new ServicoNegocioInvalidoException(
                $"Duracao do servico deve estar entre {DuracaoMinimaMinutos} e {DuracaoMaximaMinutos} minutos.");
        }
    }

    public static void ValidarServico(string? nome, string? descricao, decimal precoBase, int duracaoMinutos)
    {
        ValidarNome(nome);
        ValidarDescricao(descricao);
        ValidarPreco(precoBase);
        ValidarDuracao(duracaoMinutos);
    }

    public static void ValidarPrecoDuracaoVinculo(decimal preco, int duracaoMinutos)
    {
        ValidarPreco(preco);
        ValidarDuracao(duracaoMinutos);
    }
}

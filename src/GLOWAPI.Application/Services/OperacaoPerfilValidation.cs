using System.Text.RegularExpressions;
using GLOWAPI.Application.DTOs.Operacoes;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Usuario;

namespace GLOWAPI.Application.Services;

internal static partial class OperacaoPerfilValidation
{
    private static readonly Regex CepRegex = CepValidoRegex();

    public static string ValidarLogoBase64(
        string logo,
        string nomeCampo,
        IAvatarBase64Decoder decoder,
        Func<string, Exception> criarExcecao) =>
        ValidarLogoBase64(logo, nomeCampo, decoder, thumbnailer: null, criarExcecao);

    public static string ValidarLogoBase64(
        string logo,
        string nomeCampo,
        IAvatarBase64Decoder decoder,
        IBase64ImageThumbnailer? thumbnailer,
        Func<string, Exception> criarExcecao)
    {
        if (string.IsNullOrWhiteSpace(logo))
        {
            throw criarExcecao($"{nomeCampo} e obrigatorio.");
        }

        try
        {
            var normalizado = decoder.ValidarENormalizar(logo.Trim(), null);
            return thumbnailer is null ? normalizado : thumbnailer.ParaPersistencia(normalizado);
        }
        catch (AvatarInvalidoException ex)
        {
            throw criarExcecao(ex.Message.Replace("Avatar", nomeCampo, StringComparison.Ordinal));
        }
    }

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
        var cep = NormalizarCep(dto.Cep, criarExcecao);
        var logradouro = ValidarTextoObrigatorio(dto.Logradouro, "Logradouro", 200, criarExcecao);
        var numero = ValidarTextoObrigatorio(dto.Numero, "Numero", 20, criarExcecao);
        var bairro = ValidarTextoObrigatorio(dto.Bairro, "Bairro", 100, criarExcecao);
        var cidade = ValidarTextoObrigatorio(dto.Cidade, "Cidade", 100, criarExcecao);
        var estado = NormalizarEstado(dto.Estado, criarExcecao);
        var complemento = NormalizarComplemento(dto.Complemento, criarExcecao);

        return new Endereco
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

        var enderecoMudou = EnderecoGeograficoMudou(endereco, novoEndereco);

        endereco.Cep = novoEndereco.Cep;
        endereco.Logradouro = novoEndereco.Logradouro;
        endereco.Numero = novoEndereco.Numero;
        endereco.Bairro = novoEndereco.Bairro;
        endereco.Cidade = novoEndereco.Cidade;
        endereco.Estado = novoEndereco.Estado;
        endereco.Complemento = novoEndereco.Complemento;

        if (enderecoMudou)
        {
            endereco.Latitude = null;
            endereco.Longitude = null;
            endereco.GeocodificadoEm = null;
        }

        endereco.UpdatedAt = DateTime.UtcNow;
    }

    public static bool EnderecoEstaCompletoParaGeocodificacao(Endereco endereco) =>
        !string.IsNullOrWhiteSpace(endereco.Cep)
        && endereco.Cep != "Nao informado"
        && !string.IsNullOrWhiteSpace(endereco.Logradouro)
        && !string.IsNullOrWhiteSpace(endereco.Numero)
        && !string.IsNullOrWhiteSpace(endereco.Bairro)
        && !string.IsNullOrWhiteSpace(endereco.Cidade)
        && !string.IsNullOrWhiteSpace(endereco.Estado)
        && endereco.Estado.Length == 2;

    public static bool EnderecoPossuiCoordenadas(Endereco endereco) =>
        endereco.Latitude.HasValue && endereco.Longitude.HasValue;

    public static string MontarEnderecoGeocodificacao(Endereco endereco)
    {
        var partes = new List<string>
        {
            $"{endereco.Logradouro}, {endereco.Numero}",
            endereco.Bairro,
            endereco.Cidade,
            endereco.Estado,
            FormatarCep(endereco.Cep),
            "Brasil"
        };

        if (!string.IsNullOrWhiteSpace(endereco.Complemento))
        {
            partes.Insert(1, endereco.Complemento);
        }

        return string.Join(", ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
    }

    private static bool EnderecoGeograficoMudou(Endereco atual, Endereco novo) =>
        !string.Equals(atual.Cep, novo.Cep, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(atual.Logradouro, novo.Logradouro, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(atual.Numero, novo.Numero, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(atual.Bairro, novo.Bairro, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(atual.Cidade, novo.Cidade, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(atual.Estado, novo.Estado, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(atual.Complemento, novo.Complemento, StringComparison.OrdinalIgnoreCase);

    private static string NormalizarCep(string cep, Func<string, Exception> criarExcecao)
    {
        if (string.IsNullOrWhiteSpace(cep))
        {
            throw criarExcecao("CEP e obrigatorio.");
        }

        var apenasDigitos = new string(cep.Where(char.IsDigit).ToArray());
        if (!CepRegex.IsMatch(apenasDigitos))
        {
            throw criarExcecao("CEP deve conter 8 digitos.");
        }

        return apenasDigitos;
    }

    private static string NormalizarEstado(string estado, Func<string, Exception> criarExcecao)
    {
        var normalizado = ValidarTextoObrigatorio(estado, "Estado", 2, criarExcecao).ToUpperInvariant();
        if (normalizado.Length != 2)
        {
            throw criarExcecao("Estado deve ser a UF com 2 caracteres.");
        }

        return normalizado;
    }

    private static string NormalizarComplemento(string? complemento, Func<string, Exception> criarExcecao)
    {
        if (string.IsNullOrWhiteSpace(complemento))
        {
            return string.Empty;
        }

        var normalizado = complemento.Trim();
        if (normalizado.Length > 100)
        {
            throw criarExcecao("Complemento deve ter no maximo 100 caracteres.");
        }

        return normalizado;
    }

    private static string FormatarCep(string cep) =>
        cep.Length == 8 ? $"{cep[..5]}-{cep[5..]}" : cep;

    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex CepValidoRegex();
}

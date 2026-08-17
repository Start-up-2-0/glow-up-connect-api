using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Manutencao;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Application.Services;

public class CompactacaoImagensPersistidasService : ICompactacaoImagensPersistidasService
{
    private readonly ICompactacaoImagensRepository _repository;
    private readonly IBase64ImageThumbnailer _thumbnailer;
    private readonly ILogger<CompactacaoImagensPersistidasService> _logger;

    public CompactacaoImagensPersistidasService(
        ICompactacaoImagensRepository repository,
        IBase64ImageThumbnailer thumbnailer,
        ILogger<CompactacaoImagensPersistidasService> logger)
    {
        _repository = repository;
        _thumbnailer = thumbnailer;
        _logger = logger;
    }

    public async Task<CompactacaoImagensResultado> ProcessarLoteAsync(
        CompactacaoImagensCursor cursor,
        int tamanhoLote,
        CancellationToken cancellationToken = default)
    {
        var resultado = CompactacaoImagensResultado.Vazio;

        var usuarios = await ProcessarEntidadeAsync(
            () => _repository.ListarLoteUsuariosComAvatarAsync(cursor.Usuarios, tamanhoLote, cancellationToken),
            (id, valor) => _repository.AtualizarAvatarUsuarioAsync(id, valor, cancellationToken),
            id => cursor.Usuarios = id,
            cancellationToken);
        resultado.Somar(usuarios);

        var estabelecimentos = await ProcessarEntidadeAsync(
            () => _repository.ListarLoteEstabelecimentosComLogoAsync(cursor.Estabelecimentos, tamanhoLote, cancellationToken),
            (id, valor) => _repository.AtualizarLogoEstabelecimentoAsync(id, valor, cancellationToken),
            id => cursor.Estabelecimentos = id,
            cancellationToken);
        resultado.Somar(estabelecimentos);

        var profissionais = await ProcessarEntidadeAsync(
            () => _repository.ListarLoteProfissionaisComLogoAsync(cursor.Profissionais, tamanhoLote, cancellationToken),
            (id, valor) => _repository.AtualizarLogoProfissionalAsync(id, valor, cancellationToken),
            id => cursor.Profissionais = id,
            cancellationToken);
        resultado.Somar(profissionais);

        if (resultado.RegistrosProcessados > 0)
        {
            _logger.LogInformation(
                "Compactacao de imagens: processados={Processados}, atualizados={Atualizados}, ignorados={Ignorados}, bytesEconomizados={BytesEconomizados}",
                resultado.RegistrosProcessados,
                resultado.RegistrosAtualizados,
                resultado.RegistrosIgnorados,
                resultado.BytesEconomizados);
        }

        return resultado;
    }

    private async Task<CompactacaoImagensResultado> ProcessarEntidadeAsync(
        Func<Task<IReadOnlyList<ImagemPersistidaRegistro>>> listarLote,
        Func<int, string, Task> atualizar,
        Action<int> atualizarCursor,
        CancellationToken cancellationToken)
    {
        var resultado = CompactacaoImagensResultado.Vazio;
        var lote = await listarLote();
        if (lote.Count == 0)
        {
            return resultado;
        }

        foreach (var registro in lote)
        {
            cancellationToken.ThrowIfCancellationRequested();
            resultado.RegistrosProcessados++;

            try
            {
                var compactado = _thumbnailer.ParaPersistencia(registro.ValorAtual);
                if (compactado.Length >= registro.ValorAtual.Length)
                {
                    resultado.RegistrosIgnorados++;
                    continue;
                }

                await atualizar(registro.Id, compactado);
                resultado.RegistrosAtualizados++;
                resultado.BytesEconomizados += registro.ValorAtual.Length - compactado.Length;
            }
            catch (Exception ex)
            {
                resultado.RegistrosIgnorados++;
                _logger.LogWarning(
                    ex,
                    "Falha ao compactar imagem persistida. EntidadeId={EntidadeId}",
                    registro.Id);
            }
        }

        atualizarCursor(lote[^1].Id);
        return resultado;
    }
}

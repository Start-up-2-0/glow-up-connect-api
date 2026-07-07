using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class MovimentacaoCaixaService : IMovimentacaoCaixaService
{
    private readonly ICaixaRepository _caixaRepository;
    private readonly ILancamentoCaixaRepository _lancamentoCaixaRepository;
    private readonly ISessaoCaixaRepository _sessaoCaixaRepository;

    public MovimentacaoCaixaService(
        ICaixaRepository caixaRepository,
        ILancamentoCaixaRepository lancamentoCaixaRepository,
        ISessaoCaixaRepository sessaoCaixaRepository)
    {
        _caixaRepository = caixaRepository;
        _lancamentoCaixaRepository = lancamentoCaixaRepository;
        _sessaoCaixaRepository = sessaoCaixaRepository;
    }

    public async Task<LancamentoCaixa> RegistrarLancamentoAsync(
        int estabelecimentoId,
        RegistrarLancamentoCaixaComando comando,
        CancellationToken cancellationToken = default)
    {
        ValidarComando(comando);

        var caixa = await _caixaRepository.ObterPorEstabelecimentoComTrackingAsync(
            estabelecimentoId,
            cancellationToken);

        if (caixa is null)
        {
            throw new CaixaNegocioNaoEncontradoException();
        }

        if (caixa.ExigirSessaoCaixaAberta)
        {
            var sessaoAberta = await _sessaoCaixaRepository.ObterSessaoAbertaPorCaixaAsync(
                caixa.Id,
                cancellationToken);

            if (sessaoAberta is null)
            {
                throw new SessaoCaixaInvalidaException(
                    "E necessario abrir uma sessao de caixa antes de registrar lancamentos.");
            }

            comando = comando with { SessaoCaixaId = sessaoAberta.Id };
        }

        if (comando.AgendamentoId.HasValue
            && comando.Tipo == LancamentoCaixaTipo.EntradaAgendamento
            && await _lancamentoCaixaRepository.ExisteLancamentoAtivoPorAgendamentoETipoAsync(
                comando.AgendamentoId.Value,
                LancamentoCaixaTipo.EntradaAgendamento,
                cancellationToken))
        {
            throw new AgendamentoJaRecebidoException();
        }

        var lancamento = new LancamentoCaixa
        {
            CaixaId = caixa.Id,
            AgendamentoId = comando.AgendamentoId,
            PagamentoId = comando.PagamentoId,
            ProfissionalId = comando.ProfissionalId,
            Tipo = comando.Tipo,
            Valor = comando.Valor,
            Descricao = comando.Descricao.Trim(),
            LancamentoOriginalId = comando.LancamentoOriginalId,
            SessaoCaixaId = comando.SessaoCaixaId,
            CreateAd = DateTime.UtcNow
        };

        await _lancamentoCaixaRepository.AdicionarAsync(lancamento, cancellationToken);
        await _lancamentoCaixaRepository.SalvarAlteracoesAsync(cancellationToken);

        await RecalcularSaldosAsync(caixa.Id, cancellationToken);

        return lancamento;
    }

    public async Task RecalcularSaldosAsync(
        int caixaId,
        CancellationToken cancellationToken = default)
    {
        var caixa = await _caixaRepository.ObterPorIdAsync(caixaId, cancellationToken);
        if (caixa is null)
        {
            return;
        }

        _caixaRepository.Atualizar(caixa);

        var lancamentos = await _lancamentoCaixaRepository.ListarTodosPorCaixaAsync(
            caixaId,
            cancellationToken);
        var saldos = CaixaSaldoCalculator.Calcular(lancamentos);

        caixa.SaldoTotal = saldos.SaldoTotal;
        caixa.SaldoDisponivel = saldos.SaldoDisponivel;
        caixa.SaldoRetido = saldos.SaldoRetido;
        caixa.UpdatedAt = DateTime.UtcNow;

        await _caixaRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private static void ValidarComando(RegistrarLancamentoCaixaComando comando)
    {
        if (comando.Valor <= 0)
        {
            throw new LancamentoCaixaInvalidoException("O valor do lancamento deve ser maior que zero.");
        }

        if (string.IsNullOrWhiteSpace(comando.Descricao))
        {
            throw new LancamentoCaixaInvalidoException("A descricao do lancamento e obrigatoria.");
        }

        if (!LancamentoCaixaClassificador.EhEntrada(comando.Tipo)
            && !LancamentoCaixaClassificador.EhSaida(comando.Tipo))
        {
            throw new LancamentoCaixaInvalidoException("Tipo de lancamento nao suportado.");
        }
    }
}

# Spec — Promocao de lancamento (100 primeiros)

## Objetivo

Oferecer **14 dias gratis** e **50% de desconto permanente na mensalidade** nos **100 primeiros** tenants que contratarem qualquer plano, com cartao tokenizado no onboarding via Mercado Pago Preapproval. Ao fim dos 14 dias, a mensalidade e cobrada para continuar a assinatura.

## Campanha

| Campo | Valor |
|-------|-------|
| Codigo | `lancamento-100` |
| Limite | 100 |
| DiasTrial | 14 |
| PercentualDescontoMensalidade | 50 |
| Escopo | 1 uso por `EstabelecimentoId` |
| Contagem | `vagasRestantes = 100 - COUNT(Assinaturas com CampanhaPromocionalId da campanha)` |

## API publica

`GET /api/planos` retorna `promocaoLancamento`:

```json
{
  "disponivel": true,
  "vagasRestantes": 87,
  "diasTrial": 14,
  "percentualDescontoMensalidade": 50,
  "diasAntecedenciaAlertaFatura": 3,
  "diasAntecedenciaGeracaoCobranca": 7
}
```

## Fluxo trial

1. `POST /api/assinaturas` com `pagamento` (token do cartao).
2. `PromocaoLancamentoService.TentarReservarVagaAsync` reserva vaga somente enquanto `COUNT(Assinaturas da campanha) < Limite`.
3. Assinatura persiste `PercentualDescontoPermanente = 50` (copiado da campanha).
4. `CriarAssinaturaRecorrenteAsync` no MP com `free_trial` de 14 dias e `transaction_amount` com 50% do plano.
5. Assinatura criada com `Status = Trial`, modulos liberados, **sem** `Pagamento` inicial.
6. Primeira cobranca no **fim do trial** (`ProximaDataVencimento = inicio + 14 dias`). Depois disso, os ciclos avancam um periodo (mensal).
7. Cobrancas internas e recorrentes usam `AssinaturaValorCobranca.CalcularMensalidade` (desconto permanente, inclusive em troca de plano).

## Fallback

Se a promocao nao estiver disponivel ou a reserva falhar, o fluxo segue como pagamento imediato (`PendentePagamento` + cobranca inicial).

Em staging, `AssinaturaCobranca__DesativarPromocaoLancamento=true` força `disponivel = false` e impede reserva de vaga, mesmo com a campanha ativa no banco. Usar para testar Checkout Pro / cobranca imediata sem alterar `CampanhasPromocionais`.

# Spec — Promocao de lancamento (100 primeiros)

## Objetivo

Oferecer **30 dias gratis** nos **100 primeiros** tenants que contratarem qualquer plano, com cartao tokenizado no onboarding via Mercado Pago Preapproval.

## Campanha

| Campo | Valor |
|-------|-------|
| Codigo | `lancamento-100` |
| Limite | 100 |
| DiasTrial | 30 |
| Escopo | 1 uso por `EstabelecimentoId` |

## API publica

`GET /api/planos` retorna `promocaoLancamento`:

```json
{
  "disponivel": true,
  "vagasRestantes": 87,
  "diasTrial": 30,
  "diasVencimentoPermitidos": [5, 10, 15, 20],
  "diasAntecedenciaAlertaFatura": 3,
  "diasAntecedenciaGeracaoCobranca": 2
}
```

## Fluxo trial

1. `POST /api/assinaturas` com `diaVencimento` e `pagamento` (token do cartao).
2. `PromocaoLancamentoService.TentarReservarVagaAsync` incrementa `Utilizados` de forma atomica.
3. `CriarAssinaturaRecorrenteAsync` no MP com `free_trial` de 30 dias.
4. Assinatura criada com `Status = Trial`, modulos liberados, **sem** `Pagamento` inicial.
5. Primeira cobranca interna gerada na `ProximaDataGeracaoCobranca` pos-trial.

## Fallback

Se a promocao nao estiver disponivel ou a reserva falhar, o fluxo segue como pagamento imediato (`PendentePagamento` + cobranca inicial).

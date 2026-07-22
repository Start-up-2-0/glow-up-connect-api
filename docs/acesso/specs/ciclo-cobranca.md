# Spec — Ciclo de cobranca da assinatura

## Referencia de ciclo

A data de referencia (`DataReferenciaCiclo`) e definida automaticamente na criacao da assinatura (data de contratacao). O usuario **nao escolhe** dia de vencimento.

## Campos em Assinatura

| Campo | Descricao |
|-------|-----------|
| `DataReferenciaCiclo` | Data imutavel de contratacao usada para calcular renovacoes |
| `ProximaDataVencimento` | Proxima data de renovacao/vencimento da fatura |
| `ProximaDataGeracaoCobranca` | Vencimento - 7 dias (D-7) |
| `ProximaDataAlerta` | Vencimento - 7 dias (D-7) |

## Calculo

`CicloCobrancaAssinaturaService`:

- Primeiro ciclo: proxima renovacao mensal (ou conforme `PlanoPeriodo`) em ou apos a referencia (fim do trial ou hoje).
- Proximo ciclo: adiciona um periodo ao vencimento atual.

## Inadimplencia

| Regra | Valor |
|-------|-------|
| Tolerancia apos vencimento | 10 dias |
| Status durante tolerancia | `Inadimplente` (modulos operacionais liberados) |
| Apos tolerancia | `Expirada` + lojas com `VisivelPublicamente = false` |

## Worker

`AssinaturaCobrancaWorker` executa diariamente:

1. Alertas em `ProximaDataAlerta`.
2. Geracao de cobranca em `ProximaDataGeracaoCobranca`.
3. Marcacao de cobrancas `Atrasado` e assinatura `Inadimplente`.
4. Encerramento automatico apos 10 dias de tolerancia.

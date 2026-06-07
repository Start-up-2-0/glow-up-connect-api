# Spec — Ciclo de cobranca da assinatura

## Dia de vencimento

O tenant escolhe **5, 10, 15 ou 20** no onboarding (`IniciarAssinaturaRequestDto.DiaVencimento`).

## Campos em Assinatura

| Campo | Descricao |
|-------|-----------|
| `ProximaDataVencimento` | Proxima ocorrencia do dia escolhido |
| `ProximaDataGeracaoCobranca` | Vencimento - 2 dias (D-2) |
| `ProximaDataAlerta` | Vencimento - 3 dias (D-3) |

## Calculo

`CicloCobrancaAssinaturaService`:

- Primeiro ciclo: proximo dia permitido **em ou apos** a referencia (fim do trial ou hoje).
- Proximo ciclo: calculado a partir do vencimento atual + 1 dia.

## Worker

`AssinaturaCobrancaWorker` executa diariamente:

1. Alertas em `ProximaDataAlerta`.
2. Geracao de cobranca em `ProximaDataGeracaoCobranca`.
3. Marcacao de cobrancas `Atrasado`.

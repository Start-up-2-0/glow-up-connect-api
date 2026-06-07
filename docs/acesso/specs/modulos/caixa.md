# Spec — Modulo Caixa

## Identificacao

- **Enum:** `ModuloAssinatura.Caixa` (valor `6`)
- **Plano minimo:** **Premium**

## Objetivo

Visualizacao do caixa do estabelecimento: saldo e lancamentos financeiros internos.

## Enforcement

- **HTTP:** `[RequerModuloAssinatura(..., ModuloAssinatura.Caixa, "estabelecimentoId")]`
- **Permissao:** `CaixaVisualizar` (Owner/Admin tipicamente; Receptionist **nao** possui).

## Endpoints

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

| Metodo | Rota | Permissao |
|--------|------|-----------|
| GET | `/caixa` | CaixaVisualizar |
| GET | `/caixa/lancamentos` | CaixaVisualizar |

## O que bloqueia sem modulo

Plus e Basic recebem **403** `SUBSCRIPTION_MODULE_BLOCKED` ao acessar caixa.

## Limites

Nenhum.

## Status

**Implementado** — leitura de caixa e lancamentos.

## Evolucao

- Lancamentos manuais e gestao (`CaixaGerenciar`) — ver modulo Financeiro.

## Codigo de referencia

- `EstabelecimentosController` (rotas `/caixa`)
- Entidade `Caixa`, `LancamentoCaixa`

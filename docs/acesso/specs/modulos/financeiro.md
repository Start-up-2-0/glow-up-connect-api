# Spec — Modulo Financeiro

## Identificacao

- **Enum:** `ModuloAssinatura.Financeiro` (valor `13`)
- **Plano minimo:** **Premium**

## Objetivo

Fluxo financeiro completo do negocio: relatorios, metricas, historico financeiro e operacoes de gestao alem da visualizacao de caixa.

## Funcionalidades comerciais (catalogo)

- Fluxo financeiro
- Relatorios financeiros
- Dashboard avancado
- Metricas do estabelecimento
- Historico financeiro

## Enforcement

- **Catalogo:** incluido no Premium; aparece em `modulos[]`.
- **HTTP:** **sem** `[RequerModuloAssinatura(Financeiro)]` hoje — endpoints dedicados ainda nao expostos.
- Caixa (subconjunto) ja protegido pelo modulo `Caixa`.

## Endpoints planejados

| Area | Status |
|------|--------|
| GET `/caixa` | Implementado (modulo Caixa) |
| Relatorios financeiros | Planejado |
| Dashboard avancado | Planejado |
| Gestao de lancamentos (POST) | Planejado |

## Permissoes previstas

- `CaixaVisualizar` — leitura
- `CaixaGerenciar` — Owner; operacoes de escrita

## Limites

Nenhum.

## Status

**Planejado** — modulo no catalogo e API de planos; enforcement HTTP dedicado **pendente**.

## Codigo de referencia

- `PlanoComercialCatalogo.ModulosPremium`
- `docs/acesso/planos/plano-premium.md`

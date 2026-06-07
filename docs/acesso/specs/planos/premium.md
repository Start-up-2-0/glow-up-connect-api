# Spec — Plano Premium

## Identificacao

| Campo | Valor |
|-------|-------|
| Nome (banco) | `Premium` |
| Slug catalogo | `premium` |
| Id seed | `3` |
| Preco | R$ 199,90 / Mensal |
| Ativo | true |

## Objetivo

Operacao comercial completa: financeiro, caixa, comissoes e **destaque no marketplace**.

## Modulos incluidos

Tudo do [Plus](./plus.md), mais:

```text
Caixa
Financeiro
ComissaoProfissionais
```

## Funcionalidades adicionais (catalogo)

- Controle de caixa
- Fluxo financeiro
- Comissao automatica
- Relatorios financeiros
- Dashboard avancado
- Metricas do estabelecimento
- Historico financeiro
- Gestao completa da equipe
- Prioridade na busca e listagem do marketplace

## Limites

| Limite | Valor |
|--------|-------|
| Todos comerciais | ilimitado |
| Prioridade listagem publica | **sim** |

## Endpoints liberados (implementados)

| Modulo | Rotas |
|--------|-------|
| Caixa | GET `.../caixa`, GET `.../caixa/lancamentos` |

## Modulos no catalogo ainda sem HTTP dedicado

- **Financeiro** — relatorios e gestao avancada (planejado)
- **ComissaoProfissionais** — consulta de comissao propria (planejado)

## Personas alvo

- Estabelecimentos maduros com controle financeiro
- Negocios que buscam visibilidade no marketplace

## Criterios de aceite

- [ ] Seed com nome `Premium`, preco 199.90, limites null.
- [ ] `prioridadeListagemPublica: true` em `GET /api/planos`.
- [ ] Caixa acessivel com `CaixaVisualizar` (Owner).
- [ ] Modulos Financeiro e ComissaoProfissionais listados na API.

## Specs de modulos

- [../modulos/caixa.md](../modulos/caixa.md)
- [../modulos/financeiro.md](../modulos/financeiro.md)
- [../modulos/comissao-profissionais.md](../modulos/comissao-profissionais.md)

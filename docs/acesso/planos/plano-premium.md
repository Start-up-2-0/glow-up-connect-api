# Plano Premium

Identificacao no catalogo: nome contem **`premium`**.

Inclui **todos os modulos do Plus** (e Basic) mais os abaixo.

## Modulos adicionais (alem do Plus)

| Modulo | Disponivel |
|--------|:----------:|
| Caixa | sim |
| Financeiro | sim |
| ComissaoProfissionais | sim |

## Limites comerciais

| Limite | Valor |
|--------|-------|
| Usuarios no negocio | Ilimitado |
| Agendamentos por dia | Ilimitado |
| Prioridade na listagem publica | **Sim** |

Estabelecimentos Premium podem aparecer com prioridade em `/api/publico/estabelecimentos/proximos`.

## Funcionalidades (catalogo comercial)

Tudo do Plus, mais:

- Controle de caixa
- Fluxo financeiro
- Comissao automatica
- Relatorios financeiros
- Dashboard avancado
- Metricas do estabelecimento
- Historico financeiro
- Gestao completa da equipe
- Prioridade na busca e listagem do marketplace

## O que o Premium desbloqueia na operacao

| Area | Endpoint base | Permissao tipica |
|------|---------------|------------------|
| Resumo do caixa | GET `.../caixa` | CaixaVisualizar |
| Lancamentos | GET `.../caixa/lancamentos` | CaixaVisualizar |
| Comissoes | *(matriz Profissional; endpoints em evolucao)* | ComissaoVisualizarPropria |
| Financeiro | *(catalogo; endpoints dedicados em evolucao)* | CaixaGerenciar (Owner) |

## Modulos x roles (exemplo Owner)

Com Premium ativo, Owner tem permissao **e** modulo para:

```text
Agenda + Servicos + Horarios + Profissionais + Caixa + WhatsApp + Email
```

Receptionist no mesmo Premium **nao** ve caixa (permissoes de role, nao de plano).

## Planos anteriores

- [plano-basic.md](./plano-basic.md)
- [plano-plus.md](./plano-plus.md)

## Referencia de modulos

- [../modulos.md](../modulos.md)

# Spec — Modulo Financeiro

## Identificacao

- **Enum:** `ModuloAssinatura.Financeiro` (valor `13`)
- **Plano minimo:** **Premium**

## Objetivo

Fluxo financeiro operacional do negocio: caixa, recebimento presencial, comissoes, relatorios, contas a pagar/receber e conciliacao basica.

## Funcionalidades implementadas

- Motor de movimentacao de caixa (`MovimentacaoCaixaService`) com recalculo de saldos
- Recebimento presencial de agendamentos (`POST /agendamentos/{id}/receber`)
- Sangria e reforco (`POST /caixa/lancamentos`)
- Estornos (`POST /caixa/lancamentos/{id}/estornar`)
- Sessao de caixa (abrir/fechar)
- CRUD de regras de comissao + calculo automatico no recebimento
- Extrato do profissional (`GET /financeiro/comissoes/minhas`)
- Relatorios analiticos, fluxo de caixa e export CSV
- Contas a pagar/receber com baixa vinculada ao caixa
- Conciliacao por importacao de linhas de extrato
- Painel da rede com faturamento por unidade

## Endpoints principais

| Metodo | Rota | Permissao |
|--------|------|-----------|
| GET | `/caixa`, `/caixa/lancamentos` | `CaixaVisualizar` |
| POST | `/caixa/lancamentos` | `CaixaGerenciar` |
| POST | `/caixa/lancamentos/{id}/estornar` | `CaixaGerenciar` |
| POST | `/agendamentos/{id}/receber` | `CaixaGerenciar` |
| GET/POST | `/caixa/sessoes/*` | visualizar / gerenciar |
| GET/POST/PUT/PATCH | `/financeiro/comissoes*` | visualizar / gerenciar |
| GET | `/financeiro/comissoes/minhas` | `ComissaoVisualizarPropria` |
| GET | `/financeiro/relatorios*`, `/financeiro/fluxo-caixa` | `CaixaVisualizar` |
| GET/POST | `/financeiro/contas-receber*`, `/financeiro/contas-pagar*` | visualizar / gerenciar |
| POST | `/financeiro/conciliacao/importar` | `CaixaGerenciar` |

## Permissoes

- `CaixaVisualizar` — leitura de caixa e relatorios
- `CaixaGerenciar` — Owner/Admin; escritas financeiras
- `ComissaoVisualizarPropria` — profissional; extrato proprio

## Auditoria

Escritas financeiras registram acoes em `TipoAcaoAuditoriaNegocio` (valores 32+).

## Migration

`ModuloFinanceiroCompleto` — entidades `SessaoCaixa`, `ContaReceber`, `ContaPagar`, `ConciliacaoItem` e campos extras em `LancamentoCaixa`/`Caixa`.

## Status

**Implementado** — API e app com fluxo operacional ponta a ponta.

## Codigo de referencia

- `MovimentacaoCaixaService`, `RecebimentoAgendamentoService`, `FinanceiroNegocioService`
- `EstabelecimentosController` (rotas `/caixa` e `/financeiro`)
- App: `caixaService.ts`, views em `src/views/modulos/financeiro/`

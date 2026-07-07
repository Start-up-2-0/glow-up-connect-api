# Spec — Modulo Financeiro

## Identificacao

- **Enum:** `ModuloAssinatura.Financeiro` (valor `13`)
- **Plano minimo:** **Premium**

## Objetivo

Fluxo financeiro operacional do negocio: caixa, recebimento presencial, comissoes, relatorios, contas a pagar/receber, conciliacao e busca unificada.

## Funcionalidades implementadas

- Motor de movimentacao de caixa (`MovimentacaoCaixaService`) com recalculo de saldos
- Recebimento presencial de agendamentos (`POST /agendamentos/{id}/receber`)
- Sangria e reforco (`POST /caixa/lancamentos`)
- Estornos (`POST /caixa/lancamentos/{id}/estornar`)
- Sessao de caixa (abrir/fechar)
- CRUD de regras de comissao + calculo automatico no recebimento
- Extrato do profissional (`GET /financeiro/comissoes/minhas`)
- Relatorios analiticos, fluxo de caixa e export CSV/Excel/PDF
- Busca unificada (`GET /financeiro/busca`)
- Contas a pagar/receber com baixa vinculada ao caixa e cancelamento
- Conciliacao por importacao de linhas de extrato
- Painel da rede com faturamento por unidade
- Pagamento via webhook gera lancamento de caixa (`EntradaAgendamento`)
- Job diario marca contas vencidas (`ContasVencimentoBackgroundService`)

## Endpoints principais

| Metodo | Rota | Permissao |
|--------|------|-----------|
| GET | `/caixa`, `/caixa/lancamentos` (paginado) | `CaixaVisualizar` |
| POST | `/caixa/lancamentos` | `CaixaGerenciar` |
| POST | `/caixa/lancamentos/{id}/estornar` | `CaixaGerenciar` |
| POST | `/agendamentos/{id}/receber` | `CaixaGerenciar` |
| GET/POST | `/caixa/sessoes/*` | visualizar / gerenciar |
| GET/POST/PUT/PATCH | `/financeiro/comissoes*` | visualizar / gerenciar |
| GET | `/financeiro/comissoes/minhas` | `ComissaoVisualizarPropria` |
| GET | `/financeiro/relatorios*`, `/financeiro/fluxo-caixa` | `CaixaVisualizar` |
| GET | `/financeiro/relatorios/export?formato=csv\|xlsx\|pdf` | `CaixaVisualizar` |
| GET | `/financeiro/busca` | `CaixaVisualizar` |
| GET/POST/PATCH | `/financeiro/contas-receber*` | visualizar / gerenciar |
| GET/POST/PATCH | `/financeiro/contas-pagar*` | visualizar / gerenciar |
| GET/POST | `/financeiro/conciliacao*` | visualizar / gerenciar |

## Permissoes

- `CaixaVisualizar` — leitura de caixa e relatorios
- `CaixaGerenciar` — Owner/Admin; escritas financeiras
- `ComissaoVisualizarPropria` — profissional; extrato proprio

## Auditoria

Escritas financeiras registram acoes em `TipoAcaoAuditoriaNegocio` (valores 32+).

## Migration

`ModuloFinanceiroCompleto` — entidades `SessaoCaixa`, `ContaReceber`, `ContaPagar`, `ConciliacaoItem` e campos extras em `LancamentoCaixa`/`Caixa`.

## Status

**Implementado** — API e app com fluxo operacional ponta a ponta, dashboard com graficos, busca global e exportacoes.

## Codigo de referencia

- `MovimentacaoCaixaService`, `RecebimentoAgendamentoService`, `FinanceiroNegocioService`, `WebhookPagamentoService`
- `EstabelecimentosController` (rotas `/caixa` e `/financeiro`)
- App: `caixaService.ts`, views em `src/views/modulos/financeiro/`, componentes em `src/components/financeiro/`

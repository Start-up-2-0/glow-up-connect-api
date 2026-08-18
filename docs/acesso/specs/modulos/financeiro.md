# Spec — Modulo Financeiro

## Identificacao

- **Enum:** `ModuloAssinatura.Financeiro` (valor `13`)
- **Plano minimo:** **Premium**

## Objetivo

Fluxo financeiro operacional do negocio em torno de **Entradas**, **Saídas** e **Comissões**: dashboard enxuto, listas unificadas, recebimento presencial na agenda, relatorios e ferramentas avancadas (conciliacao, painel da rede).

## Modelo unificado (UI)

`MovimentoFinanceiro` agrega lancamentos de caixa e contas a pagar/receber:

- `id`: `lancamento:{id}` ou `conta:{id}`
- `direcao`: `entrada` | `saida`
- `status`: `recebido` | `pago` | `pendente` | `vencido` | `estornado` | `cancelado`
- `origem`: `atendimento` | `manual` | `conta` | `pagamento_online` | `comissao`

## Funcionalidades implementadas

- Motor de movimentacao de caixa (`MovimentacaoCaixaService`) com recalculo de saldos
- **Sessao de caixa automatica** quando `ExigirSessaoCaixaAberta` e nao ha sessao aberta (invisivel na UI)
- Endpoints agregadores de entradas/saidas (`MovimentosFinanceirosService`)
- Dashboard com 6 KPIs (`GET /financeiro/dashboard`)
- Recebimento presencial de agendamentos (`POST /agendamentos/{id}/receber`) — permanece na Agenda
- CRUD de regras de comissao + calculo automatico no recebimento
- Extrato do profissional (`GET /financeiro/comissoes/minhas`)
- Relatorios analiticos e export CSV/Excel/PDF
- Contas a pagar/receber (legado) com baixa vinculada ao caixa
- Conciliacao por importacao de linhas de extrato
- Painel da rede com faturamento por unidade
- Pagamento via webhook gera lancamento de caixa (`EntradaAgendamento`)

## Endpoints principais (novos agregadores)

| Metodo | Rota | Permissao |
|--------|------|-----------|
| GET | `/financeiro/dashboard` | `CaixaVisualizar` |
| GET | `/financeiro/entradas` | `CaixaVisualizar` |
| POST | `/financeiro/entradas` | `CaixaGerenciar` |
| PATCH | `/financeiro/entradas/{id}/receber` | `CaixaGerenciar` |
| GET | `/financeiro/saidas` | `CaixaVisualizar` |
| POST | `/financeiro/saidas` | `CaixaGerenciar` |
| PATCH | `/financeiro/saidas/{id}/pagar` | `CaixaGerenciar` |

## Endpoints legados (compatibilidade)

| Metodo | Rota | Permissao |
|--------|------|-----------|
| GET | `/caixa`, `/caixa/lancamentos` | `CaixaVisualizar` |
| POST | `/caixa/lancamentos`, `/caixa/lancamentos/{id}/estornar` | `CaixaGerenciar` |
| POST | `/agendamentos/{id}/receber` | `CaixaGerenciar` |
| GET/POST/PUT/PATCH | `/financeiro/comissoes*` | visualizar / gerenciar |
| GET | `/financeiro/relatorios*`, `/financeiro/fluxo-caixa` | `CaixaVisualizar` |
| GET/POST/PATCH | `/financeiro/contas-receber*`, `/financeiro/contas-pagar*` | visualizar / gerenciar |
| GET/POST | `/financeiro/conciliacao*` | visualizar / gerenciar |

## Permissoes

- `CaixaVisualizar` — leitura de caixa e relatorios
- `CaixaGerenciar` — Owner/Admin; escritas financeiras
- `ComissaoVisualizarPropria` — profissional; extrato proprio

## App (menu)

- Visao geral (`/financeiro`)
- Entradas (`/financeiro/entradas`)
- Saidas (`/financeiro/saidas`)
- Comissoes (`/financeiro/comissoes`)
- Relatorios (`/financeiro/relatorios`) — inclui Conciliacao e Painel da rede em secao Avancado

Redirects legados: `/financeiro/caixa` e `/financeiro/movimentacoes` → entradas; `/financeiro/contas` → entradas/saidas pendentes; `/financeiro/conciliacao` e `/financeiro/rede` → relatorios.

## Codigo de referencia

- `MovimentosFinanceirosService`, `MovimentacaoCaixaService`, `FinanceiroNegocioService`
- `EstabelecimentosController` (rotas `/financeiro/entradas`, `/financeiro/saidas`, `/financeiro/dashboard`)
- App: `financeiroService.ts`, `financeiro.types.ts`, views em `src/views/modulos/financeiro/`

## Status

**Implementado** — reforma Entradas/Saidas/Comissoes com API agregadora e UI simplificada.

# Spec — Modulo Agenda

## Identificacao

- **Enum:** `ModuloAssinatura.Agenda` (valor `4`)
- **Plano minimo:** **Basic**

## Objetivo

Operacao interna de agenda: visualizar compromissos, confirmar/cancelar/remarcar agendamentos, iniciar e finalizar atendimentos.

## Enforcement

- **HTTP:** `[RequerModuloAssinatura(..., ModuloAssinatura.Agenda, "estabelecimentoId")]`
- **Erro:** 403 `SUBSCRIPTION_MODULE_BLOCKED`

## Endpoints

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

| Permissao | Metodos |
|-----------|---------|
| AgendaVisualizarGeral | GET `/agenda`, GET `/agendamentos/{id}/historico` |
| AgendaVisualizarPropria | GET `/agenda/propria` |
| AgendaCriar | POST `/agendamentos/{id}/confirmar`, GET `/disponibilidade` |
| AgendaCancelar | POST `/agendamentos/{id}/cancelar`, PATCH `/agendamentos/{id}/nao-compareceu` |
| AgendaReagendar | POST `/agendamentos/{id}/remarcar` |
| AtendimentoIniciar | POST `/atendimentos/{itemId}/iniciar` |
| AtendimentoFinalizar | POST `/atendimentos/{itemId}/finalizar` |

## Limites por plano

| Plano | AgendamentosPorDia (catalogo) |
|-------|-------------------------------|
| Basic | 10 |
| Plus | ilimitado |
| Premium | ilimitado |

> Enforcement de `AgendamentosPorDia` ainda **nao implementado** no service de agendamento.

## Personas

- Owner/Admin/Manager/Receptionist: agenda geral (conforme permissao).
- Profissional: agenda propria (`AgendaVisualizarPropria`).

## Status

**Implementado** — middleware + controllers.

## Codigo de referencia

- `EstabelecimentosController` (rotas de agenda)
- `AtendimentoProfissionalService`

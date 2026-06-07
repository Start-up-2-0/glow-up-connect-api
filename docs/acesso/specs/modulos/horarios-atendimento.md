# Spec — Modulo HorariosAtendimento

## Identificacao

- **Enum:** `ModuloAssinatura.HorariosAtendimento` (valor `9`)
- **Plano minimo:** **Basic**

## Objetivo

Configurar horarios de funcionamento da loja e horarios de atendimento por profissional; calcular disponibilidade interna.

## Enforcement

- **HTTP:** `[RequerModuloAssinatura(..., ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]`

## Endpoints

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

| Permissao | Metodos |
|-----------|---------|
| HorarioVisualizar | GET `/horarios-funcionamento`, GET `/profissionais/horarios` |
| HorarioGerenciar | POST/PUT/PATCH `/horarios-funcionamento/*` |
| HorarioGerenciarProprio | POST/PUT/PATCH `/profissionais/{profissionalId}/horarios/*` |
| AgendaCriar | GET `/disponibilidade` (tambem exige modulo Agenda) |

## Regras de negocio

- Horario da loja restringe disponibilidade geral.
- Horario do profissional restringe slots individuais.
- Profissional so edita horario proprio (`HorarioGerenciarProprio` validado no service).

## Limites

Nenhum limite numerico especifico.

## Status

**Implementado**.

## Codigo de referencia

- `HorarioProfissionalNegocioService`
- `DisponibilidadeAgendaService`

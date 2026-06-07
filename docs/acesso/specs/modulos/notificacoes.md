# Spec — Modulo Notificacoes

## Identificacao

- **Enum:** `ModuloAssinatura.Notificacoes` (valor `10`)
- **Plano minimo:** **Basic**

## Objetivo

Infraestrutura de notificacoes transacionais do negocio (fila `MensagensNotificacao`, workers de envio).

## O que libera

- Enfileiramento de mensagens ao negocio e profissionais (novo agendamento, confirmacao, cancelamento).
- Base para canais Email e WhatsApp.

## Enforcement

- **Catalogo:** presente em Basic+; exposto em `GET /api/planos` e contexto do usuario.
- **HTTP:** nao ha `[RequerModuloAssinatura(Notificacoes)]` dedicado — notificacoes disparam como efeito colateral de acoes de agenda.
- **Assincrono:** `AgendamentoNotificacaoService.EnfileirarNegocioAsync` notifica equipe sem checar modulo Notificacoes explicitamente (assume tenant ativo).

## Canais dependentes

| Canal | Modulo adicional |
|-------|------------------|
| E-mail equipe/assinatura | Email (catalogo) |
| WhatsApp cliente | WhatsApp (Plus+) |

## Limites

Nenhum.

## Status

**Implementado** (infra); checagem explicita por modulo Notificacoes **nao aplicada** em todos os fluxos.

## Codigo de referencia

- `MensagemNotificacaoService`
- `AgendamentoNotificacaoService`
- `EquipeNotificacaoService`

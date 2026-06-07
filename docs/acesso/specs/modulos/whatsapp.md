# Spec — Modulo WhatsApp

## Identificacao

- **Enum:** `ModuloAssinatura.WhatsApp` (valor `12`)
- **Plano minimo:** **Plus**

## Objetivo

Alertas automaticos via WhatsApp ao **cliente** sobre agendamentos (confirmacao, cancelamento, remarcacao, lembrete).

## Enforcement

- **Assincrono:** `AgendamentoNotificacaoService.EnfileirarClienteAsync` chama `PossuiModuloPorEstabelecimentoAsync(..., WhatsApp)`.
- **HTTP:** confirmacao de WhatsApp do **estabelecimento** (`/whatsapp/iniciar-confirmacao`) usa apenas `NegocioEditar` — nao exige modulo WhatsApp.

## Comportamento

```text
Com modulo WhatsApp + cliente com opt-in + telefone valido
  -> enfileira MensagensNotificacao canal WhatsApp

Sem modulo WhatsApp
  -> nao envia WhatsApp ao cliente (e-mail pode seguir em paralelo)
```

## Funcionalidades comerciais (catalogo Plus+)

- Confirmacao automatica via WhatsApp
- Lembrete automatico de agendamento
- Aviso de cancelamento

## Limites

Nenhum.

## Dependencias

- Config `Mensageria__WhatsApp__NumeroPlataforma`
- Webhook inbound: `WebhookWhatsAppService`
- Confirmacao de numero do usuario/estabelecimento

## Status

**Implementado** (envio ao cliente); confirmacao WhatsApp do lojista documentada em `docs/frontend/confirmacao-whatsapp.md`.

## Codigo de referencia

- `AgendamentoNotificacaoService`
- `WebhookWhatsAppService`

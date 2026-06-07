# Spec — Modulo Assinatura

## Identificacao

- **Enum:** `ModuloAssinatura.Assinatura` (valor `7`)
- **Plano minimo:** qualquer assinatura **ativa**

## Objetivo

Permitir que o tenant gerencie o proprio ciclo de assinatura: contratar, pagar, trocar plano e cancelar.

## O que libera

- Inicio de assinatura (`POST /api/assinaturas`)
- Troca de plano (`POST /api/assinaturas/{id}/trocar-plano`)
- Cancelamento (`POST /api/assinaturas/{id}/cancelar`)
- Consulta de historico (quando exposto)

## Enforcement

- **Automatico:** incluido em toda assinatura ativa junto com `Estabelecimento`.
- **HTTP:** endpoints de assinatura nao usam `[RequerModuloAssinatura]`; exigem autenticacao e ownership do tenant.

## Fluxo tipico

```text
POST /api/assinaturas -> PendentePagamento
Webhook pagamento -> Ativa -> demais modulos liberados
```

## Limites

Nenhum.

## Status

**Implementado** — fluxo de onboarding, pagamento e webhook.

## Codigo de referencia

- `AssinaturaService`
- `WebhookPagamentoService`
- `AssinaturasController`

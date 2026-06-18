# RIPD — Relatório de impacto

## Escopo

Agendamento online, cobrança de assinaturas (Mercado Pago) e confirmação via WhatsApp.

## Riscos

- Vazamento de dados de clientes em agendamentos públicos
- Fraude em webhooks de pagamento
- Acesso indevido cross-tenant

## Mitigações implementadas

- Rate limit global por IP
- Assinatura HMAC webhooks MP
- RBAC por estabelecimento
- Cookie HttpOnly para refresh token
- Auditoria de autenticação

## Residual

- MFA não implementado (backlog)
- Criptografia em repouso depende do provedor MySQL

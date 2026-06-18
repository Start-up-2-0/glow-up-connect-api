# Resposta a incidentes

## Fases

1. Detecção (alertas CI, logs `IP_BURST_BLOCKED`, auditoria)
2. Contenção (bloquear IP, rotacionar secrets, revogar sessões)
3. Erradicação (patch, validar webhooks)
4. Recuperação (restore backup MySQL)
5. Lições aprendidas

## Playbooks

### Vazamento de dados

- Isolar ambiente afetado
- Preservar logs (`LogAutenticacao`, `AuditoriaNegocio`)
- Notificar titulares e ANPD conforme prazo legal

### Comprometimento de credenciais

- Rotacionar `Auth__TokenSalt`, `MercadoPago__WebhookSecret`, `Mensageria__WhatsApp__ApiKey`
- Revogar todas as sessões (`SessoesAutenticacao`)

### Webhook fraudulento

- Validar assinaturas MP
- Revisar `WebhookPagamentos` duplicados
- Reconciliar pagamentos no gateway

## Contatos

Definir responsável DPO e canal de segurança corporativo.

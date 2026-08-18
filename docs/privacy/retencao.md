# Retenção de dados

| Dado | Prazo sugerido | Ação |
|------|----------------|------|
| `LogAutenticacao` | 12 meses | Purga automática (futuro) |
| `AuditoriaNegocio` | 24 meses | Arquivo |
| `WebhookPagamento` | 5 anos | Obrigação fiscal — não apagar na exclusão de conta |
| Pagamentos / faturas | 5 anos | Obrigação fiscal — não apagar na exclusão de conta |
| Titular após D+30 de exclusão | imediato | Anonimizar (e-mail `deleted+{id}@invalid.local`); lojas `Ativo = false`; assinatura `Cancelada` |
| `IpRateLimitBlocks` | até `BlockedUntil` | Limpeza diária |
| Mensageria logs | 90 dias | Mascaramento ativo |

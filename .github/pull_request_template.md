## Checklist de segurança

- [ ] Rotas novas com autorização adequada (auth + permissão de negócio)
- [ ] Sem secrets ou tokens em código/commits
- [ ] DTOs evitam mass assignment
- [ ] Endpoints públicos considerados no rate limit global
- [ ] Webhooks validam assinatura/secret

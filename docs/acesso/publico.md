# Acesso publico (visitante)

Usuario **sem autenticacao**. Rotas marcadas com `[AllowAnonymous]`.

Nao exige `x-glow-token`. Nao passa por `PermissionMiddleware`.

## O que pode acessar

### Saude e infra

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | `/health` | Health check |

### Cadastro e autenticacao

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/usuario` | Cadastro de cliente |
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/refresh` | Renovar token |
| POST | `/api/auth/confirmar-email` | Confirmar e-mail |
| POST | `/api/auth/reenviar-confirmacao` | Reenviar confirmacao |
| POST | `/api/auth/confirmar-whatsapp` | Confirmar WhatsApp (fallback) |
| POST | `/api/auth/reenviar-confirmacao-whatsapp` | Reenviar confirmacao WhatsApp |

### Planos

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | `/api/planos` | Listar planos ativos (modulos e funcionalidades inclusos) |

### Descoberta de estabelecimentos

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | `/api/publico/estabelecimentos/proximos` | Busca por geolocalizacao |
| GET | `/api/publico/estabelecimentos/{publicGuid}` | Detalhe publico da loja |

### Agendamento publico (sem conta)

Prefixo: `/api/publico/agendar`

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | `/loja/{publicGuid}/profissionais` | Profissionais da loja |
| GET | `/loja/{publicGuid}/servicos` | Servicos da loja |
| GET | `/loja/{publicGuid}/disponibilidade` | Horarios disponiveis |
| POST | `/loja/{publicGuid}` | Criar agendamento |
| GET | `/profissional/{publicGuid}/servicos` | Servicos do autonomo |
| GET | `/profissional/{publicGuid}/disponibilidade` | Disponibilidade do autonomo |
| POST | `/profissional/{publicGuid}` | Agendar com autonomo |

### Webhooks (integracao externa)

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/webhooks/pagamentos` | Eventos de gateway de pagamento |
| POST | `/api/webhooks/whatsapp/evolution` | Eventos WhatsApp Evolution |

## Modulos de assinatura

**Nenhum.** O visitante nao depende de plano do estabelecimento para consumir rotas publicas.

A loja precisa estar ativa e com dados publicaveis; limites de plano afetam operacao interna, nao a listagem publica basica.

## O que nao pode acessar

- Qualquer rota que exija `x-glow-token`
- Gestao de estabelecimento, equipe, caixa, agenda interna
- Meus agendamentos (`/api/agendamentos/me/*`) — requer login
- Assinaturas e onboarding de negocio

## Proximo passo apos cadastro

Ver [cliente.md](./cliente.md) para fluxo autenticado de cliente.

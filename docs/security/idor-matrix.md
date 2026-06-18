# Matriz IDOR — Glow Up Connect API

Auditoria de autorização por objeto (OWASP API1).

## Camadas de proteção

1. `GlowTokenAuthenticationMiddleware` — identidade
2. `PermissionMiddleware` — `RequerModuloAssinatura` + `RequerPermissaoNegocio`
3. Serviços de domínio — `AutorizacaoNegocioService`, validação de `estabelecimentoId`

## Endpoints críticos revisados

| Área | Padrão de rota | Controle |
|------|----------------|----------|
| Estabelecimentos | `/api/estabelecimentos/{id}` | Vínculo `EstabelecimentoUsuario` |
| Agendamentos | `/api/estabelecimentos/{id}/agendamentos` | Módulo Agenda + permissão |
| Serviços | `/api/estabelecimentos/{id}/servicos` | Módulo Servicos |
| Caixa/Financeiro | módulos premium | `RequerModuloAssinatura` |
| Convites | `/api/convites/{token}` preview anônimo; aceitar exige auth | Token opaco |
| Assinaturas | titular + estabelecimento | `AssinaturaService` |
| Usuário | `/api/usuario/me` | `ICurrentUserContext` |
| Privacidade | `/api/privacidade/*` | Usuário autenticado apenas |

## Testes cross-tenant

- `PermissionMiddlewareTests` — módulo e permissão negados
- Integração: usuário sem vínculo → `403 USER_SEM_VINCULO_NEGOCIO`

## Recomendações contínuas

- Todo novo controller com `{estabelecimentoId}` deve usar attributes de permissão
- Não expor IDs sequenciais em rotas públicas (usar `publicGuid`)

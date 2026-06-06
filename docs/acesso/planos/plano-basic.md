# Plano Basic

Identificacao no catalogo: nome contem **`basic`** ou **`basico`**.

Fonte: `PlanoComercialCatalogo` em `src/GLOWAPI.Application/Services/PlanoComercialCatalogo.cs`.

## Modulos liberados

| Modulo | Disponivel |
|--------|:----------:|
| Estabelecimento | sim (base) |
| Assinatura | sim (base) |
| Agenda | sim |
| Servicos | sim |
| HorariosAtendimento | sim |
| Notificacoes | sim |
| Email | sim |
| Profissionais | **nao** |
| WhatsApp | **nao** |
| Caixa | **nao** |
| Financeiro | **nao** |
| ComissaoProfissionais | **nao** |

## Limites comerciais

| Limite | Valor |
|--------|-------|
| Usuarios no negocio | 1 |
| Agendamentos por dia | 10 |
| Prioridade na listagem publica | Nao |

Limites adicionais podem vir de `Plano.LimiteProfissionais`, `LimiteServicos`, `LimiteAgendamentos` no banco.

## Funcionalidades (catalogo comercial)

- Cadastro de servicos
- Agenda simples
- Configuracao de horarios
- Pagina publica basica
- Gestao simples de clientes
- Confirmacao por e-mail
- Cancelamento por e-mail
- Lembrete por e-mail

## O que o Basic permite operar

Com role adequada no estabelecimento (ex.: Owner):

| Area | Disponivel |
|------|:----------:|
| Perfil do negocio | sim |
| Servicos (CRUD) | sim |
| Horarios loja e profissional | sim |
| Agenda e agendamentos internos | sim |
| Atendimentos iniciar/finalizar | sim |
| Equipe / multiusuario | **nao** |
| Convites de profissionais | **nao** |
| Caixa e lancamentos | **nao** |
| WhatsApp automatico ao cliente | **nao** (e-mail sim) |

## Roles que mais usam Basic

- **Profissional autonomo** iniciante (operacao solo)
- **Estabelecimento** com um unico operador

## Endpoints bloqueados por modulo

Exemplos de 403 `SUBSCRIPTION_MODULE_BLOCKED` no Basic:

- `GET /api/estabelecimentos/{id}/caixa` — requer **Caixa** (Premium)
- `POST /api/estabelecimentos/{id}/equipe/usuarios` — requer **Profissionais** (Plus)
- Notificacoes WhatsApp ao cliente — requer **WhatsApp** (Plus)

## Upgrade sugerido

| Necessidade | Plano |
|-------------|-------|
| Equipe, convites, WhatsApp | [plano-plus.md](./plano-plus.md) |
| Caixa, financeiro, comissoes | [plano-premium.md](./plano-premium.md) |

## Consulta na API

```http
GET /api/planos
```

Campo `modulos` lista os modulos efetivos por plano cadastrado.

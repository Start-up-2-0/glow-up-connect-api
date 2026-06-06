# Acesso — Cliente

Role global: **`UserRole.Cliente`**

Usuario final que agenda e paga servicos. **Nao** possui vinculo em `EstabelecimentoUsuario` e **nao** entra na matriz de `PermissaoNegocio`.

## Pre-requisitos

- Conta criada via `POST /api/usuario` (role fixa `Cliente`)
- E-mail confirmado (para login)
- Header `x-glow-token` em rotas autenticadas

## Herda acesso publico

Tudo descrito em [publico.md](./publico.md) continua disponivel (planos, agendar sem login, descoberta).

## Endpoints exclusivos do cliente logado

### Perfil

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | `/api/usuario/me` | Perfil atual |
| PUT | `/api/usuario/me` | Atualizar perfil |
| DELETE | `/api/usuario/me` | Desativar conta |
| POST | `/api/usuario/me/whatsapp/solicitar-confirmacao` | Confirmar WhatsApp pessoal |
| POST | `/api/usuario/me/whatsapp/opt-in` | Opt-in alertas WhatsApp |

### Autenticacao

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/auth/logout` | Encerrar sessao |

### Agendamentos (ownership — so os proprios)

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/agendamentos` | Criar agendamento logado |
| GET | `/api/agendamentos/me` | Listar meus agendamentos |
| GET | `/api/agendamentos/me/{id}` | Detalhe do meu agendamento |
| POST | `/api/agendamentos/me/{id}/cancelar` | Cancelar |
| POST | `/api/agendamentos/me/{id}/remarcar` | Remarcar |

Validacao: agendamento deve pertencer ao `UsuarioClienteId` do usuario logado.

### Convites

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/convites/{token}/aceitar` | Aceitar convite para equipe/profissional |
| POST | `/api/convites/{token}/rejeitar` | Rejeitar convite |

> Ao aceitar convite, o usuario pode passar a ter vinculo de negocio e outra role global pode ser necessaria conforme fluxo de onboarding.

## Modulos de assinatura

**Nenhum modulo comercial.** Cliente nao opera tenant de negocio.

Notificacoes recebidas (e-mail/WhatsApp de agendamento) dependem do **plano do estabelecimento** onde agendou, nao do cliente.

## O que o cliente nao acessa

| Area | Motivo |
|------|--------|
| `GET /api/usuario/me/estabelecimentos` | Role Cliente — retorna **403** `CLIENTE_SEM_ACESSO_NEGOCIO` |
| `/api/estabelecimentos/{id}/*` | Exige vinculo + permissao de negocio |
| Equipe, caixa, agenda geral | Roles de estabelecimento |
| `/api/assinaturas` | Contratacao de plano de negocio |
| `/api/profissionais-autonomos/*` | Ownership de profissional autonomo |

## Resumo visual

```text
Cliente
  |-- Publico (sem token)
  |     + descoberta, agendar anonimo, planos, cadastro, login
  |
  +-- Autenticado (x-glow-token)
        + perfil proprio
        + meus agendamentos (CRUD limitado ao proprio)
        + aceitar/rejeitar convites
```

## Frontend

Ver [frontend/area-cliente-agendamento.md](../frontend/area-cliente-agendamento.md).

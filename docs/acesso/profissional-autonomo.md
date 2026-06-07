# Acesso — Profissional autonomo

Role global: **`UserRole.ProfissionalAutonomo`**

Profissional que opera **sozinho**, com assinatura propria. Internamente usa um tenant `Estabelecimento` criado no onboarding — mesmo modelo comercial de loja, sem equipe.

## Pre-requisitos

- Assinatura ativa (`POST /api/assinaturas` com `tipoAssinatura: ProfissionalAutonomo`)
- Plano pago e confirmado via webhook de pagamento
- Ownership: `Profissional.UsuarioId == usuario logado`

## Controle de acesso

Diferente das rotas de estabelecimento:

- **Nao** usa `RequerPermissaoNegocio` nos controllers de autonomo
- **Nao** usa `RequerModuloAssinatura` no middleware HTTP dessas rotas
- Valida **ownership** do profissional + **limites do plano** nos services

Modulos e limites vêm de `ModulosAssinaturaService` / `PlanoComercialCatalogo` no tenant interno.

## Endpoints (`/api/profissionais-autonomos/{profissionalId}`)

| Metodo | Rota relativa | Area | Modulo equivalente |
|--------|---------------|------|-------------------|
| PUT | `/perfil` | Perfil publico | Estabelecimento |
| GET | `/horarios` | Listar horarios | HorariosAtendimento |
| POST | `/horarios` | Criar horario | HorariosAtendimento |
| PUT | `/horarios/{id}` | Atualizar horario | HorariosAtendimento |
| PATCH | `/horarios/{id}/status` | Ativar/desativar | HorariosAtendimento |
| GET | `/servicos` | Listar servicos | Servicos |
| POST | `/servicos` | Criar servico | Servicos |
| PUT | `/servicos/{id}` | Atualizar servico | Servicos |
| PATCH | `/servicos/{id}/status` | Status servico | Servicos |

## Modulos tipicos por plano (tenant autonomo)

| Plano | Modulos uteis ao autonomo |
|-------|---------------------------|
| Basic | Agenda, Servicos, HorariosAtendimento, Email, Notificacoes |
| Plus | Basic + WhatsApp |
| Premium | Plus + Caixa, Financeiro, ComissaoProfissionais |

Modulo **Profissionais** normalmente **nao** se aplica (operacao solo).

## Assinatura

| Metodo | Rota | Descricao |
|--------|------|-----------|
| POST | `/api/assinaturas` | Iniciar assinatura + criar perfil autonomo |
| POST | `/api/assinaturas/{id}/trocar-plano` | Upgrade/downgrade |
| POST | `/api/assinaturas/{id}/cancelar` | Cancelar |

Permissao: usuario deve ser dono da assinatura (vinculo no estabelecimento interno).

## Limites do plano (enforcement em service)

| Limite | Plano Basic (padrao) |
|--------|----------------------|
| Usuarios | 1 |
| Agendamentos/dia | ilimitado |
| Servicos ativos | conforme `Plano.LimiteServicos` |

Plano Plus/Premium: limites ampliados ou ilimitados — ver planos em [planos/](./planos/).

## Herda tambem

- Rotas publicas de agendamento (`/api/publico/agendar/profissional/{publicGuid}`)
- Perfil de usuario (`/api/usuario/me`)
- Se usar app como cliente: [cliente.md](./cliente.md) (agendamentos em outras lojas)

## O que nao acessa (por padrao)

| Area | Motivo |
|------|--------|
| Equipe / convites de profissionais | Modulo Profissionais + multiusuario |
| Agenda geral de loja com varios profissionais | Operacao solo |
| Endpoints `/api/estabelecimentos/{id}/equipe/*` | Gestao de equipe |

## Descoberta de modulos no frontend

```http
GET /api/usuario/me/estabelecimentos
```

Retorna o tenant interno com `Modulos[]` e `Permissoes[]` (como Owner apos onboarding).

## Documentos relacionados

- [planos/plano-basic.md](./planos/plano-basic.md)
- [tasks-onboarding-assinatura.md](../tasks-onboarding-assinatura.md)

# Spec — Plano Basic

## Identificacao

| Campo | Valor |
|-------|-------|
| Nome (banco) | `Basic` |
| Slug catalogo | `basic` |
| Id seed | `1` |
| Preco | R$ 29,99 / Mensal |
| Ativo | true |

## Objetivo

Plano de entrada para profissional autonomo ou estabelecimento com **um unico operador**. Cobre operacao essencial sem equipe nem financeiro avancado.

## Modulos incluidos

```text
Estabelecimento, Assinatura          (base — assinatura ativa)
Agenda, Servicos, HorariosAtendimento
Notificacoes, Email
```

## Funcionalidades (catalogo)

- Cadastro de serviços
- Agenda simples
- Configuração de horários
- Página pública básica
- Gestão simples de clientes
- Confirmação por e-mail
- Cancelamento por e-mail
- Lembrete por e-mail

## Limites

| Limite | Valor | Origem |
|--------|------:|--------|
| Usuarios no negocio | 1 | catalogo |
| Agendamentos por dia | ilimitado | catalogo |
| Profissionais | 1 | banco (`LimiteProfissionais`) |
| Servicos | 10 | banco (`LimiteServicos`) |
| Agendamentos (total) | ilimitado | banco (`LimiteAgendamentos` = null) |
| Prioridade listagem publica | nao | catalogo |

## O que NAO inclui

- Equipe / multiusuario (`Profissionais`)
- WhatsApp automatico ao cliente
- Caixa e lancamentos
- Financeiro e comissoes

## Personas alvo

- Profissional autonomo iniciante
- Estabelecimento com um unico operador (Owner solo)

## Endpoints bloqueados (exemplos)

| Rota | Modulo exigido |
|------|----------------|
| POST `.../equipe/usuarios` | Profissionais (Plus) |
| GET `.../caixa` | Caixa (Premium) |

## Upgrade

| Necessidade | Plano |
|-------------|-------|
| Equipe, convites, WhatsApp | [Plus](./plus.md) |
| Caixa, financeiro | [Premium](./premium.md) |

## Criterios de aceite

- [ ] Seed com nome `Basic`, preco 29.99, limites 1/10/null (agendamentos ilimitados).
- [ ] `GET /api/planos` lista Basic com 5 modulos operacionais (+ base na assinatura ativa).
- [ ] Autonomo no Basic libera Agenda e Servicos apos pagamento (fluxo integrado).
- [ ] Convite de profissional retorna 403 no Basic.

## Specs de modulos

- [../modulos/agenda.md](../modulos/agenda.md)
- [../modulos/servicos.md](../modulos/servicos.md)
- [../modulos/horarios-atendimento.md](../modulos/horarios-atendimento.md)
- [../modulos/email.md](../modulos/email.md)

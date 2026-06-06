# Plano Plus

Identificacao no catalogo: nome contem **`plus`**.

Inclui **todos os modulos do Basic** mais os abaixo.

## Modulos adicionais (alem do Basic)

| Modulo | Disponivel |
|--------|:----------:|
| Profissionais | sim |
| WhatsApp | sim |

## Modulos ainda bloqueados

| Modulo | Disponivel |
|--------|:----------:|
| Caixa | **nao** |
| Financeiro | **nao** |
| ComissaoProfissionais | **nao** |

## Limites comerciais

| Limite | Valor |
|--------|-------|
| Usuarios no negocio | Ilimitado |
| Agendamentos por dia | Ilimitado |
| Prioridade na listagem publica | Nao |

## Funcionalidades (catalogo comercial)

Tudo do Basic, mais:

- Multiusuario
- Agenda compartilhada
- Gestao de profissionais
- Historico de clientes
- Confirmacao automatica via WhatsApp
- Lembrete automatico de agendamento
- Aviso de cancelamento
- Dashboard basico
- Relatorios basicos

## O que o Plus desbloqueia na operacao

| Area | Disponivel |
|------|:----------:|
| Cadastro de usuarios da equipe | sim |
| Alterar role/status da equipe | sim |
| Convidar profissionais | sim |
| Gerenciar status de profissionais | sim |
| Convites por link (`/api/convites/*`) | sim |
| Notificacoes WhatsApp aos clientes | sim |
| Caixa / lancamentos | **nao** |
| Comissao automatica | **nao** |

## Permissoes de negocio necessarias

Modulo sozinho nao basta. Exemplos:

| Acao | Modulo | Permissao minima |
|------|--------|------------------|
| Convidar profissional | Profissionais | ProfissionalConvidar |
| Cadastrar usuario equipe | Profissionais | EquipeGerenciar |
| Ver caixa | Caixa | — (modulo indisponivel no Plus) |

Ver roles em [../estabelecimento/](../estabelecimento/).

## Upgrade sugerido

| Necessidade | Plano |
|-------------|-------|
| Caixa, fluxo financeiro, comissoes, prioridade marketplace | [plano-premium.md](./plano-premium.md) |

## Plano anterior

- [plano-basic.md](./plano-basic.md)

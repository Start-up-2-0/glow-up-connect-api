# Modulos de assinatura

Enum: `ModuloAssinatura` em `src/GLOWAPI.Domain/Enums/ModuloAssinatura.cs`

Modulos liberados quando **assinatura ativa** + plano inclui o modulo. Sempre somados ao tenant: `Estabelecimento`, `Assinatura` (base interna).

## Catalogo completo

| Modulo | Descricao | Exige plano minimo |
|--------|-----------|-------------------|
| Estabelecimento | Base do tenant | Qualquer assinatura ativa |
| Assinatura | Gestao da propria assinatura | Qualquer assinatura ativa |
| Agenda | Agenda interna, atendimentos, agendamentos do negocio | Basic |
| Servicos | CRUD e vinculos de servicos | Basic |
| HorariosAtendimento | Horarios da loja e profissionais, disponibilidade interna | Basic |
| Notificacoes | Infra de notificacoes | Basic |
| Email | Confirmacao, lembrete, cancelamento por e-mail | Basic |
| Profissionais | Equipe, convites, multiusuario | Plus |
| WhatsApp | Confirmacao e alertas automaticos WhatsApp | Plus |
| Caixa | Resumo e lancamentos financeiros | Premium |
| Financeiro | Fluxo financeiro (catalogo comercial) | Premium |
| ComissaoProfissionais | Comissao automatica (catalogo comercial) | Premium |
| Clientes | *(enum existe; endpoints dedicados ainda nao expostos)* | — |
| ProfissionalAutonomo | *(enum existe; nao usado no middleware)* | — |

## Endpoints por modulo (estabelecimento)

Prefixo base: `/api/estabelecimentos/{estabelecimentoId}`

### Agenda

| Permissao tipica | Metodos |
|------------------|---------|
| AgendaVisualizarGeral | GET `/agenda`, GET `/agendamentos/{id}/historico` |
| AgendaVisualizarPropria | GET `/agenda/propria` |
| AgendaCriar | POST `/agendamentos/{id}/confirmar`, GET `/disponibilidade` |
| AgendaCancelar | POST `/agendamentos/{id}/cancelar`, PATCH `.../nao-compareceu` |
| AgendaReagendar | POST `/agendamentos/{id}/remarcar` |
| AtendimentoIniciar | POST `/atendimentos/{itemId}/iniciar` |
| AtendimentoFinalizar | POST `/atendimentos/{itemId}/finalizar` |

### Servicos

| Permissao | Metodos |
|-----------|---------|
| ServicoVisualizar | GET `/servicos` |
| ServicoGerenciar | POST/PUT/PATCH `/servicos/*`, vinculos `/servicos/{id}/profissionais/*` |

### HorariosAtendimento

| Permissao | Metodos |
|-----------|---------|
| HorarioVisualizar | GET `/horarios-funcionamento`, GET `/profissionais/horarios` |
| HorarioGerenciar | POST/PUT/PATCH `/horarios-funcionamento/*` |
| HorarioGerenciarProprio | POST/PUT/PATCH `/profissionais/*/horarios/*` (validacao no service) |

### Profissionais

| Permissao | Metodos |
|-----------|---------|
| EquipeGerenciar | POST/PATCH `/equipe/usuarios/*` |
| ProfissionalConvidar | POST `/equipe/profissionais`, POST `/api/estabelecimentos/.../convites/profissionais` |
| ProfissionalGerenciar | PATCH `/equipe/profissionais/{id}/status`, DELETE convites |

### Caixa (Premium)

| Permissao | Metodos |
|-----------|---------|
| CaixaVisualizar | GET `/caixa`, GET `/caixa/lancamentos` |

### Sem atributo de modulo (so permissao)

| Permissao | Metodos |
|-----------|---------|
| NegocioEditar | PUT `/perfil`, POST `/whatsapp/*` |

## WhatsApp (comportamento assincrono)

Modulo **WhatsApp** nao bloqueia HTTP diretamente em todos os fluxos. Controla envio em `AgendamentoNotificacaoService`:

- Com modulo: tenta WhatsApp ao cliente (se opt-in)
- Sem modulo: apenas e-mail (se modulo Email ativo)

## Erros HTTP

| Codigo | Code | Quando |
|--------|------|--------|
| 401 | UNAUTHORIZED | Sem token |
| 403 | SUBSCRIPTION_MODULE_BLOCKED | Assinatura inativa ou modulo ausente no plano |
| 403 | *(dominio)* | Sem permissao de negocio |
| 400 | INVALID_SUBSCRIPTION_SCOPE | Parametro estabelecimentoId ausente |

## Cruzamento plano x role

```text
Acesso efetivo = Modulos[] (plano) INTERSECAO Permissoes[] (role no estabelecimento)
```

Consultar ambos em `GET /api/usuario/me/estabelecimentos`.

## Planos

- [plano-basic.md](./planos/plano-basic.md)
- [plano-plus.md](./planos/plano-plus.md)
- [plano-premium.md](./planos/plano-premium.md)

# Role no estabelecimento — Owner / Admin

`EstablishmentUserRole.Owner` e `EstablishmentUserRole.Admin` possuem **as mesmas permissoes** em `MatrizPermissaoNegocioService`.

Dono ou administrador do negocio. Acesso operacional **maximo** dentro do que o **plano** libera.

## Permissoes de negocio (`PermissaoNegocio`)

| Permissao | Area |
|-----------|------|
| NegocioVisualizar | Ver dados do negocio |
| NegocioEditar | Editar perfil, WhatsApp comercial |
| EquipeVisualizar | Ver equipe *(endpoint dedicado futuro)* |
| EquipeGerenciar | CRUD usuarios da equipe |
| ProfissionalConvidar | Convidar profissionais |
| ProfissionalGerenciar | Ativar/desativar profissionais |
| ServicoVisualizar | Listar servicos |
| ServicoGerenciar | CRUD servicos e vinculos |
| HorarioVisualizar | Ver horarios |
| HorarioGerenciar | CRUD horarios da loja e profissionais |
| AgendaVisualizarGeral | Agenda completa |
| AgendaCriar | Confirmar agendamentos, consultar disponibilidade |
| AgendaReagendar | Remarcar |
| AgendaCancelar | Cancelar, nao compareceu |
| ClienteVisualizarGeral | Ver clientes *(matriz; endpoint futuro)* |
| CaixaVisualizar | Ver caixa e lancamentos |
| CaixaGerenciar | Gerenciar caixa *(matriz; endpoints futuros)* |

## Modulos necessarios por area

Cruzamento **permissoes acima** + modulos do plano:

| Area | Modulo minimo | Plano minimo |
|------|---------------|--------------|
| Perfil / WhatsApp loja | — (so permissao) | Basic |
| Servicos | Servicos | Basic |
| Horarios | HorariosAtendimento | Basic |
| Agenda / atendimentos | Agenda | Basic |
| Equipe e profissionais | Profissionais | Plus |
| Convites | Profissionais | Plus |
| Caixa | Caixa | Premium |
| WhatsApp ao cliente | WhatsApp | Plus |
| E-mail ao cliente | Email | Basic |

## Endpoints principais

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

### Negocio (sem modulo extra)

- `PUT /perfil`
- `POST /whatsapp/solicitar-confirmacao`
- `POST /whatsapp/confirmar`
- `POST /whatsapp/opt-in`

### Equipe (modulo Profissionais + permissoes)

- `POST /equipe/usuarios` — EquipeGerenciar
- `PATCH /equipe/usuarios/{id}/role` — EquipeGerenciar
- `PATCH /equipe/usuarios/{id}/status` — EquipeGerenciar
- `POST /equipe/profissionais` — ProfissionalConvidar
- `PATCH /equipe/profissionais/{id}/status` — ProfissionalGerenciar

### Convites (modulo Profissionais)

- `POST /api/estabelecimentos/{id}/convites/profissionais` — ProfissionalConvidar
- `DELETE /api/estabelecimentos/{id}/convites/{id}` — ProfissionalGerenciar

### Agenda (modulo Agenda)

- `GET /agenda` — AgendaVisualizarGeral
- `GET /agenda/propria` — se tambem for profissional vinculado
- `POST /agendamentos/{id}/confirmar` — AgendaCriar
- `POST /agendamentos/{id}/cancelar` — AgendaCancelar
- `POST /agendamentos/{id}/remarcar` — AgendaReagendar
- `GET /agendamentos/{id}/historico` — AgendaVisualizarGeral
- `POST /atendimentos/{itemId}/iniciar|finalizar` — Atendimento*

### Caixa (modulo Caixa — Premium)

- `GET /caixa` — CaixaVisualizar
- `GET /caixa/lancamentos` — CaixaVisualizar

## Se Owner tambem atende clientes

Com vinculo em `ProfissionalEstabelecimento`, permissoes de **Profissional** sao **somadas**:

- AgendaVisualizarPropria
- AtendimentoIniciar / Finalizar
- ClienteVisualizarProprio
- ComissaoVisualizarPropria
- HorarioGerenciarProprio

Ver [profissional.md](./profissional.md).

## Diferenca Owner vs Admin

No codigo atual: **nenhuma** — mesma matriz. Distincao e semantica de negocio (titular vs delegado).

## Planos

- Basic: opera solo, sem equipe/caixa
- Plus: equipe + WhatsApp
- Premium: caixa + financeiro + prioridade listagem

Ver [../planos/](../planos/).

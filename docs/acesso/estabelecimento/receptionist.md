# Role no estabelecimento — Receptionist

`EstablishmentUserRole.Receptionist`

Recepcionista: operacao do dia (agenda, agendamentos, clientes), sem administracao de equipe, profissionais ou financeiro.

## Permissoes de negocio

| Permissao | Disponivel |
|-----------|:----------:|
| NegocioVisualizar | sim |
| NegocioEditar | **nao** |
| EquipeVisualizar | **nao** |
| EquipeGerenciar | **nao** |
| ProfissionalConvidar | **nao** |
| ProfissionalGerenciar | **nao** |
| ServicoVisualizar | sim |
| ServicoGerenciar | **nao** |
| HorarioVisualizar | sim |
| HorarioGerenciar | **nao** |
| AgendaVisualizarGeral | sim |
| AgendaCriar | sim |
| AgendaReagendar | sim |
| AgendaCancelar | sim |
| ClienteVisualizarGeral | sim |
| CaixaVisualizar | **nao** |
| CaixaGerenciar | **nao** |
| AtendimentoIniciar | **nao** |
| AtendimentoFinalizar | **nao** |

## Modulos uteis (plano do negocio)

| Modulo | Uso pelo Receptionist |
|--------|----------------------|
| Agenda | Principal — ver e operar agendamentos |
| Servicos | Somente leitura |
| HorariosAtendimento | Somente leitura |
| Email / WhatsApp | Beneficio indireto (notificacoes enviadas ao agendar) |
| Profissionais | **nao necessario** para permissoes atuais |
| Caixa | **bloqueado** por permissao |

Plano **Basic** e suficiente para a funcao, desde que modulos Agenda + Servicos (view) + Horarios (view) estejam ativos.

## Endpoints permitidos

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

| Metodo | Rota | Modulo | Permissao |
|--------|------|--------|-----------|
| GET | `/agenda` | Agenda | AgendaVisualizarGeral |
| GET | `/disponibilidade` | HorariosAtendimento | AgendaCriar |
| POST | `/agendamentos/{id}/confirmar` | Agenda | AgendaCriar |
| POST | `/agendamentos/{id}/cancelar` | Agenda | AgendaCancelar |
| POST | `/agendamentos/{id}/remarcar` | Agenda | AgendaReagendar |
| PATCH | `/agendamentos/{id}/nao-compareceu` | Agenda | AgendaCancelar |
| GET | `/agendamentos/{id}/historico` | Agenda | AgendaVisualizarGeral |
| GET | `/servicos` | Servicos | ServicoVisualizar |
| GET | `/horarios-funcionamento` | HorariosAtendimento | HorarioVisualizar |
| GET | `/profissionais/horarios` | HorariosAtendimento | HorarioVisualizar |

## Endpoints bloqueados

- Qualquer `POST/PUT/PATCH` em servicos ou horarios
- Equipe, convites, profissionais
- Caixa
- `PUT /perfil`, WhatsApp config
- Iniciar/finalizar atendimento (reservado ao profissional)

## Perfil tipico na operacao

```text
Recepcionista
  -> ve agenda de todos os profissionais
  -> confirma, cancela, remarca
  -> consulta servicos e horarios (leitura)
  -> nao mexe em equipe, caixa ou catalogo
```

## Se Receptionist tambem for profissional vinculado

Uniao de permissoes adiciona atendimento proprio — ver [profissional.md](./profissional.md).

## Comparacao

| Capacidade | Receptionist | Manager |
|------------|:------------:|:-------:|
| Agenda geral | sim | sim |
| CRUD servicos | nao | sim |
| CRUD horarios | nao | sim |
| Convidar profissional | nao | sim |
| Caixa | nao | nao* |

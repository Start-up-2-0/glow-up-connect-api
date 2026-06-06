# Role no estabelecimento — Profissional

`EstablishmentUserRole.Profissional`

Usuario que **atende clientes** dentro do negocio. Visao restrita a operacao **propria**.

Aplicada quando:

- Role no estabelecimento e `Profissional`, **ou**
- Usuario com outra role (Owner/Admin/Manager) **tambem** tem vinculo ativo em `ProfissionalEstabelecimento` — permissoes de profissional sao **somadas**.

## Permissoes de negocio

| Permissao | Disponivel |
|-----------|:----------:|
| NegocioVisualizar | sim |
| NegocioEditar | **nao** |
| EquipeGerenciar | **nao** |
| ProfissionalConvidar | **nao** |
| ProfissionalGerenciar | **nao** |
| ServicoVisualizar | sim |
| ServicoGerenciar | **nao** |
| HorarioVisualizar | sim |
| HorarioGerenciar | **nao** |
| HorarioGerenciarProprio | sim |
| AgendaVisualizarGeral | **nao** |
| AgendaVisualizarPropria | sim |
| AgendaCriar | **nao** |
| AgendaReagendar | **nao** |
| AgendaCancelar | **nao** |
| AtendimentoVisualizarProprio | sim |
| AtendimentoIniciar | sim |
| AtendimentoFinalizar | sim |
| ClienteVisualizarProprio | sim |
| ClienteVisualizarGeral | **nao** |
| ComissaoVisualizarPropria | sim |
| CaixaVisualizar | **nao** |

## Modulos necessarios

| Area | Modulo | Plano minimo |
|------|--------|--------------|
| Agenda propria | Agenda | Basic |
| Horarios proprios | HorariosAtendimento | Basic |
| Servicos (leitura) | Servicos | Basic |
| Comissao | ComissaoProfissionais | Premium |
| Atendimentos | Agenda | Basic |

## Endpoints permitidos

Prefixo: `/api/estabelecimentos/{estabelecimentoId}`

| Metodo | Rota | Modulo | Permissao |
|--------|------|--------|-----------|
| GET | `/agenda/propria` | Agenda | AgendaVisualizarPropria |
| POST | `/atendimentos/{itemId}/iniciar` | Agenda | AtendimentoIniciar |
| POST | `/atendimentos/{itemId}/finalizar` | Agenda | AtendimentoFinalizar |
| GET | `/servicos` | Servicos | ServicoVisualizar |
| GET | `/profissionais/horarios` | HorariosAtendimento | HorarioVisualizar |
| POST | `/profissionais/{id}/horarios` | HorariosAtendimento | HorarioGerenciarProprio* |
| PUT/PATCH | `/profissionais/horarios/{id}` | HorariosAtendimento | HorarioGerenciarProprio* |

\* Validacao de `HorarioGerenciarProprio` ocorre no service quando nao ha atributo no controller.

## Endpoints bloqueados

- `GET /agenda` (geral)
- Confirmar/cancelar/remarcar agendamentos de outros
- Equipe, convites, caixa
- CRUD de servicos da loja
- Horarios de funcionamento da loja (gestao)

## Escopo de dados

```text
Profissional ve:
  - apenas agendamentos atribuidos a ele (ou repassados)
  - clientes dos seus agendamentos
  - seus horarios e servicos vinculados
  - sua comissao (Premium + endpoint futuro)
```

Implementacao: `ProfissionalEscopoAcessoService` restringe filtros quando role e Profissional.

## Uniao com roles administrativas

Exemplo: **Admin + profissional vinculado**

```text
Permissoes efetivas = Admin UNION Profissional
  -> agenda geral + agenda propria
  -> caixa (se Premium) + atendimento proprio
```

## Plano do negocio

Profissional **nao contrata** plano — depende do plano do estabelecimento.

- Basic: atende, agenda propria, e-mail
- Plus: WhatsApp ao cliente
- Premium: comissao (quando endpoints existirem)

## Comparacao

| Capacidade | Profissional | Receptionist |
|------------|:------------:|:------------:|
| Agenda propria | sim | nao |
| Agenda geral | nao | sim |
| Iniciar atendimento | sim | nao |
| Confirmar agendamento | nao | sim |
| Caixa | nao | nao |

## Documentos relacionados

- [owner-admin.md](./owner-admin.md) — uniao de permissoes
- [../modulos.md](../modulos.md)

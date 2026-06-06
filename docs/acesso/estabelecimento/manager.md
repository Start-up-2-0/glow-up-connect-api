# Role no estabelecimento — Manager

`EstablishmentUserRole.Manager`

Gestor operacional: quase tudo exceto editar perfil do negocio e caixa (por padrao).

## Permissoes de negocio

| Permissao | Disponivel |
|-----------|:----------:|
| NegocioVisualizar | sim |
| NegocioEditar | **nao** |
| EquipeVisualizar | sim |
| EquipeGerenciar | **nao** |
| ProfissionalConvidar | sim |
| ProfissionalGerenciar | sim |
| ServicoVisualizar | sim |
| ServicoGerenciar | sim |
| HorarioVisualizar | sim |
| HorarioGerenciar | sim |
| AgendaVisualizarGeral | sim |
| AgendaCriar | sim |
| AgendaReagendar | sim |
| AgendaCancelar | sim |
| ClienteVisualizarGeral | sim |
| CaixaVisualizar | **nao** (padrao) |
| CaixaGerenciar | **nao** (padrao) |

## Caixa opcional

Se `managerPodeAcessarCaixa = true` na matriz (flag futura/config):

- CaixaVisualizar
- CaixaGerenciar

Hoje o padrao e **bloqueado**.

## Modulos x o que o Manager pode usar

| Area | Modulo | Manager pode? |
|------|--------|:-------------:|
| Servicos CRUD | Servicos | sim |
| Horarios loja | HorariosAtendimento | sim |
| Agenda geral | Agenda | sim |
| Convidar profissional | Profissionais | sim |
| Status profissional | Profissionais | sim |
| Cadastrar usuario equipe | Profissionais | **nao** (falta EquipeGerenciar) |
| Editar perfil loja | — | **nao** |
| Caixa | Caixa | **nao** (sem flag) |
| WhatsApp automatico | WhatsApp | sim (plano Plus+) |

## Endpoints permitidos (exemplos)

### Sim

- `GET/POST/PUT/PATCH .../servicos/*`
- `GET/POST/PUT/PATCH .../horarios-funcionamento/*`
- `GET .../agenda`, confirmar/cancelar/remarcar agendamentos
- `POST .../equipe/profissionais` (convidar)
- `PATCH .../equipe/profissionais/{id}/status`
- `POST .../convites/profissionais`

### Nao

- `PUT .../perfil`, WhatsApp config loja — NegocioEditar
- `POST/PATCH .../equipe/usuarios/*` — EquipeGerenciar
- `GET .../caixa*` — CaixaVisualizar (padrao)

## Se Manager tambem for profissional

Permissoes de [profissional.md](./profissional.md) sao **unidas** as do Manager.

## Plano minimo recomendado

- **Plus** — convidar e gerenciar profissionais exige modulo Profissionais

## Comparacao rapida

| Capacidade | Manager | Owner/Admin |
|------------|:-------:|:-----------:|
| Agenda geral | sim | sim |
| Servicos | sim | sim |
| Convidar profissional | sim | sim |
| Gerenciar usuarios equipe | nao | sim |
| Editar perfil negocio | nao | sim |
| Caixa | nao* | sim (Premium) |

\* exceto flag `managerPodeAcessarCaixa`

# Tasks - Servicos de estabelecimento e profissional autonomo

Este documento organiza as tasks relacionadas ao cadastro, gerenciamento e vinculacao de servicos oferecidos por estabelecimentos e profissionais autonomos.

> **Alinhamento com o codigo:** apos a migration `UnifyCommercialModelToTenant`, todo servico possui apenas `EstabelecimentoId`. Profissional autonomo opera via tenant (`Estabelecimento` sintetico criado na assinatura), no mesmo padrao de horarios e assinatura. Nao reintroduzir `ProfissionalAutonomoId` em `Servico`.

## Epic

Implementar o modulo de servicos do Glow Up Connect, permitindo que estabelecimentos e profissionais autonomos cadastrem servicos com nome, descricao, valor e tempo estimado, alem de controlar quais profissionais executam cada servico.

## Objetivo do fluxo

Permitir que uma operacao ativa cadastre seus servicos:

```text
Estabelecimento (loja ou tenant autonomo)
  -> cadastra servico vinculado ao EstabelecimentoId
  -> informa nome, descricao (opcional), valor e duracao
  -> vincula profissionais executores (loja) ou auto-vincula o unico profissional (autonomo)
  -> servico ativo fica disponivel para agenda e pagina publica
```

## Conceitos

### Servico de estabelecimento (loja)

Servico criado dentro de um estabelecimento com equipe.

Pode estar:

- sem profissional vinculado inicialmente;
- vinculado a um profissional especifico;
- vinculado a varios profissionais.

O servico pertence ao estabelecimento, mesmo quando executado por um ou mais profissionais.

### Servico de profissional autonomo

Servico criado pelo profissional autonomo, mas **persistido no tenant** (`EstabelecimentoId` do negocio autonomo).

- Nao possui coluna `ProfissionalAutonomoId`.
- Rotas expostas em `ProfissionaisAutonomosController` (`/api/profissionais-autonomos/{profissionalId}/servicos`).
- Service facade resolve `EstabelecimentoId` via `ProfissionalEstabelecimento`, no padrao de `HorarioProfissionalAutonomoService`.
- Na criacao, o sistema **auto-vincula** o unico profissional autonomo ao servico (`ProfissionalServico`), para a agenda funcionar.

### Vinculo profissional-servico (`ProfissionalServico`)

Usado em lojas com multiplos profissionais e no tenant autonomo.

- Permite sobrescrever `Preco` e `DuracaoMinutos` por profissional.
- Quando nao houver sobrescrita, usar `PrecoBase` e `DuracaoMinutos` do servico.

## Campos do servico

| Campo | Obrigatorio | Regra |
|---|---|---|
| `Nome` | Sim | Max. 150 caracteres |
| `Descricao` | Nao | Max. 500 caracteres |
| `PrecoBase` | Sim | >= 0 |
| `DuracaoMinutos` | Sim | > 0 e <= 480 |
| `Ativo` | Sim | Default `true` |
| `EstabelecimentoId` | Sim | Sempre preenchido |

## Decisoes de produto (fechadas)

- **`Descricao` e opcional** — alinhado ao Fluent API atual.
- **Limite do plano conta apenas servicos ativos** — inativos nao consomem cota.
- **Servico de loja sem profissional:** visivel na administracao; **nao** aparece na pagina publica/marketplace; agendamento bloqueado com mensagem clara.
- **Edicao de servico afeta apenas novos agendamentos** — `AgendamentoItem` ja armazena `Valor`, `Inicio` e `Fim` no momento da reserva.
- **Desvinculo profissional-servico:** soft delete (`ProfissionalServico.Ativo = false`); bloquear desvinculo se existir `AgendamentoItem` futuro confirmado para o par profissional/servico.
- **Marketplace publico:** exibir faixa de preco (`PrecoBase` a maximo por profissional) quando houver variacao; exibir duracao base ou estimada por profissional quando aplicavel.

## Regras principais

- Todo servico deve possuir `EstabelecimentoId`.
- Servico de loja pode existir sem profissional vinculado.
- Servico de autonomo deve possuir vinculo ativo com o profissional dono (criado automaticamente na criacao).
- Um servico de loja pode ser executado por varios profissionais.
- O vinculo entre profissional e servico pode sobrescrever preco e duracao.
- Servico inativo nao deve aparecer para novos agendamentos nem listagens publicas.
- Criacao e reativacao de servicos devem respeitar limites do plano/assinatura (`ModuloAssinatura.Servicos`).
- Endpoints privados exigem permissao `ServicoVisualizar` (leitura) ou `ServicoGerenciar` (escrita).

## Estado atual da implementacao

| Item | Status |
|---|---|
| Entidades `Servico` e `ProfissionalServico` | Implementado |
| Permissoes `ServicoVisualizar` / `ServicoGerenciar` | Implementado |
| Vincular profissional (`ProfissionalServicoNegocioService.VincularAsync`) | Implementado |
| CRUD de servico (criar, listar, editar, status) | Implementado |
| Facade autonomo (`ServicoProfissionalAutonomoService`) | Implementado |
| Desvincular profissional | Implementado |
| Validacao de limite do plano | Implementado |
| Duracao/preco efetivos na agenda | Implementado |
| Auditoria de servicos | Implementado |
| Listagem publica de servicos | Implementado |

## Ordem sugerida de implementacao

```text
Fase 1 — Core estabelecimento
  Task 3  Validacoes
  Task 1  Criar servico
  Task 4  Listar
  Task 6  Editar
  Task 7  Ativar/inativar
  Task 13 Limites do plano

Fase 2 — Autonomo
  Task 2  Criar (facade autonomo)
  Task 5  Listar (facade autonomo)

Fase 3 — Vinculos
  Task 8  Completar vinculo (editar preco/duracao)
  Task 9  Desvincular
  Task 11 Duracao/preco efetivos

Fase 4 — Integracoes
  Task 10 Servico sem profissional
  Task 14 Agenda
  Task 15 Marketplace
  Task 12 Testes de permissao
  Task 16 Auditoria
  Task 17 Testes integrados
```

## Task 1 - Criar cadastro de servico de estabelecimento

### Descricao

Criar caso de uso e endpoint para cadastrar servicos pertencentes a um estabelecimento.

### Referencia tecnica

- Service: `ServicoNegocioService`
- Controller: `EstabelecimentosController` — `POST /api/estabelecimentos/{estabelecimentoId}/servicos`
- Atributos: `[RequerModuloAssinatura(..., ModuloAssinatura.Servicos)]`, `[RequerPermissaoNegocio(ServicoGerenciar)]`

### Criterios de aceite

- Permitir criar servico informando nome, descricao (opcional), preco base e duracao estimada.
- Vincular o servico ao `EstabelecimentoId`.
- Gerar servico ativo por padrao.
- Validar assinatura ativa e limite de servicos ativos do plano (Task 13).
- Validar permissao do usuario no estabelecimento.
- Nao exigir profissional vinculado no momento da criacao.
- Retornar os dados do servico criado.
- Incluir testes de sucesso, dados invalidos, limite de plano e permissao negada.

## Task 2 - Criar cadastro de servico de profissional autonomo

### Descricao

Criar facade e endpoint para cadastrar servicos do tenant autonomo, delegando ao `ServicoNegocioService`.

### Referencia tecnica

- Facade: `ServicoProfissionalAutonomoService` (padrao de `HorarioProfissionalAutonomoService`)
- Controller: `ProfissionaisAutonomosController` — `POST /api/profissionais-autonomos/{profissionalId}/servicos`
- Resolver `EstabelecimentoId` via `ObterContextoAutonomoAsync` (profissional autonomo + usuario autenticado + vinculo tenant).

### Criterios de aceite

- Permitir criar servico informando nome, descricao (opcional), preco base e duracao estimada.
- Persistir servico com `EstabelecimentoId` do tenant autonomo (nao usar `ProfissionalAutonomoId`).
- Garantir que o profissional pertence ao usuario autenticado e e do tipo autonomo.
- Auto-vincular o profissional autonomo ao servico via `ProfissionalServico` ativo.
- Gerar servico ativo por padrao.
- Validar assinatura ativa e limite de servicos ativos do plano.
- Retornar os dados do servico criado.
- Incluir testes de sucesso, dados invalidos, limite de plano e acesso negado.

## Task 3 - Validacoes de campos do servico

### Descricao

Definir e implementar validacoes centralizadas para servicos e vinculos profissional-servico.

### Criterios de aceite

- `Nome` obrigatorio; tamanho maximo 150 caracteres.
- `Descricao` opcional; tamanho maximo 500 caracteres.
- `PrecoBase` >= 0.
- `DuracaoMinutos` > 0 e <= 480.
- Preco e duracao do vinculo (`ProfissionalServico`) seguem as mesmas regras de faixa.
- Retornar mensagens claras para dados invalidos.
- Incluir testes das validacoes.

## Task 4 - Listagem de servicos do estabelecimento

### Descricao

Criar listagem de servicos cadastrados em um estabelecimento, com filtros para administracao e uso em agenda.

### Referencia tecnica

- `GET /api/estabelecimentos/{estabelecimentoId}/servicos`
- Permissao: `ServicoVisualizar`

### Criterios de aceite

- Listar servicos do estabelecimento.
- Permitir filtro por status ativo/inativo.
- Permitir filtro por profissional executor, quando informado.
- Permitir busca por nome.
- Exibir profissionais vinculados ao servico, quando houver.
- Respeitar permissoes do usuario no estabelecimento.
- Incluir testes de filtros e permissao.

## Task 5 - Listagem de servicos do profissional autonomo

### Descricao

Criar listagem via facade autonomo, delegando ao `ServicoNegocioService` com `EstabelecimentoId` do tenant.

### Referencia tecnica

- `GET /api/profissionais-autonomos/{profissionalId}/servicos`

### Criterios de aceite

- Listar servicos do tenant autonomo do profissional.
- Permitir filtro por status ativo/inativo.
- Permitir busca por nome.
- Garantir que o usuario autenticado so acesse servicos do proprio profissional autonomo.
- Incluir testes de filtros e acesso.

## Task 6 - Edicao de servico

### Descricao

Criar fluxo para editar dados principais de um servico.

### Referencia tecnica

- Estabelecimento: `PUT /api/estabelecimentos/{estabelecimentoId}/servicos/{servicoId}`
- Autonomo: `PUT /api/profissionais-autonomos/{profissionalId}/servicos/{servicoId}`

### Criterios de aceite

- Permitir alterar nome, descricao, preco base e duracao estimada.
- Validar permissao do usuario.
- Validar que o servico pertence ao `EstabelecimentoId` correto.
- Alteracoes afetam **apenas novos agendamentos**; historico permanece intacto via snapshot em `AgendamentoItem`.
- Incluir testes de edicao valida, acesso negado e servico inexistente.

## Task 7 - Ativacao e inativacao de servico

### Descricao

Criar fluxo para ativar e inativar servicos sem remover historico.

### Referencia tecnica

- `PATCH /api/estabelecimentos/{estabelecimentoId}/servicos/{servicoId}/status`
- Equivalente autonomo em `ProfissionaisAutonomosController`

### Criterios de aceite

- Permitir inativar servico.
- Servico inativo nao deve aparecer para novos agendamentos nem listagens publicas.
- Servico inativo deve continuar visivel em agendamentos historicos.
- Permitir reativar servico respeitando limite de servicos ativos do plano.
- Validar permissao do usuario.
- Incluir testes de ativacao, inativacao e impacto na agenda.

## Task 8 - Vincular servico de estabelecimento a profissionais

### Descricao

Associar um servico de estabelecimento a um ou mais profissionais que podem executa-lo.

### Estado atual

**Parcialmente implementado:** `ProfissionalServicoNegocioService.VincularAsync` e `POST /api/estabelecimentos/{estabelecimentoId}/servicos/{servicoId}/profissionais/{profissionalId}`.

### Pendencias desta task

- Endpoint para editar preco/duracao de vinculo existente.
- Endpoint para listar vinculos de um servico (opcional, pode compor Task 4).

### Criterios de aceite

- Permitir vincular um profissional ao servico.
- Permitir vincular varios profissionais ao mesmo servico.
- Validar que o profissional pertence ao estabelecimento.
- Validar que o servico pertence ao estabelecimento.
- Impedir vinculo duplicado ativo.
- Permitir informar preco e duracao especificos por profissional na criacao do vinculo.
- Permitir atualizar preco e duracao de vinculo existente.
- Reativar vinculo inativo existente em vez de duplicar registro.
- Incluir testes de vinculo simples, multiplo, duplicidade, edicao e profissional de outro estabelecimento.

## Task 9 - Desvincular profissional de servico

### Descricao

Inativar o vinculo entre profissional e servico.

### Referencia tecnica

- `DELETE` ou `PATCH .../status` em `/api/estabelecimentos/{estabelecimentoId}/servicos/{servicoId}/profissionais/{profissionalId}`

### Criterios de aceite

- Desvincular via `ProfissionalServico.Ativo = false` (soft delete).
- Impedir novos agendamentos para aquele profissional/servico apos desvinculo.
- Preservar historico de agendamentos ja criados.
- **Bloquear desvinculo** se existir `AgendamentoItem` futuro confirmado para o par profissional/servico.
- Validar permissao do usuario.
- Incluir testes de desvinculo, bloqueio por agendamento futuro e impacto em novos agendamentos.

## Task 10 - Servico de estabelecimento sem profissional vinculado

### Descricao

Comportamento para servicos de loja que ainda nao possuem profissional executor vinculado.

### Comportamento definido

- Cadastro permitido sem profissional.
- Visivel na administracao do estabelecimento (Task 4).
- **Nao** exibido na pagina publica/marketplace (Task 15).
- Agendamento bloqueado; disponibilidade retorna zero slots ou erro claro.
- Mensagem: servico indisponivel por falta de profissional executor.

### Criterios de aceite

- Permitir cadastrar servico sem profissional.
- Exibir servico na administracao do estabelecimento.
- Nao exibir servico sem profissional na listagem publica.
- Bloquear agendamento quando nao houver profissional disponivel.
- Retornar mensagem clara quando o servico nao puder ser agendado.
- Incluir testes do comportamento definido.

## Task 11 - Preco e duracao por profissional

### Descricao

Garantir preco e duracao efetivos por profissional em vinculos, agenda e pagamento.

### Criterios de aceite

- Permitir configurar preco especifico por profissional (criacao e edicao do vinculo).
- Permitir configurar duracao especifica por profissional (criacao e edicao do vinculo).
- Quando nao houver sobrescrita, usar `PrecoBase` e `DuracaoMinutos` do servico.
- `DisponibilidadeAgendaService` deve usar duracao efetiva do profissional ao gerar slots.
- Fluxo de agendamento/pagamento deve usar preco efetivo do profissional.
- Incluir testes de fallback e sobrescrita.

## Task 12 - Permissoes para gerenciar servicos

### Descricao

Aplicar permissoes nos endpoints de servicos.

### Estado atual

Matriz ja implementada em `MatrizPermissaoNegocioService`:

- `Owner`, `Admin`, `Manager`: `ServicoVisualizar` + `ServicoGerenciar`
- `Receptionist`: apenas `ServicoVisualizar`
- `Profissional` da loja: apenas `ServicoVisualizar`
- Profissional autonomo: `Owner` no tenant autonomo (via `EstabelecimentoUsuario`)

### Criterios de aceite

- Endpoints de leitura exigem `ServicoVisualizar`.
- Endpoints de escrita exigem `ServicoGerenciar`.
- Profissional autonomo gerencia servicos do proprio tenant.
- Usuario sem vinculo nao acessa servicos privados.
- Incluir testes por perfil nos controllers.

## Task 13 - Integracao de servicos com assinatura e limites

### Descricao

Integrar criacao e reativacao de servicos com os limites do plano contratado.

### Regra definida

- Contar **apenas servicos ativos** do `EstabelecimentoId`.
- `LimiteServicos` nulo no plano = sem limite.

### Criterios de aceite

- Validar assinatura ativa e modulo `Servicos` antes de criar servico.
- Validar limite de servicos ativos do plano na criacao e reativacao.
- Bloquear operacao quando limite for atingido.
- Servicos inativos nao contam no limite.
- Retornar erro claro quando plano nao permitir mais servicos.
- Incluir testes com plano limitado e plano sem limite.

## Task 14 - Integracao de servicos com agenda

### Descricao

Garantir que a agenda use apenas servicos ativos e executaveis pelo profissional selecionado.

### Estado atual

`DisponibilidadeAgendaService` ja valida servico ativo, vinculo `ProfissionalServico` e horarios. **Pendente:** usar duracao efetiva por profissional (Task 11).

### Criterios de aceite

- Agendamento de loja valida que o servico pertence ao estabelecimento.
- Agendamento de loja valida que o profissional executa o servico, quando profissional for informado.
- Agendamento de autonomo valida servico do tenant e vinculo com o profissional autonomo.
- Servico inativo nao pode ser agendado.
- Duracao efetiva do profissional deve calcular inicio/fim dos slots e itens.
- Preco efetivo deve ser usado no pagamento.
- Incluir testes de validacao da agenda.

## Task 15 - Integracao de servicos com marketplace

### Descricao

Expor servicos publicamente para clientes na pagina de agendamento.

### Referencia tecnica

- Endpoints publicos em `AgendamentoPublicoController` ou controller dedicado.
- Apenas servicos ativos; sem dados administrativos.

### Comportamento definido

- Loja: exibir servico **somente se** possuir ao menos um profissional executor ativo.
- Autonomo: exibir servicos ativos do tenant com vinculo ativo.
- Preco: exibir valor unico ou faixa (`PrecoBase` .. maximo por profissional).
- Duracao: exibir base ou estimada quando houver variacao por profissional.

### Criterios de aceite

- Exibir apenas servicos ativos elegiveis para agendamento.
- Nao exibir servicos de loja sem profissional vinculado.
- Exibir preco base ou faixa de preco quando houver variacao por profissional.
- Exibir duracao base ou duracao estimada.
- Nao expor dados administrativos.
- Incluir testes de retorno publico.

## Task 16 - Auditoria de alteracoes de servico

### Descricao

Registrar alteracoes sensiveis nos servicos para rastreabilidade operacional.

### Referencia tecnica

- Padrao de `HorarioFuncionamentoNegocioService` + `IAuditoriaNegocioService`
- Novos valores em `TipoAcaoAuditoriaNegocio` (ex.: `ServicoCriado`, `ServicoAlterado`, `ServicoStatusAlterado`, `ProfissionalServicoVinculado`, `ProfissionalServicoDesvinculado`)

### Criterios de aceite

- Auditar criacao de servico.
- Auditar edicao de preco e duracao base.
- Auditar ativacao/inativacao.
- Auditar vinculo, edicao e desvinculo de profissionais.
- Registrar usuario executor e estabelecimento afetado.
- Nao registrar dados sensiveis desnecessarios.
- Incluir testes de auditoria.

## Task 17 - Testes integrados do modulo de servicos

### Descricao

Criar testes integrados cobrindo o fluxo completo de servicos para estabelecimento e profissional autonomo.

### Criterios de aceite

- Testar criacao de servico de estabelecimento sem profissional.
- Testar criacao de servico de estabelecimento com um profissional vinculado.
- Testar criacao de servico de estabelecimento com varios profissionais.
- Testar criacao de servico autonomo com auto-vinculo.
- Testar permissao negada para recepcionista e profissional da loja.
- Testar limite de plano (apenas ativos).
- Testar servico inativo bloqueado para agendamento.
- Testar servico de loja sem profissional oculto no publico.
- Testar preco/duracao especificos por profissional na agenda.
- Testar bloqueio de desvinculo com agendamento futuro confirmado.

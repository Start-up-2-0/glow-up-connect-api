# Tasks - Modulo de Agendamento

Este documento organiza as tasks do fluxo completo de agendamento no backend GLOWAPI, cobrindo visitante por link publico, cliente logado, validacao de disponibilidade, gestao de status, historico, auditoria e notificacoes.

## Epic

Implementar o modulo de agendamento do Glow Up Connect, permitindo reservas publicas e autenticadas com validacao server-side de servicos, horarios, conflitos e totais financeiros.

## Objetivo do fluxo

```text
Cliente (visitante ou logado)
  -> seleciona estabelecimento/profissional/servicos/data/horario
  -> backend valida vinculos, horarios e conflitos
  -> backend calcula duracao e valor total
  -> agendamento nasce como PendenteConfirmacao
  -> estabelecimento confirma, cancela ou remarca
  -> sistema registra historico, auditoria e notifica WhatsApp
```

## Decisoes de produto

- Pagamento e presencial; sem gateway neste epic.
- Status inicial: `PendenteConfirmacao`.
- Multiplos servicos geram itens sequenciais no mesmo profissional.
- Visitante informa nome, e-mail e telefone; logado usa dados da sessao.
- Links publicos usam `PublicGuid` de estabelecimento e profissional.

## Task 1 - Modelo de agendamento para visitante

### Descricao

Estender entidade `Agendamento` para suportar visitantes sem conta.

### Criterios de aceite

- `UsuarioClienteId` nullable.
- Campos `ClienteNome`, `ClienteEmail`, `ClienteTelefone`.
- Enum `OrigemAgendamento`.
- Status `PendenteConfirmacao`, `Remarcado`, `NaoCompareceu`.
- Migration aplicavel.

## Task 2 - Historico de agendamento

### Descricao

Criar entidade `AgendamentoHistorico` para rastrear transicoes de status.

### Criterios de aceite

- Registrar status anterior, novo status, executor, motivo e payload.
- Listagem por agendamento via endpoint autenticado do estabelecimento.

## Task 3 - Validacao central de agendamento

### Descricao

Implementar `AgendamentoValidador` com todas as regras de negocio.

### Criterios de aceite

- Validar estabelecimento, profissional, servicos e vinculos.
- Validar horario dentro de funcionamento e atendimento.
- Validar conflito de ocupacao.
- Calcular duracao e valor no servidor.
- Exigir contato do visitante; ignorar contato enviado pelo logado.

## Task 4 - Disponibilidade multi-servico e profissional vinculado

### Descricao

Estender consulta publica de disponibilidade e servicos.

### Criterios de aceite

- Aceitar `servicoIds` na consulta de slots.
- Duracao total = soma das duracoes efetivas.
- Link publico do profissional funciona para autonomo e vinculado.
- Servicos publicos filtrados por profissional executor.

## Task 5 - Criacao de agendamento publico e logado

### Descricao

Implementar `AgendamentoNegocioService` e endpoints de criacao.

### Endpoints

- `POST /api/publico/agendar/loja/{publicGuid}`
- `POST /api/publico/agendar/profissional/{publicGuid}`
- `POST /api/agendamentos`
- `GET /api/agendamentos/me`

### Criterios de aceite

- Retornar 201 com totais calculados pelo backend.
- Proteger contra dupla reserva (409).
- Persistir historico e auditoria de criacao.

## Task 6 - Acoes do estabelecimento

### Descricao

Permitir confirmar, cancelar, remarcar e marcar nao compareceu.

### Endpoints

- `POST /api/estabelecimentos/{id}/agendamentos/{agendamentoId}/confirmar`
- `POST /api/estabelecimentos/{id}/agendamentos/{agendamentoId}/cancelar`
- `POST /api/estabelecimentos/{id}/agendamentos/{agendamentoId}/remarcar`
- `PATCH /api/estabelecimentos/{id}/agendamentos/{agendamentoId}/nao-compareceu`
- `GET /api/estabelecimentos/{id}/agendamentos/{agendamentoId}/historico`

### Criterios de aceite

- Cancelar/remarcar exigem motivo.
- Revalidar disponibilidade na remarcacao.
- Registrar historico e auditoria em cada acao.

## Task 7 - Notificacoes WhatsApp

### Descricao

Enfileirar notificacoes para estabelecimento e profissional.

### Criterios de aceite

- Canal inicial: WhatsApp.
- Eventos: criado, confirmado, cancelado, remarcado.
- Payload enxuto para LGPD.

## Task 8 - Testes

### Descricao

Cobrir fluxos principais com testes unitarios e integrados.

### Criterios de aceite

- Criacao publica com sucesso.
- Visitante sem telefone retorna 400.
- Conflito de horario retorna 409.
- Confirmar/cancelar com permissao.
- Notificacao WhatsApp enfileirada.

## Fora de escopo

- Pagamento online.
- Criacao automatica de conta para visitante.
- Push/SMS/e-mail para cliente.
- Multiplos profissionais no mesmo agendamento.

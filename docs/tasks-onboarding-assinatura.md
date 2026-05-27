# Tasks - Onboarding comercial, assinatura e liberacao de modulos

Este documento organiza as tasks relacionadas ao fluxo em que um usuario autenticado contrata um plano, escolhe operar como estabelecimento ou profissional autonomo, realiza o pagamento e tem os modulos liberados conforme a assinatura.

## Epic

Implementar o onboarding comercial do Glow Up Connect com criacao da operacao, assinatura, pagamento e liberacao de modulos.

## Objetivo do fluxo

Permitir que um usuario saia de uma conta comum/autenticada para uma operacao comercial ativa na plataforma:

```text
Usuario autenticado
  -> escolhe plano
  -> escolhe tipo de operacao
  -> cria estabelecimento ou profissional autonomo
  -> inicia assinatura
  -> realiza pagamento
  -> webhook confirma pagamento
  -> assinatura fica ativa
  -> modulos do plano sao liberados
```

Tipos de operacao:

- `Estabelecimento`: cria estabelecimento e vincula o usuario como dono/responsavel.
- `ProfissionalAutonomo`: cria ou ativa o perfil profissional autonomo do usuario.

## Premissas

- A assinatura pode pertencer a um estabelecimento ou a um profissional autonomo.
- Uma assinatura nao deve pertencer aos dois tipos ao mesmo tempo.
- A assinatura inicia como `PendentePagamento`.
- O pagamento inicial deve estar vinculado a assinatura.
- A assinatura so deve liberar modulos apos confirmacao do pagamento.
- Webhooks de pagamento devem ser idempotentes.
- A regra de liberacao de modulos deve considerar plano, status da assinatura e limites contratados.

## Ordem sugerida

1. Listagem de planos.
2. Inicio de assinatura.
3. Onboarding de estabelecimento.
4. Onboarding de profissional autonomo.
5. Criacao do pagamento da assinatura.
6. Abstracao de gateway de pagamento.
7. Webhook de pagamento.
8. Confirmacao de pagamento da assinatura.
9. Liberacao de modulos por plano.
10. Protecao de endpoints por assinatura/modulo.
11. Troca de plano.
12. Cancelamento ou suspensao.
13. Notificacoes transacionais.
14. Testes integrados do fluxo completo.

## Task 1 - Listagem de planos disponiveis para contratacao

### Descricao

Criar endpoint para listar planos ativos disponiveis para contratacao por estabelecimento ou profissional autonomo. A listagem deve retornar preco, periodo, limites e recursos liberados por plano.

### Criterios de aceite

- Retornar apenas planos ativos.
- Exibir limites do plano: profissionais, servicos, agendamentos e modulos.
- Nao expor planos inativos ou administrativos.
- Incluir testes da listagem.

## Task 2 - Inicio da assinatura

### Descricao

Criar caso de uso para iniciar uma assinatura a partir de um usuario autenticado, plano escolhido e tipo de operacao: `Estabelecimento` ou `ProfissionalAutonomo`.

### Criterios de aceite

- Receber `PlanoId` e `TipoAssinatura`.
- Validar se o plano existe e esta ativo.
- Validar se o usuario pode iniciar esse tipo de assinatura.
- Criar assinatura com status `PendentePagamento`.
- Impedir assinatura duplicada ativa para a mesma operacao.
- Incluir testes de sucesso e erro.

## Task 3 - Onboarding de estabelecimento

### Descricao

Criar o fluxo de criacao de estabelecimento durante a contratacao de assinatura. Quando o usuario escolher operar como estabelecimento, o sistema deve criar o estabelecimento e vincular o usuario como dono/responsavel.

### Criterios de aceite

- Criar estabelecimento com dados basicos.
- Gerar `PublicGuid`.
- Vincular usuario em `EstabelecimentoUsuario`.
- Definir role interna como `Owner`.
- Criar caixa inicial do estabelecimento, se aplicavel.
- Vincular assinatura ao `EstabelecimentoId`.
- Incluir testes do vinculo entre usuario, estabelecimento e assinatura.

## Task 4 - Onboarding de profissional autonomo

### Descricao

Criar o fluxo de criacao ou ativacao do perfil de profissional autonomo durante a contratacao de assinatura.

### Criterios de aceite

- Criar perfil profissional vinculado ao usuario.
- Definir tipo como `Autonomo`.
- Gerar `PublicGuid`.
- Criar caixa inicial do profissional, se aplicavel.
- Vincular assinatura ao `ProfissionalAutonomoId`.
- Impedir criacao duplicada de profissional autonomo para o mesmo usuario.
- Incluir testes do vinculo entre usuario, profissional e assinatura.

## Task 5 - Criacao do pagamento da assinatura

### Descricao

Criar o servico responsavel por gerar o pagamento inicial da assinatura no gateway configurado.

### Criterios de aceite

- Criar registro de pagamento vinculado a assinatura.
- Status inicial deve ser `Pendente`.
- Enviar dados necessarios ao gateway.
- Armazenar identificador externo do pagamento.
- Retornar dados de checkout, link, QR Code ou payload equivalente.
- Tratar falha do gateway sem ativar assinatura.
- Incluir testes com gateway mockado.

## Task 6 - Abstracao de gateway de pagamento

### Descricao

Criar uma interface de integracao para gateways de pagamento, permitindo implementacao futura para Mercado Pago, AbacatePay ou outro provedor.

### Criterios de aceite

- Criar interface na camada Application.
- Implementar provider inicial na Infrastructure.
- Nao acoplar regra de negocio diretamente ao SDK/API do gateway.
- Retornar resultado padronizado de cobranca.
- Registrar request/response sem dados sensiveis.
- Incluir testes unitarios da camada de aplicacao com mock.

## Task 7 - Webhook de pagamento

### Descricao

Criar endpoint para receber eventos do gateway de pagamento e registrar os webhooks recebidos antes do processamento.

### Criterios de aceite

- Registrar todo webhook em `WebhookPagamento`.
- Garantir idempotencia por `EventId` ou identificador externo.
- Validar assinatura/autenticidade do gateway, se disponivel.
- Processar eventos de pagamento aprovado, recusado, cancelado e expirado.
- Nao processar evento duplicado duas vezes.
- Incluir testes de idempotencia.

## Task 8 - Confirmacao de pagamento da assinatura

### Descricao

Processar webhook de pagamento aprovado e atualizar o pagamento e a assinatura vinculada.

### Criterios de aceite

- Atualizar pagamento para `Pago`.
- Preencher `PagoEm`.
- Atualizar assinatura para `Ativa`.
- Definir inicio/fim da assinatura conforme periodo do plano.
- Atualizar `UltimoPagamentoId`.
- Nao ativar assinatura se pagamento estiver recusado, cancelado ou expirado.
- Incluir testes do fluxo de ativacao.

## Task 9 - Liberacao de modulos por plano

### Descricao

Criar servico para verificar quais modulos e limites estao disponiveis para uma assinatura ativa.

### Criterios de aceite

- Validar se existe assinatura ativa.
- Retornar modulos liberados conforme plano contratado.
- Aplicar limites de profissionais, servicos e agendamentos.
- Bloquear modulos quando assinatura estiver pendente, suspensa, expirada ou cancelada.
- Criar estrutura reutilizavel para middleware, filtros ou services.
- Incluir testes de permissao por plano/status.

## Task 10 - Middleware ou policy de acesso por assinatura

### Descricao

Criar mecanismo para proteger endpoints que dependem de assinatura ativa e modulo liberado.

### Criterios de aceite

- Bloquear acesso sem assinatura ativa.
- Bloquear acesso a modulo nao incluso no plano.
- Retornar erro claro para assinatura pendente ou expirada.
- Permitir uso em endpoints de estabelecimento e profissional autonomo.
- Incluir testes de autorizacao.

## Task 11 - Troca de plano

### Descricao

Criar fluxo para trocar o plano de uma assinatura existente de estabelecimento ou profissional autonomo.

### Criterios de aceite

- Validar assinatura existente.
- Validar novo plano ativo.
- Criar cobranca quando houver diferenca de valor ou nova recorrencia.
- Atualizar assinatura apos confirmacao do pagamento.
- Manter historico via pagamentos.
- Incluir testes para troca valida e invalida.

## Task 12 - Cancelamento ou suspensao da assinatura

### Descricao

Criar fluxo para cancelar ou suspender assinatura conforme solicitacao do usuario ou evento recebido do gateway.

### Criterios de aceite

- Permitir cancelamento de assinatura ativa.
- Atualizar status para `Cancelada` ou `Suspensa`.
- Registrar `CanceladoEm`, quando aplicavel.
- Bloquear modulos apos cancelamento/suspensao.
- Processar evento de cancelamento vindo do gateway.
- Incluir testes do bloqueio apos cancelamento.

## Task 13 - Notificacoes transacionais da assinatura

### Descricao

Criar notificacoes assincronas para eventos importantes da assinatura, usando a mensageria ja existente.

### Criterios de aceite

- Enfileirar e-mail de assinatura iniciada.
- Enfileirar e-mail de pagamento confirmado.
- Enfileirar e-mail de pagamento recusado.
- Enfileirar e-mail de assinatura cancelada ou suspensa.
- Usar `IMensagemNotificacaoService`.
- Nao chamar provedor de e-mail diretamente.
- Incluir testes validando o enfileiramento.

## Task 14 - Testes integrados do fluxo completo

### Descricao

Criar testes cobrindo o fluxo ponta a ponta de contratacao, pagamento e liberacao dos modulos.

### Criterios de aceite

- Testar assinatura de estabelecimento.
- Testar assinatura de profissional autonomo.
- Testar pagamento pendente.
- Testar webhook de pagamento aprovado.
- Testar liberacao de modulos apos assinatura ativa.
- Testar bloqueio antes da confirmacao do pagamento.
- Testar idempotencia do webhook.


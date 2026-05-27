# PRs - Onboarding comercial, assinatura e liberacao de modulos

Este documento quebra as tasks de `docs/tasks-onboarding-assinatura.md` em PRs menores e revisaveis.

## Estrategia

- Cada PR deve entregar uma fatia validavel do fluxo.
- PRs devem seguir a ordem abaixo sempre que houver dependencia.
- Evitar misturar regra de assinatura, pagamento, webhook e autorizacao no mesmo PR.
- Cada PR deve incluir testes proporcionais ao risco da alteracao.
- Controllers devem permanecer finos; regra de negocio deve ficar na camada Application.

## PR 01 - Listagem de planos disponiveis

### Base

Task 1 - Listagem de planos disponiveis para contratacao.

### Titulo sugerido

`feat: listar planos ativos para contratacao`

### Branch sugerida

`feat/listar-planos-contratacao`

### Escopo

- Criar endpoint `GET /api/planos`.
- Criar caso de uso/service para listar planos ativos.
- Retornar preco, periodo, limites e modulos do plano.
- Filtrar planos inativos.

### Fora de escopo

- Criacao/edicao administrativa de planos.
- Contratacao de assinatura.
- Pagamento.

### Checklist

- [ ] Endpoint retorna apenas planos ativos.
- [ ] Response inclui limites de profissionais, servicos, agendamentos e modulos.
- [ ] Planos inativos nao aparecem.
- [ ] Testes cobrem listagem e filtro de ativos.

## PR 02 - Inicio de assinatura pendente

### Base

Task 2 - Inicio da assinatura.

### Titulo sugerido

`feat: iniciar assinatura com status pendente`

### Branch sugerida

`feat/iniciar-assinatura-pendente`

### Escopo

- Criar endpoint para iniciar assinatura.
- Receber `PlanoId` e `TipoAssinatura`.
- Validar plano ativo.
- Criar assinatura com status `PendentePagamento`.
- Validar duplicidade de assinatura ativa/pendente para a mesma operacao.

### Fora de escopo

- Criacao de pagamento no gateway.
- Webhook.
- Ativacao da assinatura.
- Criacao completa de estabelecimento/profissional autonomo, salvo stub minimo se necessario.

### Checklist

- [ ] Plano inexistente ou inativo retorna erro.
- [ ] Assinatura nasce como `PendentePagamento`.
- [ ] Assinatura nao pertence simultaneamente a estabelecimento e profissional autonomo.
- [ ] Duplicidade de assinatura e bloqueada.
- [ ] Testes de sucesso e erro foram adicionados.

## PR 03 - Onboarding de estabelecimento na assinatura

### Base

Task 3 - Onboarding de estabelecimento.

### Titulo sugerido

`feat: criar estabelecimento durante contratacao`

### Branch sugerida

`feat/onboarding-estabelecimento-assinatura`

### Escopo

- Receber dados iniciais do estabelecimento no fluxo de assinatura.
- Criar estabelecimento.
- Gerar `PublicGuid`.
- Vincular usuario como `Owner` em `EstabelecimentoUsuario`.
- Vincular assinatura ao `EstabelecimentoId`.
- Criar caixa inicial se ja houver entidade/regra pronta para isso.

### Fora de escopo

- Profissionais do estabelecimento.
- Servicos.
- Horarios.
- Pagamento real no gateway.

### Checklist

- [ ] Estabelecimento e criado com dados basicos.
- [ ] Usuario autenticado vira `Owner`.
- [ ] Assinatura fica vinculada ao estabelecimento.
- [ ] Criacao duplicada indevida e bloqueada.
- [ ] Testes cobrem vinculo usuario/estabelecimento/assinatura.

## PR 04 - Onboarding de profissional autonomo na assinatura

### Base

Task 4 - Onboarding de profissional autonomo.

### Titulo sugerido

`feat: criar profissional autonomo durante contratacao`

### Branch sugerida

`feat/onboarding-profissional-autonomo-assinatura`

### Escopo

- Receber dados iniciais do profissional autonomo no fluxo de assinatura.
- Criar ou ativar perfil profissional autonomo do usuario.
- Gerar `PublicGuid`.
- Vincular assinatura ao `ProfissionalAutonomoId`.
- Criar caixa inicial se ja houver entidade/regra pronta para isso.

### Fora de escopo

- Servicos do autonomo.
- Horarios do autonomo.
- Pagamento real no gateway.

### Checklist

- [ ] Profissional autonomo e criado ou ativado.
- [ ] Usuario autenticado e dono do perfil.
- [ ] Assinatura fica vinculada ao profissional autonomo.
- [ ] Duplicidade de profissional autonomo e bloqueada.
- [ ] Testes cobrem vinculo usuario/profissional/assinatura.

## PR 05 - Abstracao de gateway de pagamento

### Base

Task 6 - Abstracao de gateway de pagamento.

### Titulo sugerido

`feat: adicionar abstracao de gateway de pagamento`

### Branch sugerida

`feat/abstracao-gateway-pagamento`

### Escopo

- Criar interface de gateway na camada Application.
- Criar modelos de request/response de cobranca.
- Criar provider inicial fake/stub ou provider configuravel na Infrastructure.
- Padronizar erros de gateway.
- Evitar acoplamento de regra de negocio com SDK externo.

### Fora de escopo

- Integracao real com Mercado Pago ou AbacatePay, se ainda nao houver credenciais/decisao.
- Webhook.
- Ativacao de assinatura.

### Checklist

- [ ] Interface fica em Application.
- [ ] Implementacao tecnica fica em Infrastructure.
- [ ] Resultado de cobranca e padronizado.
- [ ] Request/response nao registram dados sensiveis.
- [ ] Testes usam mock da interface.

## PR 06 - Criacao do pagamento inicial da assinatura

### Base

Task 5 - Criacao do pagamento da assinatura.

### Titulo sugerido

`feat: criar pagamento inicial de assinatura`

### Branch sugerida

`feat/pagamento-inicial-assinatura`

### Escopo

- Criar pagamento vinculado a assinatura.
- Definir status inicial `Pendente`.
- Chamar gateway pela abstracao.
- Armazenar identificador externo do pagamento.
- Retornar dados de checkout/link/QR Code conforme resposta do gateway.

### Fora de escopo

- Confirmacao por webhook.
- Ativacao da assinatura.
- Lancamentos de caixa.

### Checklist

- [ ] Pagamento e criado vinculado a assinatura.
- [ ] Status inicial e `Pendente`.
- [ ] Falha do gateway nao ativa assinatura.
- [ ] Identificador externo e persistido.
- [ ] Testes cobrem sucesso e falha do gateway.

## PR 07 - Registro de webhooks de pagamento

### Base

Task 7 - Webhook de pagamento.

### Titulo sugerido

`feat: registrar webhooks de pagamento`

### Branch sugerida

`feat/registrar-webhooks-pagamento`

### Escopo

- Criar endpoint de webhook.
- Persistir payload recebido em `WebhookPagamento`.
- Garantir idempotencia por identificador do evento.
- Validar autenticidade do gateway quando houver mecanismo disponivel.

### Fora de escopo

- Ativar assinatura.
- Gerar lancamentos financeiros.
- Enviar notificacoes.

### Checklist

- [ ] Todo webhook e registrado antes do processamento.
- [ ] Evento duplicado nao e processado novamente.
- [ ] Payload e armazenado com seguranca.
- [ ] Erros de processamento ficam rastreaveis.
- [ ] Testes cobrem idempotencia.

## PR 08 - Confirmacao de pagamento e ativacao da assinatura

### Base

Task 8 - Confirmacao de pagamento da assinatura.

### Titulo sugerido

`feat: ativar assinatura apos pagamento aprovado`

### Branch sugerida

`feat/ativar-assinatura-pagamento-aprovado`

### Escopo

- Processar evento de pagamento aprovado.
- Atualizar pagamento para `Pago`.
- Preencher `PagoEm`.
- Atualizar assinatura para `Ativa`.
- Definir inicio/fim conforme periodo do plano.
- Atualizar `UltimoPagamentoId`.

### Fora de escopo

- Troca de plano.
- Cancelamento.
- Notificacoes.

### Checklist

- [ ] Pagamento aprovado ativa assinatura.
- [ ] Pagamento recusado/cancelado/expirado nao ativa assinatura.
- [ ] Inicio e fim da assinatura sao calculados corretamente.
- [ ] Processamento e idempotente.
- [ ] Testes cobrem ativacao e eventos nao aprovados.

## PR 09 - Servico de liberacao de modulos por plano

### Base

Task 9 - Liberacao de modulos por plano.

### Titulo sugerido

`feat: verificar modulos liberados por assinatura`

### Branch sugerida

`feat/modulos-liberados-assinatura`

### Escopo

- Criar service de verificacao de assinatura ativa.
- Retornar modulos liberados conforme plano.
- Aplicar limites do plano.
- Bloquear modulos quando assinatura nao estiver ativa.

### Fora de escopo

- Middleware/policy em endpoints.
- Alterar fluxos de servicos, agenda ou caixa.

### Checklist

- [ ] Assinatura ativa libera modulos do plano.
- [ ] Assinatura pendente/suspensa/expirada/cancelada bloqueia modulos.
- [ ] Limites sao retornados ou validados.
- [ ] Testes cobrem plano/status/limites.

## PR 10 - Protecao de endpoints por assinatura e modulo

### Base

Task 10 - Middleware ou policy de acesso por assinatura.

### Titulo sugerido

`feat: proteger endpoints por assinatura e modulo`

### Branch sugerida

`feat/proteger-endpoints-assinatura-modulo`

### Escopo

- Criar middleware, filtro ou atributo de permissao por modulo.
- Validar assinatura ativa.
- Validar modulo liberado.
- Retornar erro padronizado quando acesso for bloqueado.

### Fora de escopo

- Regras internas de equipe do estabelecimento.
- Criacao de novos modulos.

### Checklist

- [ ] Endpoint protegido exige assinatura ativa.
- [ ] Modulo nao contratado retorna bloqueio.
- [ ] Resposta segue envelope de erro do projeto.
- [ ] Testes cobrem acesso permitido e negado.

## PR 11 - Troca de plano

### Base

Task 11 - Troca de plano.

### Titulo sugerido

`feat: solicitar troca de plano da assinatura`

### Branch sugerida

`feat/troca-plano-assinatura`

### Escopo

- Criar endpoint/caso de uso para troca de plano.
- Validar assinatura existente.
- Validar novo plano ativo.
- Criar cobranca quando necessario.
- Atualizar plano apos confirmacao de pagamento ou regra definida.

### Fora de escopo

- Prorata sofisticada, salvo se for definida como regra do produto.
- Interface administrativa de planos.

### Checklist

- [ ] Novo plano precisa estar ativo.
- [ ] Assinatura existente precisa ser valida.
- [ ] Historico fica rastreavel por pagamento.
- [ ] Troca invalida e bloqueada.
- [ ] Testes cobrem troca valida e invalida.

## PR 12 - Cancelamento e suspensao de assinatura

### Base

Task 12 - Cancelamento ou suspensao da assinatura.

### Titulo sugerido

`feat: cancelar e suspender assinatura`

### Branch sugerida

`feat/cancelar-suspender-assinatura`

### Escopo

- Criar fluxo de cancelamento pelo usuario.
- Processar evento de cancelamento/suspensao do gateway.
- Atualizar status da assinatura.
- Registrar `CanceladoEm` quando aplicavel.
- Bloquear modulos apos cancelamento/suspensao.

### Fora de escopo

- Reembolso.
- Chargeback.
- Reativacao automatica.

### Checklist

- [ ] Assinatura ativa pode ser cancelada.
- [ ] Assinatura suspensa/cancelada bloqueia modulos.
- [ ] Evento do gateway atualiza status.
- [ ] Testes cobrem cancelamento e bloqueio.

## PR 13 - Notificacoes transacionais da assinatura

### Base

Task 13 - Notificacoes transacionais da assinatura.

### Titulo sugerido

`feat: notificar eventos de assinatura`

### Branch sugerida

`feat/notificacoes-assinatura`

### Escopo

- Enfileirar notificacao de assinatura iniciada.
- Enfileirar notificacao de pagamento confirmado.
- Enfileirar notificacao de pagamento recusado.
- Enfileirar notificacao de assinatura cancelada/suspensa.
- Usar `IMensagemNotificacaoService`.

### Fora de escopo

- Envio direto por provedor.
- Campanhas ou comunicados manuais.

### Checklist

- [ ] Eventos importantes enfileiram mensagens.
- [ ] Mensagens usam a fila existente.
- [ ] Nao ha chamada direta a provedor externo.
- [ ] Testes validam DTO enfileirado.

## PR 14 - Testes integrados do fluxo completo

### Base

Task 14 - Testes integrados do fluxo completo.

### Titulo sugerido

`test: cobrir fluxo integrado de assinatura`

### Branch sugerida

`test/fluxo-integrado-assinatura`

### Escopo

- Cobrir fluxo de estabelecimento.
- Cobrir fluxo de profissional autonomo.
- Cobrir pagamento pendente.
- Cobrir webhook aprovado.
- Cobrir liberacao de modulos.
- Cobrir bloqueio antes da confirmacao.
- Cobrir idempotencia do webhook.

### Fora de escopo

- Novas regras de produto.
- Refactors estruturais.

### Checklist

- [ ] Fluxo completo de estabelecimento passa.
- [ ] Fluxo completo de autonomo passa.
- [ ] Webhook aprovado ativa assinatura.
- [ ] Antes do pagamento, modulos ficam bloqueados.
- [ ] Webhook duplicado nao duplica efeitos.

# Tasks - Servicos de estabelecimento e profissional autonomo

Este documento organiza as tasks relacionadas ao cadastro, gerenciamento e vinculacao de servicos oferecidos por estabelecimentos e profissionais autonomos.

## Epic

Implementar o modulo de servicos do Glow Up Connect, permitindo que estabelecimentos e profissionais autonomos cadastrem servicos com nome, descricao, valor e tempo estimado, alem de controlar quais profissionais executam cada servico.

## Objetivo do fluxo

Permitir que uma operacao ativa cadastre seus servicos:

```text
Estabelecimento ou profissional autonomo
  -> cadastra servico
  -> informa nome, descricao, valor e duracao
  -> define disponibilidade do servico
  -> vincula profissionais executores, quando for servico de estabelecimento
  -> servico fica disponivel para agenda e marketplace
```

## Tipos de servico

### Servico de estabelecimento

Servico criado dentro de um estabelecimento.

Pode estar:

- sem profissional vinculado inicialmente;
- vinculado a um profissional especifico;
- vinculado a varios profissionais.

O servico pertence ao estabelecimento, mesmo quando executado por um ou mais profissionais.

### Servico de profissional autonomo

Servico criado por um profissional autonomo.

Pertence diretamente ao profissional autonomo e nao depende de estabelecimento.

## Campos obrigatorios do servico

Todo servico deve possuir:

- `Nome`;
- `Descricao`;
- `PrecoBase`;
- `DuracaoMinutos`;
- status ativo/inativo.

## Regras principais

- Servico de estabelecimento deve possuir `EstabelecimentoId`.
- Servico de profissional autonomo deve possuir `ProfissionalAutonomoId`.
- Um servico nao deve pertencer a estabelecimento e profissional autonomo ao mesmo tempo.
- Servico de estabelecimento pode existir sem profissional vinculado.
- Um servico de estabelecimento pode ser executado por varios profissionais.
- O vinculo entre profissional e servico pode sobrescrever preco e duracao, se necessario.
- Servico inativo nao deve aparecer para agendamento.
- Criacao de servicos deve respeitar limites do plano/assinatura.

## Task 1 - Criar cadastro de servico de estabelecimento

### Descricao

Criar caso de uso e endpoint para cadastrar servicos pertencentes a um estabelecimento.

### Criterios de aceite

- Permitir criar servico informando nome, descricao, valor e duracao estimada.
- Vincular o servico ao `EstabelecimentoId`.
- Gerar servico ativo por padrao, salvo regra contraria.
- Validar assinatura ativa e limite de servicos do plano.
- Validar permissao do usuario no estabelecimento.
- Nao exigir profissional vinculado no momento da criacao.
- Retornar os dados do servico criado.
- Incluir testes de sucesso, dados invalidos, limite de plano e permissao negada.

## Task 2 - Criar cadastro de servico de profissional autonomo

### Descricao

Criar caso de uso e endpoint para cadastrar servicos pertencentes a um profissional autonomo.

### Criterios de aceite

- Permitir criar servico informando nome, descricao, valor e duracao estimada.
- Vincular o servico ao `ProfissionalAutonomoId`.
- Garantir que o profissional pertence ao usuario autenticado.
- Gerar servico ativo por padrao, salvo regra contraria.
- Validar assinatura ativa e limite de servicos do plano.
- Retornar os dados do servico criado.
- Incluir testes de sucesso, dados invalidos, limite de plano e acesso negado.

## Task 3 - Validacoes de campos do servico

### Descricao

Definir e implementar validacoes para os campos obrigatorios do servico.

### Criterios de aceite

- `Nome` deve ser obrigatorio.
- `Descricao` deve ser obrigatoria ou opcional conforme decisao de produto.
- `PrecoBase` deve ser maior ou igual a zero.
- `DuracaoMinutos` deve ser maior que zero.
- `DuracaoMinutos` deve respeitar limite maximo razoavel para agenda.
- Campos de texto devem respeitar tamanho maximo.
- Retornar mensagens claras para dados invalidos.
- Incluir testes das validacoes.

## Task 4 - Listagem de servicos do estabelecimento

### Descricao

Criar listagem de servicos cadastrados em um estabelecimento, com filtros para administracao e uso em agenda.

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

Criar listagem de servicos cadastrados por um profissional autonomo.

### Criterios de aceite

- Listar servicos do profissional autonomo.
- Permitir filtro por status ativo/inativo.
- Permitir busca por nome.
- Garantir que o usuario autenticado so liste seus proprios servicos autonomos, salvo perfil administrativo.
- Incluir testes de filtros e acesso.

## Task 6 - Edicao de servico

### Descricao

Criar fluxo para editar dados principais de um servico.

### Criterios de aceite

- Permitir alterar nome, descricao, valor base e duracao estimada.
- Validar permissao do usuario.
- Validar que o servico pertence ao estabelecimento ou profissional autonomo correto.
- Nao quebrar agendamentos historicos ja criados.
- Definir se alteracoes afetam apenas novos agendamentos.
- Incluir testes de edicao valida, acesso negado e servico inexistente.

## Task 7 - Ativacao e inativacao de servico

### Descricao

Criar fluxo para ativar e inativar servicos sem remover historico.

### Criterios de aceite

- Permitir inativar servico.
- Servico inativo nao deve aparecer para novos agendamentos.
- Servico inativo deve continuar visivel em agendamentos historicos.
- Permitir reativar servico, se as regras do plano permitirem.
- Validar permissao do usuario.
- Incluir testes de ativacao, inativacao e impacto na agenda.

## Task 8 - Vincular servico de estabelecimento a profissionais

### Descricao

Criar fluxo para associar um servico de estabelecimento a um ou mais profissionais que podem executa-lo.

### Criterios de aceite

- Permitir vincular um profissional ao servico.
- Permitir vincular varios profissionais ao mesmo servico.
- Validar que o profissional pertence ao estabelecimento.
- Validar que o servico pertence ao estabelecimento.
- Impedir vinculo duplicado ativo.
- Permitir informar preco e duracao especificos por profissional, quando aplicavel.
- Incluir testes de vinculo simples, multiplo, duplicidade e profissional de outro estabelecimento.

## Task 9 - Desvincular profissional de servico

### Descricao

Criar fluxo para remover ou inativar o vinculo entre profissional e servico.

### Criterios de aceite

- Permitir desvincular profissional do servico.
- Impedir novos agendamentos para aquele profissional/servico apos desvinculo.
- Preservar historico de agendamentos ja criados.
- Validar permissao do usuario.
- Definir comportamento quando houver agendamentos futuros para o vinculo removido.
- Incluir testes de desvinculo e impacto em novos agendamentos.

## Task 10 - Servico de estabelecimento sem profissional vinculado

### Descricao

Definir comportamento para servicos de estabelecimento que ainda nao possuem profissional executor vinculado.

### Criterios de aceite

- Permitir cadastrar servico sem profissional.
- Exibir servico na administracao do estabelecimento.
- Definir se servico sem profissional aparece ou nao no marketplace/agendamento publico.
- Bloquear agendamento do servico quando nao houver profissional disponivel, salvo regra explicita.
- Retornar mensagem clara quando o servico nao puder ser agendado por falta de profissional.
- Incluir testes do comportamento definido.

## Task 11 - Preco e duracao por profissional

### Descricao

Permitir que um profissional vinculado a um servico tenha preco e duracao especificos, sobrescrevendo os dados base do servico quando necessario.

### Criterios de aceite

- Permitir configurar preco especifico por profissional.
- Permitir configurar duracao especifica por profissional.
- Quando nao houver sobrescrita, usar `PrecoBase` e `DuracaoMinutos` do servico.
- Agenda deve considerar a duracao efetiva do profissional.
- Pagamento deve considerar o preco efetivo.
- Incluir testes de fallback e sobrescrita.

## Task 12 - Permissoes para gerenciar servicos

### Descricao

Definir e aplicar permissoes para criacao, edicao, ativacao, inativacao e vinculacao de servicos.

### Criterios de aceite

- `Owner`, `Admin` e `Manager` podem gerenciar servicos do estabelecimento.
- `Receptionist` nao pode gerenciar servicos.
- `Profissional` nao pode gerenciar servicos da loja por padrao.
- Profissional autonomo pode gerenciar seus proprios servicos.
- Usuario sem vinculo nao pode acessar servicos privados.
- Incluir testes por perfil.

## Task 13 - Integracao de servicos com assinatura e limites

### Descricao

Integrar criacao e ativacao de servicos com os limites definidos no plano contratado.

### Criterios de aceite

- Validar assinatura ativa antes de permitir criar servico.
- Validar limite de servicos ativos do plano.
- Bloquear criacao quando limite for atingido.
- Definir se servicos inativos contam ou nao no limite.
- Retornar erro claro quando plano nao permitir mais servicos.
- Incluir testes com plano limitado e plano sem limite.

## Task 14 - Integracao de servicos com agenda

### Descricao

Garantir que a agenda use apenas servicos ativos e executaveis pelo profissional selecionado.

### Criterios de aceite

- Agendamento de loja deve validar que o servico pertence ao estabelecimento.
- Agendamento de loja deve validar que o profissional executa o servico, quando profissional for informado.
- Agendamento de profissional autonomo deve validar que o servico pertence ao profissional.
- Servico inativo nao pode ser agendado.
- Duracao efetiva do servico deve ser usada para calcular inicio/fim.
- Valor efetivo do servico deve ser usado no pagamento.
- Incluir testes de validacao da agenda.

## Task 15 - Integracao de servicos com marketplace

### Descricao

Definir quais servicos aparecem publicamente para clientes no marketplace ou pagina publica de agendamento.

### Criterios de aceite

- Exibir apenas servicos ativos.
- Para estabelecimento, definir se exibe servicos sem profissional vinculado.
- Exibir preco base ou faixa de preco quando houver variacao por profissional.
- Exibir duracao base ou duracao estimada.
- Nao expor dados administrativos.
- Incluir testes de retorno publico.

## Task 16 - Auditoria de alteracoes de servico

### Descricao

Registrar alteracoes sensiveis nos servicos para rastreabilidade operacional.

### Criterios de aceite

- Auditar criacao de servico.
- Auditar edicao de preco e duracao.
- Auditar ativacao/inativacao.
- Auditar vinculo e desvinculo de profissionais.
- Registrar usuario executor e entidade afetada.
- Nao registrar dados sensiveis desnecessarios.
- Incluir testes de auditoria.

## Task 17 - Testes integrados do modulo de servicos

### Descricao

Criar testes integrados cobrindo o fluxo completo de servicos para estabelecimento e profissional autonomo.

### Criterios de aceite

- Testar criacao de servico de estabelecimento sem profissional.
- Testar criacao de servico de estabelecimento com um profissional.
- Testar criacao de servico de estabelecimento com varios profissionais.
- Testar criacao de servico de profissional autonomo.
- Testar permissao negada para recepcionista e profissional da loja.
- Testar limite de plano.
- Testar servico inativo bloqueado para agendamento.
- Testar preco/duracao especificos por profissional.


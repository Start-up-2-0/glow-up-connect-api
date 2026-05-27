# Tasks - Horarios de funcionamento e atendimento

Este documento organiza as tasks relacionadas a configuracao de horarios do estabelecimento e dos profissionais, incluindo profissionais vinculados a estabelecimento e profissionais autonomos.

## Epic

Implementar o modulo de horarios do Glow Up Connect, permitindo configurar quando um estabelecimento funciona e quando cada profissional atende.

## Objetivo do fluxo

Permitir que a agenda considere disponibilidade real:

```text
Estabelecimento ou profissional autonomo
  -> configura dias de atendimento
  -> configura horarios de inicio e fim
  -> sistema valida conflitos e regras
  -> agenda usa os horarios para ofertar slots disponiveis
```

## Conceitos

### Horario de funcionamento do estabelecimento

Representa os dias e horarios em que a loja esta aberta.

Exemplo:

```text
Segunda a sexta: 09:00 - 18:00
Sabado: 09:00 - 13:00
Domingo: fechado
```

### Horario de atendimento do profissional no estabelecimento

Representa os dias e horarios em que um profissional atende dentro de uma loja especifica.

Exemplo:

```text
Profissional Joao na Barbearia X
Terca a sexta: 10:00 - 17:00
Sabado: 09:00 - 13:00
```

Esse horario deve respeitar o horario de funcionamento do estabelecimento.

### Horario de atendimento do profissional autonomo

Representa os dias e horarios em que o profissional autonomo atende na propria operacao.

Ele nao depende de estabelecimento.

## Regras principais

- Estabelecimento possui horario de funcionamento proprio.
- Profissional vinculado a estabelecimento possui horario de atendimento por estabelecimento.
- Profissional autonomo possui horario de atendimento proprio.
- Horario do profissional dentro da loja deve estar dentro do horario de funcionamento do estabelecimento.
- Agenda deve considerar horario da loja, horario do profissional, duracao do servico e bloqueios futuros.
- Horarios inativos nao devem gerar disponibilidade.
- Alteracoes de horario nao devem quebrar agendamentos historicos.

## Task 1 - Cadastro de horario de funcionamento do estabelecimento

### Descricao

Criar fluxo para cadastrar os dias e horarios em que um estabelecimento funciona.

### Criterios de aceite

- Permitir cadastrar dia da semana, hora de inicio e hora de fim.
- Vincular o horario ao `EstabelecimentoId`.
- Validar que hora de fim seja maior que hora de inicio.
- Permitir mais de um intervalo no mesmo dia, se definido pelo produto.
- Validar permissao do usuario no estabelecimento.
- Criar horario ativo por padrao.
- Incluir testes de sucesso, horario invalido e permissao negada.

## Task 2 - Listagem de horarios de funcionamento do estabelecimento

### Descricao

Criar endpoint/caso de uso para listar os horarios de funcionamento cadastrados no estabelecimento.

### Criterios de aceite

- Listar horarios por estabelecimento.
- Permitir filtro por dia da semana.
- Permitir filtro por status ativo/inativo.
- Ordenar por dia da semana e hora de inicio.
- Respeitar permissao do usuario.
- Incluir testes de listagem e filtros.

## Task 3 - Edicao de horario de funcionamento do estabelecimento

### Descricao

Criar fluxo para editar horarios de funcionamento do estabelecimento.

### Criterios de aceite

- Permitir alterar dia da semana, hora de inicio e hora de fim.
- Validar que hora de fim seja maior que hora de inicio.
- Validar se a alteracao impacta horarios ativos de profissionais.
- Definir se deve bloquear alteracao que deixe profissional fora do horario da loja.
- Nao alterar agendamentos historicos.
- Validar permissao do usuario.
- Incluir testes de edicao valida, invalida e impacto em profissionais.

## Task 4 - Ativacao e inativacao de horario de funcionamento

### Descricao

Criar fluxo para ativar e inativar horarios de funcionamento do estabelecimento.

### Criterios de aceite

- Permitir inativar horario de funcionamento.
- Horario inativo nao deve gerar disponibilidade.
- Validar impacto em horarios de profissionais e agendamentos futuros.
- Permitir reativar horario.
- Validar permissao do usuario.
- Incluir testes de ativacao, inativacao e impacto na agenda.

## Task 5 - Cadastro de horario de profissional no estabelecimento

### Descricao

Criar fluxo para definir os dias e horarios em que um profissional atende dentro de um estabelecimento.

### Criterios de aceite

- Permitir cadastrar dia da semana, hora de inicio e hora de fim.
- Vincular horario ao `ProfissionalId`.
- Vincular horario ao `EstabelecimentoId`.
- Validar que o profissional esta ativo no estabelecimento.
- Validar que o horario esta dentro do funcionamento da loja.
- Validar que hora de fim seja maior que hora de inicio.
- Criar horario ativo por padrao.
- Validar permissao do usuario no estabelecimento.
- Incluir testes de sucesso, profissional sem vinculo, horario fora da loja e permissao negada.

## Task 6 - Listagem de horarios de profissional no estabelecimento

### Descricao

Criar listagem dos horarios de atendimento dos profissionais dentro do estabelecimento.

### Criterios de aceite

- Listar horarios por estabelecimento.
- Permitir filtro por profissional.
- Permitir filtro por dia da semana.
- Permitir filtro por status ativo/inativo.
- Profissional comum deve visualizar apenas seus proprios horarios.
- Owner/Admin/Manager podem visualizar horarios de todos os profissionais.
- Recepcionista pode visualizar horarios para montar agenda, se definido pela matriz de acesso.
- Incluir testes por perfil.

## Task 7 - Edicao de horario de profissional no estabelecimento

### Descricao

Criar fluxo para editar horario de atendimento de profissional dentro de um estabelecimento.

### Criterios de aceite

- Permitir alterar dia da semana, hora de inicio e hora de fim.
- Validar que o profissional pertence ao estabelecimento.
- Validar que novo horario esta dentro do funcionamento da loja.
- Validar conflitos com outros horarios do mesmo profissional.
- Validar impacto em agendamentos futuros.
- Validar permissao do usuario.
- Incluir testes de edicao valida, horario fora da loja, conflito e permissao negada.

## Task 8 - Ativacao e inativacao de horario de profissional no estabelecimento

### Descricao

Criar fluxo para ativar e inativar horarios de atendimento de profissional dentro da loja.

### Criterios de aceite

- Permitir inativar horario do profissional.
- Horario inativo nao deve gerar disponibilidade.
- Validar impacto em agendamentos futuros.
- Permitir reativar horario, desde que respeite funcionamento da loja.
- Validar permissao do usuario.
- Incluir testes de ativacao, inativacao e impacto na agenda.

## Task 9 - Cadastro de horario de profissional autonomo

### Descricao

Criar fluxo para cadastrar os dias e horarios em que um profissional autonomo atende.

### Criterios de aceite

- Permitir cadastrar dia da semana, hora de inicio e hora de fim.
- Vincular horario ao `ProfissionalId`.
- Garantir que o profissional e autonomo.
- Garantir que o profissional pertence ao usuario autenticado, salvo perfil administrativo.
- Validar que hora de fim seja maior que hora de inicio.
- Criar horario ativo por padrao.
- Incluir testes de sucesso, profissional nao autonomo, acesso negado e horario invalido.

## Task 10 - Listagem de horarios de profissional autonomo

### Descricao

Criar listagem dos horarios de atendimento do profissional autonomo.

### Criterios de aceite

- Listar horarios do profissional autonomo.
- Permitir filtro por dia da semana.
- Permitir filtro por status ativo/inativo.
- Garantir acesso apenas ao dono do perfil autonomo, salvo perfil administrativo.
- Ordenar por dia da semana e hora de inicio.
- Incluir testes de listagem e acesso.

## Task 11 - Edicao de horario de profissional autonomo

### Descricao

Criar fluxo para editar horarios de atendimento do profissional autonomo.

### Criterios de aceite

- Permitir alterar dia da semana, hora de inicio e hora de fim.
- Validar que hora de fim seja maior que hora de inicio.
- Validar conflitos com outros horarios do mesmo profissional.
- Validar impacto em agendamentos futuros.
- Garantir acesso apenas ao dono do perfil autonomo, salvo perfil administrativo.
- Incluir testes de edicao valida, conflito e acesso negado.

## Task 12 - Ativacao e inativacao de horario de profissional autonomo

### Descricao

Criar fluxo para ativar e inativar horarios de atendimento do profissional autonomo.

### Criterios de aceite

- Permitir inativar horario.
- Horario inativo nao deve gerar disponibilidade.
- Validar impacto em agendamentos futuros.
- Permitir reativar horario.
- Garantir acesso apenas ao dono do perfil autonomo, salvo perfil administrativo.
- Incluir testes de ativacao, inativacao e impacto na agenda.

## Task 13 - Validacao de conflitos de horarios

### Descricao

Criar regra reutilizavel para impedir conflitos entre horarios ativos do mesmo profissional ou do mesmo estabelecimento quando aplicavel.

### Criterios de aceite

- Impedir intervalos sobrepostos para o mesmo profissional no mesmo dia.
- Impedir intervalos duplicados.
- Permitir intervalos separados no mesmo dia, se definido pelo produto.
- Validar conflitos na criacao e edicao.
- Retornar mensagem clara quando houver conflito.
- Incluir testes de sobreposicao, duplicidade e intervalos validos.

## Task 14 - Validacao de horarios com agendamentos futuros

### Descricao

Definir e implementar comportamento quando uma alteracao de horario impactar agendamentos futuros.

### Criterios de aceite

- Detectar agendamentos futuros que ficariam fora do novo horario.
- Definir se a alteracao deve ser bloqueada ou permitida com aviso.
- Nao alterar agendamentos historicos.
- Nao cancelar agendamentos automaticamente sem regra explicita.
- Retornar dados suficientes para o front exibir conflito ao usuario.
- Incluir testes com agendamentos futuros afetados.

## Task 15 - Geracao de disponibilidade para agenda

### Descricao

Criar servico para calcular disponibilidade de agenda usando horarios, servicos, duracao, profissionais e agendamentos existentes.

### Criterios de aceite

- Considerar horario de funcionamento da loja para agendamento de estabelecimento.
- Considerar horario do profissional no estabelecimento.
- Considerar horario do profissional autonomo.
- Considerar duracao efetiva do servico.
- Ignorar horarios inativos.
- Remover slots ja ocupados por agendamentos existentes.
- Retornar slots disponiveis por data/profissional.
- Incluir testes de disponibilidade.

## Task 16 - Permissoes para gerenciar horarios

### Descricao

Definir e aplicar permissoes para criacao, edicao, ativacao e inativacao de horarios.

### Criterios de aceite

- `Owner`, `Admin` e `Manager` podem gerenciar horarios do estabelecimento.
- `Receptionist` pode visualizar horarios, mas nao gerenciar por padrao.
- `Profissional` pode visualizar seus proprios horarios.
- Definir se profissional pode sugerir/editar seus proprios horarios.
- Profissional autonomo pode gerenciar seus proprios horarios.
- Usuario sem vinculo nao pode acessar horarios privados.
- Incluir testes por perfil.

## Task 17 - Integracao de horarios com marketplace

### Descricao

Definir como os horarios impactam a exibicao publica de disponibilidade no marketplace e pagina de agendamento.

### Criterios de aceite

- Nao exibir slots fora do horario configurado.
- Nao exibir slots de profissional inativo.
- Nao exibir slots de servico sem profissional disponivel, quando aplicavel.
- Considerar horario da loja e do profissional para estabelecimento.
- Considerar apenas horario do autonomo para profissional autonomo.
- Incluir testes de retorno publico.

## Task 18 - Auditoria de alteracoes de horarios

### Descricao

Registrar alteracoes sensiveis em horarios para rastreabilidade operacional.

### Criterios de aceite

- Auditar criacao de horario.
- Auditar edicao de horario.
- Auditar ativacao/inativacao.
- Registrar usuario executor.
- Registrar estabelecimento ou profissional afetado.
- Registrar resumo da alteracao sem dados sensiveis.
- Incluir testes de auditoria.

## Task 19 - Testes integrados do modulo de horarios

### Descricao

Criar testes integrados cobrindo os principais fluxos de horarios para estabelecimento, profissional vinculado e profissional autonomo.

### Criterios de aceite

- Testar cadastro de horario de funcionamento do estabelecimento.
- Testar cadastro de horario de profissional dentro da loja.
- Testar bloqueio de horario do profissional fora do funcionamento da loja.
- Testar cadastro de horario de profissional autonomo.
- Testar conflito de horarios.
- Testar impacto em disponibilidade da agenda.
- Testar permissoes de owner, manager, receptionist e profissional.


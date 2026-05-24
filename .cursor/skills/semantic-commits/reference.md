# Referencia — commits semanticos

## Template de mensagem

```
<tipo>(<escopo>): <verbo no infinitivo ou 3a pessoa> <o que foi feito>
```

Regras:
- Maximo ~72 caracteres na primeira linha
- Sem ponto final
- Descrever **o que** e **por que implicito**, nao listar arquivos
- Portugues; preferir ASCII (`excecoes`, `autenticacao`, `configuracoes`)

## Ordem ideal de commits (feature completa)

Exemplo para autenticacao (referencia do script `scripts/commit-auth-feature.ps1`):

1. `feat(domain): adicionar hierarquia de excecoes de autenticacao`
2. `feat(domain): adicionar entidades de sessao e log de autenticacao`
3. `feat(usuario): adicionar controle de tentativas e bloqueio temporario`
4. `feat(auth): adicionar opcoes e contratos de autenticacao`
5. `feat(auth): adicionar hash de senha e contexto do usuario atual`
6. `refactor(usuario): injetar hash de senha no servico de usuario`
7. `feat(auth): implementar servico de assinatura glow token`
8. `feat(infrastructure): adicionar repositorios e configuracoes de autenticacao`
9. `chore(database): adicionar migration de sessoes de autenticacao`
10. `chore(database): refatorar schema de sessao para glow token`
11. `feat(auth): implementar servicos de autenticacao e sessao`
12. `feat(api): adicionar modelos padronizados de resposta da API`
13. `feat(auth): adicionar controller de auth e DTOs de login e refresh`
14. `feat(api): adicionar middleware global de excecoes de autenticacao`
15. `feat(auth): adicionar middleware de autenticacao x-glow-token`
16. `refactor(auth): substituir pipeline JWT por glow token`
17. `refactor(usuario): limpar imports do controller de usuario`
18. `test(auth): adicionar testes unitarios de dominio servicos e middleware`
19. `test(auth): adicionar testes de integracao do controller de auth`

## Checklist antes de cada commit

- [ ] Apenas um tipo de mudanca (domain OU service OU test...)
- [ ] Mensagem descreve a responsabilidade, nao "varios arquivos"
- [ ] Nenhum `bin/`, `obj/`, `.env`, `appsettings.Development.json`
- [ ] Sem `Co-authored-by: Cursor` no corpo do commit
- [ ] Commit compila isoladamente quando possivel

## Autoria Git (sem Cursor)

Commits devem usar **somente** `git config user.name` e `user.email` locais.

O script `commit-auth-feature.ps1`:
- Define `GIT_AUTHOR_*` e `GIT_COMMITTER_*` explicitamente
- Usa `--no-signoff`
- Rejeita trailers com Cursor

**Recomendacao:** usuario executa commits manualmente ou via script `.cmd` no proprio terminal.

## Criar script batch para nova feature

Copiar `scripts/commit-auth-feature.ps1` e ajustar array `$commits` com:
- `Message` — mensagem em portugues
- `Paths` — paths relativos ao repo (pastas ou arquivos)

Manter funcoes `Assert-NoCursorCoAuthor` e `Assert-NoForbiddenPaths`.

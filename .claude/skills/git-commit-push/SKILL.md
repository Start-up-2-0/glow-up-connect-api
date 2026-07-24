---
name: git-commit-push
description: Commits changes and pushes to a specified branch in glow-up-connect-api.
arguments:
  message:
    type: string
    description: A mensagem de commit a ser usada, seguindo o padrão convencional em português (ex: "feat(modulo): Adiciona nova funcionalidade").
  branch:
    type: string
    description: A branch para onde fazer o push. O padrão é 'staging'.
---

### \`git-commit-push\`

Esta skill executa \`git add .\`, \`git commit -m "<message>"\`, e \`git push origin <branch>\` dentro do diretório \`glow-up-connect-api\`.

**Uso:**

\`\`\`
/git-commit-push message="feat(core): Adiciona validação de email no cadastro" branch="main"
\`\`\`

**Exemplos de padrão de mensagem de commit (Português):**

- \`feat(modulo): Adiciona nova funcionalidade X\`
- \`fix(bug): Corrige problema Y no módulo Z\`
- \`refactor(arquivo): Refatora código em arquivo A\`
- \`docs(readme): Atualiza documentação do README\`
- \`style(css): Ajusta espaçamento e formatação\`

\`\`\`javascript
const { message, branch = 'staging' } = args;

if (!message) {
  throw new Error("A mensagem de commit é obrigatória.");
}

await Bash({
  description: \`Stage changes, commit with message "${message}", and push to branch "${branch}" in glow-up-connect-api.\`,
  command: \`cd glow-up-connect-api && git add . && git commit -m "${message}" && git push origin \${branch}\`
});

return \`Alterações commitadas com a mensagem: "${message}" e enviadas para a branch: "${branch}" no repositório glow-up-connect-api.\`;
\`\`\`

# DevSecOps — Glow Up Connect

## CI API (`ci-main.yml`, `ci-staging.yml`)

| Job | Ferramenta |
|-----|------------|
| secrets | Gitleaks |
| sca | `dotnet list package --vulnerable` |
| container | Trivy (Dockerfile) |
| sast | CodeQL (C#) |

## CI App (`ci.yml`)

| Job | Ferramenta |
|-----|------------|
| sca | `npm audit --audit-level=high` |
| container | Trivy (Dockerfile) |

## Dependabot

Atualizações semanais de NuGet e npm nos dois repositórios.

## Interpretação de falhas

- **Gitleaks:** remover secret e rotacionar credencial
- **CVE HIGH:** atualizar pacote ou documentar exceção temporária
- **Trivy:** corrigir imagem base ou pacote OS

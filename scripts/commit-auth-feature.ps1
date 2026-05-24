#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the x-glow-token authentication feature.

.DESCRIPTION
  Run this script directly in your own PowerShell terminal (outside Cursor agent).
  Commits use ONLY your local git user.name / user.email - no Co-authored-by trailers.

  Usage (choose ONE):

    # Option A - from repo root in PowerShell (recommended)
    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\commit-auth-feature.ps1

    # Option B - double-click or run the .cmd launcher (avoids Notepad opening)
    scripts\commit-auth-feature.cmd

  Do NOT double-click the .ps1 file - Windows opens it in Notepad by default.

  Options:
    -DryRun   Show what would be committed without creating commits
    -SkipBuild  Skip dotnet build verification after all commits
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $RepoRoot

function Assert-GitUserConfigured {
    $name = git config user.name 2>$null
    $email = git config user.email 2>$null
    if ([string]::IsNullOrWhiteSpace($name) -or [string]::IsNullOrWhiteSpace($email)) {
        throw 'Configure git user.name and user.email before running this script.'
    }
    Write-Host ("Git author: {0} <{1}>" -f $name, $email) -ForegroundColor Cyan
    return @{ Name = $name; Email = $email }
}

function Assert-NoCursorCoAuthor {
    param([string]$Message)
    if ($Message -match '(?i)co-authored-by:\s*cursor') {
        throw 'Commit message contains Cursor co-author trailer. Aborting.'
    }
}

function Assert-NoForbiddenPaths {
    param([string[]]$Paths)
    foreach ($path in $Paths) {
        if ($path -match '\\bin\\|\\obj\\|appsettings\.Development\.json') {
            throw "Forbidden path staged: $path"
        }
    }
}

function New-FeatureCommit {
    param(
        [Parameter(Mandatory)]
        [string]$Message,
        [Parameter(Mandatory)]
        [string[]]$Paths,
        [hashtable]$GitUser
    )

    Assert-NoCursorCoAuthor -Message $Message

    $existing = @()
    foreach ($path in $Paths) {
        $full = Join-Path $RepoRoot $path
        $tracked = git ls-files -- $path 2>$null
        $hasStatus = git status --porcelain -- $path 2>$null

        if ((Test-Path $full) -or $tracked -or $hasStatus) {
            $existing += $path
        }
    }

    if ($existing.Count -eq 0) {
        Write-Warning "Skip (no files): $Message"
        return
    }

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Green
    $existing | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

    if ($DryRun) {
        return
    }

    git add -- $existing
    if ($LASTEXITCODE -ne 0) { throw "git add failed for: $Message" }

    $staged = git diff --cached --name-only
    if (-not $staged) {
        Write-Warning "Nothing staged for: $Message"
        return
    }

    Assert-NoForbiddenPaths -Paths $staged

    $env:GIT_AUTHOR_NAME = $GitUser.Name
    $env:GIT_AUTHOR_EMAIL = $GitUser.Email
    $env:GIT_COMMITTER_NAME = $GitUser.Name
    $env:GIT_COMMITTER_EMAIL = $GitUser.Email

    git commit -m $Message --no-signoff
    if ($LASTEXITCODE -ne 0) { throw "git commit failed for: $Message" }

    $body = git log -1 --format=%B
    if ($body -match '(?i)co-authored-by:\s*cursor') {
        throw "Cursor co-author detected in commit '$Message'. Run: git reset --soft HEAD~1"
    }

    Write-Host "    OK $(git rev-parse --short HEAD)" -ForegroundColor Green
}

$gitUser = Assert-GitUserConfigured

Write-Host ""
Write-Host "Repository: $RepoRoot" -ForegroundColor Cyan
if ($DryRun) {
    Write-Host 'DRY RUN - no commits will be created' -ForegroundColor Yellow
}

$commits = @(
    @{
        Message = 'feat(domain): adicionar hierarquia de excecoes de autenticacao'
        Paths   = @('src/GLOWAPI.Domain/Exceptions/Auth/')
    },
    @{
        Message = 'feat(domain): adicionar entidades de sessao e log de autenticacao'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/SessaoAutenticacao.cs',
            'src/GLOWAPI.Domain/Entities/LogAutenticacao.cs'
        )
    },
    @{
        Message = 'feat(usuario): adicionar controle de tentativas e bloqueio temporario'
        Paths   = @('src/GLOWAPI.Domain/Entities/Usuario.cs')
    },
    @{
        Message = 'feat(auth): adicionar opcoes e contratos de autenticacao'
        Paths   = @(
            'src/GLOWAPI.Application/Options/',
            'src/GLOWAPI.Application/Models/',
            'src/GLOWAPI.Application/Interfaces/Repositories/ILogAutenticacaoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/ISessaoAutenticacaoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IPasswordHasher.cs',
            'src/GLOWAPI.Application/Interfaces/Services/ICurrentUserContext.cs',
            'src/GLOWAPI.Application/Interfaces/Services/ISecurityAuditLogger.cs'
        )
    },
    @{
        Message = 'feat(auth): adicionar hash de senha e contexto do usuario atual'
        Paths   = @(
            'src/GLOWAPI.Application/Services/BcryptPasswordHasher.cs',
            'src/GLOWAPI.Application/Services/CurrentUserContext.cs',
            'src/GLOWAPI.Application/GLOWAPI.Application.csproj'
        )
    },
    @{
        Message = 'refactor(usuario): injetar hash de senha no servico de usuario'
        Paths   = @('src/GLOWAPI.Application/Services/UsuarioService.cs')
    },
    @{
        Message = 'feat(auth): implementar servico de assinatura glow token'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IGlowTokenService.cs',
            'src/GLOWAPI.Infrastructure/Security/GlowTokenService.cs'
        )
    },
    @{
        Message = 'feat(infrastructure): adicionar repositorios e configuracoes de autenticacao'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Repositories/SessaoAutenticacaoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/LogAutenticacaoRepository.cs',
            'src/GLOWAPI.Infrastructure/Configurations/SessaoAutenticacaoConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Configurations/LogAutenticacaoConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Configurations/UsuarioConfiguration.cs',
            'src/GLOWAPI.Infrastructure/ApplicationDbContext.cs'
        )
    },
    @{
        Message = 'chore(database): adicionar migration de sessoes de autenticacao'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260524004250_CreateSessoesAutenticacao.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260524004250_CreateSessoesAutenticacao.Designer.cs'
        )
    },
    @{
        Message = 'chore(database): refatorar schema de sessao para glow token'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260524005716_RefactorSessaoAutenticacaoGlowToken.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260524005716_RefactorSessaoAutenticacaoGlowToken.Designer.cs',
            'src/GLOWAPI.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs'
        )
    },
    @{
        Message = 'feat(auth): implementar servicos de autenticacao e sessao'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IAuthService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAuthSessionService.cs',
            'src/GLOWAPI.Application/Services/AuthService.cs',
            'src/GLOWAPI.Application/Services/AuthSessionService.cs',
            'src/GLOWAPI.Infrastructure/Security/SecurityAuditLogger.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs',
            'src/GLOWAPI.Infrastructure/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(api): adicionar modelos padronizados de resposta da API'
        Paths   = @('src/GLOWAPI.API/Models/')
    },
    @{
        Message = 'feat(auth): adicionar controller de auth e DTOs de login e refresh'
        Paths   = @(
            'src/GLOWAPI.API/DTOs/Auth/',
            'src/GLOWAPI.API/Controllers/AuthController.cs',
            'src/GLOWAPI.API/Controllers/HealthController.cs'
        )
    },
    @{
        Message = 'feat(api): adicionar middleware global de excecoes de autenticacao'
        Paths   = @('src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs')
    },
    @{
        Message = 'feat(auth): adicionar middleware de autenticacao x-glow-token'
        Paths   = @(
            'src/GLOWAPI.API/Middlewares/GlowTokenAuthenticationMiddleware.cs',
            'src/GLOWAPI.API/Middlewares/PermissionMiddleware.cs'
        )
    },
    @{
        Message = 'refactor(auth): substituir pipeline JWT por glow token'
        Paths   = @(
            'src/GLOWAPI.API/Program.cs',
            'src/GLOWAPI.API/appsettings.json',
            'src/GLOWAPI.API/GLOWAPI.API.csproj',
            'src/GLOWAPI.Infrastructure/GLOWAPI.Infrastructure.csproj'
        )
    },
    @{
        Message = 'refactor(usuario): limpar imports do controller de usuario'
        Paths   = @('src/GLOWAPI.API/Controllers/UsuarioController.cs')
    },
    @{
        Message = 'test(auth): adicionar testes unitarios de dominio servicos e middleware'
        Paths   = @(
            'tests/GLOWAPI.Tests/Helpers/',
            'tests/GLOWAPI.Tests/Unit/Domain/',
            'tests/GLOWAPI.Tests/Unit/Infrastructure/GlowTokenServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AuthServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AuthSessionServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/UsuarioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/API/ExceptionMiddlewareTests.cs',
            'tests/GLOWAPI.Tests/Unit/API/GlowTokenAuthenticationMiddlewareTests.cs'
        )
    },
    @{
        Message = 'test(auth): adicionar testes de integracao do controller de auth'
        Paths   = @(
            'tests/GLOWAPI.Tests/GLOWAPI.Tests.csproj',
            'tests/GLOWAPI.Tests/Integration/',
            'tests/GLOWAPI.Tests/UnitTest1.cs'
        )
    }
)

foreach ($commit in $commits) {
    New-FeatureCommit -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ""
    Write-Host "=== Remaining uncommitted files ===" -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "=== Running dotnet test ===" -ForegroundColor Cyan
        dotnet test
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ""
    Write-Host "Done. $(($commits | Measure-Object).Count) commits planned/processed." -ForegroundColor Green
    Write-Host "Verify authors:" -ForegroundColor Cyan
    git log --oneline -n 20 --format="%h %an %ae %s"
}

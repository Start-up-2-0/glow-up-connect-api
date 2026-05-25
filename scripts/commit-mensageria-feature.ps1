#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the async messaging (mensageria) feature.

.DESCRIPTION
  Run this script directly in your own PowerShell terminal (outside Cursor agent).
  Commits use ONLY your local git user.name / user.email - no Co-authored-by trailers.

  Usage (choose ONE):

    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\commit-mensageria-feature.ps1

    scripts\commit-mensageria-feature.cmd

  Do NOT double-click the .ps1 file - Windows opens it in Notepad by default.

  Options:
    -DryRun     Show what would be committed without creating commits
    -SkipBuild  Skip dotnet test after all commits
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
        Message = 'feat(domain): adicionar entidades e enums de mensageria'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/MensagemNotificacao.cs',
            'src/GLOWAPI.Domain/Entities/MensagemNotificacaoLog.cs',
            'src/GLOWAPI.Domain/Enums/StatusMensagemNotificacao.cs',
            'src/GLOWAPI.Domain/Enums/CanalMensagemNotificacao.cs'
        )
    },
    @{
        Message = 'feat(domain): adicionar excecoes de mensageria'
        Paths   = @('src/GLOWAPI.Domain/Exceptions/Mensageria/')
    },
    @{
        Message = 'feat(application): adicionar options, DTOs e interfaces de mensageria'
        Paths   = @(
            'src/GLOWAPI.Application/Options/MensageriaOptions.cs',
            'src/GLOWAPI.Application/DTOs/Mensageria/',
            'src/GLOWAPI.Application/Interfaces/Repositories/IMensagemNotificacaoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IMensagemNotificacaoService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IMensagemNotificacaoProcessadorService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IProvedorMensagem.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IProvedorMensagemResolver.cs'
        )
    },
    @{
        Message = 'feat(application): implementar services e models de mensageria'
        Paths   = @(
            'src/GLOWAPI.Application/Models/Mensageria/',
            'src/GLOWAPI.Application/Mensageria/',
            'src/GLOWAPI.Application/Services/MensagemNotificacaoService.cs',
            'src/GLOWAPI.Application/Services/MensagemNotificacaoProcessadorService.cs',
            'src/GLOWAPI.Application/Services/ProvedorMensagemResolver.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs',
            'src/GLOWAPI.Application/GLOWAPI.Application.csproj'
        )
    },
    @{
        Message = 'feat(infrastructure): adicionar configuracoes e repositorio de mensageria'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Configurations/MensagemNotificacaoConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Configurations/MensagemNotificacaoLogConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Repositories/MensagemNotificacaoRepository.cs',
            'src/GLOWAPI.Infrastructure/ApplicationDbContext.cs'
        )
    },
    @{
        Message = 'chore(database): adicionar migration de mensagens de notificacao'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260525020449_CreateMensagensNotificacao.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260525020449_CreateMensagensNotificacao.Designer.cs',
            'src/GLOWAPI.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs'
        )
    },
    @{
        Message = 'feat(infrastructure): adicionar provedores stub de mensageria'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Mensageria/',
            'src/GLOWAPI.Infrastructure/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(api): adicionar endpoints workers e configuracao de mensageria'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/MensagemNotificacaoController.cs',
            'src/GLOWAPI.API/Workers/',
            'src/GLOWAPI.API/Program.cs',
            'src/GLOWAPI.API/appsettings.json',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs'
        )
    },
    @{
        Message = 'test(mensageria): adicionar testes unitarios e ajuste de integracao'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Domain/MensagemNotificacaoTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/MensagemNotificacaoServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/MensagemNotificacaoProcessadorServiceTests.cs',
            'tests/GLOWAPI.Tests/Integration/GlowApiWebApplicationFactory.cs'
        )
    },
    @{
        Message = 'docs(mensageria): adicionar documentacao tecnica do modulo'
        Paths   = @('context/Mensageria-assincrona.md')
    }
)

foreach ($commit in $commits) {
    New-FeatureCommit -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ""
    Write-Host "=== Remaining uncommitted files ===" -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' 'context/' 'scripts/' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "=== Running dotnet test ===" -ForegroundColor Cyan
        dotnet test
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ""
    Write-Host "Done. $(($commits | Measure-Object).Count) commits planned/processed." -ForegroundColor Green
    Write-Host "Verify authors:" -ForegroundColor Cyan
    git log --oneline -n 15 --format="%h %an %ae %s"
}

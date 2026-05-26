#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the Resend email integration.

.DESCRIPTION
  Run in your own PowerShell terminal (outside Cursor agent).
  Usage:
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\commit-resend-email-feature.ps1
    scripts\commit-resend-email-feature.cmd
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

    if ($DryRun) { return }

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
        Message = 'feat(application): adicionar MensageriaEmailOptions'
        Paths   = @('src/GLOWAPI.Application/Options/MensageriaEmailOptions.cs')
    },
    @{
        Message = 'feat(infrastructure): integrar Resend em ProvedorMensagemEmail'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/GLOWAPI.Infrastructure.csproj',
            'src/GLOWAPI.Infrastructure/Mensageria/',
            'src/GLOWAPI.Infrastructure/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(api): configurar DI Resend e validacao de variaveis em hosted'
        Paths   = @(
            'src/GLOWAPI.API/Configuration/HostedConfigurationValidator.cs',
            'src/GLOWAPI.API/Program.cs',
            'src/GLOWAPI.API/appsettings.json'
        )
    },
    @{
        Message = 'test(mensageria): adicionar testes do provedor de email Resend'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Infrastructure/ProvedorMensagemEmailTests.cs',
            'tests/GLOWAPI.Tests/Unit/Infrastructure/EmailConteudoHtmlTests.cs'
        )
    },
    @{
        Message = 'docs(mensageria): documentar envio de email com Resend'
        Paths   = @(
            'context/Mensageria-assincrona.md',
            'README.md',
            'docs/railway-staging-setup.md'
        )
    }
)

foreach ($commit in $commits) {
    New-FeatureCommit -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ""
    Write-Host "=== Remaining uncommitted files ===" -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' 'context/' 'docs/' 'scripts/' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "=== Running dotnet test ===" -ForegroundColor Cyan
        dotnet test
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ""
    Write-Host "Done." -ForegroundColor Green
    git log --oneline -n 8 --format="%h %an %s"
}

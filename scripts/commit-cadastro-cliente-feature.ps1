#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the public client signup, email confirmation and avatar base64 feature.

.DESCRIPTION
  Run this script directly in your own PowerShell terminal (outside Cursor agent).
  Commits use ONLY your local git user.name / user.email - no Co-authored-by trailers.

  Usage (choose ONE):

    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\commit-cadastro-cliente-feature.ps1

    scripts\commit-cadastro-cliente-feature.cmd

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
        Message = 'feat(domain): adicionar confirmacao email avatar e excecoes em usuario'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/Usuario.cs',
            'src/GLOWAPI.Domain/Exceptions/Auth/EmailNaoConfirmadoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Usuario/AvatarInvalidoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Usuario/ConfirmacaoEmailInvalidaException.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar options interfaces e DTOs de cadastro cliente'
        Paths   = @(
            'src/GLOWAPI.Application/Options/AuthOptions.cs',
            'src/GLOWAPI.Application/Options/AvatarOptions.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAvatarBase64Decoder.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IConfirmacaoEmailService.cs',
            'src/GLOWAPI.Application/DTOs/Usuario/CadastrarClienteDto.cs',
            'src/GLOWAPI.Application/DTOs/Usuario/CadastroClienteResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Auth/ConfirmarEmailRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Auth/ReenviarConfirmacaoRequestDto.cs'
        )
    },
    @{
        Message = 'feat(application): implementar confirmacao email avatar e cadastro cliente'
        Paths   = @(
            'src/GLOWAPI.Application/Services/AvatarBase64Decoder.cs',
            'src/GLOWAPI.Application/Services/ConfirmacaoEmailService.cs',
            'src/GLOWAPI.Application/Services/UsuarioService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IUsuarioService.cs',
            'src/GLOWAPI.Application/Services/AuthService.cs',
            'src/GLOWAPI.Application/Services/AuthSessionService.cs',
            'src/GLOWAPI.Application/DTOs/Auth/LoginResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Usuario/UsuarioResponseDto.cs',
            'src/GLOWAPI.Application/Models/Auth/AuthModels.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(infrastructure): estender repositorio e configuracao de usuario'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Configurations/UsuarioConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Repositories/UsuarioRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IUsuarioRepository.cs'
        )
    },
    @{
        Message = 'chore(database): adicionar migration confirmacao email e avatar base64'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260525033013_AddUsuarioConfirmacaoEmailEAvatar.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260525033013_AddUsuarioConfirmacaoEmailEAvatar.Designer.cs',
            'src/GLOWAPI.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs'
        )
    },
    @{
        Message = 'feat(api): adicionar cadastro cliente e endpoints de confirmacao email'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/UsuarioController.cs',
            'src/GLOWAPI.API/Controllers/AuthController.cs',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs',
            'src/GLOWAPI.API/Program.cs',
            'src/GLOWAPI.API/appsettings.json'
        )
    },
    @{
        Message = 'test(usuario): adicionar testes de cadastro confirmacao e avatar base64'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Application/AvatarBase64DecoderTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ConfirmacaoEmailServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/UsuarioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AuthServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/API/ExceptionMiddlewareTests.cs',
            'tests/GLOWAPI.Tests/Integration/AuthControllerTests.cs'
        )
    },
    @{
        Message = 'docs(api): documentar fluxo de cadastro confirmacao e avatar base64'
        Paths   = @('README.md')
    },
    @{
        Message = 'chore(scripts): adicionar batch de commits cadastro cliente'
        Paths   = @(
            'scripts/commit-cadastro-cliente-feature.ps1',
            'scripts/commit-cadastro-cliente-feature.cmd'
        )
    }
)

foreach ($commit in $commits) {
    New-FeatureCommit -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ""
    Write-Host "=== Remaining uncommitted files ===" -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' 'context/' 'scripts/' 'README.md' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "=== Running dotnet test ===" -ForegroundColor Cyan
        dotnet test
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ""
    Write-Host "Done. $(($commits | Measure-Object).Count) commits planned/processed." -ForegroundColor Green
    Write-Host "Verify authors:" -ForegroundColor Cyan
    git log --oneline -n 12 --format="%h %an %ae %s"
}

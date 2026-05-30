#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for profissionais, equipe, acesso and multi-establishment work.

.DESCRIPTION
  Run this script directly in your own PowerShell terminal.
  Commits use ONLY your local git user.name / user.email - no Co-authored-by trailers.

  Usage:
    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\commit-profissionais-estabelecimento-acesso-feature.ps1

    scripts\commit-profissionais-estabelecimento-acesso-feature.cmd

  Options:
    -DryRun      Show what would be committed without creating commits
    -SkipBuild   Skip dotnet test after all commits
    -SkipBranch  Do not create/switch to the feature branch
    -BranchName  Override branch name. Default: feat/equipe-acesso-multiestabelecimento

  Note:
    Several tasks touched shared files like DependencyInjection.cs, ExceptionMiddleware.cs
    and EstabelecimentosController.cs. To keep this script safe to run once, commits are
    grouped by delivery blocks instead of interactive hunk staging.
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$SkipBranch,
    [string]$BranchName = 'feat/equipe-acesso-multiestabelecimento'
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

function Ensure-FeatureBranch {
    param([string]$Name)

    if ($SkipBranch) {
        Write-Host "Skipping branch switch/creation." -ForegroundColor Yellow
        return
    }

    $current = git branch --show-current
    if ($current -eq $Name) {
        Write-Host "Already on branch: $Name" -ForegroundColor Cyan
        return
    }

    $exists = git branch --list $Name
    if ($DryRun) {
        if ($exists) {
            Write-Host "DRY RUN - would switch to existing branch: $Name" -ForegroundColor Yellow
        }
        else {
            Write-Host "DRY RUN - would create and switch to branch: $Name" -ForegroundColor Yellow
        }
        return
    }

    if ($exists) {
        git switch $Name
    }
    else {
        git switch -c $Name
    }

    if ($LASTEXITCODE -ne 0) { throw "Could not switch/create branch: $Name" }
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
Write-Host "Target branch: $BranchName" -ForegroundColor Cyan
if ($DryRun) {
    Write-Host 'DRY RUN - no commits will be created' -ForegroundColor Yellow
}

Ensure-FeatureBranch -Name $BranchName

$commits = @(
    @{
        Message = 'docs: planejar acesso de profissionais no negocio'
        Paths   = @('docs/tasks-profissionais-estabelecimento-acesso.md')
    },
    @{
        Message = 'feat(access): definir matriz e autorizacao por negocio'
        Paths   = @(
            'src/GLOWAPI.Domain/Enums/PermissaoNegocio.cs',
            'src/GLOWAPI.Domain/Enums/EstablishmentUserRole.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/',
            'src/GLOWAPI.Application/Models/Autorizacao/',
            'src/GLOWAPI.Application/Interfaces/Services/IMatrizPermissaoNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAutorizacaoNegocioService.cs',
            'src/GLOWAPI.Application/Services/MatrizPermissaoNegocioService.cs',
            'src/GLOWAPI.Application/Services/AutorizacaoNegocioService.cs',
            'tests/GLOWAPI.Tests/Unit/Application/MatrizPermissaoNegocioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AutorizacaoNegocioServiceTests.cs'
        )
    },
    @{
        Message = 'feat(api): proteger rotas por permissao e modulo do negocio'
        Paths   = @(
            'src/GLOWAPI.API/Attributes/RequerPermissaoNegocioAttribute.cs',
            'src/GLOWAPI.API/Middlewares/PermissionMiddleware.cs',
            'tests/GLOWAPI.Tests/Unit/API/PermissionMiddlewareTests.cs'
        )
    },
    @{
        Message = 'feat(team): gerenciar usuarios e profissionais do negocio'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Equipe/',
            'src/GLOWAPI.Application/Interfaces/Services/IEquipeNegocioService.cs',
            'src/GLOWAPI.Application/Services/EquipeNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IUsuarioRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IEstabelecimentoUsuarioRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IProfissionalEstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/UsuarioRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/EstabelecimentoUsuarioRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ProfissionalEstabelecimentoRepository.cs',
            'tests/GLOWAPI.Tests/Unit/Application/EquipeNegocioServiceTests.cs'
        )
    },
    @{
        Message = 'feat(agenda): aplicar escopo profissional em agenda e atendimento'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Agenda/',
            'src/GLOWAPI.Application/Models/Agenda/',
            'src/GLOWAPI.Application/Interfaces/Services/IProfissionalEscopoAcessoService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAgendaNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAtendimentoProfissionalService.cs',
            'src/GLOWAPI.Application/Services/ProfissionalEscopoAcessoService.cs',
            'src/GLOWAPI.Application/Services/AgendaNegocioService.cs',
            'src/GLOWAPI.Application/Services/AtendimentoProfissionalService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoItemRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoItemRepository.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ProfissionalEscopoAcessoServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AgendaNegocioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AtendimentoProfissionalServiceTests.cs'
        )
    },
    @{
        Message = 'feat(finance): proteger caixa e lancamentos por perfil'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Caixa/',
            'src/GLOWAPI.Application/Models/Caixa/',
            'src/GLOWAPI.Application/Interfaces/Services/ICaixaNegocioService.cs',
            'src/GLOWAPI.Application/Services/CaixaNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/ICaixaRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/ILancamentoCaixaRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/CaixaRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/LancamentoCaixaRepository.cs',
            'tests/GLOWAPI.Tests/Unit/Application/CaixaNegocioServiceTests.cs'
        )
    },
    @{
        Message = 'feat(professionals): configurar servicos e horarios por profissional'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/',
            'src/GLOWAPI.Application/Interfaces/Services/IProfissionalServicoNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IHorarioProfissionalNegocioService.cs',
            'src/GLOWAPI.Application/Services/ProfissionalServicoNegocioService.cs',
            'src/GLOWAPI.Application/Services/HorarioProfissionalNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IProfissionalServicoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IHorarioAtendimentoProfissionalRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IHorarioFuncionamentoEstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ProfissionalServicoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/HorarioAtendimentoProfissionalRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/HorarioFuncionamentoEstabelecimentoRepository.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ProfissionalServicoNegocioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/HorarioProfissionalNegocioServiceTests.cs'
        )
    },
    @{
        Message = 'feat(audit): auditar acoes sensiveis do negocio'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/AuditoriaNegocio.cs',
            'src/GLOWAPI.Domain/Enums/TipoAcaoAuditoriaNegocio.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAuditoriaNegocioService.cs',
            'src/GLOWAPI.Application/Services/AuditoriaNegocioService.cs',
            'src/GLOWAPI.Infrastructure/Configurations/AuditoriaNegocioConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260529172737_AddAuditoriaNegocio.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260529172737_AddAuditoriaNegocio.Designer.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AuditoriaNegocioServiceTests.cs'
        )
    },
    @{
        Message = 'feat(team): notificar convites e alteracoes de acesso'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IEquipeNotificacaoService.cs',
            'src/GLOWAPI.Application/Services/EquipeNotificacaoService.cs',
            'tests/GLOWAPI.Tests/Unit/Application/EquipeNotificacaoServiceTests.cs'
        )
    },
    @{
        Message = 'feat(access): suportar contexto multiestabelecimento do usuario'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/UsuarioController.cs',
            'src/GLOWAPI.Application/DTOs/Usuario/EstabelecimentoAcessoResponseDto.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IUsuarioNegocioContextoService.cs',
            'src/GLOWAPI.Application/Services/UsuarioNegocioContextoService.cs',
            'tests/GLOWAPI.Tests/Unit/Application/UsuarioNegocioContextoServiceTests.cs'
        )
    },
    @{
        Message = 'feat(invites): adicionar convites seguros para profissionais'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/ConvitesController.cs',
            'src/GLOWAPI.Application/DTOs/Convites/',
            'src/GLOWAPI.Application/Interfaces/Repositories/IConviteNegocioRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IConviteNegocioService.cs',
            'src/GLOWAPI.Application/Services/ConviteNegocioService.cs',
            'src/GLOWAPI.Domain/Entities/ConviteNegocio.cs',
            'src/GLOWAPI.Domain/Enums/StatusConviteNegocio.cs',
            'src/GLOWAPI.Domain/Enums/TipoConviteNegocio.cs',
            'src/GLOWAPI.Infrastructure/Configurations/ConviteNegocioConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ConviteNegocioRepository.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260530003516_AddConvitesNegocio.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260530003516_AddConvitesNegocio.Designer.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ConviteNegocioServiceTests.cs'
        )
    },
    @{
        Message = 'feat(api): expor endpoints de equipe acesso e multiestabelecimento'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/EstabelecimentosController.cs',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs',
            'src/GLOWAPI.Infrastructure/ApplicationDbContext.cs',
            'src/GLOWAPI.Infrastructure/DependencyInjection.cs',
            'src/GLOWAPI.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs',
            'tests/GLOWAPI.Tests/Unit/API/EstabelecimentosControllerAcessoTests.cs',
            'tests/GLOWAPI.Tests/Integration/AcessoNegocioControllerTests.cs'
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
    Write-Host "Done. $(($commits | Measure-Object).Count) commits planned/processed." -ForegroundColor Green
    Write-Host "Verify authors:" -ForegroundColor Cyan
    git log --oneline -n 20 --format="%h %an %ae %s"
}

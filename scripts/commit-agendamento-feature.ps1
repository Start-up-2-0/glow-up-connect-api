#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the agendamento epic (docs/tasks-agendamento.md).

.DESCRIPTION
  Run in your own PowerShell terminal (not via double-click on .ps1).

  Usage:
    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    scripts\commit-agendamento-feature.cmd
    scripts\commit-agendamento-feature.cmd -DryRun

  Options:
    -DryRun      Show planned commits without creating them
    -SkipBuild   Skip dotnet test after all commits
    -SkipBranch  Do not create/switch feature branch
    -BranchName  Default: feat/agendamento

  Notes:
    - Commits alinhados ao plano do modulo de agendamento (backend).
    - Order: domain -> application -> infrastructure -> api -> testes -> docs -> scripts.
    - Sem Co-authored-by; usa apenas user.name/user.email locais.
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$SkipBranch,
    [string]$BranchName = 'feat/agendamento'
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
        Write-Host 'Skipping branch switch/creation.' -ForegroundColor Yellow
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
            Write-Host "DRY RUN - would switch to branch: $Name" -ForegroundColor Yellow
        }
        else {
            Write-Host "DRY RUN - would create branch: $Name" -ForegroundColor Yellow
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

    Write-Host ''
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

Write-Host ''
Write-Host "Repository: $RepoRoot" -ForegroundColor Cyan
Write-Host "Target branch: $BranchName" -ForegroundColor Cyan
if ($DryRun) {
    Write-Host 'DRY RUN - no commits will be created' -ForegroundColor Yellow
}

Ensure-FeatureBranch -Name $BranchName

$commits = @(
    @{
        Message = 'feat(domain): estender agendamento para visitante e novos status'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/Agendamento.cs',
            'src/GLOWAPI.Domain/Enums/AgendamentoStatus.cs'
        )
    },
    @{
        Message = 'feat(domain): adicionar entidade agendamento historico e origem'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/AgendamentoHistorico.cs',
            'src/GLOWAPI.Domain/Enums/OrigemAgendamento.cs',
            'src/GLOWAPI.Domain/Enums/TipoAcaoAuditoriaNegocio.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/AgendamentoDadosClienteInvalidosException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/AgendamentoNaoEncontradoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/AgendamentoServicosInvalidosException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/AgendamentoStatusInvalidoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/HorarioIndisponivelException.cs'
        )
    },
    @{
        Message = 'chore(database): migration agendamento visitante e historico'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260530190322_ExtendAgendamentoVisitanteHistorico.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260530190322_ExtendAgendamentoVisitanteHistorico.Designer.cs',
            'src/GLOWAPI.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs',
            'src/GLOWAPI.Infrastructure/Configurations/AgendamentoConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Configurations/AgendamentoHistoricoConfiguration.cs',
            'src/GLOWAPI.Infrastructure/ApplicationDbContext.cs',
            'src/GLOWAPI.Infrastructure/DependencyInjection.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoHistoricoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoHistoricoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IProfissionalEstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ProfissionalEstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoItemRepository.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar agendamento validador e excecoes'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IAgendamentoValidador.cs',
            'src/GLOWAPI.Application/Validators/AgendamentoValidador.cs',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs'
        )
    },
    @{
        Message = 'refactor(application): estender disponibilidade para multiplos servicos'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/ConsultarDisponibilidadeAgendaDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/DisponibilidadeAgendaResponseDto.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IDisponibilidadeAgendaService.cs',
            'src/GLOWAPI.Application/Services/DisponibilidadeAgendaService.cs'
        )
    },
    @{
        Message = 'feat(application): estender links publicos profissional e servicos filtrados'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IServicoNegocioService.cs',
            'src/GLOWAPI.Application/Services/ServicoNegocioService.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar agendamento negocio service'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IAgendamentoNegocioService.cs',
            'src/GLOWAPI.Application/Services/AgendamentoNegocioService.cs',
            'src/GLOWAPI.Application/Models/Agendamento/',
            'src/GLOWAPI.Application/DTOs/Agendamento/'
        )
    },
    @{
        Message = 'feat(application): adicionar agendamento notificacao service'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IAgendamentoNotificacaoService.cs',
            'src/GLOWAPI.Application/Services/AgendamentoNotificacaoService.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(api): endpoints publicos de criacao e profissionais'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/AgendamentoPublicoController.cs'
        )
    },
    @{
        Message = 'feat(api): endpoints autenticados de agendamento'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/AgendamentosController.cs'
        )
    },
    @{
        Message = 'feat(api): endpoints de confirmacao cancelamento e remarcacao'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/EstabelecimentosController.cs',
            'src/GLOWAPI.Application/DTOs/Agenda/AgendaGeralResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Agenda/AgendaProfissionalResponseDto.cs'
        )
    },
    @{
        Message = 'test(agendamento): adicionar testes unitarios e integrados'
        Paths   = @(
            'tests/GLOWAPI.Tests/Integration/AgendamentoIntegracaoTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AgendamentoNegocioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AgendamentoNotificacaoServiceTests.cs'
        )
    },
    @{
        Message = 'docs(agendamento): adicionar tasks-agendamento.md'
        Paths   = @('docs/tasks-agendamento.md')
    },
    @{
        Message = 'chore(scripts): adicionar script de commits semanticos de agendamento'
        Paths   = @(
            'scripts/commit-agendamento-feature.cmd',
            'scripts/commit-agendamento-feature.ps1'
        )
    }
)

foreach ($commit in $commits) {
    New-FeatureCommit -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ''
    Write-Host '=== Remaining uncommitted files ===' -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' 'docs/' 'scripts/' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ''
        Write-Host '=== Running dotnet test (filter Agendamento) ===' -ForegroundColor Cyan
        dotnet test tests/GLOWAPI.Tests/GLOWAPI.Tests.csproj --filter 'FullyQualifiedName~Agendamento'
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ''
    Write-Host "Done. $(($commits | Measure-Object).Count) commits processed." -ForegroundColor Green
    Write-Host 'Verify authors:' -ForegroundColor Cyan
    git log --oneline -n 20 --format='%h %an %s'
}

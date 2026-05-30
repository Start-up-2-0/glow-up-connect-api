#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the horarios de atendimento epic (tasks 1-19).

.DESCRIPTION
  Run in your own PowerShell terminal (not via double-click on .ps1).

  Usage:
    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\commit-horarios-atendimento-feature.ps1

    scripts\commit-horarios-atendimento-feature.cmd
    scripts\commit-horarios-atendimento-feature.cmd -DryRun

  Options:
    -DryRun      Show planned commits without creating them
    -SkipBuild   Skip dotnet test after all commits
    -SkipBranch  Do not create/switch feature branch
    -BranchName  Default: feat/horarios-atendimento

  Notes:
    - Commits alinhados a docs/tasks-horarios-atendimento.md (tasks 1-19).
    - Tasks 3 e 7 (edicao isolada) estao agrupadas nas tasks 4 e 8 (CRUD completo).
    - Cada arquivo entra em um unico commit; arquivos compartilhados vao na task que
      fecha o bloco (ex.: services em 4/8, API em 12/17/19).
    - Order: domain -> application -> infrastructure -> api -> tests.
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$SkipBranch,
    [string]$BranchName = 'feat/horarios-atendimento'
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
        [int]$TaskNumber,
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
        Write-Warning "Task $TaskNumber - skip (no files): $Message"
        return
    }

    Write-Host ''
    Write-Host "==> [Task $TaskNumber] $Message" -ForegroundColor Green
    $existing | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

    if ($DryRun) {
        return
    }

    git add -- $existing
    if ($LASTEXITCODE -ne 0) { throw "git add failed for task $TaskNumber" }

    $staged = git diff --cached --name-only
    if (-not $staged) {
        Write-Warning "Task $TaskNumber - nothing staged: $Message"
        return
    }

    Assert-NoForbiddenPaths -Paths $staged

    $env:GIT_AUTHOR_NAME = $GitUser.Name
    $env:GIT_AUTHOR_EMAIL = $GitUser.Email
    $env:GIT_COMMITTER_NAME = $GitUser.Name
    $env:GIT_COMMITTER_EMAIL = $GitUser.Email

    git commit -m $Message --no-signoff
    if ($LASTEXITCODE -ne 0) { throw "git commit failed for task $TaskNumber" }

    $body = git log -1 --format=%B
    if ($body -match '(?i)co-authored-by:\s*cursor') {
        throw "Cursor co-author in task $TaskNumber commit. Run: git reset --soft HEAD~1"
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
        Task    = 1
        Message = 'feat(horarios): cadastrar horario de funcionamento do estabelecimento'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/CriarHorarioFuncionamentoRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/HorarioFuncionamentoResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/HorarioFuncionamentoFiltroDto.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/HorarioFuncionamentoNaoEncontradoException.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IHorarioFuncionamentoNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IHorarioFuncionamentoEstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/HorarioFuncionamentoEstabelecimentoRepository.cs'
        )
    },
    @{
        Task    = 2
        Message = 'feat(horarios): listar horarios de funcionamento do estabelecimento'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Infrastructure/HorarioFuncionamentoEstabelecimentoRepositoryTests.cs'
        )
    },
    @{
        Task    = 4
        Message = 'feat(horarios): crud e status de horario de funcionamento'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/AtualizarHorarioFuncionamentoRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/AtualizarStatusHorarioFuncionamentoRequestDto.cs',
            'src/GLOWAPI.Application/Services/HorarioFuncionamentoNegocioService.cs',
            'tests/GLOWAPI.Tests/Unit/Application/HorarioFuncionamentoNegocioServiceTests.cs'
        )
    },
    @{
        Task    = 5
        Message = 'feat(horarios): cadastrar horario de profissional no estabelecimento'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/CriarHorarioProfissionalRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/HorarioProfissionalResponseDto.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IHorarioProfissionalNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IHorarioAtendimentoProfissionalRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/HorarioAtendimentoProfissionalRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ProfissionalEstabelecimentoRepository.cs'
        )
    },
    @{
        Task    = 6
        Message = 'feat(horarios): listar horarios de profissional no estabelecimento'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/HorarioProfissionalFiltroDto.cs'
        )
    },
    @{
        Task    = 8
        Message = 'feat(horarios): crud e status de horario de profissional no estabelecimento'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/AtualizarHorarioProfissionalRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/AtualizarStatusHorarioProfissionalRequestDto.cs',
            'src/GLOWAPI.Application/Services/HorarioProfissionalNegocioService.cs',
            'tests/GLOWAPI.Tests/Unit/Application/HorarioProfissionalNegocioServiceTests.cs'
        )
    },
    @{
        Task    = 9
        Message = 'feat(horarios): cadastrar horario de profissional autonomo'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IHorarioProfissionalAutonomoService.cs',
            'src/GLOWAPI.Application/Services/HorarioProfissionalAutonomoService.cs'
        )
    },
    @{
        Task    = 10
        Message = 'feat(horarios): listar horarios de profissional autonomo'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/HorarioProfissionalAutonomoFiltroDto.cs'
        )
    },
    @{
        Task    = 11
        Message = 'feat(horarios): editar horario de profissional autonomo'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Application/HorarioProfissionalAutonomoServiceTests.cs'
        )
    },
    @{
        Task    = 12
        Message = 'feat(api): expor ativacao de horario de profissional autonomo'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/ProfissionaisAutonomosController.cs'
        )
    },
    @{
        Task    = 13
        Message = 'feat(horarios): validar conflitos entre horarios ativos'
        Paths   = @(
            'src/GLOWAPI.Application/Validators/HorarioIntervaloValidador.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/HorarioAtendimentoConflitanteException.cs',
            'tests/GLOWAPI.Tests/Unit/Application/HorarioIntervaloValidadorTests.cs'
        )
    },
    @{
        Task    = 14
        Message = 'feat(horarios): bloquear alteracao com agendamentos futuros impactados'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/AgendamentoFuturoImpactadoDto.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/HorarioAlteracaoImpactaAgendamentosFuturosException.cs',
            'src/GLOWAPI.Application/Validators/HorarioAgendamentoImpactoValidador.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoItemRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoItemRepository.cs',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs',
            'src/GLOWAPI.API/Models/ApiResponse.cs'
        )
    },
    @{
        Task    = 15
        Message = 'feat(horarios): calcular disponibilidade de agenda por slots'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Horarios/ConsultarDisponibilidadeAgendaDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/DisponibilidadeAgendaResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/SlotDisponivelResponseDto.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IDisponibilidadeAgendaService.cs',
            'src/GLOWAPI.Application/Services/DisponibilidadeAgendaService.cs',
            'src/GLOWAPI.Application/Helpers/GeradorSlotsDisponibilidade.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IServicoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ServicoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IProfissionalServicoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ProfissionalServicoRepository.cs'
        )
    },
    @{
        Task    = 16
        Message = 'feat(horarios): aplicar permissoes de visualizar e gerenciar horarios'
        Paths   = @(
            'src/GLOWAPI.Domain/Enums/PermissaoNegocio.cs',
            'src/GLOWAPI.Application/Services/MatrizPermissaoNegocioService.cs'
        )
    },
    @{
        Task    = 17
        Message = 'feat(api): expor disponibilidade publica para marketplace'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/AgendamentoPublicoController.cs'
        )
    },
    @{
        Task    = 18
        Message = 'feat(horarios): auditar alteracoes de horarios do negocio'
        Paths   = @(
            'src/GLOWAPI.Domain/Enums/TipoAcaoAuditoriaNegocio.cs'
        )
    },
    @{
        Task    = 19
        Message = 'test(horarios): cobrir fluxos integrados e complementos de API'
        Paths   = @(
            'src/GLOWAPI.Application/DependencyInjection.cs',
            'src/GLOWAPI.API/Controllers/EstabelecimentosController.cs',
            'tests/GLOWAPI.Tests/Integration/HorariosAtendimentoIntegracaoTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/DisponibilidadeAgendaServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Infrastructure/AgendamentoItemRepositoryTests.cs',
            'tests/GLOWAPI.Tests/Unit/Infrastructure/HorarioAtendimentoProfissionalRepositoryTests.cs',
            'tests/GLOWAPI.Tests/Unit/API/EstabelecimentosControllerAcessoTests.cs'
        )
    }
)

foreach ($commit in $commits) {
    if (-not $commit.Paths -or $commit.Paths.Count -eq 0) {
        Write-Warning "Task $($commit.Task) - skip (sem arquivos exclusivos; coberto na task 4 ou 8): $($commit.Message)"
        continue
    }

    New-FeatureCommit -TaskNumber $commit.Task -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ''
    Write-Host '=== Remaining uncommitted files ===' -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' 'docs/' 'scripts/' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ''
        Write-Host '=== Running dotnet test ===' -ForegroundColor Cyan
        dotnet test
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ''
    Write-Host "Done. $($commits.Count) task commits processed." -ForegroundColor Green
    Write-Host 'Verify authors:' -ForegroundColor Cyan
    git log --oneline -n 25 --format='%h %an %s'
}

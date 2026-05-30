#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for the servicos epic (tasks 1-17).

.DESCRIPTION
  Run in your own PowerShell terminal (not via double-click on .ps1).

  Usage:
    scripts\commit-servicos-feature.cmd
    scripts\commit-servicos-feature.cmd -DryRun

  Options:
    -DryRun      Show planned commits without creating them
    -SkipBuild   Skip dotnet test after all commits
    -SkipBranch  Do not create/switch feature branch
    -BranchName  Default: feat/servicos
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$SkipBranch,
    [string]$BranchName = 'feat/servicos'
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

if (-not $SkipBranch -and -not $DryRun) {
    $currentBranch = git branch --show-current
    if ($currentBranch -ne $BranchName) {
        git checkout -B $BranchName
        if ($LASTEXITCODE -ne 0) { throw "Failed to checkout branch $BranchName" }
    }
}

$commits = @(
    @{
        Message = 'feat(domain): adicionar excecoes e acoes de auditoria de servicos'
        Paths   = @(
            'src/GLOWAPI.Domain/Exceptions/Negocios/ServicoNegocioInvalidoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/LimiteServicosNegocioExcedidoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/ProfissionalServicoComAgendamentoFuturoException.cs',
            'src/GLOWAPI.Domain/Enums/TipoAcaoAuditoriaNegocio.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar validador e dtos de servico'
        Paths   = @(
            'src/GLOWAPI.Application/Validators/ServicoValidador.cs',
            'src/GLOWAPI.Application/DTOs/Servicos/',
            'src/GLOWAPI.Application/DTOs/Equipe/AtualizarProfissionalServicoRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Equipe/AtualizarStatusProfissionalServicoRequestDto.cs'
        )
    },
    @{
        Message = 'feat(application): implementar servico de negocio de servicos'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IServicoNegocioService.cs',
            'src/GLOWAPI.Application/Services/ServicoNegocioService.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar facade de servicos do profissional autonomo'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Services/IServicoProfissionalAutonomoService.cs',
            'src/GLOWAPI.Application/Services/ServicoProfissionalAutonomoService.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar edicao desvinculo e precificacao efetiva de servicos'
        Paths   = @(
            'src/GLOWAPI.Application/Helpers/ServicoPrecificacaoHelper.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IProfissionalServicoNegocioService.cs',
            'src/GLOWAPI.Application/Services/ProfissionalServicoNegocioService.cs',
            'src/GLOWAPI.Application/Services/DisponibilidadeAgendaService.cs',
            'src/GLOWAPI.Application/DTOs/Horarios/DisponibilidadeAgendaResponseDto.cs'
        )
    },
    @{
        Message = 'feat(infrastructure): estender repositorios de servicos e agendamento'
        Paths   = @(
            'src/GLOWAPI.Application/Interfaces/Repositories/IServicoRepository.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoItemRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/ServicoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoItemRepository.cs'
        )
    },
    @{
        Message = 'feat(api): adicionar endpoints de servicos do estabelecimento e autonomo'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/EstabelecimentosController.cs',
            'src/GLOWAPI.API/Controllers/ProfissionaisAutonomosController.cs',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs'
        )
    },
    @{
        Message = 'feat(api): adicionar endpoints publicos de servicos'
        Paths   = @('src/GLOWAPI.API/Controllers/AgendamentoPublicoController.cs')
    },
    @{
        Message = 'test(servicos): adicionar testes unitarios do core de servicos'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Application/ServicoValidadorTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ServicoNegocioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ServicoProfissionalAutonomoServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ServicoPrecificacaoHelperTests.cs'
        )
    },
    @{
        Message = 'test(servicos): adicionar testes de vinculo disponibilidade e permissao'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Application/ProfissionalServicoNegocioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/DisponibilidadeAgendaServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/API/EstabelecimentosControllerAcessoTests.cs'
        )
    },
    @{
        Message = 'test(servicos): adicionar testes integrados do modulo de servicos'
        Paths   = @('tests/GLOWAPI.Tests/Integration/ServicosIntegracaoTests.cs')
    },
    @{
        Message = 'docs(servicos): atualizar estado da implementacao das tasks'
        Paths   = @('docs/tasks-servicos.md')
    },
    @{
        Message = 'chore(scripts): adicionar script de commits semanticos de servicos'
        Paths   = @(
            'scripts/commit-servicos-feature.cmd',
            'scripts/commit-servicos-feature.ps1'
        )
    }
)

foreach ($commit in $commits) {
    New-FeatureCommit -Message $commit.Message -Paths $commit.Paths -GitUser $gitUser
}

if (-not $DryRun) {
    Write-Host ""
    Write-Host "=== Remaining uncommitted files ===" -ForegroundColor Cyan
    git status --short -- 'src/' 'tests/' 'docs/' 'scripts/' | Where-Object { $_ -notmatch '\\bin\\|\\obj\\' }

    if (-not $SkipBuild) {
        Write-Host ""
        Write-Host "=== Running dotnet test (filter Servico) ===" -ForegroundColor Cyan
        dotnet test tests/GLOWAPI.Tests/GLOWAPI.Tests.csproj --filter "FullyQualifiedName~Servico"
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ""
    Write-Host "Done. $(($commits | Measure-Object).Count) commits planned/processed." -ForegroundColor Green
    Write-Host "Verify authors:" -ForegroundColor Cyan
    git log --oneline -n 20 --format="%h %an %ae %s"
}

#Requires -Version 5.1
<#
.SYNOPSIS
  Creates semantic commits for area cliente epics (geolocalizacao, agendamento logado, WhatsApp).

.DESCRIPTION
  Run in your own PowerShell terminal (not via double-click on .ps1).

  Usage:
    cd C:\Users\James\Documents\programacao\glow-up-connect-api
    scripts\commit-area-cliente-feature.cmd
    scripts\commit-area-cliente-feature.cmd -DryRun

  Options:
    -DryRun      Show planned commits without creating them
    -SkipBuild   Skip dotnet test after all commits
    -SkipBranch  Do not create/switch feature branch
    -BranchName  Default: feat/area-cliente

  Order: domain -> database -> application -> infrastructure -> api -> testes -> docs -> scripts
#>
[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$SkipBuild,
    [switch]$SkipBranch,
    [string]$BranchName = 'feat/area-cliente'
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
        Message = 'feat(domain): adicionar geolocalizacao em endereco e excecoes'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/Endereco.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/EnderecoOperacaoInvalidoException.cs',
            'src/GLOWAPI.Domain/Exceptions/Negocios/LocalizacaoClienteInvalidaException.cs'
        )
    },
    @{
        Message = 'feat(domain): adicionar confirmacao whatsapp em usuario e estabelecimento'
        Paths   = @(
            'src/GLOWAPI.Domain/Entities/Usuario.cs',
            'src/GLOWAPI.Domain/Entities/Estabelecimento.cs',
            'src/GLOWAPI.Domain/Entities/WhatsAppConfirmacaoEntidade.cs',
            'src/GLOWAPI.Domain/Exceptions/Usuario/ConfirmacaoWhatsAppInvalidaException.cs'
        )
    },
    @{
        Message = 'chore(database): adicionar migration geolocalizacao endereco'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260531001753_AddEnderecoGeolocalizacao.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260531001753_AddEnderecoGeolocalizacao.Designer.cs',
            'src/GLOWAPI.Infrastructure/Configurations/EnderecoConfiguration.cs'
        )
    },
    @{
        Message = 'chore(database): adicionar migration confirmacao whatsapp usuario'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260531003748_AddUsuarioConfirmacaoWhatsApp.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260531003748_AddUsuarioConfirmacaoWhatsApp.Designer.cs',
            'src/GLOWAPI.Infrastructure/Configurations/UsuarioConfiguration.cs'
        )
    },
    @{
        Message = 'chore(database): adicionar migration confirmacao whatsapp estabelecimento'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Migrations/20260531005111_AddEstabelecimentoConfirmacaoWhatsApp.cs',
            'src/GLOWAPI.Infrastructure/Migrations/20260531005111_AddEstabelecimentoConfirmacaoWhatsApp.Designer.cs',
            'src/GLOWAPI.Infrastructure/Configurations/EstabelecimentoConfiguration.cs',
            'src/GLOWAPI.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar geocodificacao e descoberta de estabelecimentos'
        Paths   = @(
            'src/GLOWAPI.Application/Helpers/GeolocalizacaoHelper.cs',
            'src/GLOWAPI.Application/Options/GeocodificacaoOptions.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IEnderecoGeocodificacaoService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IEstabelecimentoDescobertaService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IGeocodificadorService.cs',
            'src/GLOWAPI.Application/Services/EnderecoGeocodificacaoService.cs',
            'src/GLOWAPI.Application/Services/EstabelecimentoDescobertaService.cs',
            'src/GLOWAPI.Application/Models/Geolocalizacao/',
            'src/GLOWAPI.Application/DTOs/Estabelecimentos/EstabelecimentoProximoResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Estabelecimentos/EstabelecimentoPublicoResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Operacoes/EnderecoOperacaoDto.cs',
            'src/GLOWAPI.Application/DTOs/Operacoes/EnderecoOperacaoResponseDto.cs',
            'src/GLOWAPI.Application/Services/OperacaoPerfilValidation.cs',
            'src/GLOWAPI.Application/Services/AssinaturaService.cs',
            'src/GLOWAPI.Application/GLOWAPI.Application.csproj'
        )
    },
    @{
        Message = 'feat(infrastructure): adicionar geocodificador nominatim e repositorio proximos'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Geolocalizacao/',
            'src/GLOWAPI.Application/Interfaces/Repositories/IEstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/EstabelecimentoRepository.cs',
            'src/GLOWAPI.Infrastructure/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar area cliente agendamento enriquecida'
        Paths   = @(
            'src/GLOWAPI.Application/DTOs/Agendamento/AgendamentoClienteFiltroDto.cs',
            'src/GLOWAPI.Application/DTOs/Agendamento/AgendamentoClienteResponseDto.cs',
            'src/GLOWAPI.Application/Models/Agendamento/AgendamentoClienteFiltro.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IAgendamentoNegocioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IAgendamentoRepository.cs',
            'src/GLOWAPI.Application/Services/AgendamentoNegocioService.cs',
            'src/GLOWAPI.Infrastructure/Repositories/AgendamentoRepository.cs'
        )
    },
    @{
        Message = 'feat(application): adicionar confirmacao whatsapp inbound email e alertas'
        Paths   = @(
            'src/GLOWAPI.Application/Helpers/TelefoneHelper.cs',
            'src/GLOWAPI.Application/Helpers/ConfirmacaoWhatsAppCodigoHelper.cs',
            'src/GLOWAPI.Application/Helpers/EmailDestinoHelper.cs',
            'src/GLOWAPI.Application/Helpers/EvolutionWebhookParser.cs',
            'src/GLOWAPI.Application/Options/MensageriaWhatsAppOptions.cs',
            'src/GLOWAPI.Application/Options/AuthOptions.cs',
            'src/GLOWAPI.Application/DTOs/Mensageria/WhatsAppConfirmacaoInstrucoesDto.cs',
            'src/GLOWAPI.Application/DTOs/Auth/ConfirmarWhatsAppRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Auth/ReenviarConfirmacaoWhatsAppRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Estabelecimentos/ConfirmarWhatsAppEstabelecimentoRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Usuario/WhatsAppOptInRequestDto.cs',
            'src/GLOWAPI.Application/DTOs/Estabelecimentos/EstabelecimentoPerfilResponseDto.cs',
            'src/GLOWAPI.Application/DTOs/Usuario/UsuarioResponseDto.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IConfirmacaoWhatsAppService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IConfirmacaoWhatsAppEstabelecimentoService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IWebhookWhatsAppService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IEstabelecimentoPerfilService.cs',
            'src/GLOWAPI.Application/Interfaces/Services/IUsuarioService.cs',
            'src/GLOWAPI.Application/Mensageria/ConfirmacaoWhatsAppTemplate.cs',
            'src/GLOWAPI.Application/Mensageria/ConfirmacaoWhatsAppEmailTemplate.cs',
            'src/GLOWAPI.Application/Mensageria/ConfirmacaoWhatsAppInstrucoesBuilder.cs',
            'src/GLOWAPI.Application/Mensageria/AgendamentoClienteWhatsAppTemplate.cs',
            'src/GLOWAPI.Application/Models/Mensageria/WhatsAppConfirmacaoInboundResultado.cs',
            'src/GLOWAPI.Application/Services/ConfirmacaoWhatsAppService.cs',
            'src/GLOWAPI.Application/Services/ConfirmacaoWhatsAppEstabelecimentoService.cs',
            'src/GLOWAPI.Application/Services/WebhookWhatsAppService.cs',
            'src/GLOWAPI.Application/Services/AgendamentoNotificacaoService.cs',
            'src/GLOWAPI.Application/Services/EstabelecimentoPerfilService.cs',
            'src/GLOWAPI.Application/Services/ProfissionalAutonomoPerfilService.cs',
            'src/GLOWAPI.Application/Services/UsuarioService.cs',
            'src/GLOWAPI.Application/Interfaces/Repositories/IUsuarioRepository.cs',
            'src/GLOWAPI.Infrastructure/Repositories/UsuarioRepository.cs',
            'src/GLOWAPI.Application/DependencyInjection.cs'
        )
    },
    @{
        Message = 'feat(infrastructure): integrar evolution api whatsapp'
        Paths   = @(
            'src/GLOWAPI.Infrastructure/Mensageria/Provedores/ProvedorMensagemWhatsApp.cs'
        )
    },
    @{
        Message = 'feat(api): endpoints publicos estabelecimentos e agendamento cliente'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/EstabelecimentosPublicosController.cs',
            'src/GLOWAPI.API/Controllers/AgendamentosController.cs'
        )
    },
    @{
        Message = 'feat(auth): endpoints confirmacao whatsapp webhook e opt-in'
        Paths   = @(
            'src/GLOWAPI.API/Controllers/AuthController.cs',
            'src/GLOWAPI.API/Controllers/UsuarioController.cs',
            'src/GLOWAPI.API/Controllers/EstabelecimentosController.cs',
            'src/GLOWAPI.API/Controllers/WebhooksWhatsAppController.cs',
            'src/GLOWAPI.API/Middlewares/ExceptionMiddleware.cs'
        )
    },
    @{
        Message = 'feat(api): configurar mensageria whatsapp e geocodificacao'
        Paths   = @(
            'src/GLOWAPI.API/Program.cs',
            'src/GLOWAPI.API/Configuration/HostedConfigurationValidator.cs',
            'src/GLOWAPI.API/appsettings.json'
        )
    },
    @{
        Message = 'test(application): adicionar testes geolocalizacao e endereco'
        Paths   = @(
            'tests/GLOWAPI.Tests/Helpers/EnderecoOperacaoDtoBuilder.cs',
            'tests/GLOWAPI.Tests/Unit/Application/GeolocalizacaoHelperTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/EnderecoGeocodificacaoServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/EstabelecimentoDescobertaServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/OperacaoPerfilValidationTests.cs',
            'tests/GLOWAPI.Tests/Unit/Infrastructure/NominatimGeocodificadorClientTests.cs',
            'tests/GLOWAPI.Tests/Integration/EstabelecimentosPublicosControllerTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AssinaturaServiceTests.cs',
            'tests/GLOWAPI.Tests/Integration/AssinaturasControllerTests.cs',
            'tests/GLOWAPI.Tests/Integration/FluxoIntegradoAssinaturaTests.cs'
        )
    },
    @{
        Message = 'test(application): adicionar testes area cliente agendamento'
        Paths   = @(
            'tests/GLOWAPI.Tests/Integration/AgendamentoClienteIntegracaoTests.cs'
        )
    },
    @{
        Message = 'test(auth): adicionar testes confirmacao whatsapp e evolution'
        Paths   = @(
            'tests/GLOWAPI.Tests/Unit/Application/ConfirmacaoWhatsAppServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ConfirmacaoWhatsAppEstabelecimentoServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ConfirmacaoWhatsAppEmailTemplateTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/WebhookWhatsAppServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/TelefoneHelperTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/AgendamentoNotificacaoServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/EstabelecimentoPerfilServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/ProfissionalAutonomoPerfilServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Application/UsuarioServiceTests.cs',
            'tests/GLOWAPI.Tests/Unit/Infrastructure/ProvedorMensagemWhatsAppTests.cs',
            'tests/GLOWAPI.Tests/Integration/GlowApiWebApplicationFactory.cs'
        )
    },
    @{
        Message = 'docs(frontend): adicionar guias area cliente geolocalizacao e whatsapp'
        Paths   = @(
            'docs/frontend/',
            'docs/frontend-auth.md'
        )
    },
    @{
        Message = 'chore(scripts): adicionar script commits semanticos area cliente'
        Paths   = @(
            'scripts/commit-area-cliente-feature.cmd',
            'scripts/commit-area-cliente-feature.ps1'
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
        Write-Host '=== Running dotnet test ===' -ForegroundColor Cyan
        dotnet test GLOWAPI.sln
        if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed after commits.' }
    }

    Write-Host ''
    Write-Host "Done. $(($commits | Measure-Object).Count) commits processed." -ForegroundColor Green
    Write-Host 'Verify authors:' -ForegroundColor Cyan
    git log --oneline -n 25 --format='%h %an %s'
}

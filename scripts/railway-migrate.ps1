param(
    [string]$Environment = "staging",
    [string]$Service = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    if (-not (Get-Command railway -ErrorAction SilentlyContinue)) {
        throw "Railway CLI nao encontrado. Instale: npm i -g @railway/cli"
    }

    railway environment $Environment

    $runArgs = @("run")
    if ($Service) {
        $runArgs += @("--service", $Service)
    }
    $runArgs += @(
        "dotnet", "ef", "database", "update",
        "--project", "src/GLOWAPI.Infrastructure",
        "--startup-project", "src/GLOWAPI.API"
    )

    & railway @runArgs
}
finally {
    Pop-Location
}

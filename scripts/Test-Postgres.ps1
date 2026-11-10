[CmdletBinding()]
param([switch]$Coverage)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$connectionString = $env:ConnectionStrings__GarupaDb
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    [xml]$project = Get-Content -Raw -LiteralPath (Join-Path $projectRoot 'Garupa API\Garupa API.csproj')
    $secretsId = [string]$project.Project.PropertyGroup.UserSecretsId
    $secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretsId\secrets.json"
    if (Test-Path -LiteralPath $secretsPath) {
        $secrets = Get-Content -Raw -LiteralPath $secretsPath | ConvertFrom-Json
        $connectionString = $secrets.'ConnectionStrings:GarupaDb'
        if (-not $connectionString -and $secrets.ConnectionStrings) {
            $connectionString = $secrets.ConnectionStrings.GarupaDb
        }
    }
}
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw 'Configure ConnectionStrings__GarupaDb ou o User Secret ConnectionStrings:GarupaDb.'
}

$previousTestConnection = $env:GARUPA_TEST_POSTGRES
try {
    $env:GARUPA_TEST_POSTGRES = $connectionString
    $coverageArguments = @()
    if ($Coverage) {
        $coverageArguments = @('--collect:XPlat Code Coverage', '--results-directory', (Join-Path $projectRoot '.local\coverage-postgres'))
    }
    & dotnet test (Join-Path $projectRoot 'Garupa API.sln') `
        --artifacts-path (Join-Path $projectRoot '.local\postgres-tests') `
        --no-restore --logger 'console;verbosity=minimal' @coverageArguments
    if ($LASTEXITCODE -ne 0) { throw 'A validacao com PostgreSQL falhou.' }
}
finally {
    $env:GARUPA_TEST_POSTGRES = $previousTestConnection
}

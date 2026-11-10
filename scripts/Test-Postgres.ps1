[CmdletBinding()]
param([switch]$Coverage)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Use-DotNet.ps1')
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

$taskSettings = New-Object System.Data.Common.DbConnectionStringBuilder
$taskSettings.set_ConnectionString($connectionString)
if ($taskSettings.get_Item('Host') -notin @('127.0.0.1', 'localhost') -or $taskSettings.get_Item('Database') -ne 'garupa_sp') {
    throw 'Os testes exigem o banco local garupa_sp.'
}
$taskPort = 5432
if ($taskSettings.ContainsKey('Port')) { $taskPort = [int]$taskSettings.get_Item('Port') }
$taskProbe = New-Object System.Net.Sockets.TcpClient
try {
    if (-not $taskProbe.ConnectAsync([string]$taskSettings.get_Item('Host'), $taskPort).Wait(5000)) {
        throw 'Tempo limite ao conectar.'
    }
}
catch {
    throw 'PostgreSQL indisponivel. Inicie scripts/Start-LocalDatabase.ps1 antes da validacao.'
}
finally {
    $taskProbe.Dispose()
}

$previousTestConnection = $env:GARUPA_TEST_POSTGRES
try {
    $env:GARUPA_TEST_POSTGRES = $connectionString
    $coverageArguments = @()
    if ($Coverage) {
        $coverageArguments = @('--collect:XPlat Code Coverage', '--results-directory', (Join-Path $projectRoot '.local\coverage-postgres'),
            '--filter', 'FullyQualifiedName!~DesempenhoTests')
    }
    & dotnet test (Join-Path $projectRoot 'Garupa API.sln') `
        --artifacts-path (Join-Path $projectRoot '.local\postgres-tests') `
        --no-restore -m:1 --disable-build-servers --logger 'console;verbosity=minimal' @coverageArguments
    if ($LASTEXITCODE -ne 0) { throw 'A validacao com PostgreSQL falhou.' }
    if ($Coverage) {
        # O benchmark roda sem instrumentação para medir a aplicação sem o custo do coletor.
        & dotnet test (Join-Path $projectRoot 'Garupa.Tests\Garupa.Tests.csproj') `
            --artifacts-path (Join-Path $projectRoot '.local\postgres-tests') `
            --no-build --no-restore --filter 'FullyQualifiedName~DesempenhoTests' --logger 'console;verbosity=minimal'
        if ($LASTEXITCODE -ne 0) { throw 'O benchmark com PostgreSQL falhou.' }
    }
}
finally {
    $env:GARUPA_TEST_POSTGRES = $previousTestConnection
}

[CmdletBinding()]
param(
    [string]$PostgresBin = 'C:\Program Files\PostgreSQL\17\bin'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dataDirectory = Join-Path $projectRoot '.local\postgres'
$logPath = Join-Path $projectRoot '.local\postgres.log'

foreach ($executable in @('initdb.exe', 'pg_ctl.exe', 'psql.exe', 'pg_isready.exe')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PostgresBin $executable))) {
        throw "Executavel PostgreSQL ausente: $executable. Informe -PostgresBin."
    }
}

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

$connection = New-Object System.Data.Common.DbConnectionStringBuilder
$connection.set_ConnectionString($connectionString)
if ($connection.get_Item('Host') -notin @('localhost', '127.0.0.1') -or
    $connection.get_Item('Port') -ne '5433' -or
    $connection.get_Item('Database') -ne 'garupa_sp') {
    throw 'Este script exige Host=127.0.0.1 ou localhost, Port=5433 e Database=garupa_sp.'
}
$databaseUser = [string]$connection.get_Item('Username')
$databasePassword = [string]$connection.get_Item('Password')
if ([string]::IsNullOrWhiteSpace($databaseUser) -or [string]::IsNullOrWhiteSpace($databasePassword)) {
    throw 'A conexao deve incluir Username e Password.'
}

New-Item -ItemType Directory -Path (Split-Path -Parent $dataDirectory) -Force | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $dataDirectory 'PG_VERSION'))) {
    $probe = New-Object System.Net.Sockets.TcpClient
    try {
        if ($probe.ConnectAsync('127.0.0.1', 5433).Wait(1000)) {
            throw 'A porta 5433 ja esta ocupada. Verifique a instancia existente.'
        }
    }
    catch [System.AggregateException] {
        # Conexao recusada significa que a porta esta livre.
    }
    finally {
        $probe.Dispose()
    }

    $passwordFile = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText($passwordFile, $databasePassword, (New-Object System.Text.UTF8Encoding($false)))
        & (Join-Path $PostgresBin 'initdb.exe') -D $dataDirectory -U $databaseUser `
            --auth=scram-sha-256 --encoding=UTF8 --locale=C "--pwfile=$passwordFile"
        if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel inicializar o PostgreSQL local.' }
    }
    finally {
        Remove-Item -LiteralPath $passwordFile -Force
    }
}

& (Join-Path $PostgresBin 'pg_ctl.exe') -D $dataDirectory status *> $null
if ($LASTEXITCODE -ne 0) {
    $arguments = '-D "{0}" -l "{1}" -o "-h 127.0.0.1 -p 5433" -w start' -f $dataDirectory, $logPath
    $process = Start-Process -FilePath (Join-Path $PostgresBin 'pg_ctl.exe') `
        -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Nao foi possivel iniciar o banco. Consulte $logPath."
    }
}

$previousPassword = $env:PGPASSWORD
$previousTimeout = $env:PGCONNECT_TIMEOUT
try {
    $env:PGPASSWORD = $databasePassword
    $env:PGCONNECT_TIMEOUT = '5'
    $exists = & (Join-Path $PostgresBin 'psql.exe') -X -w -h 127.0.0.1 -p 5433 `
        -U $databaseUser -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = 'garupa_sp'"
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao autenticar no banco local.' }
    if ([string]::IsNullOrWhiteSpace($exists)) {
        & (Join-Path $PostgresBin 'psql.exe') -X -w -h 127.0.0.1 -p 5433 `
            -U $databaseUser -d postgres -v ON_ERROR_STOP=1 -c 'CREATE DATABASE garupa_sp'
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar o banco garupa_sp.' }
    }
}
finally {
    $env:PGPASSWORD = $previousPassword
    $env:PGCONNECT_TIMEOUT = $previousTimeout
}

Write-Output 'PostgreSQL local disponivel em 127.0.0.1:5433, banco garupa_sp.'

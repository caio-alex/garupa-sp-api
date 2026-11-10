$ErrorActionPreference = 'Stop'
$taskProjectRoot = Split-Path -Parent $PSScriptRoot
$taskLocalDotnet = Join-Path $taskProjectRoot '.local\dotnet\dotnet.exe'
if (Test-Path -LiteralPath $taskLocalDotnet) {
    $taskDotnetDirectory = Split-Path -Parent $taskLocalDotnet
    $env:DOTNET_ROOT = $taskDotnetDirectory
    $env:DOTNET_ROOT_X64 = $taskDotnetDirectory
    $env:DOTNET_HOST_PATH = $taskLocalDotnet
    if (($env:PATH -split ';') -notcontains $taskDotnetDirectory) {
        $env:PATH = $taskDotnetDirectory + ';' + $env:PATH
    }
}

& dotnet --version
if ($LASTEXITCODE -ne 0) {
    throw 'Instale o SDK .NET 10.0.404 ou uma atualizacao do mesmo feature band.'
}

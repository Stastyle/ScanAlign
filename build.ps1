#!/usr/bin/env pwsh
# ScanAlign build/test helper. Usage: ./build.ps1 [restore|build|test|all]
param([string]$Task = "all")

$ErrorActionPreference = "Stop"
$dotnet = "dotnet"

function Invoke-Step($name, $cmd) {
    Write-Host "==> $name" -ForegroundColor Cyan
    & $cmd
    if ($LASTEXITCODE -ne 0) { throw "$name failed (exit $LASTEXITCODE)" }
}

switch ($Task) {
    "restore" { Invoke-Step "restore" { & $dotnet restore } }
    "build"   { Invoke-Step "build"   { & $dotnet build -c Debug --nologo } }
    "test"    { Invoke-Step "test"    { & $dotnet test  -c Debug --nologo } }
    "all" {
        Invoke-Step "restore" { & $dotnet restore }
        Invoke-Step "build"   { & $dotnet build -c Debug --nologo }
        Invoke-Step "test"    { & $dotnet test  -c Debug --nologo }
    }
    default { throw "Unknown task '$Task'. Use restore|build|test|all." }
}
Write-Host "OK" -ForegroundColor Green

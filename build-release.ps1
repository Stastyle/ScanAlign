#!/usr/bin/env pwsh
# Builds ScanAlign release artifacts into ./artifacts:
#   - ScanAlign-Setup-<version>.exe            (Inno Setup installer, self-contained)
#   - ScanAlign-<version>-win-x64-portable.zip (portable, self-contained, no install)
# Usage: ./build-release.ps1 [-Configuration Release] [-Runtime win-x64]
param(
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64"
)
$ErrorActionPreference = "Stop"

$root        = $PSScriptRoot
$artifacts   = Join-Path $root "artifacts"
$publishDir  = Join-Path $artifacts "publish"
$portableDir = Join-Path $artifacts "portable"
$app         = Join-Path $root "src/ScanAlign.App/ScanAlign.App.csproj"

# --- version (from Directory.Build.props) ---
[xml]$props  = Get-Content (Join-Path $root "Directory.Build.props")
$version     = ($props.Project.PropertyGroup.Version     | Select-Object -First 1).Trim()
$fileVersion = ($props.Project.PropertyGroup.FileVersion | Select-Object -First 1).Trim()
Write-Host "==> ScanAlign $version ($fileVersion) [$Configuration/$Runtime]" -ForegroundColor Cyan

# --- clean ---
if (Test-Path $artifacts) { Remove-Item $artifacts -Recurse -Force }
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

# --- 1. folder publish (consumed by the installer) ---
Write-Host "==> Publishing self-contained folder build" -ForegroundColor Cyan
dotnet publish $app -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false -p:DebugType=none -p:DebugSymbols=false `
    -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { throw "folder publish failed (exit $LASTEXITCODE)" }

# --- 2. single-file publish (consumed by the portable zip) ---
Write-Host "==> Publishing self-contained single-file build" -ForegroundColor Cyan
dotnet publish $app -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false `
    -o $portableDir --nologo
if ($LASTEXITCODE -ne 0) { throw "portable publish failed (exit $LASTEXITCODE)" }

Get-ChildItem $portableDir -Filter *.pdb -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
foreach ($doc in "README.md", "LICENSE", "CHANGELOG.md") {
    Copy-Item (Join-Path $root $doc) $portableDir
}

# --- 3. portable zip ---
$zip = Join-Path $artifacts "ScanAlign-$version-win-x64-portable.zip"
Write-Host "==> Creating portable zip" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $portableDir "*") -DestinationPath $zip -Force

# --- 4. installer (Inno Setup) ---
$iscc = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe" }
if (-not (Test-Path $iscc)) { throw "ISCC.exe not found. Install Inno Setup 6 (winget install JRSoftware.InnoSetup)." }

Write-Host "==> Building installer with $iscc" -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$version" "/DMyAppVersionInfo=$fileVersion" `
    "/DSourceDir=$publishDir" "/DOutputDir=$artifacts" `
    (Join-Path $root "installer\ScanAlign.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC failed (exit $LASTEXITCODE)" }

Write-Host "`nOK - release artifacts:" -ForegroundColor Green
Get-ChildItem $artifacts -File | Select-Object Name, @{N='SizeMB';E={[math]::Round($_.Length/1MB,2)}} | Format-Table -AutoSize

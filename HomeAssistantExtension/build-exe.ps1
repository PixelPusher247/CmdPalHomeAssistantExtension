#Requires -Version 7
<#
.SYNOPSIS
    Publishes the extension and packages it as an Inno Setup installer.
.PARAMETER Arch
    Target architecture: x64 or arm64.
.PARAMETER Version
    Semantic version string written into the installer (e.g. 1.0.0).
.EXAMPLE
    .\build-exe.ps1 -Arch x64 -Version 1.0.0
#>
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Arch = 'x64',
    [string]$Version = '0.0.1'
)

$ErrorActionPreference = 'Stop'

$rid        = if ($Arch -eq 'arm64') { 'win-arm64' } else { 'win-x64' }
$winArch    = if ($Arch -eq 'arm64') { 'arm64' } else { 'x64' }
$publishDir = "publish\$Arch"

Write-Host "==> Publishing $rid ..."
dotnet publish HomeAssistantExtension.csproj `
    -c Release `
    -r $rid `
    --self-contained `
    -p:EnableMsixTooling=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

Write-Host "==> Copying Assets ..."
if (Test-Path "$publishDir\Assets") { Remove-Item "$publishDir\Assets" -Recurse -Force }
Copy-Item -Path "Assets" -Destination "$publishDir\Assets" -Recurse

Write-Host "==> Generating sparse AppxManifest.xml ..."
# AppX version must be 4-component (major.minor.patch.0)
$appxVersion = "$Version.0"
(Get-Content "AppxManifest-sparse.xml") `
    -replace '__VERSION__', $appxVersion `
    -replace '__ARCH__',    $winArch |
    Set-Content "$publishDir\AppxManifest.xml" -Encoding UTF8

Write-Host "==> Compiling Inno Setup installer ($Arch) ..."
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) { throw "ISCC.exe not found — install Inno Setup 6 first (choco install innosetup)" }

& $iscc "/DAppVersion=$Version" "/DArch=$Arch" setup-template.iss

if ($LASTEXITCODE -ne 0) { throw "Inno Setup compiler failed (exit $LASTEXITCODE)" }

Write-Host "==> Done: output\HomeAssistantExtension-$Version-$Arch.exe"

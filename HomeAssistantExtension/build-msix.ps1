#Requires -Version 7
<#
.SYNOPSIS
    Publishes the extension, packs it as a signed MSIX, and optionally installs it.

.PARAMETER Arch
    Target architecture: x64 or arm64. Default: x64.

.PARAMETER Version
    Semantic version string. Default: reads from HomeAssistantExtension.csproj.

.PARAMETER CertThumbprint
    Thumbprint of a certificate already in the CurrentUser\My or LocalMachine\My store.
    Reads from cert-thumbprint.txt in the script directory when not specified.

.PARAMETER CertPath
    Path to a PFX file. Used in CI where the cert isn't in the store.

.PARAMETER CertPassword
    Password for the PFX file.

.PARAMETER Install
    Switch: after building, run Add-AppxPackage to install the MSIX immediately.

.EXAMPLE
    # First-time local build (after running create-signing-cert.ps1)
    .\build-msix.ps1

    # Specific version and arch, then auto-install
    .\build-msix.ps1 -Arch arm64 -Version 1.2.0 -Install

    # CI usage with PFX file
    .\build-msix.ps1 -Arch x64 -Version 1.2.0 -CertPath signing.pfx -CertPassword $env:CERT_PASS
#>
param(
    [ValidateSet('x64', 'arm64')]
    [string]$Arch = 'x64',

    [string]$Version,

    [string]$CertThumbprint,
    [string]$CertPath,
    [string]$CertPassword,

    [switch]$Install
)

$ErrorActionPreference = 'Stop'

# ── Resolve version ───────────────────────────────────────────────────────────
if (-not $Version) {
    [xml]$proj = Get-Content (Join-Path $PSScriptRoot 'HomeAssistantExtension.csproj')
    $Version = ($proj.Project.PropertyGroup | ForEach-Object { $_.Version } |
                Where-Object { $_ } | Select-Object -First 1)
    if (-not $Version) { $Version = '0.0.1' }
}

$rid     = if ($Arch -eq 'arm64') { 'win-arm64' } else { 'win-x64' }
$winArch = if ($Arch -eq 'arm64') { 'arm64' } else { 'x64' }
$publishDir = Join-Path $PSScriptRoot "publish\$Arch"
$outputDir  = Join-Path $PSScriptRoot "output"
$msixPath   = Join-Path $outputDir "HomeAssistantExtension-$Version-$Arch.msix"

# ── Resolve signing cert ──────────────────────────────────────────────────────
if (-not $CertThumbprint -and -not $CertPath) {
    $thumbprintFile = Join-Path $PSScriptRoot 'cert-thumbprint.txt'
    if (Test-Path $thumbprintFile) {
        $CertThumbprint = (Get-Content $thumbprintFile -Raw).Trim()
    } else {
        $cert = Get-ChildItem 'Cert:\CurrentUser\My' |
                Where-Object { $_.Subject -eq 'CN=PixelPusher247' } |
                Select-Object -First 1
        if ($cert) {
            $CertThumbprint = $cert.Thumbprint
        } else {
            throw "No signing certificate found.`nRun .\create-signing-cert.ps1 first (as Administrator)."
        }
    }
}

# ── Find Windows SDK tools ────────────────────────────────────────────────────
function Find-SdkTool([string]$Name) {
    $roots = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
        "${env:ProgramFiles}\Windows Kits\10\bin"
    )
    foreach ($root in $roots) {
        $hit = Get-ChildItem "$root\*\x64\$Name" -ErrorAction SilentlyContinue |
               Sort-Object { [version]($_.Directory.Parent.Name) } -Descending |
               Select-Object -First 1 -ExpandProperty FullName
        if ($hit) { return $hit }
    }
    throw "$Name not found — install the Windows 10/11 SDK."
}

$makeappx = Find-SdkTool 'makeappx.exe'
$signtool  = Find-SdkTool 'signtool.exe'

# ── Publish ───────────────────────────────────────────────────────────────────
Write-Host "==> Publishing $rid ..."
Push-Location $PSScriptRoot
try {
    dotnet publish HomeAssistantExtension.csproj `
        -c Release `
        -r $rid `
        --self-contained `
        -p:EnableMsixTooling=false `
        -o $publishDir

    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
} finally { Pop-Location }

# ── Copy assets ───────────────────────────────────────────────────────────────
Write-Host "==> Copying Assets and Public folder ..."
$assetsSource = Join-Path $PSScriptRoot 'Assets'
$assetsDest   = Join-Path $publishDir 'Assets'
if (Test-Path $assetsDest) { Remove-Item $assetsDest -Recurse -Force }
Copy-Item $assetsSource $assetsDest -Recurse

# PublicFolder declared in the manifest must physically exist in the package
$publicDest = Join-Path $publishDir 'Public'
if (-not (Test-Path $publicDest)) { New-Item $publicDest -ItemType Directory | Out-Null }

# ── Generate AppxManifest.xml ─────────────────────────────────────────────────
Write-Host "==> Generating AppxManifest.xml ..."
$appxVersion = "$Version.0"
(Get-Content (Join-Path $PSScriptRoot 'AppxManifest-sparse.xml') -Raw) `
    -replace '__VERSION__', $appxVersion `
    -replace '__ARCH__',    $winArch |
    Set-Content (Join-Path $publishDir 'AppxManifest.xml') -Encoding UTF8

# ── Pack MSIX ────────────────────────────────────────────────────────────────
Write-Host "==> Packing MSIX ..."
$null = New-Item $outputDir -ItemType Directory -Force
if (Test-Path $msixPath) { Remove-Item $msixPath -Force }

& $makeappx pack /v /o /d $publishDir /p $msixPath
if ($LASTEXITCODE -ne 0) { throw "makeappx failed (exit $LASTEXITCODE)" }

# ── Sign MSIX ────────────────────────────────────────────────────────────────
Write-Host "==> Signing MSIX ..."
if ($CertPath) {
    $signArgs = @('sign', '/fd', 'SHA256', '/f', $CertPath)
    if ($CertPassword) { $signArgs += '/p', $CertPassword }
    $signArgs += $msixPath
} else {
    $signArgs = @('sign', '/fd', 'SHA256', '/sha1', $CertThumbprint, $msixPath)
}
& $signtool @signArgs
if ($LASTEXITCODE -ne 0) { throw "signtool failed (exit $LASTEXITCODE)" }

Write-Host ""
Write-Host "==> Built: $msixPath"

# ── Optional install ──────────────────────────────────────────────────────────
if ($Install) {
    Write-Host "==> Installing MSIX ..."
    # Remove any previous version first (Add-AppxPackage fails if same version is already installed)
    Get-AppxPackage -Name 'PixelPusher247.HomeAssistantExtension' |
        Remove-AppxPackage -ErrorAction SilentlyContinue
    Add-AppxPackage -Path $msixPath
    Write-Host "==> Installed. Restart PowerToys to load the extension."
} else {
    Write-Host ""
    Write-Host "Install with:"
    Write-Host "  Get-AppxPackage -Name 'PixelPusher247.HomeAssistantExtension' | Remove-AppxPackage -ErrorAction SilentlyContinue"
    Write-Host "  Add-AppxPackage '$msixPath'"
    Write-Host ""
    Write-Host "Then restart PowerToys."
}

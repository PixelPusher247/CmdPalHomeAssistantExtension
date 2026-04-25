#Requires -Version 7
<#
.SYNOPSIS
    Creates a self-signed code signing certificate for the HomeAssistantExtension MSIX package.

.DESCRIPTION
    Creates a certificate with Subject "CN=PixelPusher247" (must match the Publisher in
    Package.appxmanifest and AppxManifest-sparse.xml). Installs it to LocalMachine\TrustedPeople
    so Windows trusts the signed MSIX without Developer Mode.

    Run this ONCE as Administrator before your first local MSIX build.

.PARAMETER OutputPath
    Where to save the exported PFX file. Default: signing.pfx in the script directory.

.PARAMETER Password
    Password to protect the PFX. Prompts if not provided.

.EXAMPLE
    # Run as Administrator
    .\create-signing-cert.ps1

    # With explicit password
    .\create-signing-cert.ps1 -Password (ConvertTo-SecureString "MyPass123" -AsPlainText -Force)
#>
param(
    [string]$OutputPath,
    [SecureString]$Password
)

$ErrorActionPreference = 'Stop'

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script as Administrator (needed to install the cert into LocalMachine\TrustedPeople)."
}

if (-not $OutputPath) {
    $OutputPath = Join-Path $PSScriptRoot "signing.pfx"
}

if (-not $Password) {
    $Password = Read-Host -Prompt "Certificate password" -AsSecureString
}

$subject = "CN=PixelPusher247"

Write-Host "==> Creating self-signed certificate ($subject) ..."
$cert = New-SelfSignedCertificate `
    -Subject $subject `
    -Type CodeSigning `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @(
        "2.5.29.37={text}1.3.6.1.5.5.7.3.3",   # Extended Key Usage: Code Signing
        "2.5.29.19={text}"                        # Basic Constraints: not a CA
    ) `
    -NotAfter (Get-Date).AddYears(10)

Write-Host "==> Exporting PFX to $OutputPath ..."
Export-PfxCertificate -Cert $cert -FilePath $OutputPath -Password $Password | Out-Null

Write-Host "==> Installing to LocalMachine\TrustedPeople (so MSIX installs without Developer Mode) ..."
$store = [System.Security.Cryptography.X509Certificates.X509Store]::new(
    [System.Security.Cryptography.X509Certificates.StoreName]::TrustedPeople,
    [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
try {
    $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $store.Add($cert)
} finally {
    $store.Close()
}

# Also add to LocalMachine\Root so the chain validates
$rootStore = [System.Security.Cryptography.X509Certificates.X509Store]::new(
    [System.Security.Cryptography.X509Certificates.StoreName]::Root,
    [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
try {
    $rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $rootStore.Add($cert)
} finally {
    $rootStore.Close()
}

$thumbprint = $cert.Thumbprint
Write-Host "==> Certificate thumbprint: $thumbprint"
$thumbprint | Out-File (Join-Path $PSScriptRoot "cert-thumbprint.txt") -Encoding UTF8 -NoNewline

# Write Base64-encoded PFX for GitHub Actions secret
$pfxBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $OutputPath))
$base64 = [Convert]::ToBase64String($pfxBytes)
$base64OutPath = Join-Path $PSScriptRoot "signing-cert-base64.txt"
$base64 | Out-File $base64OutPath -Encoding UTF8 -NoNewline

Write-Host ""
Write-Host "Done. Next steps:"
Write-Host "  1. Run .\build-msix.ps1 to build and sign the MSIX"
Write-Host "  2. Install: Add-AppxPackage .\output\HomeAssistantExtension-0.0.1-x64.msix"
Write-Host "  3. Restart PowerToys — the extension will appear"
Write-Host ""
Write-Host "For GitHub Actions CI releases:"
Write-Host "  - Add SIGNING_CERT_PFX secret  = contents of signing-cert-base64.txt"
Write-Host "  - Add SIGNING_CERT_PASSWORD secret = the password you just entered"

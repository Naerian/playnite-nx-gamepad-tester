$ErrorActionPreference = "Stop"

$projectDir = Join-Path $PSScriptRoot "GamepadTester"
$solution = Join-Path $projectDir "GamepadTester.sln"
$outputDir = Join-Path $projectDir "bin\Debug"
$playniteExtensionDir = "C:\Playnite\Extensions\GamepadTester"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
$certSubject = "CN=GamepadTester Local Development"

function Get-GamepadTesterSigningCertificate {
    $cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $certSubject } |
        Select-Object -First 1

    if (!$cert) {
        $cert = New-SelfSignedCertificate `
            -Subject $certSubject `
            -Type CodeSigningCert `
            -CertStoreLocation Cert:\CurrentUser\My `
            -KeyUsage DigitalSignature `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -HashAlgorithm SHA256 `
            -NotAfter (Get-Date).AddYears(5)
    }

    $tempCert = Join-Path $env:TEMP "GamepadTesterLocalDevelopment.cer"
    Export-Certificate -Cert $cert -FilePath $tempCert -Force | Out-Null
    Import-Certificate -FilePath $tempCert -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
    Import-Certificate -FilePath $tempCert -CertStoreLocation Cert:\CurrentUser\TrustedPublisher | Out-Null

    return $cert
}

function Sign-GamepadTesterAssembly {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    if (!(Test-Path -LiteralPath $Path)) {
        throw "Cannot sign missing file: $Path"
    }

    $signature = Set-AuthenticodeSignature -FilePath $Path -Certificate $Certificate -HashAlgorithm SHA256
    if ($signature.Status -ne "Valid") {
        throw "Signing failed for $Path. Status: $($signature.Status). $($signature.StatusMessage)"
    }
}

if (!(Test-Path -LiteralPath $msbuild)) {
    throw "MSBuild was not found at $msbuild"
}

& $msbuild $solution /p:Configuration=Debug
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$signingCertificate = Get-GamepadTesterSigningCertificate
$builtAssembly = Join-Path $outputDir "GamepadTester.dll"
Sign-GamepadTesterAssembly -Path $builtAssembly -Certificate $signingCertificate

New-Item -ItemType Directory -Force -Path $playniteExtensionDir | Out-Null
try {
    Copy-Item -LiteralPath `
        (Join-Path $outputDir "GamepadTester.dll"), `
        (Join-Path $outputDir "GamepadTester.pdb"), `
        (Join-Path $outputDir "extension.yaml"), `
        (Join-Path $outputDir "icon.png") `
        -Destination $playniteExtensionDir `
        -Force
}
catch {
    throw "Could not deploy Gamepad Tester. Close Playnite and run this script again. Original error: $($_.Exception.Message)"
}

$deployedAssembly = Join-Path $playniteExtensionDir "GamepadTester.dll"
Sign-GamepadTesterAssembly -Path $deployedAssembly -Certificate $signingCertificate

$localizationSource = Join-Path $outputDir "Localization"
$localizationDestination = Join-Path $playniteExtensionDir "Localization"
if (Test-Path -LiteralPath $localizationSource) {
    New-Item -ItemType Directory -Force -Path $localizationDestination | Out-Null
    Copy-Item -Path (Join-Path $localizationSource "*.xaml") -Destination $localizationDestination -Force
}

Write-Host "Gamepad Tester deployed to $playniteExtensionDir"

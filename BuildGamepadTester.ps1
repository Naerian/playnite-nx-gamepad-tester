$ErrorActionPreference = "Stop"

$projectDir = Join-Path $PSScriptRoot "GamepadTester"
$solution = Join-Path $projectDir "GamepadTester.sln"
$outputDir = Join-Path $projectDir "bin\Debug"
$playniteExtensionDir = "C:\Playnite\Extensions\GamepadTester"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
$certificateSubject = "CN=Gamepad Tester Development"

function Get-OrCreate-CodeSigningCertificate {
    $certificate = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $certificateSubject -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if ($null -eq $certificate) {
        $certificate = New-SelfSignedCertificate `
            -Subject $certificateSubject `
            -Type CodeSigningCert `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -HashAlgorithm SHA256 `
            -CertStoreLocation Cert:\CurrentUser\My `
            -NotAfter (Get-Date).AddYears(5)
    }

    foreach ($storeName in @("TrustedPublisher", "Root")) {
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, "CurrentUser")
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        try {
            $exists = $store.Certificates | Where-Object { $_.Thumbprint -eq $certificate.Thumbprint } | Select-Object -First 1
            if ($null -eq $exists) {
                $store.Add($certificate)
            }
        }
        finally {
            $store.Close()
        }
    }

    return $certificate
}

function Sign-PluginAssembly {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    if (!(Test-Path -LiteralPath $Path)) {
        throw "Cannot sign missing assembly: $Path"
    }

    $signature = Set-AuthenticodeSignature -FilePath $Path -Certificate $Certificate -HashAlgorithm SHA256
    if ($signature.Status -ne "Valid") {
        throw "Could not sign $Path. Signature status: $($signature.Status) - $($signature.StatusMessage)"
    }
}

if (!(Test-Path -LiteralPath $msbuild)) {
    throw "MSBuild was not found at $msbuild"
}

& $msbuild $solution /t:Rebuild /p:Configuration=Debug
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$certificate = Get-OrCreate-CodeSigningCertificate
$builtAssembly = Join-Path $outputDir "GamepadTester.dll"
Sign-PluginAssembly -Path $builtAssembly -Certificate $certificate

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

$localizationSource = Join-Path $outputDir "Localization"
$localizationDestination = Join-Path $playniteExtensionDir "Localization"
if (Test-Path -LiteralPath $localizationSource) {
    New-Item -ItemType Directory -Force -Path $localizationDestination | Out-Null
    Copy-Item -Path (Join-Path $localizationSource "*.xaml") -Destination $localizationDestination -Force
}

$mediaSource = Join-Path $outputDir "media"
$mediaDestination = Join-Path $playniteExtensionDir "media"
if (Test-Path -LiteralPath $mediaSource) {
    if (Test-Path -LiteralPath $mediaDestination) {
        Remove-Item -LiteralPath $mediaDestination -Recurse -Force
    }

    Copy-Item -LiteralPath $mediaSource -Destination $playniteExtensionDir -Recurse -Force
}

$deployedAssembly = Join-Path $playniteExtensionDir "GamepadTester.dll"
Sign-PluginAssembly -Path $deployedAssembly -Certificate $certificate

Write-Host "Gamepad Tester deployed to $playniteExtensionDir"

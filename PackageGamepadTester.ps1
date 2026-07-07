$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $root "GamepadTester"
$outputDir = Join-Path $projectDir "bin\Debug"
$manifestPath = Join-Path $outputDir "extension.yaml"

if (!(Test-Path $manifestPath)) {
    & (Join-Path $root "BuildGamepadTester.ps1")
}

if (!(Test-Path $manifestPath)) {
    throw "Build output not found. Run BuildGamepadTester.ps1 first."
}

$versionLine = Get-Content $manifestPath | Where-Object { $_ -match "^Version:\s*(.+)$" } | Select-Object -First 1
if (!$versionLine) {
    throw "Could not read extension version from extension.yaml."
}

$version = ($versionLine -replace "^Version:\s*", "").Trim()
$distDir = Join-Path $root "dist"
$packageDir = Join-Path $distDir "GamepadTester-package"
$zipPath = Join-Path $distDir "GamepadTester-$version.zip"
$pextPath = Join-Path $distDir "GamepadTester-$version.pext"

if (Test-Path $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageDir | Out-Null

Copy-Item -LiteralPath (Join-Path $outputDir "GamepadTester.dll") -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $outputDir "extension.yaml") -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $outputDir "icon.png") -Destination $packageDir

$localizationSource = Join-Path $outputDir "Localization"
if (Test-Path $localizationSource) {
    Copy-Item -LiteralPath $localizationSource -Destination $packageDir -Recurse
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path $pextPath) {
    Remove-Item -LiteralPath $pextPath -Force
}

Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -Force
Move-Item -LiteralPath $zipPath -Destination $pextPath
Remove-Item -LiteralPath $packageDir -Recurse -Force

Write-Host "Created $pextPath"

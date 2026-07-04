$ErrorActionPreference = "Stop"

$projectDir = Join-Path $PSScriptRoot "GamepadTester"
$solution = Join-Path $projectDir "GamepadTester.sln"
$outputDir = Join-Path $projectDir "bin\Debug"
$playniteExtensionDir = "C:\Playnite\Extensions\GamepadTester"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if (!(Test-Path -LiteralPath $msbuild)) {
    throw "MSBuild was not found at $msbuild"
}

& $msbuild $solution /t:Rebuild /p:Configuration=Debug
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

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

Write-Host "Gamepad Tester deployed to $playniteExtensionDir"

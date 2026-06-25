param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

dotnet build (Join-Path $root "FrutigerAeroRecolor.csproj") -c $Configuration

$manifest = Get-Content (Join-Path $root "thunderstore\manifest.json") -Raw | ConvertFrom-Json
$version = $manifest.version_number
$dll = Join-Path $root "bin\$Configuration\net472\FrutigerAeroRecolor.dll"

if (-not (Test-Path $dll)) {
    throw "Build output not found: $dll"
}

$stage = Join-Path $env:TEMP "FrutigerAeroRecolor-thunderstore-$version"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $stage "J4EGER") -Force | Out-Null

Copy-Item $dll (Join-Path $stage "J4EGER\FrutigerAeroRecolor.dll") -Force
Copy-Item (Join-Path $root "thunderstore\icon.png") (Join-Path $stage "icon.png") -Force
Copy-Item (Join-Path $root "thunderstore\screenshot.png") (Join-Path $stage "screenshot.png") -Force
Copy-Item (Join-Path $root "thunderstore\manifest.json") (Join-Path $stage "manifest.json") -Force
Copy-Item (Join-Path $root "thunderstore\README.md") (Join-Path $stage "README.md") -Force
Copy-Item (Join-Path $root "CHANGELOG.md") (Join-Path $stage "CHANGELOG.md") -Force

$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$zip = Join-Path $dist "J4EGER-FrutigerAeroRecolor-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage\*" -DestinationPath $zip -Force

Write-Host "Thunderstore package: $zip"

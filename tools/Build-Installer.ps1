param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "release-build\AudioMirrorApp"
$payloadDir = Join-Path $root "AudioMirrorSetup\Payload"
$payloadZip = Join-Path $payloadDir "AudioMirrorApp-win-x64.zip"
$installerOut = Join-Path $root "release-build\AudioMirrorSetup"
$appAssetZip = Join-Path $root "AudioMirrorApp-win-x64.zip"
$setupAssetZip = Join-Path $root "AudioMirrorSetup-win-x64.zip"

dotnet publish (Join-Path $root "AudioMirrorApp\AudioMirrorApp.csproj") `
    --configfile (Join-Path $root "AudioMirrorApp\NuGet.Config") `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "AudioMirrorApp publish failed with exit code $LASTEXITCODE"
}

New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null
if (Test-Path -LiteralPath $payloadZip) {
    Remove-Item -LiteralPath $payloadZip -Force
}

Get-ChildItem -LiteralPath $publishDir -File |
    Where-Object { $_.Name -ne "AudioMirrorApp.pdb" -and $_.Name -ne "settings.json" } |
    Compress-Archive -DestinationPath $payloadZip

dotnet publish (Join-Path $root "AudioMirrorSetup\AudioMirrorSetup.csproj") `
    --configfile (Join-Path $root "AudioMirrorSetup\NuGet.Config") `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -o $installerOut
if ($LASTEXITCODE -ne 0) {
    throw "AudioMirrorSetup publish failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath $payloadZip -Destination $appAssetZip -Force
if (Test-Path -LiteralPath $setupAssetZip) {
    Remove-Item -LiteralPath $setupAssetZip -Force
}

Get-ChildItem -LiteralPath $installerOut -File -Filter "AudioMirrorSetup.exe" |
    Compress-Archive -DestinationPath $setupAssetZip

Write-Host "Installer:"
Write-Host (Join-Path $installerOut "AudioMirrorSetup.exe")
Write-Host "Release assets:"
Write-Host $appAssetZip
Write-Host $setupAssetZip

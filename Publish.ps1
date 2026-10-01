[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$PackageFile = '',
    [string]$Repository = 'https://thunderstore.io',
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string]$TeamName = 'DocZee'
)
$ErrorActionPreference = 'Stop'
if (!$PackageFile) {
    $PackageFile = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'artifacts') -Filter 'HenEggPickup-*-Thunderstore.zip' |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (!$PackageFile -or !(Test-Path -LiteralPath $PackageFile -PathType Leaf)) {
    throw 'Build a Thunderstore ZIP first, or pass -PackageFile.'
}
$PackageFile = (Resolve-Path -LiteralPath $PackageFile).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($PackageFile)
try {
    foreach ($required in @('manifest.json', 'README.md', 'icon.png', 'BepInEx/plugins/HenEggPickup/HenEggPickup.dll')) {
        if (!$zip.GetEntry($required)) { throw "Missing ZIP entry: $required" }
    }
    $reader = [IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose() }
} finally { $zip.Dispose() }
if ($manifest.name -ne 'HenEggPickup' -or $manifest.version_number -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$') {
    throw 'ZIP does not contain a valid HenEggPickup manifest.'
}
$page = "$Repository/c/valheim/p/$TeamName/$($manifest.name)/"
Write-Host "Package: $PackageFile"
Write-Host "Destination: $page (version $($manifest.version_number))"
if (!$PSCmdlet.ShouldProcess($page, "Publish version $($manifest.version_number) to Thunderstore")) { return }

$token = $env:THUNDERSTORE_API_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    $token = [Environment]::GetEnvironmentVariable('THUNDERSTORE_API_TOKEN', 'User')
}
if ([string]::IsNullOrWhiteSpace($token)) { throw 'THUNDERSTORE_API_TOKEN is not set.' }
$previousToken = $env:TCLI_AUTH_TOKEN
Push-Location $PSScriptRoot
try {
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Thunderstore CLI restore failed.' }
    $env:TCLI_AUTH_TOKEN = $token
    & dotnet tool run tcli -- publish --file $PackageFile --config-path (Join-Path $PSScriptRoot 'thunderstore.toml') `
        --repository $Repository --package-namespace $TeamName --package-name $manifest.name --package-version $manifest.version_number
    if ($LASTEXITCODE -ne 0) { throw "Thunderstore publish failed with exit code $LASTEXITCODE." }
    Write-Host "Published: $page"
} finally {
    $env:TCLI_AUTH_TOKEN = $previousToken
    Pop-Location
}

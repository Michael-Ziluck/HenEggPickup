[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProfilePath = (Join-Path $env:APPDATA 'r2modmanPlus-local/Valheim/profiles/Default'),
    [string]$GamePath = 'E:\Games\SteamLibrary\steamapps\common\Valheim',
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ($Build) {
    & (Join-Path $PSScriptRoot 'Build.ps1') -GamePath $GamePath
}
$dll = Join-Path $root 'bin/Release/net48/HenEggPickup.dll'
if (!(Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'Build Release first, or pass -Build.' }
$plugins = Join-Path $ProfilePath 'BepInEx/plugins'
if (!(Test-Path -LiteralPath $plugins -PathType Container)) { throw "Not an existing BepInEx profile: $ProfilePath" }
$installed = @(Get-ChildItem -LiteralPath $plugins -Filter 'HenEggPickup.dll' -File -Recurse)
if ($installed.Count -gt 1) { throw 'Multiple HenEggPickup DLLs are installed. Keep one copy before deploying.' }
$target = if ($installed.Count -eq 1) { $installed[0].FullName } else { Join-Path $plugins 'HenEggPickup/HenEggPickup.dll' }
if (!$PSCmdlet.ShouldProcess($target, 'Deploy Hen Egg Pickup DLL')) { return }
if (Get-Process -Name valheim -ErrorAction SilentlyContinue) { throw 'Exit Valheim before deploying.' }

$backup = $null
if (Test-Path -LiteralPath $target -PathType Leaf) {
    $backupDir = Join-Path $root '.local/deploy-backups'
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    $backup = Join-Path $backupDir ("HenEggPickup-{0}-{1}.dll" -f (Get-Date -Format 'yyyyMMdd-HHmmss'), [guid]::NewGuid().ToString('N'))
    Copy-Item -LiteralPath $target -Destination $backup
    if ((Get-FileHash -LiteralPath $backup).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Backup verification failed.' }
}
New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
try {
    Copy-Item -LiteralPath $dll -Destination $target -Force
    if ((Get-FileHash -LiteralPath $dll).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw 'Deployed DLL does not match the build.' }
} catch {
    if ($backup) { Copy-Item -LiteralPath $backup -Destination $target -Force }
    else { Remove-Item -LiteralPath $target -ErrorAction SilentlyContinue }
    throw
}
Write-Host "Deployed: $target"
if ($backup) { Write-Host "Previous DLL: $backup" }

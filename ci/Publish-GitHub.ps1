param([Parameter(Mandatory)][string]$PackageFile)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Package-Manifest.ps1')
$manifest = Get-PackageManifest $PackageFile
$tag = "v$($manifest.version_number)"
& gh release view $tag --repo $env:GITHUB_REPOSITORY --json tagName 2>$null
if ($LASTEXITCODE -eq 0) { Write-Host "$tag already exists; retaining the original release."; return }
$changelog = Get-Content (Join-Path (Split-Path $PSScriptRoot -Parent) 'CHANGELOG.md') -Raw
$notes = [regex]::Match($changelog, '(?ms)^## ' + [regex]::Escape($manifest.version_number) + '\s*\r?\n(.*?)(?=^## |\z)').Groups[1].Value.Trim()
if (!$notes) { throw 'Add release notes to CHANGELOG.md before publishing.' }
$notesPath = Join-Path $env:RUNNER_TEMP 'release-notes.md'
$notes | Set-Content $notesPath -Encoding utf8
& gh release create $tag $PackageFile --repo $env:GITHUB_REPOSITORY --target $env:GITHUB_SHA --title "$($manifest.name) $($manifest.version_number)" --notes-file $notesPath
if ($LASTEXITCODE -ne 0) { throw 'GitHub release creation failed.' }

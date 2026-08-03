<#
.SYNOPSIS
Builds the Korean and English eslee Folder Locker installers.

.DESCRIPTION
Publishes the three executables per language, copies the display-named aliases,
runs the release privacy check, then compiles the Inno Setup installer for each
language. Outputs land in artifacts\installer\ with SHA-256 hashes printed.

Requires: .NET SDK, Inno Setup 6 (ISCC.exe on PATH or in a standard location).
#>
[CmdletBinding()]
param(
    [string]$Version = "1.2.0",
    [ValidateSet("ko", "en", "both")]
    [string]$Language = "both"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts"

function Find-Iscc {
    $candidates = @(
        (Get-Command "ISCC.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }

    if (-not $candidates) {
        throw "ISCC.exe (Inno Setup 6) was not found. Install Inno Setup 6 first."
    }

    return $candidates | Select-Object -First 1
}

function Publish-Language([string]$lang) {
    $publishDir = Join-Path $artifacts "publish-$lang"
    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    # Stale Release outputs built before the privacy hardening can still carry
    # PDB files; incremental builds may copy them into the publish output. Wipe
    # Release intermediates so every publish is built with current settings.
    foreach ($project in "FolderGate.App", "FolderGate.Core", "FolderGate.ElevatedHelper", "FolderGate.RecoveryTool") {
        foreach ($sub in "bin\Release", "obj\Release") {
            $stale = Join-Path $repoRoot "src\$project\$sub"
            if (Test-Path $stale) {
                Remove-Item $stale -Recurse -Force
            }
        }
    }

    foreach ($project in "FolderGate.App", "FolderGate.ElevatedHelper", "FolderGate.RecoveryTool") {
        # Out-Host keeps build output visible without polluting the function's
        # return value (PowerShell functions return all pipeline output).
        dotnet publish (Join-Path $repoRoot "src\$project\$project.csproj") `
            -c Release -r win-x64 --self-contained false `
            -p:AppLanguage=$lang -o $publishDir --nologo | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $project ($lang)."
        }
    }

    if ($lang -eq "ko") {
        Copy-Item (Join-Path $publishDir "FolderGate.App.exe") (Join-Path $publishDir "eslee폴더잠금기.exe") -Force
        Copy-Item (Join-Path $publishDir "FolderGate.ElevatedHelper.exe") (Join-Path $publishDir "eslee폴더잠금기_권한도우미.exe") -Force
        Copy-Item (Join-Path $publishDir "FolderGate.RecoveryTool.exe") (Join-Path $publishDir "eslee폴더잠금기_복구도구.exe") -Force
    }
    else {
        Copy-Item (Join-Path $publishDir "FolderGate.App.exe") (Join-Path $publishDir "eslee-folder-locker.exe") -Force
        Copy-Item (Join-Path $publishDir "FolderGate.ElevatedHelper.exe") (Join-Path $publishDir "eslee-folder-locker-helper.exe") -Force
        Copy-Item (Join-Path $publishDir "FolderGate.RecoveryTool.exe") (Join-Path $publishDir "eslee-folder-locker-recovery.exe") -Force
    }

    & (Join-Path $repoRoot "tools\Verify-ReleasePrivacy.ps1") -PublishRoot $publishDir
    return $publishDir
}

function Build-Installer([string]$lang, [string]$publishDir, [string]$iscc) {
    & $iscc `
        "/DAppLanguage=$lang" `
        "/DAppVersion=$Version" `
        "/DSourceDir=$publishDir" `
        "/DOutputDir=$(Join-Path $artifacts 'installer')" `
        (Join-Path $PSScriptRoot "eslee-folder-locker.iss")
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed for language '$lang'."
    }
}

$iscc = Find-Iscc
Write-Host "Using ISCC: $iscc"

$languages = if ($Language -eq "both") { @("ko", "en") } else { @($Language) }
foreach ($lang in $languages) {
    Write-Host "=== Publishing ($lang) ===" -ForegroundColor Cyan
    $publishDir = Publish-Language $lang
    Write-Host "=== Building installer ($lang) ===" -ForegroundColor Cyan
    Build-Installer $lang $publishDir $iscc
}

Write-Host "=== Installer SHA-256 ===" -ForegroundColor Cyan
Get-ChildItem (Join-Path $artifacts "installer") -Filter "*.exe" | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    Write-Host ("{0}  {1}" -f $hash, $_.Name)
}

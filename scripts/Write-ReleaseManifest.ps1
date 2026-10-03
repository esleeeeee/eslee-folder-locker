[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [Parameter(Mandatory)][string[]]$AssetPaths,
    [Parameter(Mandatory)][string[]]$VerifiedChecks
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$commit = & git -c "safe.directory=$root" -C $root rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve release source commit.' }
$dirty = & git -c "safe.directory=$root" -C $root status --porcelain --untracked-files=no
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect release source state.' }
if ($AssetPaths.Count -eq 0 -or $VerifiedChecks.Count -eq 0) { throw 'Assets and successful checks are required.' }
$files = foreach ($relative in $AssetPaths) {
    $path = [IO.Path]::GetFullPath((Join-Path $root $relative))
    $boundary = $root.TrimEnd('\') + '\'
    if (-not $path.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase)) { throw "Asset outside repository: $relative" }
    $item = Get-Item -LiteralPath $path -ErrorAction Stop
    if ($item.PSIsContainer -or $item.Length -eq 0 -or -not $item.Name.Contains($Version)) { throw "Invalid release asset: $relative" }
    @{ file=$item.Name; path=$relative; bytes=$item.Length; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$output = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$manifest = Join-Path $output "release-manifest-v$Version.json"
@{ version=$Version; source_sha=$commit; source_dirty=[bool]$dirty; sdk=(& dotnet --version); verified_checks=$VerifiedChecks; assets=@($files) } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifest -Encoding utf8
$lines = @($files | ForEach-Object { "$($_.sha256)  $($_.file)" })
$lines += "$((Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($manifest))"
$lines | Set-Content -LiteralPath (Join-Path $output "SHA256SUMS-v$Version.txt") -Encoding ascii

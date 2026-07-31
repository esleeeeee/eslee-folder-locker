[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$PublishRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$forbiddenPathPatterns = @(
    'C:\Users\',
    '/Users/',
    '/home/'
)

$violations = [System.Collections.Generic.List[string]]::new()
$checkedFiles = 0

foreach ($root in $PublishRoot) {
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Publish directory was not found: $root"
    }

    foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse) {
        $checkedFiles++
        if ($file.Extension -ieq '.pdb') {
            $violations.Add("Unexpected PDB: $($file.FullName)")
            continue
        }

        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $texts = @(
            [System.Text.Encoding]::UTF8.GetString($bytes),
            [System.Text.Encoding]::Unicode.GetString($bytes)
        )

        foreach ($pattern in $forbiddenPathPatterns) {
            if ($texts.Where({ $_.Contains($pattern, [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0) {
                $violations.Add("Absolute user path '$pattern' in $($file.FullName)")
            }
        }
    }
}

if ($violations.Count -gt 0) {
    throw ($violations -join [Environment]::NewLine)
}

Write-Host "Release privacy check passed for $checkedFiles files."

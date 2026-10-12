param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$backupRoot = Join-Path $PSScriptRoot 'backout/seed-trial-20261012'
$manifestPath = Join-Path $backupRoot 'changes.json'
if (!(Test-Path -LiteralPath $manifestPath)) { throw 'The completed change manifest is missing.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$prefix = $repoRoot.TrimEnd('\') + '\'
$verified = @()
foreach ($entry in $manifest.files) {
    $targetPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $entry.path))
    if (!$targetPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'A rollback target is outside this repository.' }
    if (!(Test-Path -LiteralPath $targetPath)) { throw "Missing rollback target: $($entry.path)" }
    $currentHash = (Get-FileHash -LiteralPath $targetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($currentHash -ne $entry.after) { throw "Later changes detected in $($entry.path). Preserve those changes before reverting this experiment." }
    if ($entry.backup) {
        $savedPath = [IO.Path]::GetFullPath((Join-Path $backupRoot $entry.backup))
        if (!$savedPath.StartsWith($backupRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'An invalid backup path was recorded.' }
        if ((Get-FileHash -LiteralPath $savedPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.before) { throw 'A saved baseline was changed.' }
        $verified += [pscustomobject]@{target=$targetPath; backup=$savedPath; operation='restore'; relative=$entry.path}
    } else {
        $verified += [pscustomobject]@{target=$targetPath; backup=$null; operation='remove'; relative=$entry.path}
    }
}
foreach ($item in $verified) {
    Write-Output "$($item.operation): $($item.relative)"
    if ($Apply) {
        if ($item.operation -eq 'restore') { Copy-Item -LiteralPath $item.backup -Destination $item.target -Force }
        else { Remove-Item -LiteralPath $item.target }
    }
}
if ($Apply) { Write-Output 'Seed trial code removed; saved prior entry point restored. Reports, results and unrelated work were retained.' }
else { Write-Output 'Preview only. Add -Apply to perform this checked rollback.' }

param(
    [int]$Passes = 3,
    [int]$Seconds = 15,
    [int]$WarmupSeconds = 5,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$runnerPath = Join-Path $PSScriptRoot 'bin/Release/net10.0/Phototaxis.Runner.dll'
if (-not (Test-Path -LiteralPath $runnerPath)) { throw 'Build the Release runner before comparing backends.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoPath 'artifacts/phototaxis/compact-backend/measured' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$records = [Collections.Generic.List[object]]::new()
foreach ($mode in @('cold', 'warm')) {
    $warmup = if ($mode -eq 'warm') { $WarmupSeconds } else { 0 }
    for ($pass = 1; $pass -le $Passes; $pass++) {
        $order = if ($pass % 2 -eq 1) { @('planned', 'compact') } else { @('compact', 'planned') }
        foreach ($backend in $order) {
            $raw = & dotnet $runnerPath profile $Seconds 20 --cognitive --roving --fixed-resources --backend $backend --warmup $warmup
            if ($LASTEXITCODE -ne 0) { throw "$backend profile failed." }
            $record = $raw | ConvertFrom-Json
            $record | Add-Member -NotePropertyName mode -NotePropertyValue $mode
            $record | Add-Member -NotePropertyName pass -NotePropertyValue $pass
            $record | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory "$backend-$mode-$pass.json")
            $records.Add($record)
            [PSCustomObject]@{ Mode=$mode; Pass=$pass; Backend=$backend; Wall=[math]::Round($record.wallSeconds,3);
                Speed=[math]::Round($record.actualSpeed,2); AllocatedMB=[math]::Round($record.allocatedBytes/1000000,1);
                Alive=$record.alive } | ConvertTo-Json -Compress | Write-Output
        }
    }
}
foreach ($mode in @('cold', 'warm')) {
    $digests = @($records | Where-Object mode -EQ $mode | Select-Object -ExpandProperty stateDigest -Unique)
    if ($digests.Length -ne 1) { throw "State mismatch in $mode profiles. Retained all records for inspection." }
}
$summary = foreach ($mode in @('cold', 'warm')) {
    foreach ($backend in @('Planned', 'Compact')) {
        $rows = @($records | Where-Object { $_.mode -eq $mode -and $_.backend -eq $backend })
        $times = @($rows.wallSeconds | Sort-Object)
        $middle = [int][math]::Floor($times.Length/2)
        $median = if ($times.Length % 2) { $times[$middle] } else { ($times[$middle-1]+$times[$middle])/2 }
        [PSCustomObject]@{mode=$mode; backend=$backend; medianWall=$median; medianSpeed=$Seconds/$median;
            meanAllocation=($rows.allocatedBytes | Measure-Object -Average).Average;
            stateDigest=$rows[0].stateDigest; nativeAssembly=$rows[0].nativeAssembly }
    }
}
$summary | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'comparison.json')
$summary | ConvertTo-Json -Depth 12 | Write-Output

param(
    [ValidateRange(1, 20)][int]$Passes = 3,
    [ValidateRange(1, 3600)][int]$Seconds = 20,
    [ValidateRange(0, 3600)][int]$WarmupSeconds = 5,
    [ValidateRange(1, 1000)][int]$Population = 20,
    [string]$Seeds = '7,19,31,43',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$runnerPath = Join-Path $PSScriptRoot 'bin/Release/net10.0/Phototaxis.Runner.dll'
if (-not (Test-Path -LiteralPath $runnerPath)) { throw 'Build the Release runner before comparing.' }
$trialSeeds = @($Seeds.Split(',') | ForEach-Object { [int]$_.Trim() })
if ($trialSeeds.Count -lt 1) { throw 'Provide at least one seed.' }
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoPath ('artifacts/phototaxis/simd-parallel/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$configurations = @(
    [PSCustomObject]@{ backend='compact'; parallel=1 },
    [PSCustomObject]@{ backend='compactsimd'; parallel=1 },
    [PSCustomObject]@{ backend='compact'; parallel=2 },
    [PSCustomObject]@{ backend='compact'; parallel=4 }
)
$records = [Collections.Generic.List[object]]::new()
for ($pass = 1; $pass -le $Passes; $pass++) {
    # Alternate forward/reverse order and rotate the first configuration.
    $order = if ($pass % 2) { @(0,1,2,3) } else { @(3,2,1,0) }
    $offset = ($pass - 1) % $order.Length
    $order = @($order[$offset..($order.Length-1)]) + @($order | Select-Object -First $offset)
    foreach ($index in $order) {
        $configuration = $configurations[$index]
        $backend = $configuration.backend; $parallel = $configuration.parallel
        $recordPath = Join-Path $OutputDirectory "$backend-p$parallel-pass$pass.json"
        $raw = & dotnet $runnerPath trials --seconds $Seconds --warmup $WarmupSeconds --population $Population --count $trialSeeds.Count --parallel $parallel --seeds $Seeds --backend $backend
        if ($LASTEXITCODE -ne 0) { throw "$backend / $parallel parallel trials failed." }
        $record = $raw | ConvertFrom-Json
        $record | Add-Member -NotePropertyName pass -NotePropertyValue $pass
        $record | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $recordPath
        $records.Add($record)
        [PSCustomObject]@{ Pass=$pass; Backend=$backend; Parallel=$parallel;
            BatchWall=[math]::Round($record.batchWallSeconds,3);
            AggregateSpeed=[math]::Round($record.endToEndSimulatedSpeed,2);
            SIMDCalls=($record.trials.profile.execution.simdBatches | Measure-Object -Sum).Sum
        } | ConvertTo-Json -Compress | Write-Output
    }
}
foreach ($seed in ($trialSeeds | Select-Object -Unique)) {
    $digests = @($records.trials | Where-Object seed -EQ $seed | ForEach-Object { $_.profile.stateDigest } | Select-Object -Unique)
    if ($digests.Length -ne 1) { throw "State mismatch for seed $seed. Raw records retained." }
}
$assemblies = @($records.trials.profile.nativeAssembly | Select-Object -Unique)
if ($assemblies.Length -ne 1) { throw 'Assembly changed during comparison. Raw records retained.' }
$summary = foreach ($configuration in $configurations) {
    $rows = @($records | Where-Object { $_.backend -eq $configuration.backend -and $_.maxParallel -eq $configuration.parallel })
    $times = @($rows.batchWallSeconds | Sort-Object)
    $middle = [int][math]::Floor($times.Length/2)
    $median = if ($times.Length % 2) { $times[$middle] } else { ($times[$middle-1]+$times[$middle])/2 }
    [PSCustomObject]@{ backend=$configuration.backend; parallel=$configuration.parallel;
        medianBatchWall=$median; endToEndSimulatedSpeed=$Seconds*$trialSeeds.Count/$median;
        meanTimedAllocation=($rows.summedTimedAllocatedBytes | Measure-Object -Average).Average;
        meanTimedCpuSeconds=($rows.summedTimedCpuSeconds | Measure-Object -Average).Average;
        simdBatches=($rows.trials.profile.execution.simdBatches | Measure-Object -Sum).Sum;
        simdLanes=($rows.trials.profile.execution.simdLanes | Measure-Object -Sum).Sum }
}
[PSCustomObject]@{ seconds=$Seconds; warmup=$WarmupSeconds; population=$Population; seeds=$trialSeeds;
    passes=$Passes; statesMatch=$true; nativeAssembly=$assemblies[0]; host=$records[0].host;
    summary=$summary } | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'comparison.json')
$summary | ConvertTo-Json -Depth 12 | Write-Output
Write-Output "Raw measurements: $OutputDirectory"

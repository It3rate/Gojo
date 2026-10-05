param([string]$Directory = (Join-Path $PSScriptRoot 'results/first-campaign'))
$ErrorActionPreference = 'Stop'
# Observer-only aggregation. These numbers never feed the native experiment.
$rows = @(Get-ChildItem -LiteralPath $Directory -Filter summary.json -Recurse |
    Sort-Object FullName | ForEach-Object {
        $summary = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
        $manifest = Get-Content -Raw -LiteralPath (Join-Path $_.Directory.FullName 'manifest.json') | ConvertFrom-Json
        $inspection = Get-Content -Raw -LiteralPath (Join-Path $_.Directory.FullName 'latest-checkpoint-inspection.json') | ConvertFrom-Json
        $t = $summary.Totals; $c = $summary.Current; $e = $c.Expenses
        [pscustomobject]@{
            Run = $_.Directory.Name; Seed = $manifest.Seed; Scenario = $manifest.Scenario
            Horizon = $manifest.Horizon; Prospects = $manifest.Prospects; Rotation = $manifest.Rotation
            Observations = $summary.Position; Evaluated = $t.Evaluated; Unknown = $t.Unknown
            SelfError = $t.SelfError; DeliveredError = $t.DeliveredError; Relief = $t.DeliveredRelief
            SelfDirectionMatches = $t.SelfDirectionMatches; DeliveredDirectionMatches = $t.DeliveredDirectionMatches
            Cost = $c.Spent; Inspection = $e.Inspection; Activation = $e.Activation; Cancellation = $e.Cancellation
            Calculation = $e.Calculation; SelfCost = $e.Self
            Activations = $t.Activations; Renewals = $t.Renewals; Cancellations = $t.Cancellations; Expiries = $t.Expiries
            Deliveries = $c.Deliveries; Fees = ($c.Routes | Measure-Object Fees -Sum).Sum
            MaximumInspected = $t.MaximumInspected; MaximumActive = $t.MaximumActive
            DuplicateRelief = $t.Sources[0].ConditionalRelief; DuplicateDeliveries = $t.Sources[0].Deliveries
            NativeWallMilliseconds = $t.NativeWallMilliseconds; NativeAllocationBytes = $t.NativeAllocationBytes
            ReportWallMilliseconds = $t.ReportWallMilliseconds; ReportAllocationBytes = $t.ReportAllocationBytes
            Carriers = $inspection.Carriers; Relations = $inspection.Relations; CheckpointBytes = $inspection.Bytes
            CheckpointWallMilliseconds = $inspection.WallMilliseconds; CheckpointAllocationBytes = $inspection.AllocationBytes
            Conserved = $t.Conserved -and $c.Accounted
            CostCategoriesAgree = $c.Spent -eq ($e.Inspection + $e.Activation + $e.Cancellation + $e.Calculation + $e.Self)
        }
    })
if ($rows.Count -ne 24 -or @($rows | Where-Object Observations -ne 241).Count -ne 0) {
    throw 'The first campaign requires 24 completed 241-observation runs.'
}
$rows | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Directory 'comparison.json') -Encoding utf8
$rows | Format-Table Run, SelfError, DeliveredError, Relief, Cost, Inspection, Activations, Renewals, Cancellations

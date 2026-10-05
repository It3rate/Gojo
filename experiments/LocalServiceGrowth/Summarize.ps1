param(
    [string]$Directory = (Join-Path $PSScriptRoot 'results/first-campaign')
)
$ErrorActionPreference = 'Stop'

# Observer-only aggregation. These values never enter the native actors.
$rows = @(Get-ChildItem -LiteralPath $Directory -Filter summary.json -Recurse |
    Sort-Object FullName | ForEach-Object {
        $summary = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
        $manifest = Get-Content -Raw -LiteralPath (Join-Path $_.Directory.FullName 'manifest.json') | ConvertFrom-Json
        $inspection = Get-Content -Raw -LiteralPath (Join-Path $_.Directory.FullName 'latest-checkpoint-inspection.json') | ConvertFrom-Json
        $totals = $summary.Totals
        $current = $summary.Current
        $bases = @($totals.Consumers | ForEach-Object { $_.Coarse; $_.Fine })
        [pscustomobject]@{
            Run = $_.Directory.Name
            Seed = $manifest.Seed
            Scenario = $manifest.Scenario
            DirectAccess = $manifest.DirectAccess
            Learning = $manifest.Learning
            SupportWeight = $manifest.SupportWeight
            Observations = $summary.Position
            Spent = $current.Spent
            Operating = $current.ActualExpenses.Operating
            Contact = $current.ActualExpenses.Contact
            Construction = $current.ActualExpenses.Construction
            Trial = $current.ActualExpenses.Trial
            GrossBenefit = ($bases | Measure-Object Gross -Sum).Sum
            CoarseError = ($totals.Consumers | ForEach-Object { $_.Coarse.DeliveredError } | Measure-Object -Sum).Sum
            FineError = $totals.Consumers[1].Fine.DeliveredError
            FineGross = $totals.Consumers[1].Fine.Gross
            FinePaidEvaluated = $totals.Consumers[1].Fine.PaidEvaluated
            Paid = ($totals.Consumers | Measure-Object Purchases -Sum).Sum
            EvaluatedPaid = ($bases | Measure-Object PaidEvaluated -Sum).Sum
            Unresolved = ($bases | Measure-Object Unresolved -Sum).Sum
            PositivePaid = ($bases | Measure-Object PositivePaid -Sum).Sum
            HarmfulPaid = ($bases | Measure-Object HarmfulPaid -Sum).Sum
            FirstPromotion = $totals.FirstPromotion
            UpstreamInputFees = $totals.UpstreamInputFees
            UpstreamDependencyObservations = $totals.UpstreamDependencyObservations
            IntermediaryReceipts = $current.Services[1].Receipts
            IntermediaryExpenses = $current.Services[1].Expenses
            MaximumInspected = ($totals.Consumers | Measure-Object MaximumInspectedPerSearch -Maximum).Maximum
            NativeWallMilliseconds = $totals.NativeWallMilliseconds
            NativeAllocationBytes = $totals.NativeAllocationBytes
            ReportWallMilliseconds = $totals.ReportWallMilliseconds
            ReportAllocationBytes = $totals.ReportAllocationBytes
            Carriers = $inspection.Carriers
            Relations = $inspection.Relations
            CheckpointBytes = $inspection.Bytes
            CheckpointWallMilliseconds = $inspection.WallMilliseconds
            CheckpointAllocationBytes = $inspection.AllocationBytes
            Conserved = $totals.Conserved -and $current.Accounted
            CostCategoriesAgree = $current.Spent -eq ($current.ActualExpenses.Operating + $current.ActualExpenses.Contact +
                $current.ActualExpenses.Construction + $current.ActualExpenses.Trial)
        }
    })
if ($rows.Count -ne 21 -or @($rows | Where-Object Observations -ne 360).Count -ne 0) {
    throw 'The first campaign requires all 21 completed 360-observation runs.'
}
$rows | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $Directory 'comparison.json') -Encoding utf8
$rows | Format-Table Run, Spent, GrossBenefit, FirstPromotion, UpstreamInputFees, FinePaidEvaluated, Conserved

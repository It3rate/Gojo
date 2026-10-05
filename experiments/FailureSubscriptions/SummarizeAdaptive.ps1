param([string]$Directory = (Join-Path $PSScriptRoot 'results/adaptive-first-campaign'))
$ErrorActionPreference = 'Stop'
# Observer-only aggregation; no result is fed back into native policy.
$campaign = Get-Content -Raw -LiteralPath (Join-Path $Directory 'campaign.json') | ConvertFrom-Json
$rows = @(Get-ChildItem -LiteralPath $Directory -Filter summary.json -Recurse |
    Sort-Object FullName | ForEach-Object {
        $summary = Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json
        $manifest = Get-Content -Raw -LiteralPath (Join-Path $_.Directory.FullName 'manifest.json') | ConvertFrom-Json
        $inspection = Get-Content -Raw -LiteralPath (Join-Path $_.Directory.FullName 'latest-checkpoint-inspection.json') | ConvertFrom-Json
        $t = $summary.Totals; $c = $summary.Current; $e = $c.Expenses
        $acceptedTotal = ($t.AcceptedHorizons.psobject.Properties | ForEach-Object { [long]$_.Name * [long]$_.Value } | Measure-Object -Sum).Sum
        $consumed = ($t.Sources | ForEach-Object { $_.Evaluated + $_.Unknown } | Measure-Object -Sum).Sum
        $remaining = ($c.Routes | Measure-Object Remaining -Sum).Sum
        $policy = if ($manifest.Horizon -eq 0) { 'alone' }
            elseif (-not $manifest.AdaptiveAttention) { 'original5' }
            elseif ($manifest.AdaptiveHorizon) { 'adaptive8' } else { 'guarded5' }
        [pscustomobject]@{
            Run = $_.Directory.Name; Policy = $policy; Seed = $manifest.Seed; Scenario = $manifest.Scenario
            SourceFingerprint = $manifest.SourceFingerprint; ProtocolFingerprint = $manifest.ProtocolFingerprint
            Observations = $summary.Position; Evaluated = $t.Evaluated; Unknown = $t.Unknown
            SelfError = $t.SelfError; DeliveredError = $t.DeliveredError; Relief = $t.DeliveredRelief
            SelfDirectionMatches = $t.SelfDirectionMatches; DeliveredDirectionMatches = $t.DeliveredDirectionMatches
            Cost = $c.Spent; RemainingPool = $c.Reservoir; Inspection = $e.Inspection
            Activation = $e.Activation; Cancellation = $e.Cancellation; Calculation = $e.Calculation; SelfCost = $e.Self
            Activations = $t.Activations; Renewals = $t.Renewals; Cancellations = $t.Cancellations; Expiries = $t.Expiries
            SelfSelections = $t.SelfSelections; ShadowDeliveries = $t.ShadowDeliveries; ShadowFees = $t.ShadowDeliveryFees
            NonpositiveScoredDeliveries = $t.NonpositiveScoredDeliveries; SuppressedShopping = $t.SuppressedShopping
            MaximumVisited = $t.MaximumVisited; MaximumActive = $t.MaximumActive
            AdvertisedHorizons = $t.AdvertisedHorizons; UnavailableDurationOffers = $t.UnavailableDurationOffers
            AcceptedHorizons = $t.AcceptedHorizons; OfferedHorizons = $t.OfferedHorizons; BackedHorizons = $t.BackedHorizons
            ShortenedOffers = $t.ShortenedOffers; AbandonedOpportunities = $t.AbandonedOpportunities
            AcceptedOpportunities = $acceptedTotal; ConsumedOpportunities = $consumed; RemainingOpportunities = $remaining
            OpportunityAccounting = $acceptedTotal -eq ($consumed + $t.AbandonedOpportunities + $remaining)
            QuotedCommitmentCost = $t.QuotedCommitmentCost
            DuplicateRelief = $t.Sources[0].ConditionalRelief; DuplicateDeliveries = $t.Sources[0].Deliveries
            DuplicateSelections = $t.Sources[0].Selected
            NativeWallMilliseconds = $t.NativeWallMilliseconds; NativeAllocationBytes = $t.NativeAllocationBytes
            ReportWallMilliseconds = $t.ReportWallMilliseconds; ReportAllocationBytes = $t.ReportAllocationBytes
            Carriers = $inspection.Carriers; Relations = $inspection.Relations; CheckpointBytes = $inspection.Bytes
            PeakCarriers = $t.PeakRetainedCarriers; PeakRelations = $t.PeakRetainedRelations
            Conserved = $t.Conserved -and $c.Accounted
            CostCategoriesAgree = $c.Spent -eq ($e.Inspection + $e.Activation + $e.Cancellation + $e.Calculation + $e.Self)
        }
    })
if ($rows.Count -ne $campaign.Runs -or @($rows | Where-Object Observations -ne $campaign.Steps).Count -ne 0) {
    throw 'The declared campaign is incomplete; do not summarize a partial comparison as a finished one.'
}
if (@($rows.SourceFingerprint | Sort-Object -Unique).Count -ne 1 -or
    @($rows.ProtocolFingerprint | Sort-Object -Unique).Count -ne 1) {
    throw 'Mixed implementations or protocols cannot be presented as the frozen campaign.'
}
$rows | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Directory 'comparison.json') -Encoding utf8
$rows | Format-Table Run, SelfError, DeliveredError, Cost, Inspection, SuppressedShopping, SelfSelections, ShadowDeliveries

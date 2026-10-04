param(
    [string]$Baseline = "$PSScriptRoot/results-lifecycle",
    [string]$Current = "$PSScriptRoot/results-dynamic"
)

$ErrorActionPreference = 'Stop'

# Observer-only comparison. Neither these hashes nor replay projections enter
# the native learner. Exclude physical identities and diagnostic graph counts;
# compare every other field, including all recorded numeric/boolean behavior.
function Get-BehaviorHash([string]$Path) {
    $replay = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    foreach ($frame in $replay.frames) {
        foreach ($field in @('nativeRelations', 'nativeCarriers', 'nativeAttachedRelations')) {
            $frame.PSObject.Properties.Remove($field)
        }
        foreach ($node in $frame.nodes) { $node.PSObject.Properties.Remove('id') }
    }
    $canonical = ConvertTo-Json -InputObject $replay -Depth 100 -Compress
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($canonical)))
}

$comparisons = foreach ($name in @('changing-patterns', 'large-zigzag-control',
    'ordered-pulse-control', 'reordered-and-flat', 'shuffled-pulse-control')) {
    $oldHash = Get-BehaviorHash (Join-Path $Baseline "$name.json")
    $newHash = Get-BehaviorHash (Join-Path $Current "$name.json")
    [pscustomobject]@{
        replay = "$name.json"
        sameObservedBehavior = $oldHash -ceq $newHash
        lifecycleSha256 = $oldHash
        dynamicSha256 = $newHash
    }
}
$comparisons | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Current 'behavior-comparison.json')
$comparisons | Select-Object replay, sameObservedBehavior | Format-Table
if ($comparisons.Where({ -not $_.sameObservedBehavior }).Count) {
    throw 'Observed behavior changed. Inspect the replay difference before claiming equivalence.'
}

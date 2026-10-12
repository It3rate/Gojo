param(
    [ValidateRange(1, 1440)][int]$Minutes = 5,
    [int]$Seed = 7,
    [ValidateRange(1, 100)][int]$Population = 20,
    [ValidateRange(1, 86400)][int]$WallSeconds = 120,
    [string]$OutputDirectory = '',
    [switch]$FixedResources
)
$ErrorActionPreference = 'Stop'
$repoPath = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoPath ('artifacts/phototaxis/training-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$projectPath = Join-Path $PSScriptRoot 'Phototaxis.Runner.csproj'
Write-Host "Training $Minutes simulated minutes at maximum speed, up to $WallSeconds wall seconds. Ctrl+C stops and saves."
$trainingArgs = @('train', ($Minutes * 60), $Seed, $Population, $OutputDirectory, $WallSeconds)
if ($FixedResources) { $trainingArgs += '--fixed-resources' }
& dotnet run --project $projectPath -c Release -- @trainingArgs
if ($LASTEXITCODE -ne 0) { throw 'Training failed.' }
Write-Host "Results: $OutputDirectory"

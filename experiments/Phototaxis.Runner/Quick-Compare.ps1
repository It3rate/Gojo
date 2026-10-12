param([int]$Seconds = 60, [string]$Seeds = '7,19', [string]$Output = '', [switch]$SkipBuild,
    [ValidateSet('vision', 'ecology', 'roving')][string]$Experiment = 'vision')
$ErrorActionPreference = 'Stop'
$runnerProject = Join-Path $PSScriptRoot 'Phototaxis.Runner.csproj'
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $PSScriptRoot "../../artifacts/phototaxis/$Experiment/$(Get-Date -Format 'yyyyMMdd-HHmmss')"
}
if (-not $SkipBuild) {
    & dotnet build $runnerProject -c Release --nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Native runner build failed.' }
}
& dotnet run --no-build --project $runnerProject -c Release -- $Experiment $Seconds $Seeds $Output
if ($LASTEXITCODE -ne 0) { throw 'Comparison failed.' }

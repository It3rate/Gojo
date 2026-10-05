param(
    [string]$Replay,
    [int]$Frame = 0,
    [string]$Render
)

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'NegotiatedPorts.Viewer.csproj'
$replayPath = if ($Replay) {
    [System.IO.Path]::GetFullPath($Replay)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../NegotiatedPorts/results/changing-input.json'))
}
$viewerArguments = @(('--replay=' + $replayPath), ('--frame=' + $Frame))
if ($Render) { $viewerArguments += '--render=' + [System.IO.Path]::GetFullPath($Render) }
& dotnet run --project $projectPath -c Release -- @viewerArguments
exit $LASTEXITCODE

param([int]$Port = 4318, [switch]$SkipBuild, [string]$KeyFile = '')
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'Phototaxis.Browser.csproj'
$publishPath = Join-Path $PSScriptRoot 'publish-browser'
if (-not $SkipBuild) {
    & dotnet publish $projectPath -c Release -o $publishPath -p:UseSharedCompilation=false --nologo -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Browser publish failed.' }
}
$pythonPath = 'C:\Program Files\Python310\python.exe'
if (-not (Test-Path -LiteralPath $pythonPath)) { $pythonPath = (Get-Command python -ErrorAction Stop).Source }
$serverPath = Join-Path $PSScriptRoot 'serve.py'
$contentPath = Join-Path $publishPath 'wwwroot'
Write-Host "Open http://127.0.0.1:$Port (Ctrl+C stops the preview)."
& $pythonPath $serverPath $contentPath $Port $KeyFile
if ($LASTEXITCODE -ne 0) { throw "Preview server exited with code $LASTEXITCODE." }

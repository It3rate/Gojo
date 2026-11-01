$ErrorActionPreference = 'Stop'
$explorerProject = Join-Path $PSScriptRoot 'Gojo.PrimitiveExplorer.csproj'
dotnet run --project $explorerProject
if ($LASTEXITCODE -ne 0) { throw "Primitive Explorer exited with code $LASTEXITCODE" }

param([string]$GameDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $GameDir 'Cities_Data\Managed'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$out = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$outArg = '/out:' + (Join-Path $out 'MetropolisTerrainLots.dll')
$icitiesArg = '/reference:' + (Join-Path $managed 'ICities.dll')
$unityArg = '/reference:' + (Join-Path $managed 'UnityEngine.dll')
$gameArg = '/reference:' + (Join-Path $managed 'Assembly-CSharp.dll')
$colossalArg = '/reference:' + (Join-Path $managed 'ColossalManaged.dll')
& $compiler /nologo /target:library /optimize+ $outArg $icitiesArg $unityArg $gameArg $colossalArg (Join-Path $PSScriptRoot 'src\LotGeometry.cs') (Join-Path $PSScriptRoot 'src\LotDraft.cs') (Join-Path $PSScriptRoot 'src\MetropolisTerrainLots.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$testOut = '/out:' + (Join-Path $out 'LotGeometryTests.exe')
& $compiler /nologo /target:exe $testOut (Join-Path $PSScriptRoot 'src\LotGeometry.cs') (Join-Path $PSScriptRoot 'src\LotDraft.cs') (Join-Path $PSScriptRoot 'tests\LotGeometryTests.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $out 'LotGeometryTests.exe')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

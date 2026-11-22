param(
    [string]$DotNet = 'dotnet',
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [Parameter(Mandatory=$true)][string]$ResultsDirectory
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$BuildDirectory = [IO.Path]::GetFullPath($BuildDirectory)
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory $ResultsDirectory -Force | Out-Null
Push-Location $root
try {
    & $DotNet run --project MissionPlannerTests\ParameterDescriptions\ParameterDescriptions.csproj -c Release
    if ($LASTEXITCODE) { throw 'Parameter description core tests failed' }
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $probe = Join-Path $ResultsDirectory 'ParameterTableProbe.exe'
    & $csc /nologo /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "/out:$probe" MissionPlannerTests\ParameterDescriptions\ParameterTableProbe.cs.txt
    if ($LASTEXITCODE) { throw 'Parameter table probe compilation failed' }
    Copy-Item (Join-Path $BuildDirectory 'MissionPlanner.exe.config') "$probe.config" -Force
    & $probe $BuildDirectory $ResultsDirectory
    if ($LASTEXITCODE) { throw 'Parameter table UI checks failed' }
} finally { Pop-Location }

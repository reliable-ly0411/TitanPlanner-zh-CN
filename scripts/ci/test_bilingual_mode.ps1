param(
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [Parameter(Mandatory=$true)][string]$ResultsDirectory,
    [switch]$AllowInteractive
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$BuildDirectory = [IO.Path]::GetFullPath($BuildDirectory)
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory $ResultsDirectory -Force | Out-Null
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$probe = Join-Path $ResultsDirectory 'NativeModeProbe.exe'
& $csc /nologo /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll `
    "/out:$probe" (Join-Path $root 'MissionPlannerTests\FlightModeDisplay\NativeModeProbe.cs.txt')
if ($LASTEXITCODE) { throw 'Native mode probe compilation failed' }
Copy-Item (Join-Path $BuildDirectory 'MissionPlanner.exe.config') "$probe.config" -Force
$probeArgs = @()
if ($AllowInteractive) { $probeArgs += '--allow-interactive' }
foreach ($culture in @('en-US','zh-Hans','zh-CN')) {
    & $probe $BuildDirectory (Join-Path $ResultsDirectory $culture) $culture @probeArgs 2>&1 |
        Tee-Object -FilePath (Join-Path $ResultsDirectory "$culture.log")
    if ($LASTEXITCODE) { throw "Native bilingual mode checks failed for $culture" }
}

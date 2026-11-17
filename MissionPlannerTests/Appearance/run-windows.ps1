param(
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [Parameter(Mandatory=$true)][string]$ResultsDirectory
)
$ErrorActionPreference = 'Stop'
$BuildDirectory = (Resolve-Path $BuildDirectory).Path
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path $ResultsDirectory) { throw 'Use a fresh isolated results directory.' }
New-Item -ItemType Directory $ResultsDirectory | Out-Null
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$probe = Join-Path $ResultsDirectory 'AppearanceNativeProbe.exe'
& $csc /nologo /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "/out:$probe" (Join-Path $PSScriptRoot 'NativeProbe.cs.txt')
if ($LASTEXITCODE) { throw 'Appearance probe compilation failed.' }
Copy-Item (Join-Path $BuildDirectory 'MissionPlanner.exe.config') "$probe.config"
foreach ($culture in @('zh-CN', 'zh-Hans', 'en-US')) {
    $output = Join-Path $ResultsDirectory $culture
    foreach ($phase in @('seed', 'verify', 'damage', 'fallback', 'reset', 'defaults')) {
        & $probe $BuildDirectory $output $culture $phase
        if ($LASTEXITCODE) { throw "Appearance probe failed: $culture $phase" }
    }
}
# Separate processes test disk persistence. Full app launch, pinned taskbar icons and real DPI remain manual checks.

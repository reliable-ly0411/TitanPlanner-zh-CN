# Tests the actual bitmask control with inert UI events and a recording write stub.
# Supply a writable QA directory; a fresh per-run subdirectory retains the evidence.
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$csc = Join-Path $env:SystemRoot 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $csc -PathType Leaf)) {
    throw 'The existing Windows .NET Framework C# compiler is required.'
}

$source = Join-Path $root 'Controls/MavlinkCheckBoxBitMask.cs'
$parentSource = [IO.File]::ReadAllText((Join-Path $root 'GCSViews/ConfigurationView/ConfigFriendlyParams.cs'))
$template = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'tests/fixtures/BitmaskRefreshProbe.cs.txt'))
$pattern = '(?m)^[ \t]*\(\(MavlinkCheckBoxBitMask\)items\[0\]\)\.ValueChanged -= Control_ValueChanged;\r?\n[ \t]*\(\(MavlinkCheckBoxBitMask\)items\[0\]\)\.Value = Convert\.ToSingle\(value\);\r?\n[ \t]*\(\(MavlinkCheckBoxBitMask\)items\[0\]\)\.ValueChanged \+= Control_ValueChanged;'
$reuse = [regex]::Matches($parentSource, $pattern)
if ($reuse.Count -ne 1 -or ([regex]::Matches($template, '/\*EXACT_REUSE\*/')).Count -ne 1) {
    throw 'The pinned refresh excerpt or test template changed; inspect the real refresh path.'
}

$dir = Join-Path (Resolve-Path -LiteralPath $OutputDirectory).Path ('bitmask-qa-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $dir | Out-Null
$probe = Join-Path $dir 'Probe.cs'
$binary = Join-Path $dir 'Probe.exe'
[IO.File]::WriteAllText($probe, $template.Replace('/*EXACT_REUSE*/', $reuse[0].Value.Trim()))
& $csc /noconfig /nologo /warnaserror+ /r:System.dll /r:System.Core.dll /r:System.Drawing.dll "/out:$binary" $probe $source
if ($LASTEXITCODE -ne 0) { throw "Offline bitmask probe failed to compile: $LASTEXITCODE" }

& $binary | Tee-Object -FilePath (Join-Path $dir 'probe.log')
if ($LASTEXITCODE -ne 0) { throw "Offline bitmask probe failed: $LASTEXITCODE" }

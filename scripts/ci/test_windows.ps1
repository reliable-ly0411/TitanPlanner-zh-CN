param(
    [string]$DotNet = 'dotnet',
    [Parameter(Mandatory=$true)][string]$BuildDirectory,
    [Parameter(Mandatory=$true)][string]$ResultsDirectory,
    [switch]$IncludeNetwork
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$BuildDirectory = [IO.Path]::GetFullPath($BuildDirectory)
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory $ResultsDirectory -Force | Out-Null
Push-Location $root
try {
    & $DotNet run --project MissionPlannerTests\Localization\Localization.csproj -c Release
    if ($LASTEXITCODE) { throw 'Translation runtime tests failed' }
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $probe = Join-Path $ResultsDirectory 'LanguageVariantsProbe.exe'
    & $csc /nologo /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "/out:$probe" MissionPlannerTests\Localization\LanguageVariantsProbe.cs.txt
    if ($LASTEXITCODE) { throw 'UI probe compilation failed' }
    # Resolve dependency versions exactly as the built application does.
    Copy-Item (Join-Path $BuildDirectory 'MissionPlanner.exe.config') "$probe.config" -Force
    foreach ($culture in @('zh-Hans','zh-CN')) {
        & $probe $BuildDirectory (Join-Path $ResultsDirectory $culture) $culture
        if ($LASTEXITCODE) { throw "UI probe failed for $culture" }
    }
    $sitlProbe = Join-Path $ResultsDirectory 'SitlLayoutProbe.exe'
    & $csc /nologo /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll "/out:$sitlProbe" MissionPlannerTests\Localization\SitlLayoutProbe.cs.txt
    if ($LASTEXITCODE) { throw 'SITL layout probe compilation failed' }
    Copy-Item (Join-Path $BuildDirectory 'MissionPlanner.exe.config') "$sitlProbe.config" -Force
    foreach ($culture in @('en-US','zh-Hans','zh-CN')) {
        & $sitlProbe $BuildDirectory (Join-Path $ResultsDirectory "sitl-$culture") $culture
        if ($LASTEXITCODE) { throw "SITL layout probe failed for $culture" }
    }
    $filter = @('--filter','TestCategory!=Network&TestCategory!=Hardware')
    if ($IncludeNetwork) { $filter = @('--filter','TestCategory!=Hardware') }
    & $DotNet test MissionPlannerTests\MissionPlannerTests.csproj -c Release -m:2 `
        "-p:TitanPlannerOutputDirectory=$($BuildDirectory.TrimEnd('\'))\" `
        -p:GenerateResourceUsePreserializedResources=true -p:GenerateSerializationAssemblies=Off `
        --logger 'trx;LogFileName=windows-tests.trx' --results-directory $ResultsDirectory @filter -- RunConfiguration.TestSessionTimeout=180000
    if ($LASTEXITCODE) { throw 'Windows regression tests failed' }
} finally {
    Pop-Location
}

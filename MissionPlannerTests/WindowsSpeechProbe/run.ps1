[CmdletBinding()]
param(
    [string]$BinaryDirectory = $PSScriptRoot,
    [string]$OfflinePackDirectory = ''
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT -or
    -not [Environment]::Is64BitOperatingSystem) {
    throw '此薄件需要 64 位 Windows。'
}
$source = (Resolve-Path -LiteralPath $BinaryDirectory).ProviderPath
$resources = 'WindowsSpeechProbe.resources.dll'
$parts = @('WindowsSpeechProbe.exe', 'WindowsSpeechProbe.exe.config',
           "zh-CN\$resources", "zh-Hans\$resources")
foreach ($part in $parts) {
    if (-not [IO.File]::Exists((Join-Path $source $part))) { throw "薄件缺少 $part" }
}
$runRoot = Join-Path ([IO.Path]::GetTempPath()) ('TitanPlanner-SpeechProbe-' + [Guid]::NewGuid().ToString('N'))
if ([IO.Directory]::Exists($runRoot)) { throw '临时目录已经存在。' }
[void][IO.Directory]::CreateDirectory($runRoot)
$overall = 0
try {
    foreach ($variant in @('full', 'neutral')) {
        $bin = Join-Path $runRoot $variant
        [void][IO.Directory]::CreateDirectory($bin)
        foreach ($part in $parts) {
            if ($variant -eq 'neutral' -and $part.StartsWith('zh-CN\')) { continue }
            $destination = Join-Path $bin $part
            [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
            [IO.File]::Copy((Join-Path $source $part), $destination)
        }
    }

    $scenarios = @('auto', 'choose', 'reload', 'missing', 'neutral', 'priority')
    if (-not [string]::IsNullOrWhiteSpace($OfflinePackDirectory)) {
        $packSource = (Resolve-Path -LiteralPath $OfflinePackDirectory).ProviderPath
        if (-not [IO.Directory]::Exists($packSource)) { throw '离线晓晓音色包路径必须是目录。' }
        $utf8 = New-Object System.Text.UTF8Encoding -ArgumentList $false
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $offlineTexts = @('这是地面站语音试听。', '日志存储芯片已满，已停止记录日志')
            $target = Join-Path $runRoot 'settings/SpeechProbe/Speech/zh-CN-XiaoxiaoNeural'
            [void][IO.Directory]::CreateDirectory($target)
            foreach ($text in $offlineTexts) {
                $normalized = $text.Normalize([System.Text.NormalizationForm]::FormC)
                $name = [BitConverter]::ToString($sha.ComputeHash($utf8.GetBytes($normalized))).Replace('-', '').ToLowerInvariant() + '.wav'
                $sourceFile = Join-Path $packSource $name
                if (-not [IO.File]::Exists($sourceFile)) { throw "离线音色包缺少 $name" }
                [IO.File]::Copy($sourceFile, (Join-Path $target $name))
            }
        } finally {
            $sha.Dispose()
        }
        $scenarios += 'offline'
    }

    # 每一组都是新进程；只有本次生成的临时配置在组选音与重新读取之间共享。
    foreach ($scenario in $scenarios) {
        $variant = if ($scenario -eq 'neutral') { 'neutral' } else { 'full' }
        $bin = Join-Path $runRoot $variant
        Write-Host "[$scenario]"
        $process = New-Object System.Diagnostics.Process
        try {
            $process.StartInfo.FileName = Join-Path $bin 'WindowsSpeechProbe.exe'
            $process.StartInfo.Arguments = $scenario
            $process.StartInfo.WorkingDirectory = $bin
            $process.StartInfo.UseShellExecute = $false
            if (-not $process.Start()) { throw '未能启动本组薄件。' }
            if (-not $process.WaitForExit(150000)) {
                $process.Kill()
                $process.WaitForExit()
                Write-Host '失败  本组超时。'
                $overall = 1
            } elseif ($process.ExitCode -eq 2) {
                if ($overall -eq 0) { $overall = 2 }
            } elseif ($process.ExitCode -ne 0) {
                $overall = 1
            }
        } finally {
            $process.Dispose()
        }
    }
} finally {
    [IO.Directory]::Delete($runRoot, $true)
}
Write-Host "汇总退出码 $overall（0 通过，1 失败，2 部分待验）"
exit $overall

param([string] $CodexDirectory = $(if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE '.codex' }))
$ErrorActionPreference = 'Stop'
$loaderPath = Join-Path $PSScriptRoot 'load-project-rules.ps1'
if (-not (Test-Path -LiteralPath $loaderPath)) { throw 'The rule loader is missing.' }
$configPath = Join-Path $CodexDirectory 'hooks.json'
$config = if (Test-Path -LiteralPath $configPath) { Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json } else { [pscustomobject]@{} }
$before = $config | ConvertTo-Json -Depth 30 -Compress
if (-not $config.PSObject.Properties['hooks']) { $config | Add-Member -NotePropertyName hooks -NotePropertyValue ([pscustomobject]@{}) }
$command = 'powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + $loaderPath + '"'
foreach ($eventName in @('SessionStart', 'UserPromptSubmit')) {
    if (-not $config.hooks.PSObject.Properties[$eventName]) { continue }
    $keptGroups = [System.Collections.Generic.List[object]]::new()
    foreach ($group in @($config.hooks.$eventName)) {
        $keptHandlers = @($group.hooks | Where-Object { $_.command -ne $command })
        if ($keptHandlers.Count -gt 0) {
            $group.hooks = $keptHandlers
            $keptGroups.Add($group)
        }
    }
    if ($keptGroups.Count -eq 0) {
        $config.hooks.PSObject.Properties.Remove($eventName)
    } else {
        $config.hooks.$eventName = @($keptGroups.ToArray())
    }
}
$handler = [pscustomobject]@{ type = 'command'; command = $command; timeout = 15; additionalContextLimit = 0 }
$sessionGroups = @(if ($config.hooks.PSObject.Properties['SessionStart']) { $config.hooks.SessionStart })
$sessionGroups += [pscustomobject]@{ hooks = @($handler) }
$config.hooks | Add-Member -NotePropertyName SessionStart -NotePropertyValue @($sessionGroups) -Force
$after = $config | ConvertTo-Json -Depth 30 -Compress
if ($after -cne $before) {
    New-Item -ItemType Directory -Path $CodexDirectory -Force | Out-Null
    if (Test-Path -LiteralPath $configPath) {
        $backupPath = $configPath + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
        Copy-Item -LiteralPath $configPath -Destination $backupPath
    }
    $json = $config | ConvertTo-Json -Depth 30
    [System.IO.File]::WriteAllText($configPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}
Write-Output "Configured one vlg SessionStart hook: $configPath"
Write-Output 'The vlg UserPromptSubmit handler is removed. Review the SessionStart definition in Codex Settings > Hooks if Codex requests trust.'

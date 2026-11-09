param([string] $CodexDirectory = $(if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE '.codex' }))
$ErrorActionPreference = 'Stop'
$loaderPath = Join-Path $PSScriptRoot 'load-project-rules.ps1'
if (-not (Test-Path -LiteralPath $loaderPath)) { throw 'The rule loader is missing.' }
$configPath = Join-Path $CodexDirectory 'hooks.json'
$config = if (Test-Path -LiteralPath $configPath) { Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json } else { [pscustomobject]@{} }
if (-not $config.PSObject.Properties['hooks']) { $config | Add-Member -NotePropertyName hooks -NotePropertyValue ([pscustomobject]@{}) }
$command = 'powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + $loaderPath + '"'
$changed = $false
foreach ($eventName in @('SessionStart', 'UserPromptSubmit')) {
    $groups = if ($config.hooks.PSObject.Properties[$eventName]) { @($config.hooks.$eventName) } else { @() }
    $alreadyInstalled = @($groups | ForEach-Object { $_.hooks } | Where-Object { $_.command -eq $command })
    if ($alreadyInstalled.Count -gt 0) { continue }
    $handler = [pscustomobject]@{ type = 'command'; command = $command; timeout = 15; additionalContextLimit = 0 }
    $groups += [pscustomobject]@{ hooks = @($handler) }
    $config.hooks | Add-Member -NotePropertyName $eventName -NotePropertyValue @($groups) -Force
    $changed = $true
}
if ($changed) {
    New-Item -ItemType Directory -Path $CodexDirectory -Force | Out-Null
    if (Test-Path -LiteralPath $configPath) {
        $backupPath = $configPath + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
        Copy-Item -LiteralPath $configPath -Destination $backupPath
    }
    $json = $config | ConvertTo-Json -Depth 30
    [System.IO.File]::WriteAllText($configPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}
Write-Output "Configured: $configPath"
Write-Output 'Review and trust both new hooks in Codex Settings > Hooks (or /hooks in the CLI). They remain inactive until trusted.'

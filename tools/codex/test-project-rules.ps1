$ErrorActionPreference = 'Stop'
$hostExe = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$testTempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$fixture = Join-Path $testTempRoot ('vlg-hook-test-' + [guid]::NewGuid().ToString('N'))
$project = Join-Path $fixture 'project'
$oldWorktree = Join-Path $fixture 'old-worktree'
$otherProject = Join-Path $fixture 'project-other'
$passed = 0

function Git-Checked([string] $directory, [string[]] $gitArguments) {
    & git -C $directory @gitArguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Fixture Git command failed: $gitArguments" }
}
function Assert-Hook([string] $label, [string] $directory, [string] $eventName, [string] $expected, [string] $source = 'startup') {
    $payload = @{ cwd = $directory; hook_event_name = $eventName; source = $source; prompt = 'Repair the missing rules file if necessary.' } | ConvertTo-Json -Compress
    $output = $payload | & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $project 'tools\codex\load-project-rules.ps1')
    if ($LASTEXITCODE -ne 0) { throw "$label exited with $LASTEXITCODE." }
    if ($expected -eq 'empty') {
        if ($output) { throw "$label unexpectedly supplied context." }
    } else {
        $result = $output | ConvertFrom-Json
        foreach ($field in @('decision', 'continue', 'stopReason', 'permissionDecision')) {
            if ($result.PSObject.Properties[$field] -or ($result.hookSpecificOutput -and $result.hookSpecificOutput.PSObject.Properties[$field])) { throw "$label returned control field $field; the loader must only supply context." }
        }
        if ($result.hookSpecificOutput.hookEventName -ne $eventName) { throw "$label returned the wrong event." }
        $context = $result.hookSpecificOutput.additionalContext
        if (-not $context -or [System.Text.Encoding]::UTF8.GetByteCount($context) -gt 65536) { throw "$label returned empty or oversized context." }
        if ($expected -eq 'context') {
            foreach ($marker in @('GUIDE_CONTENT', 'RULES_CONTENT', 'CODE_CONTENT', 'INDEX_CONTENT', ([string][char]0x416))) {
                if ([regex]::Matches($context, [regex]::Escape($marker)).Count -ne 1) { throw "$label omitted or repeated $marker." }
            }
        } elseif (-not $result.systemMessage -or -not $context.Contains('incomplete')) {
            throw "$label omitted the non-blocking diagnostic."
        }
    }
    $script:passed++
    Write-Output "PASS: $label"
}
try {
    New-Item -ItemType Directory -Path $project, $otherProject -Force | Out-Null
    Git-Checked $project @('init', '-q', '-b', 'dev')
    Git-Checked $project @('-c', 'user.name=Hook Test', '-c', 'user.email=hook-test@example.invalid', 'commit', '-q', '--allow-empty', '-m', 'old checkout without instructions')
    Git-Checked $project @('worktree', 'add', '-q', '--detach', $oldWorktree, 'HEAD')
    New-Item -ItemType Directory -Path (Join-Path $project 'tools\codex'), (Join-Path $project '.agent\memory'), (Join-Path $oldWorktree 'nested') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'load-project-rules.ps1') -Destination (Join-Path $project 'tools\codex\load-project-rules.ps1')
    $files = @{ 'AGENTS.md'='GUIDE_CONTENT'; 'rules.md'=('RULES_CONTENT ' + [char]0x416); 'code-rules.md'='CODE_CONTENT'; '.agent/memory/INDEX.md'='INDEX_CONTENT' }
    foreach ($name in $files.Keys) { [System.IO.File]::WriteAllText((Join-Path $project $name), $files[$name], [System.Text.UTF8Encoding]::new($false)) }
    Git-Checked $project @('add', '.')
    Git-Checked $project @('-c', 'user.name=Hook Test', '-c', 'user.email=hook-test@example.invalid', 'commit', '-q', '-m', 'current rules')
    Git-Checked $otherProject @('init', '-q', '-b', 'dev')
    Assert-Hook 'new session' $project 'SessionStart' 'context'
    $codeRules = Join-Path $project 'code-rules.md'
    Remove-Item -LiteralPath $codeRules
    Assert-Hook 'missing rules allow repair with a diagnostic' $project 'SessionStart' 'diagnostic'
    Assert-Hook 'repair prompt is never intercepted' $project 'UserPromptSubmit' 'empty'
    [System.IO.File]::WriteAllText($codeRules, '')
    Assert-Hook 'empty rules allow recovery' $project 'SessionStart' 'diagnostic'
    [System.IO.File]::WriteAllText($codeRules, ('x' * 66000))
    Assert-Hook 'oversized rules allow recovery without oversized output' $project 'SessionStart' 'diagnostic'
    Git-Checked $project @('restore', 'code-rules.md')
    Assert-Hook 'repaired rules load normally' $project 'SessionStart' 'context'
    foreach ($source in @('resume', 'clear', 'compact', 'fork')) { Assert-Hook $source $project 'SessionStart' 'context' $source }
    Assert-Hook 'ordinary prompt adds no duplicate context' $project 'UserPromptSubmit' 'empty'
    Assert-Hook 'follow-up prompt adds no duplicate context' $project 'UserPromptSubmit' 'empty'
    Assert-Hook 'old detached worktree without AGENTS.md' $oldWorktree 'SessionStart' 'context'
    Assert-Hook 'worktree subdirectory' (Join-Path $oldWorktree 'nested') 'SessionStart' 'context'
    Assert-Hook 'unrelated repository' $otherProject 'SessionStart' 'empty'
    Assert-Hook 'non-repository directory' $fixture 'SessionStart' 'empty'
    Assert-Hook 'unconfigured event' $project 'Stop' 'empty'
    Git-Checked $project @('checkout', '-q', '--detach')
    Assert-Hook 'committed dev fallback without dev checkout' $oldWorktree 'SessionStart' 'context'

    $testCodex = Join-Path $fixture 'codex-home'
    New-Item -ItemType Directory -Path $testCodex | Out-Null
    $loaderCommand = 'powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + (Join-Path $PSScriptRoot 'load-project-rules.ps1') + '"'
    $legacyHandler = @{ type='command'; command=$loaderCommand; timeout=15; additionalContextLimit=0 }
    $legacy = @{ description='Existing hooks'; hooks=@{
        Stop=@(@{ hooks=@(@{ type='command'; command='existing-handler' }) });
        SessionStart=@(@{ hooks=@($legacyHandler) }, @{ hooks=@($legacyHandler, @{ type='command'; command='existing-session-handler' }) });
        UserPromptSubmit=@(@{ hooks=@($legacyHandler, @{ type='command'; command='existing-prompt-handler' }) })
    } }
    $configPath = Join-Path $testCodex 'hooks.json'
    [System.IO.File]::WriteAllText($configPath, ($legacy | ConvertTo-Json -Depth 10))
    $installerPath = Join-Path $PSScriptRoot 'install-project-rules.ps1'
    & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $installerPath -CodexDirectory $testCodex | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'First installer run failed.' }
    $firstInstall = [System.IO.File]::ReadAllText($configPath)
    & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $installerPath -CodexDirectory $testCodex | Out-Null
    if ($LASTEXITCODE -ne 0 -or [System.IO.File]::ReadAllText($configPath) -cne $firstInstall) { throw 'Installer is not idempotent.' }
    $installed = $firstInstall | ConvertFrom-Json
    $sessionHandlers = @($installed.hooks.SessionStart | ForEach-Object { $_.hooks })
    $promptHandlers = @($installed.hooks.UserPromptSubmit | ForEach-Object { $_.hooks })
    if ($installed.description -ne 'Existing hooks' -or $installed.hooks.Stop[0].hooks[0].command -ne 'existing-handler' -or $sessionHandlers.command -notcontains 'existing-session-handler' -or $promptHandlers.command -notcontains 'existing-prompt-handler') { throw 'Installer overwrote unrelated hooks.' }
    if (@($sessionHandlers | Where-Object { $_.command -eq $loaderCommand }).Count -ne 1 -or $promptHandlers.command -contains $loaderCommand) { throw 'Installer retained duplicate or per-prompt vlg hooks.' }
    if (@(Get-ChildItem -LiteralPath $testCodex -Filter 'hooks.json.backup-*').Count -ne 1) { throw 'Installer backup/idempotence failed.' }
    $passed++
    Write-Output 'PASS: migration removes only vlg duplicates and is idempotent'
    $freshCodex = Join-Path $fixture 'fresh-codex-home'
    & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $installerPath -CodexDirectory $freshCodex | Out-Null
    $fresh = Get-Content -LiteralPath (Join-Path $freshCodex 'hooks.json') -Raw | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or $fresh.hooks.PSObject.Properties['UserPromptSubmit'] -or @($fresh.hooks.SessionStart).Count -ne 1) { throw 'Fresh install must contain only one SessionStart hook.' }
    $passed++
    Write-Output 'PASS: fresh install contains one SessionStart hook'
    Write-Output "$passed hook integration checks passed."
} finally {
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixture)
    if (-not $resolvedFixture.StartsWith($testTempRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}

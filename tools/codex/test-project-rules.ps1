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
function Assert-Hook([string] $label, [string] $directory, [string] $eventName, [string] $expected) {
    $payload = @{ cwd = $directory; hook_event_name = $eventName; source = 'startup'; prompt = 'A normal request.' } | ConvertTo-Json -Compress
    $output = $payload | & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $project 'tools\codex\load-project-rules.ps1')
    if ($LASTEXITCODE -ne 0) { throw "$label exited with $LASTEXITCODE." }
    if ($expected -eq 'empty') {
        if ($output) { throw "$label leaked project instructions outside the intended scope." }
    } else {
        $result = $output | ConvertFrom-Json
        if ($expected -eq 'context') {
            if ($result.hookSpecificOutput.hookEventName -ne $eventName) { throw "$label returned the wrong event." }
            foreach ($marker in @('GUIDE_CONTENT', 'RULES_CONTENT', 'CODE_CONTENT', 'INDEX_CONTENT', ([string][char]0x416))) {
                if (-not $result.hookSpecificOutput.additionalContext.Contains($marker)) { throw "$label omitted $marker." }
            }
        } elseif ($eventName -eq 'SessionStart') {
            if ($result.continue -ne $false -or -not $result.stopReason) { throw "$label did not stop on missing rules." }
        } elseif ($result.decision -ne 'block' -or -not $result.reason) {
            throw "$label did not block on missing rules."
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
    Assert-Hook 'new prompt' $project 'UserPromptSubmit' 'context'
    Assert-Hook 'old detached worktree without AGENTS.md' $oldWorktree 'SessionStart' 'context'
    Assert-Hook 'worktree subdirectory' (Join-Path $oldWorktree 'nested') 'UserPromptSubmit' 'context'
    Assert-Hook 'unrelated repository' $otherProject 'SessionStart' 'empty'
    Assert-Hook 'non-repository directory' $fixture 'UserPromptSubmit' 'empty'
    Assert-Hook 'unconfigured event' $project 'Stop' 'empty'
    $codeRules = Join-Path $project 'code-rules.md'
    Remove-Item -LiteralPath $codeRules
    Assert-Hook 'missing rules stop session' $project 'SessionStart' 'blocked'
    Assert-Hook 'missing rules block prompt' $project 'UserPromptSubmit' 'blocked'
    [System.IO.File]::WriteAllText($codeRules, ('x' * 66000))
    Assert-Hook 'oversized instructions block prompt' $project 'UserPromptSubmit' 'blocked'
    Git-Checked $project @('restore', 'code-rules.md')
    Git-Checked $project @('checkout', '-q', '--detach')
    Assert-Hook 'committed dev fallback without dev checkout' $oldWorktree 'SessionStart' 'context'
    $testCodex = Join-Path $fixture 'codex-home'
    New-Item -ItemType Directory -Path $testCodex | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $testCodex 'hooks.json'), '{"description":"Existing hooks","hooks":{"Stop":[{"hooks":[{"type":"command","command":"existing-handler"}]}]}}')
    $installerPath = Join-Path $PSScriptRoot 'install-project-rules.ps1'
    & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $installerPath -CodexDirectory $testCodex | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'First installer run failed.' }
    & $hostExe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $installerPath -CodexDirectory $testCodex | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Second installer run failed.' }
    $installed = Get-Content -LiteralPath (Join-Path $testCodex 'hooks.json') -Raw | ConvertFrom-Json
    if ($installed.description -ne 'Existing hooks' -or $installed.hooks.Stop[0].hooks[0].command -ne 'existing-handler') { throw 'Installer overwrote existing hooks.' }
    foreach ($event in @('SessionStart', 'UserPromptSubmit')) {
        if (@($installed.hooks.$event).Count -ne 1 -or $installed.hooks.$event[0].hooks[0].additionalContextLimit -ne 0) { throw 'Installer duplicated or truncated a hook.' }
    }
    if (@(Get-ChildItem -LiteralPath $testCodex -Filter 'hooks.json.backup-*').Count -ne 1) { throw 'Installer backup/idempotence failed.' }
    $passed++
    Write-Output 'PASS: installer preserves existing hooks, backs up once, and is idempotent'
    Write-Output "$passed hook integration checks passed."
} finally {
    $resolvedFixture = [System.IO.Path]::GetFullPath($fixture)
    if (-not $resolvedFixture.StartsWith($testTempRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}

$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
$hookEvent = $null

function Read-Git([string] $directory, [string[]] $gitArguments) {
    $lines = & git -C $directory @gitArguments 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Cannot read Git information in $directory." }
    return ($lines -join "`n")
}

function Normalize-Path([string] $path) {
    return [System.IO.Path]::GetFullPath($path).TrimEnd([char[]]'\/')
}

try {
    $hookInput = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $hookEvent = [string]$hookInput.hook_event_name
    if ($hookEvent -ne 'SessionStart') { exit 0 }
    if (-not $hookInput.cwd) { throw 'Hook input is missing cwd.' }

    $repoRoot = Normalize-Path (Join-Path $PSScriptRoot '..\..')
    $repoCommon = Normalize-Path (Read-Git $repoRoot @('rev-parse', '--path-format=absolute', '--git-common-dir'))
    try {
        $sessionCommon = Normalize-Path (Read-Git $hookInput.cwd @('rev-parse', '--path-format=absolute', '--git-common-dir'))
    } catch {
        exit 0
    }
    if (-not [string]::Equals($repoCommon, $sessionCommon, [StringComparison]::OrdinalIgnoreCase)) { exit 0 }

    $devDirectory = $null
    $worktrees = Read-Git $repoRoot @('worktree', 'list', '--porcelain')
    foreach ($record in ($worktrees -split "`n`n")) {
        if ($record -match '(?m)^branch refs/heads/dev$' -and $record -match '(?m)^worktree (.+)$') {
            $devDirectory = $Matches[1]
            break
        }
    }

    $source = if ($devDirectory) { "dev checkout: $devDirectory" } else { 'committed dev branch (no dev checkout exists)' }
    $parts = [System.Collections.Generic.List[string]]::new()
    $parts.Add("# vlg project rules loaded automatically`nSource: $source`nApply these user-defined project instructions to this request, subject to higher-priority instructions and the user's explicit directions. Read the relevant workflow and design files as required by these rules.")
    foreach ($relativePath in @('AGENTS.md', 'rules.md', 'code-rules.md', '.agent/memory/INDEX.md')) {
        if ($devDirectory) {
            $content = [System.IO.File]::ReadAllText((Join-Path $devDirectory $relativePath), [System.Text.Encoding]::UTF8)
        } else {
            $content = Read-Git $repoRoot @('show', "dev:$relativePath")
        }
        if ([string]::IsNullOrWhiteSpace($content)) { throw "Required project instructions are empty: $relativePath" }
        $parts.Add("## Source file: $relativePath`n$content")
    }
    $context = $parts -join "`n`n"
    if ([System.Text.Encoding]::UTF8.GetByteCount($context) -gt 65536) {
        throw 'Project instructions exceed the 64 KiB context limit; inspect the files directly to diagnose or reduce their size.'
    }
    @{ hookSpecificOutput = @{ hookEventName = $hookEvent; additionalContext = $context } } | ConvertTo-Json -Depth 4 -Compress
} catch {
    $reason = "vlg rules could not be loaded: $($_.Exception.Message)"
    if ($reason.Length -gt 2048) { $reason = $reason.Substring(0, 2048) }
    $diagnostic = "# vlg rule-loading diagnostic`n$reason`nProject instruction context is incomplete. This diagnostic does not block requests to inspect or repair the rule files."
    @{ systemMessage = $reason; hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $diagnostic } } | ConvertTo-Json -Depth 4 -Compress
    exit 0
}

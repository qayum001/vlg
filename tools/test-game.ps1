<#
Builds and tests the game without opening a window or using Computer Use.
Godot input, UI, simulation pause, audio buses and persistence run in headless mode.
All test user data and logs stay under build/game-tests/.
#>
param(
    [string]$Godot = "C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
)
$ErrorActionPreference = "Stop"
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if (-not (Test-Path -LiteralPath $Godot)) { throw "Godot .NET not found: $Godot (pass -Godot <path>)" }
$runDirectory = Join-Path $repo ("build\game-tests\" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
$savedAppData = $env:APPDATA
$savedPhase = $env:VLG_GAME_TEST_PHASE
$savedDirectory = $env:VLG_GAME_TEST_DIRECTORY

function Invoke-Checked([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Exe exited with code $LASTEXITCODE" }
}

Push-Location $repo
try {
    Invoke-Checked "dotnet" @("test", "--project", "tests/Vlg.Core.Tests/Vlg.Core.Tests.csproj")
    Invoke-Checked "dotnet" @("build", "game/Vlg.csproj")
    $env:APPDATA = Join-Path $runDirectory "appdata"
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $env:VLG_GAME_TEST_DIRECTORY = $runDirectory
    Invoke-Checked $Godot @("--headless", "--path", "game", "--import", "--log-file", (Join-Path $runDirectory "import.log"))
    foreach ($phase in @("settings", "reload", "corrupt")) {
        $env:VLG_GAME_TEST_PHASE = $phase
        if ($phase -eq "corrupt") {
            $settingsFile = Join-Path $env:APPDATA "Godot\app_userdata\vlg\settings.json"
            Set-Content -LiteralPath $settingsFile -Value "{invalid json" -Encoding utf8
        }
        $log = Join-Path $runDirectory "$phase.log"
        Invoke-Checked $Godot @("--headless", "--path", "game", "--resolution", "1280x800", "--fixed-fps", "60", "--quit-after", "1800",
            "--scene", "res://tests/game_smoke.tscn", "--log-file", $log)
        if (-not (Select-String -LiteralPath $log -Pattern "^GAME_SMOKE_PASS ${phase}:" -Quiet)) {
            throw "Smoke test did not complete: $phase (see $log)"
        }
    }
    Write-Host "Game tests passed. Logs: $runDirectory"
}
finally {
    $env:APPDATA = $savedAppData
    $env:VLG_GAME_TEST_PHASE = $savedPhase
    $env:VLG_GAME_TEST_DIRECTORY = $savedDirectory
    Pop-Location
}

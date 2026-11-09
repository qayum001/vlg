#requires -Version 5.1
param([switch]$Build)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path $repoRoot 'build\release'
$readyFile = 'release-ready.txt'

try {
    if ($Build) {
        $godot = $env:VLG_GODOT_EXE
        if ([string]::IsNullOrWhiteSpace($godot)) {
            $godot = 'C:\Projects\Games\Godot\engine\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
        }
        if (-not (Test-Path -LiteralPath $godot -PathType Leaf)) {
            throw 'Godot .NET was not found. Set VLG_GODOT_EXE to the Godot 4.7.2 .NET console executable.'
        }

        $releaseDir = Join-Path $releaseRoot (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
        New-Item -ItemType Directory -Path $releaseDir | Out-Null
        $exe = Join-Path $releaseDir 'vlg.exe'
        $projectDir = Join-Path $repoRoot 'game'
        Write-Host 'Building the Windows release...'
        Push-Location $repoRoot
        try {
            & $godot --headless --path $projectDir --export-release 'Windows Desktop' $exe --quit
            if ($LASTEXITCODE -ne 0) {
                throw "Release export failed (exit code $LASTEXITCODE). The previous release is still available."
            }
        }
        finally {
            Pop-Location
        }

        if (-not (Test-Path -LiteralPath $exe -PathType Leaf) -or
            -not (Test-Path -LiteralPath (Join-Path $releaseDir 'data_Vlg_windows_x86_64\Vlg.dll') -PathType Leaf)) {
            throw 'The export did not produce a complete game. The previous release is still available.'
        }
        Set-Content -LiteralPath (Join-Path $releaseDir $readyFile) -Value (Get-Date).ToString('o') -Encoding ASCII
    }
    else {
        $release = $null
        if (Test-Path -LiteralPath $releaseRoot -PathType Container) {
            $release = Get-ChildItem -LiteralPath $releaseRoot -Directory |
                Where-Object {
                    (Test-Path -LiteralPath (Join-Path $_.FullName $readyFile) -PathType Leaf) -and
                    (Test-Path -LiteralPath (Join-Path $_.FullName 'vlg.exe') -PathType Leaf) -and
                    (Test-Path -LiteralPath (Join-Path $_.FullName 'data_Vlg_windows_x86_64\Vlg.dll') -PathType Leaf)
                } |
                Sort-Object Name -Descending |
                Select-Object -First 1
        }
        if ($null -eq $release) {
            throw 'No completed release build found. Run build-and-launch.bat first.'
        }
        $releaseDir = $release.FullName
        $exe = Join-Path $releaseDir 'vlg.exe'
    }

    Write-Host "Launching release: $exe"
    Start-Process -FilePath $exe -WorkingDirectory $releaseDir | Out-Null
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

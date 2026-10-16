<#
Runs the horde stress benchmark matrix on an exported build and collects CSVs.
Each run opens the game in exclusive fullscreen and quits by itself (Esc aborts a run).

Example (Vega 8 iGPU on the dev laptop is GPU index 1; list GPUs with: vlg.console.exe --verbose --quit):
  powershell -ExecutionPolicy Bypass -File tools\bench.ps1 -GpuIndex 1
#>
param(
    [string]$Exe = "$PSScriptRoot\..\build\stress\vlg.console.exe",
    [int]$GpuIndex = -1,
    [string[]]$Modes = @("enemies", "overdraw", "particles"),
    [string[]]$Renderers = @("forward_plus", "mobile"),
    [string[]]$Presets = @("low"),
    [string]$Driver = "d3d12",
    [string]$OutDir = ""
)
$ErrorActionPreference = "Stop"
# "powershell -File" passes "a,b" as one string; split so both call styles work.
$Modes = $Modes -split ","
$Renderers = $Renderers -split ","
$Presets = $Presets -split ","
if (-not (Test-Path $Exe)) { throw "build not found: $Exe (export it first, see features.md)" }
if ($OutDir -eq "") { $OutDir = Join-Path $PSScriptRoot ("..\build\bench\" + (Get-Date -Format "yyyyMMdd_HHmmss")) }
New-Item -ItemType Directory -Force $OutDir | Out-Null

foreach ($preset in $Presets) {
    foreach ($renderer in $Renderers) {
        foreach ($mode in $Modes) {
            $csv = Join-Path $OutDir "$mode`_$preset`_$renderer`_$Driver.csv"
            $engineArgs = @("--rendering-method", $renderer, "--rendering-driver", $Driver)
            if ($GpuIndex -ge 0) { $engineArgs += @("--gpu-index", "$GpuIndex") }
            $gameArgs = @("--", "--bench", $mode, "--preset", $preset, "--out", $csv)
            Write-Host "== $mode / $preset / $renderer / $Driver"
            & $Exe @engineArgs @gameArgs | Select-String -Pattern "^(step|bench)" | ForEach-Object { Write-Host "   $_" }
            if ($LASTEXITCODE -ne 0) { Write-Warning "exit code $LASTEXITCODE" }
        }
    }
}
Write-Host "CSV files: $OutDir"

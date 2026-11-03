# Publishes MemIndicator and installs it to %LOCALAPPDATA%\MemIndicator, replacing a running copy.
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\MemIndicator\MemIndicator.csproj'
$publishDir = Join-Path $PSScriptRoot 'publish'
$installDir = Join-Path $env:LOCALAPPDATA 'MemIndicator'
$installedExe = Join-Path $installDir 'MemIndicator.exe'

dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$running = Get-Process MemIndicator -ErrorAction SilentlyContinue
if ($running) {
    # Ask for a clean exit so the tray icon is removed; force-kill only if that does not work
    try {
        $exitEvent = [Threading.EventWaitHandle]::OpenExisting('Local\MemIndicator-Exit')
        $exitEvent.Set() | Out-Null
        $exitEvent.Dispose()
    } catch [Threading.WaitHandleCannotBeOpenedException] {}
    $running | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
    $running | Where-Object { -not $_.HasExited } | Stop-Process -Force
    $running | Wait-Process
}

New-Item -ItemType Directory -Force $installDir | Out-Null
Copy-Item (Join-Path $publishDir 'MemIndicator.exe') $installedExe -Force
Start-Process $installedExe

Write-Host "MemIndicator instalado em $installedExe"

# Builds CrosshairOverlay.exe from src\Program.cs using the .NET Framework compiler
# that ships with Windows (no SDK, no NuGet, no internet required).

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src  = Join-Path $root 'src\Program.cs'
$csc  = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc - Windows .NET Framework 4.x is required."
}

$refs = @('/r:System.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll')
$code = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'

Write-Host 'Building CrosshairOverlay.exe ...'
& $csc /nologo /codepage:65001 /target:winexe /optimize+ `
    /out:"$root\CrosshairOverlay.exe" @refs $src
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)" }

Write-Host 'Building console build for CLI tools (--selftest / --render / --snapshot) ...'
& $csc /nologo /codepage:65001 /target:exe /optimize+ `
    /out:"$root\CrosshairOverlay.CLI.exe" @refs $src
if ($LASTEXITCODE -ne 0) { throw "CLI build failed ($LASTEXITCODE)" }

Write-Host ''
Write-Host 'Done:'
Write-Host "  $root\CrosshairOverlay.exe      (GUI, double click this)"
Write-Host "  $root\CrosshairOverlay.CLI.exe  (console build, for --selftest etc.)"

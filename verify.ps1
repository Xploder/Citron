param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist\portable'))
$ErrorActionPreference = 'Stop'
$root = $OutputDirectory
foreach ($arch in @('x64','x86')) {
    $worker = Join-Path $PSScriptRoot ".build\workers\$arch\Citron.Worker.exe"
    $raw = & $worker check
    if ($LASTEXITCODE -ne 0) { throw "Read-only check failed for $arch" }
    $result = $raw | ConvertFrom-Json
    if (!$result.ok -or $result.architecture -ne $arch) { throw "Architecture mismatch for $arch" }
    if (!$result.configured) {
        $scan = & $worker scan | ConvertFrom-Json
        if ($LASTEXITCODE -eq 0 -or $scan.ok) { throw 'An unconfigured worker must refuse simulation access' }
        Write-Host "PASS $arch : unconfigured-access guard"
    }
    Write-Host "PASS $arch : read-only check and process architecture"
}
$files = @(Get-ChildItem $root -Recurse -File)
if ($files.Count -ne 1 -or $files[0].Name -ne 'Citron.exe') { throw 'Portable output must contain only Citron.exe' }
if ($files[0].Length -ge 50MB) { throw 'Portable executable exceeded the 50 MiB size budget' }
Write-Host 'PASS single executable and 50 MiB size budget'
exit 0

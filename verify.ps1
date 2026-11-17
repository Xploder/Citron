$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot 'dist\Citron'
foreach ($arch in @('x64','x86')) {
    $worker = Join-Path $root "workers\$arch\Citron.Worker.exe"
    $raw = & $worker check
    if ($LASTEXITCODE -ne 0) { throw "Read-only check failed for $arch" }
    $result = $raw | ConvertFrom-Json
    if (!$result.ok -or $result.architecture -ne $arch) { throw "Architecture mismatch for $arch" }
    if (!$result.configured) {
        $scan = & $worker scan | ConvertFrom-Json
        if ($LASTEXITCODE -eq 0 -or $scan.ok) { throw 'An unconfigured worker must refuse simulation access' }
    }
    Write-Host "PASS $arch : check result and unconfigured-access guard"
}
foreach ($asset in @('Citron.App.exe','Citron.App.pri','Assets\Citron.ico','Assets\CitronPaused.ico','Assets\Citron.png')) {
    if (!(Test-Path (Join-Path $root $asset))) { throw "Missing distribution asset: $asset" }
}
Write-Host 'PASS distribution assets'

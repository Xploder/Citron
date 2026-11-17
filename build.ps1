param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$env:MSBuildEnableWorkloadResolver = 'false'
$root = $PSScriptRoot
$destination = Join-Path $root 'dist\Citron'
dotnet publish "$root\src\Citron.App\Citron.App.csproj" -c $Configuration -r win-x64 --self-contained true -o $destination
if ($LASTEXITCODE -ne 0) { throw 'App build failed' }
foreach ($architecture in @('x64', 'x86')) {
    dotnet publish "$root\src\Citron.Worker\Citron.Worker.csproj" -c $Configuration -r "win-$architecture" --self-contained true -p:PlatformTarget=$architecture -o "$destination\workers\$architecture"
    if ($LASTEXITCODE -ne 0) { throw "Worker $architecture build failed" }
}
Write-Host "Built: $destination\Citron.App.exe"

param([string]$Configuration = 'Release', [string]$DotnetPath = 'dotnet', [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist\portable'), [switch]$InProcessTasks)
$ErrorActionPreference = 'Stop'
$env:MSBuildEnableWorkloadResolver = 'false'
$root = $PSScriptRoot
$cache = Join-Path $root '.build'
$env:DOTNET_CLI_HOME = Join-Path $cache 'dotnet-home'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $cache 'http-cache'
$env:NUGET_SCRATCH = Join-Path $cache 'scratch'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$workers = Join-Path $cache 'workers'
foreach ($architecture in @('x64', 'x86')) {
    & $DotnetPath build "$root\src\Citron.Worker\Citron.Worker.csproj" -c $Configuration "-p:PlatformTarget=$architecture" "-p:IntermediateOutputPath=obj\$architecture\$Configuration\" -o "$workers\$architecture"
    if ($LASTEXITCODE -ne 0) { throw "Worker $architecture build failed" }
}
& $DotnetPath publish "$root\src\Citron.App\Citron.App.csproj" -c $Configuration -r win-x64 --self-contained true "-p:CitronWorkerRoot=$workers" "-p:CitronInProcessTasks=$($InProcessTasks.IsPresent)" -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'App build failed' }
$files = @(Get-ChildItem $OutputDirectory -Recurse -File)
if ($files.Count -ne 1 -or $files[0].Name -ne 'Citron.exe') { throw 'Portable output must contain exactly Citron.exe. Use an empty output directory.' }
Write-Host ("Built one executable: {0} ({1:N2} MiB)" -f $files[0].FullName, ($files[0].Length / 1MB))

<#
.SYNOPSIS
    Builds LogDeck: publishes the self-contained app for each architecture and lays it out
    as the install folder, C:\Program Files\LogDeck.

.DESCRIPTION
    dist\<arch>\ holds LogDeck.exe, resources.pri, the compiled XAML and the Windows App SDK
    runtime. -Zip also writes dist\logdeck-<arch>.zip, the payload cimipkg packages.

.PARAMETER Version
    The release version, YYYY.MM.DD.HHMM. Defaults to the current time.

.PARAMETER Arch
    x64, arm64, or both (the default).

.EXAMPLE
    .\build.ps1 -Arch x64 -Zip
#>
param(
    [string]$Version = (Get-Date -Format 'yyyy.MM.dd.HHmm'),
    [ValidateSet('x64', 'arm64')][string[]]$Arch = @('x64', 'arm64'),
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
. "$PSScriptRoot\eng\Publish-AppResources.ps1"

# The assembly file version takes at most 65535 per part, so YYYY.MM.DD.HHMM is stamped
# as is (HHMM tops out at 2359) and leading zeros are dropped: 2026.10.07.0905 -> 2026.10.7.905.
$parts = $Version.Split('.') | ForEach-Object { [int]$_ }
if ($parts.Count -ne 4) { throw "Version must be YYYY.MM.DD.HHMM, got $Version" }
$fileVersion = $parts -join '.'

$appProject = Join-Path $PSScriptRoot 'src\App\LogDeck.App.csproj'
foreach ($a in $Arch) {
    $rid = "win-$a"
    $out = Join-Path $PSScriptRoot "publish\$rid"
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }

    dotnet publish $appProject -c Release -r $rid --self-contained -o $out `
        -p:Version=$fileVersion -p:AssemblyVersion=$fileVersion -p:FileVersion=$fileVersion `
        -p:InformationalVersion=$Version
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid" }

    if (-not (Publish-AppResources -Arch $a -OutputDir (Resolve-Path $out).Path -AppProjectDir (Resolve-Path 'src\App').Path)) {
        throw "resources.pri generation failed for $rid"
    }
    foreach ($required in 'LogDeck.exe', 'resources.pri', 'Assets\LogDeck.ico') {
        if (-not (Test-Path (Join-Path $out $required))) { throw "Publish output for $rid is missing $required" }
    }

    $dist = Join-Path $PSScriptRoot "dist\$a"
    if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
    New-Item -ItemType Directory -Path $dist -Force | Out-Null
    Copy-Item "$out\*" $dist -Recurse -Force
    Write-Host "LogDeck $Version ($a): $((Get-ChildItem $dist -Recurse -File).Count) files in $dist"

    if ($Zip) {
        $zipPath = Join-Path $PSScriptRoot "dist\logdeck-$a.zip"
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path "$dist\*" -DestinationPath $zipPath -Force
        Write-Host "  $zipPath"
    }
}

[CmdletBinding()]
param(
    [string] $InnoCompiler = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    [string] $OutputDirectory = 'D:\Codexwork\Build\PdfToolbox'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'PdfToolbox\PdfToolbox.csproj'
$publishDirectory = Join-Path $root 'artifacts\publish'

if (-not (Test-Path -LiteralPath $InnoCompiler)) {
    throw "Inno Setup compiler not found: $InnoCompiler"
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& $InnoCompiler (Join-Path $root 'installer\PdfToolbox.iss')
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE"
}

$builtInstaller = Join-Path $root 'artifacts\PdfToolbox-1.0.0-win-x64-Setup.exe'
Copy-Item -LiteralPath $builtInstaller -Destination $OutputDirectory -Force
Get-FileHash -LiteralPath (Join-Path $OutputDirectory 'PdfToolbox-1.0.0-win-x64-Setup.exe') -Algorithm SHA256

[CmdletBinding()]
param(
    [string] $InnoCompiler = '',
    [string] $OutputDirectory = 'D:\Codexwork\Build\PdfToolbox',
    [string] $SignerSubject = 'CN=Mirin',
    [string] $TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'PdfToolbox\PdfToolbox.csproj'
$publishDirectory = Join-Path $root 'artifacts\publish'
$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter 'signtool.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
$certificate = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $SignerSubject -and $_.HasPrivateKey -and ($_.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3') } |
    Sort-Object NotAfter -Descending | Select-Object -First 1

$innoCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
)
if ([string]::IsNullOrWhiteSpace($InnoCompiler)) {
    $InnoCompiler = $innoCandidates | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } | Select-Object -First 1
}
if (-not $InnoCompiler -or -not (Test-Path -LiteralPath $InnoCompiler)) {
    throw "Inno Setup compiler not found: $InnoCompiler"
}
if (-not $signtool -or -not $certificate) { throw 'Windows SDK signtool.exe and a private-key CN=Mirin code-signing certificate are required.' }

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:DebugType=None -p:DebugSymbols=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}
Get-ChildItem -LiteralPath $publishDirectory -Recurse -File -Filter '*.pdb' | Remove-Item -Force

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
# Sign the framework-dependent application binaries before packaging.
Get-ChildItem -LiteralPath $publishDirectory -Recurse -File |
    Where-Object { $_.Extension -in @('.exe', '.dll') } |
    ForEach-Object {
        & $signtool sign /sha1 $certificate.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 /d 'PdfToolbox' $_.FullName
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $($_.FullName)" }
    }
& $InnoCompiler (Join-Path $root 'installer\PdfToolbox.iss')
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE"
}

$builtInstaller = Join-Path $root 'artifacts\PdfToolbox-1.0.1-win-x64-Setup.exe'
Copy-Item -LiteralPath $builtInstaller -Destination $OutputDirectory -Force
$outputInstaller = Join-Path $OutputDirectory 'PdfToolbox-1.0.1-win-x64-Setup.exe'
& $signtool sign /sha1 $certificate.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 /d 'PdfToolbox Setup' $outputInstaller
if ($LASTEXITCODE -ne 0) { throw 'Installer signing failed.' }
& $signtool verify /pa /all $outputInstaller
if ($LASTEXITCODE -ne 0) { throw 'Installer signature verification failed.' }
Get-FileHash -LiteralPath $outputInstaller -Algorithm SHA256

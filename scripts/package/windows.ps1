param(
    [string]$Rid = "win-x64",
    [string]$Version = $(git describe --tags --always --dirty 2>$null)
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "local"
}

$Root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$PublishDir = Join-Path $Root "artifacts/publish/$Rid"
$PackageRoot = Join-Path $Root "artifacts/package"
$AppDir = Join-Path $PackageRoot "iLearn-$Version-$Rid"
$ZipPath = Join-Path $PackageRoot "iLearn-$Version-$Rid.zip"
$InstallerPath = Join-Path $PackageRoot "iLearn-$Version-$Rid-setup.exe"

Remove-Item -Recurse -Force $PublishDir, $AppDir -ErrorAction SilentlyContinue
Remove-Item -Force $ZipPath, $InstallerPath -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $PublishDir, $AppDir, $PackageRoot | Out-Null

dotnet publish "$Root/iLearn/iLearn.csproj" `
    -c Release `
    -r $Rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -o $PublishDir

Copy-Item -Recurse -Force (Join-Path $PublishDir "*") $AppDir
Compress-Archive -Path $AppDir -DestinationPath $ZipPath -Force

$InnoSetup = Get-Command iscc -ErrorAction SilentlyContinue
if ($InnoSetup) {
    $IssPath = Join-Path $PackageRoot "iLearn-$Version-$Rid.iss"
    @"
[Setup]
AppName=iLearn
AppVersion=$Version
DefaultDirName={autopf}\iLearn
DefaultGroupName=iLearn
OutputDir=$PackageRoot
OutputBaseFilename=iLearn-$Version-$Rid-setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "$AppDir\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\iLearn"; Filename: "{app}\iLearn.exe"
Name: "{commondesktop}\iLearn"; Filename: "{app}\iLearn.exe"
"@ | Set-Content -Encoding UTF8 $IssPath
    & $InnoSetup.Source $IssPath
} else {
    Write-Host "Inno Setup CLI 'iscc' not found; installer exe skipped."
}

Write-Host "Created:"
Write-Host "  $ZipPath"
if (Test-Path $InstallerPath) {
    Write-Host "  $InstallerPath"
}

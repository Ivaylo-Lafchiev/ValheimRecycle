# package_release.ps1
# Builds the project in Release mode and packages it into a zip file for NexusMods

$ErrorActionPreference = "Stop"

$PluginFile = "ValheimRecycle.cs"
$Regex = '\[BepInPlugin\(".*?",\s*".*?",\s*"(.*?)"\)\]'

# Extract version from source code
$Content = Get-Content $PluginFile
$Match = [regex]::Match($Content, $Regex)
if (-not $Match.Success) {
    Write-Host "Failed to find version in $PluginFile" -ForegroundColor Red
    exit 1
}
$Version = $Match.Groups[1].Value
Write-Host "Found version $Version" -ForegroundColor Cyan

Write-Host "Building project in Release mode..." -ForegroundColor Cyan
dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

$OutputDir = "Releases"
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$ZipPath = "$OutputDir\ValheimRecycle_v$Version.zip"
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}

Write-Host "Packaging zip file..." -ForegroundColor Cyan
$TempDir = "$OutputDir\temp_zip"
if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempDir | Out-Null

# Copy files to temp directory
Copy-Item "bin\Release\ValheimRecycle.dll" -Destination $TempDir
if (Test-Path "README.md") {
    Copy-Item "README.md" -Destination $TempDir
}

# Compress
Compress-Archive -Path "$TempDir\*" -DestinationPath $ZipPath -Force

# Clean up
Remove-Item $TempDir -Recurse -Force

Write-Host "Successfully created release archive: $ZipPath" -ForegroundColor Green

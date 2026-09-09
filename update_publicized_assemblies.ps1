# This script updates the publicized assemblies for Valheim.
# It uses the official BepInEx AssemblyPublicizer CLI tool.

$valheimManagedPath = "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed"
$publicizedPath = Join-Path $valheimManagedPath "publicized_assemblies"

# Ensure the output directory exists
if (-not (Test-Path $publicizedPath)) {
    New-Item -ItemType Directory -Path $publicizedPath | Out-Null
}

Write-Host "Installing/Updating BepInEx.AssemblyPublicizer.Cli..."
dotnet tool install -g BepInEx.AssemblyPublicizer.Cli 2>$null
dotnet tool update -g BepInEx.AssemblyPublicizer.Cli 2>$null

# List of common assemblies to publicize
$assemblies = @(
    "assembly_valheim.dll",
    "assembly_guiutils.dll",
    "assembly_utils.dll"
)

foreach ($asm in $assemblies) {
    $asmPath = Join-Path $valheimManagedPath $asm
    if (Test-Path $asmPath) {
        Write-Host "Publicizing $asm..."
        assembly-publicizer "$asmPath" --output "$publicizedPath\${asm}_publicized.dll"
    } else {
        Write-Host "Could not find $asmPath" -ForegroundColor Red
    }
}

Write-Host "Done! Publicized assemblies are located in $publicizedPath" -ForegroundColor Green

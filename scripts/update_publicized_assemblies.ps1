# This script updates the publicized assemblies for Valheim,
# and then decompiles those publicized assemblies for easy AI referencing.
# It uses the official BepInEx AssemblyPublicizer CLI tool and ilspycmd.

$valheimPath = "C:\Program Files (x86)\Steam\steamapps\common\Valheim"
$valheimManagedPath = Join-Path $valheimPath "valheim_Data\Managed"
$publicizedPath = Join-Path $valheimManagedPath "publicized_assemblies"
$decompiledPath = Join-Path $valheimPath "DecompiledSource"

# Ensure the output directories exist
if (-not (Test-Path $publicizedPath)) {
    New-Item -ItemType Directory -Path $publicizedPath | Out-Null
}
if (-not (Test-Path $decompiledPath)) {
    New-Item -ItemType Directory -Path $decompiledPath | Out-Null
}

Write-Host "Installing/Updating BepInEx.AssemblyPublicizer.Cli..."
dotnet tool install -g BepInEx.AssemblyPublicizer.Cli 2>$null
dotnet tool update -g BepInEx.AssemblyPublicizer.Cli 2>$null

Write-Host "Checking for ilspycmd..."
try {
    $null = ilspycmd --version
} catch {
    Write-Host "ilspycmd is not installed. Installing it globally now..."
    dotnet tool install -g ilspycmd
}

# List of common assemblies to publicize and decompile
$assemblies = @(
    "assembly_valheim.dll",
    "assembly_guiutils.dll",
    "assembly_utils.dll"
)

foreach ($asm in $assemblies) {
    $asmPath = Join-Path $valheimManagedPath $asm
    if (Test-Path $asmPath) {
        $pubAsm = "${asm}_publicized.dll"
        $pubPath = Join-Path $publicizedPath $pubAsm
        
        Write-Host "Publicizing $asm..."
        assembly-publicizer "$asmPath" --output "$pubPath"
        
        $outFolder = Join-Path $decompiledPath ($asm -replace '.dll$', '')
        if (-not (Test-Path $outFolder)) {
            New-Item -ItemType Directory -Path $outFolder | Out-Null
        }
        
        Write-Host "Decompiling publicized $asm into $outFolder..." -ForegroundColor Cyan
        # Decompile the publicized assembly into the target folder
        ilspycmd -p -o "$outFolder" "$pubPath"
    } else {
        Write-Host "Could not find $asmPath" -ForegroundColor Red
    }
}

Write-Host "Done! Publicized assemblies are in $publicizedPath" -ForegroundColor Green
Write-Host "Decompiled source code is in $decompiledPath" -ForegroundColor Green

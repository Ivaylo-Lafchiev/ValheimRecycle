# ValheimRecycle
Valheim mod for recovering crafting materials from crafted/upgraded items.

Full details: https://www.nexusmods.com/valheim/mods/425

Instructions:

1. Install Visual Studio or the .NET SDK
2. Download BepInEx for Valheim and extract the zip file into your root Valheim directory (or use a mod manager)
3. Set the Environment Variable `VALHEIM_INSTALL_PATH` to your Valheim install directory (e.g., `C:\Program Files (x86)\Steam\steamapps\common\Valheim`) and load the existing .csproj
4. **Generate Publicized Assemblies**: Open a PowerShell window in the project folder and run `.\update_publicized_assemblies.ps1`. This script will use the official BepInEx tool to generate publicized game DLLs into your Valheim directory so the mod can compile against them. You only need to re-run this when Valheim updates.
5. Build the project! (The `.csproj` has a post-build event that will automatically copy your compiled mod to `BepInEx\scripts\` for easy hot-reloading).
6. Optional - Add ScriptEngine & ConfigurationManager plugins for live debugging and testing.

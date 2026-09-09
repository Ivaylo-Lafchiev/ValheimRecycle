# Valheim Recycle - Agentic Guidelines

This file contains behavioral constraints and architectural guidelines learned from refactoring the `ValheimRecycle` mod for Valheim v1.0. 
Agents should read these rules before making code modifications to this project.

## Architecture & Patching Rules

### 1. Avoid Dangerous Prefix Patches
**NEVER** use `[HarmonyPrefix]` patches that `return false;` to completely hijack and replace massive vanilla UI methods (like `UpdateRecipe` or `UpdateCraftingPanel`). 
- **Why:** This is extremely brittle. If Iron Gate adds a new feature to that UI (e.g., Ashlands magic crafting) or another mod modifies the UI, your prefix will overwrite and break it.
- **Instead:** Use `[HarmonyPostfix]` to let the base game handle the heavy lifting (like populating tooltips and progress bars), and then perform targeted modifications to the UI elements you need to change.

### 2. Targeted Execution Interception
If you need to prevent a vanilla method from executing its final logic (e.g., stopping the game from crafting an item so you can recycle it instead), use a `[HarmonyPrefix]` on the *smallest possible method* at the end of the chain.
- **Example:** Instead of prefixing `UpdateCraftingPanel` to hijack the progress bar, we let `UpdateCraftingPanel` run normally and prefixed `DoCrafting()`. The native timer runs, and when it finishes, our prefix on `DoCrafting()` intercepts the call, executes `DoRecycle()`, and returns `false`.

### 3. State Management
Do not rely on the visual state of Unity UI elements (e.g., `!recycleButton.interactable`) to track the mod's internal state machine.
- **Why:** UI states can be reset by the game's native `Update` loops, causing race conditions or state desyncs.
- **Instead:** Use explicit static variables (e.g., `public static bool IsRecycleTabActive = false;`) and sync the UI *to* the state, not the other way around.

## Tooling & Workflow

### 1. Hot-Reloading Development (ScriptEngine)
This project is configured for rapid development using `BepInEx.ScriptEngine`.
- The `.csproj` contains a `PostBuildEvent` that automatically copies the compiled `.dll` directly to the `$(VALHEIM_INSTALL_PATH)\BepInEx\scripts` directory.
- **To test changes:** Run `dotnet build`, Alt-Tab into Valheim, and press **F6**. `ScriptEngine` will automatically unload the old DLL and load the new one.
- **Note:** Ensure Valheim's initial launch loads the mod from `scripts`, not `plugins`, otherwise `ScriptEngine` cannot hot-reload it.

### 2. Publicized Assemblies
Valheim modding often requires accessing private fields/methods. 
- Use the `scripts\update_publicized_assemblies.ps1` script to automate stripping `private` modifiers from the game's `.dll`s using `AssemblyPublicizer.Cli`.
- This ensures the project compiles cleanly without requiring extensive use of Harmony reflection.

### 3. NexusMods Releases
To prepare the mod for release to NexusMods:
1. Bump the version in `[BepInPlugin("...", "...", "X.X.X")]` within `ValheimRecycle.cs`.
2. Run `.\scripts\package_release.ps1`.
3. The script will compile the mod in `Release` mode and generate a `.zip` archive (containing the `.dll` and `README.md`) in the `Releases\` folder, ready for upload.

### 4. Fetching Game Source Code (Decompiling)
When you need to reference Valheim's internal source code:
- **NEVER** dump or save decompiled `.cs` files into the mod's workspace root or source directories. This clutters the repository.
- **INSTEAD**, run the `scripts\update_publicized_assemblies.ps1` script. This will use `ilspycmd` to decompile the core game assemblies directly into `C:\Program Files (x86)\Steam\steamapps\common\Valheim\DecompiledSource`.
- You can then use your search and view tools directly on that Valheim installation directory to read the source code without polluting the mod's workspace or the AI's temporary scratch space.

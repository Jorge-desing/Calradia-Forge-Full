# Calradia Forge Bannerlord Mod Template

Install the local template package with `dotnet new install <CalradiaForge.Mod.Template.nupkg>`, then create a module with:

```text
dotnet new calradiaforge-mod --name MyBannerlordMod
```

The generated module targets .NET Framework 4.7.2, references the versioned `CalradiaForge.Sdk` NuGet package for compilation, and declares `CalradiaForge` in `SubModule.xml`. The SDK assembly is not copied into the generated module. Game assemblies are read from the local Bannerlord `bin/Win64_Shipping_Client` folder and are never bundled with the template.

Set `BANNERLORD_GAME_PATH` to the Bannerlord installation root, or pass `-p:GamePath=<path>` when building the generated project. Without game assemblies, the project can still be generated and its SDK package restored, but it cannot be compiled or loaded in-game.

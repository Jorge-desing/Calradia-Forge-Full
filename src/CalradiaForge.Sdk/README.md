# CalradiaForge.Sdk

This package provides the Calradia Forge SDK contracts and content builders for Bannerlord modules. It targets .NET Framework 4.7.2 for game modules and .NET 8.0 for portable tooling.

For an in-game module, install the Calradia Forge runtime module and declare `CalradiaForge` as a dependency in `SubModule.xml`. The package reference is compile-time only: do not ship another `CalradiaForge.Sdk.dll` with your module. Bannerlord and TaleWorlds assemblies are not included; point `GamePath` at your local Bannerlord installation when compiling a module.

The product package version and `ForgeApi.Version` are separate compatibility values. Check the API constant in the SDK assembly/source when comparing contracts.

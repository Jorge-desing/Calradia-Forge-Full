# Guides

## Extension contracts

Use the public C# SDK for module-side contracts and examples. The Desktop declarative page catalog is an internal WPF presentation mechanism and is not a game-module dependency. Bannerlord-native view structure remains in Gauntlet XML, with view-model behavior in C#.

## Assembly workbench

Desktop uses AsmResolver to inspect .NET Portable Executable metadata without loading the analyzed file into the CLR. Inspection reports metadata and bounded assembly references. It does not claim to prove safety or compatibility.

The optional version edit accepts a JSON request and writes a separate output file. First run a `preview-set-assembly-version` request to review the input/output paths, hashes, current version, and requested version. Only after review, change the operation to `apply-set-assembly-version`. A successful apply creates `<output>.source.bak`, validates the output by reopening its metadata, and leaves the original untouched. Strong-name-signed, mixed-mode, native, malformed, unsupported, or colliding inputs are rejected.

Example request, with paths relative to the JSON file:

```json
{
  "operation": "preview-set-assembly-version",
  "input": "ExampleMod.dll",
  "output": "patched/ExampleMod.dll",
  "version": "1.2.3.0"
}
```

For the write step, change only `operation` to `apply-set-assembly-version`; the output must not already exist.

## Icon assets

The selected Game-icons.net SVG files and per-asset CC BY attribution are retained under the Desktop resource folder. `tools/generate_game_icons.py` recreates the WPF geometry dictionary and attribution manifest. No remote asset is loaded at runtime.

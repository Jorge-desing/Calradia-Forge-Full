---
name: calradia-forge-docfx-pipeline
description: Author standardized C# XML documentation comments, compile the DocFX static documentation site, and extract in-game contextual help for Gauntlet UI and Desktop in Calradia Forge.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
---

# Calradia Forge DocFX & In-Game Help Pipeline Skill

Use this skill when documenting C# code, building the DocFX API reference site, or updating in-game help resources for Bannerlord Gauntlet UI and Calradia Forge Desktop.

---

## 1. C# XML Docstrings Architecture

Every public class, struct, interface, enum, method, and property across `CalradiaForge.Core`, `CalradiaForge.Mod`, and `CalradiaForge.Sdk` must adhere to the standardized XML docstring contract:

```csharp
/// <summary>
/// [One-line concise summary of purpose and behavior.]
/// 
/// [Detailed explanation of mechanics, internal state invariants, and engine constraints.]
/// 
/// Lifecycle: [When the object or hook is created, registered, invoked, and finalized.]
/// Thread Safety: [Main thread only, free-threaded, or requires lock/Interlocked synchronization.]
/// Performance: [Measured allocation profile and complexity for the stated workload. Do not claim zero allocations without measuring the complete call path, including caller-owned enumeration, selectors, callbacks, and result construction.]
/// 
/// Example:
/// <code>
/// // Concrete usage example demonstrating correct consumption
/// ForgeCampaignEvents.SubscribeWeave("MyEventId", ctx => { ... });
/// </code>
/// </summary>
/// <param name="paramName">Description of parameter constraints and expected values.</param>
/// <returns>Description of return value semantics, nullability, or status flags.</returns>
```

### Critical Gotchas for XML Comments:
- **No HTML Tags Inside Summary**: Prefer plain text or standard `<c>`/`<code>` tags. Do not use `<br>` or `<b>` directly inside `<summary>`.
- **Anti-Shadowing Names**: Never refer to `Campaign` as a class or namespace; refer to `TaleWorlds.CampaignSystem.Campaign` if disambiguating.
- **Nullability & Exceptions**: Always declare `<exception cref="...">` when throwing expected guards (e.g. `ArgumentNullException`, `InvalidOperationException`).

---

## 2. DocFX Site Build Workflow

DocFX compiles XML docstrings and markdown guides into a static documentation portal stored in `docs-site/generated-site`.

### Compilation Steps:
1. **Build SDK Assembly First**:
   ```powershell
   dotnet build src/CalradiaForge.Sdk/CalradiaForge.Sdk.csproj -c Release
   ```
2. **Execute DocFX Build Script**:
   ```powershell
   tools/build_docs.ps1
   ```
3. **Verify Generated Output**:
   - Verify that `docs-site/generated-site/index.html` exists and includes API references for `CalradiaForge.Sdk` and `CalradiaForge.Core`.
   - Ensure the static site is ready for packaging into `CalradiaForge-Source-SDK-<version>.zip`.

---

## 3. In-Game Contextual Help Extraction

The script `tools/generate_in_game_help.py` parses the DocFX-generated metadata and C# XML docstrings to generate localized in-game help files consumed by:
- Gauntlet UI in-game help overlays and tooltips (`Hint.HintText`).
- Desktop workbench help flyouts and argument suggestions.

### Extraction Command:
```bash
python tools/generate_in_game_help.py
```

### Verification:
- Checks that command arguments, descriptions, and syntax hints match the active `PanelViewModel` actions and arguments.
- Confirms zero missing strings or fallback key warnings.

---

## 4. Packaging Gate Integration

Release packaging in `tools/package.ps1` explicitly verifies this pipeline:
```powershell
dotnet build src/CalradiaForge.Sdk/CalradiaForge.Sdk.csproj -c Release --no-restore
& tools/build_docs.ps1
if ($LASTEXITCODE -ne 0) { throw 'DocFX documentation build failed.' }
& python tools/generate_in_game_help.py
if ($LASTEXITCODE -ne 0) { throw 'DocFX-backed in-game help generation failed.' }
```
A failure in docstring parsing or DocFX build will block the official packaging process.

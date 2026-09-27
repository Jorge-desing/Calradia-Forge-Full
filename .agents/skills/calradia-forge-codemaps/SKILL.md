---
name: calradia-forge-codemaps
description: Audit, synchronize, and generate Calradia Forge Canonical Codemaps (Architecture, Campaign Behaviors, SDK & GameModels). Use whenever adding or modifying core behaviors, models, events, or framework components.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
---

# Calradia Forge Canonical Codemaps Skill

Use this skill when auditing, updating, or generating the canonical architecture and behavior codemaps in `docs/`:
- `docs/CODEMAP_ARCHITECTURE.md`: High-level system structure, assembly dependencies, Core components, and SubModule lifecycle.
- `docs/CODEMAP_CAMPAIGN_BEHAVIORS.md`: CampaignBehaviorBase lifecycle, complete event catalog, anti-lag time slicing, and save system safety.
- `docs/CODEMAP_SDK_GAMEMODELS.md`: SDK public service contracts, decorator pattern implementations, and ExplainedNumber conventions.

---

## 1. Codemap Synchronization Triggers

Trigger this skill whenever:
1. A new class, interface, behavior, or service is added or refactored in `src/`.
2. A new `CampaignEvents` subscription is introduced in `src/CalradiaForge.Mod/CampaignBehaviors/`.
3. A new `GameModel` decorator is added or modified in `src/CalradiaForge.Sdk/`.
4. A new Harmony patch, bootstrap hook, or ForgeWeave event mesh handler is registered.
5. Updating dependency relationships across `CalradiaForge.Core`, `CalradiaForge.Mod`, `CalradiaForge.Sdk`, and `CalradiaForge.Desktop`.

---

## 2. Codemap Specific Guidelines

### A. `docs/CODEMAP_ARCHITECTURE.md`
- **Assembly Hierarchy**: Verify the strict linear dependency:
  ```
  TaleWorlds.* (Native Engine)
      ↓
  CalradiaForge.Core (Framework Layer)
      ↓                    ↓
  CalradiaForge.Mod   CalradiaForge.Sdk
      ↓                    ↓
  CalradiaForge.Desktop (Companion)
  ```
  *Rule*: `CalradiaForge.Mod` must NEVER reference `CalradiaForge.Desktop`; `CalradiaForge.Desktop` must NEVER reference `TaleWorlds.*` directly.
- **SubModule Lifecycle**: Maintain the ASCII call graph in `OnSubModuleLoad()`, `OnGameStart()`, `OnCampaignStart()`, `OnApplicationTick()`, and `OnSubModuleUnloaded()`.
- **Core Framework Registry**: Document all components in `CalradiaForge.Core` (`ForgeBehaviorLoader`, `ModRuleAuditor`, `ForgeWeaveEngine`, `ForgeConfig`, `ForgeLogger`, `ForgeBootstrapper`).
- **Anti-Shadowing Invariants**: Ensure zero classes, sub-namespaces, or folders are named `Campaign` or `Localization` (violating `GEMINI.md`).

### B. `docs/CODEMAP_CAMPAIGN_BEHAVIORS.md`
- **Event Catalog**: Document every event hook registered via `CampaignEvents.*.AddNonSerializedListener(this, ...)`.
- **Dual-Registration Contract**:
  - `OnGameStart`: Register auto-discovered `[AutoRegisterBehavior]` and lightweight simulation listeners.
  - `OnCampaignStart`: Register state-heavy listeners requiring active campaign world data.
- **Anti-Lag Time-Slicing (Modulo-24)**:
  - Document hourly and daily time-slicing formulas (`hero.Id.GetHashCode() % 24 == currentHour`).
  - Enforce zero GC heap allocations in tick handlers (no LINQ, pre-sized buffers, no closure captures).
- **Save System & Statelessness Contract**:
  - Ensure zero `SaveableTypeDefiner` inheritance and empty `SyncData` in stateless behaviors.
  - If stateful behaviors are documented, allocate a unique `SaveableTypeDefiner` base ID $\ge 2,500,000$.

### C. `docs/CODEMAP_SDK_GAMEMODELS.md`
- **Public SDK Contracts**: Document methods, thread models, and scopes for:
  - `ForgeData`: Campaign-scoped key-value state.
  - `ForgeAgentMemory`: Mission-scoped episodic, semantic, and procedural agent memory.
  - `ForgeCampaignEvents`: Additive event mesh with bounded budgets (`SubscribeWeave`).
  - `ForgeUI`: Gauntlet panel integration contracts.
  - `ForgeDetour`: Method redirection engine.
- **Decorator Pattern Schema**:
  - Show how vanilla GameModels are wrapped (`_previousModel`).
  - Document bonus and penalty additions via `ExplainedNumber.Add(...)` or `AddFactor(...)` with localized `TextObject` explanations.

---

## 3. Verification Workflow

Before completing any codemap update:
1. **Source Inspection**: Inspect raw `.cs` files to extract exact class names, method signatures, and event hooks.
2. **Build Verification**:
   ```powershell
   dotnet build CalradiaForge.sln -c Release
   ```
3. **Statelessness & Architecture Verification**:
   ```powershell
   tools/verify_stateless_behavior.ps1
   ```
4. **Test Alignment**: Run `tools/Run-CalradiaForge-Core-Tests.bat` to confirm all behavioral assertions pass.

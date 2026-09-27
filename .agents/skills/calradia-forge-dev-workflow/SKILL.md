---
name: calradia-forge-dev-workflow
description: End-to-end engineering workflow for Calradia Forge changes. Routes work by target framework and subsystem, selects relevant tests and audits, and handles release packaging only when requested.
metadata:
  version: "1.0.0"
  author: "calradia-forge-team"
  category: "engineering-workflow"
  hub_skill: "calradia-forge-dev-workflow"
  related_skills: ["calradia-forge-dotnet", "bannerlord-dotnet-artisan", "code-reviewer", "ponytail", "calradia-forge-docs"]
---

# Calradia Forge Development & Engineering Lifecycle

Use this dedicated workspace skill for disciplined end-to-end software engineering across the Calradia Forge repository. It coordinates planning, implementation, verification, auditing, packaging, and documentation.

This isolated project skill contains Calradia Forge's required lifecycle and verification gates; it does not depend on an upstream workflow plugin being installed or remaining unchanged. The protected local [`superpowers`](../superpowers/SKILL.md) snapshot remains as a backup of source knowledge. This skill and its linked project specialists are the active source of truth.

## Before starting complex work

- Re-read the current user request and applicable workspace rules, including after context compaction; direct user instructions take precedence.
- Identify the target subsystem and load its project gateway and process guidance before implementation-specific skills.
- Keep task planning and checkpoints in the repository's `task.md` or `PROJECT_PLAN.md`, as applicable.

---

## 1. The Six-Stage Development Lifecycle

```
[1. Local Planning] ──► [2. Gateway & TFM] ──► [3. Safe Implementation]
         ▲                                                │
         │                                                ▼
[6. Docs & Release] ◄── [5. Release Packaging (when requested)] ◄── [4. Test Verification]
```

### Stage 1: Local Planning & Vibe Coding (`task.md`)
- Per `RULE[user_global]`, all task tracking, checklists, and milestones must be recorded directly in `task.md` or `PROJECT_PLAN.md` in the workspace root.
- Never rely on paid third-party cloud services or external token-consuming trackers.

### Stage 2: Architecture & Gateway Routing
- Route every C# / .NET task through [`calradia-forge-dotnet`](../calradia-forge-dotnet/SKILL.md) to evaluate Target Framework boundaries (`net472` for game module vs `net8.0-windows` for Desktop).
- Route simulation logic through [`bannerlord-dotnet-artisan`](../bannerlord-dotnet-artisan/SKILL.md) to enforce single-threaded execution, modulo-24 time-slicing, and zero GC allocations in tick loops.
- Check GEMINI anti-shadowing rules (zero folders, namespaces, or types named `Campaign` or `Localization`).

### Stage 3: Safe, Minimal Implementation
- Consult [`ponytail`](../ponytail/SKILL.md) to trace the real flow and stop at the smallest correct coding change.
- Never add speculative abstractions, unrequested interfaces, or unused parameters.
- Preserve 100% of existing public APIs and backward compatibility.

### Stage 4: Rigorous Test & Multi-Agent Verification
Execute the official non-interactive test suite and agent verification pipelines:
```powershell
# 1. Statelessness & architectural constraints
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1

# 2. Master test runner (Assets, Core 250 tests, ForgeWeave 37 tests, Desktop 51 tests, Render 274 tests)
cmd.exe /c "tools\Run-CalradiaForge-Tests.bat --skip-build --no-pause <nul"

# 3. Google Antigravity Autonomous Agents verification suite
py -3.12 tools/run_forge_agents.py verify
py -3.12 -m unittest tests/test_forge_agents.py
```

### Stage 5: Static Audit, Autonomous BugHunting & Quality Review
- Run `ModRuleAuditor.Audit()` to check for architectural violations (`GEMINI_CAMPAIGN_SHADOWING`, `FORGEWEAVE_UNTHROTTLED_PULSE`, `FORGEWEAVE_UNSAFE_WRITER`, `DESKTOP_PATH_SHADOWING`, `WPF_CONVERTER_SIGNATURE`).
- Run autonomous agent bug hunting and token compaction telemetry:
  ```powershell
  py -3.12 tools/run_forge_agents.py bughunt --compaction-preset balanced
  ```
- Use [`code-reviewer`](../code-reviewer/SKILL.md) to inspect pull requests for anti-patterns and performance smells.

### Stage 6: Documentation, Fast Packaging & Git Synchronization
- Follow [`calradia-forge-docs`](../calradia-forge-docs/SKILL.md) for documentation and validation records (`VALIDATION-<VERSION>.md`).
- Run the packaging script when concluding milestones or upon user request (`auto_packaging.md`):
  ```powershell
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\package.ps1 -Version "<version>"
  ```
  *(Utiliza `FastPackageEngine`: poda de directorios raíz, staging cero-residuos en memoria y compresión paralela multihilo)*.
- Sincronizar cambios y subir a GitHub siguiendo `RULE[git_sync_workflow.md]` (`git push origin main`) previa verificación de compresión e integridad de hashes SHA-256.
- Update canonical codemaps (`docs/CODEMAP_*.md`) if behaviors, models, or memory facts were modified.


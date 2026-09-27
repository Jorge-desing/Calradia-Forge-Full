---
name: development-workflow
description: Enforces the use of advanced UI/UX, frontend, code-review, and .NET/Bannerlord modding skills during development.
trigger: always_on
---

# Advanced Development Workflow

When performing complex development tasks, you must automatically leverage the following specialized skills based on context:

1. **UI/UX Design & Formatting:**
   - Always consult the `uiux-designer` skill before implementing new UI layouts (WPF or Gauntlet) to ensure accessibility, scaling, and contrast best practices.
   - Use the `generative_ui` skill when creating interactive visual widgets, diagrams, or mockups within the chat for the user.

2. **Frontend & Client-Side Code:**
   - If writing or refactoring modern frontend components, leverage the `frontend-expert` skill to enforce performance and state-management patterns.

3. **Quality Assurance & PRs:**
   - When finalizing a feature, submitting a Pull Request, or refactoring large blocks of code, invoke the `code-reviewer` skill to perform automated checks for anti-patterns and code smells.

4. **.NET & C# Modding Workflow (`calradia-forge-dotnet` + .NET guidance):**
   - **First-Step Gateway:** Whenever modifying, adding, or refactoring C# files, project definitions (`.csproj`), or solution filters, invoke [`calradia-forge-dotnet`](../skills/calradia-forge-dotnet/SKILL.md), which contains the project's target framework rules (`net472` for the game module vs `net8.0-windows` for Desktop) and KISS guidance. An installed `dotnet-artisan:using-dotnet` skill may supplement general advice; the preserved local snapshot is not required.
   - **Engine Grounding:** For game code, pair the .NET guidance with [`bannerlord-dotnet-artisan`](../skills/bannerlord-dotnet-artisan/SKILL.md) and the relevant Bannerlord domain skill.

5. **Documentation Workflow (`/calradia-forge-docs`):**
   - Whenever creating, updating, or maintaining technical documentation, specs, codemaps, or release logs, invoke the [`calradia-forge-docs`](../skills/calradia-forge-docs/SKILL.md) orchestrator per [.agents/rules/calradia_forge_docs.md](calradia_forge_docs.md) and [.agents/rules/docs_generation_workflow.md](docs_generation_workflow.md) (English/Spanish parity, codemap sync, append-only improvement records with integrity checks, DocFX help, and release evidence).

6. **Skill Taxonomy & Insulated Governance:**
   - The local skills remain discoverable in `.agents/skills/`; consult the grouped index in [the taxonomy](../../docs/SKILLS_TAXONOMY.md). Preserved source snapshots `using-dotnet`, `docs-generator`, and `superpowers` must remain intact as backups.
   - The isolated project skills `calradia-forge-dotnet`, `calradia-forge-docs`, and `calradia-forge-dev-workflow` contain the active project rules and do not depend on the snapshots or upstream plugins at runtime.

7. **Codex & Multi-Agent Compatibility:**
   - Maintain cross-tool parity across Google Antigravity (`GEMINI.md`, `.agents/`) and OpenAI Codex (`AGENTS.md`, `CODEX.md`, `.codex/`).
   - For Windows 10 Computer Use workflows, leverage `CodexCaptureCompat` via `tools/Manage-CodexCaptureCompat.ps1` per `.agents/rules/codex_compatibility.md`.
   - For in-game codex and encyclopedia features, use `CalradiaForge.Core.SDK.UI.ForgeEncyclopediaExtender`.

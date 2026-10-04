# Bannerlord C# Naming Conventions

- **CRITICAL**: Within `src/CalradiaForge.Mod` and its associated packages, never name a folder, sub-namespace, or class `Campaign` or `Localization`. These names can shadow `TaleWorlds.CampaignSystem.Campaign` or `TaleWorlds.Localization` when their namespaces are imported, breaking references such as `Campaign.Current`. Use `CampaignBehaviors`, `DataExtensions`, `CampaignExtensions`, or `LocalizationSync` instead.

## Bilingual documentation parity

Pair technical guides in `docs/` synchronously. The usual pair is `<TOPIC>.md` and `<TOPIC>.es.md`; preserve the aliases recognized by `agents.tools.audit_documentation_parity`: `DESKTOP.md` ↔ `ASSEMBLY_WORKBENCH.es.md` and `VALIDATION-<VERSION>.md` ↔ `VALIDACION-<VERSION>.es.md`. Architecture and system-design guides use their own `.es.md` counterparts. Run `tools/Run-CalradiaForge-Python-Checks.bat --ledger --no-pause`; missing counterparts must fail the gate.

## Harmony independence

Never add Harmony/`0Harmony` as a package, assembly, runtime, or distribution dependency. Forge may offer optional read-only diagnostics through reflection over an exact runtime that another component has already loaded, using verified public query members; do not load Harmony or mutate, reorder, or remove third-party patches. These observations are bounded review evidence, not a compatibility or coexistence guarantee, complete detection of patch backends, conflict attribution from a shared target alone, or a sandbox/performance bound. Follow Rule F in `AGENTS.md`.

## Optional Google Antigravity tooling

The `agents/` Google Antigravity integration is an optional developer-tooling profile, not a Calradia Forge or Bannerlord runtime dependency. The configured roster is one coordinator plus five specialists: `ForgeMasterAgent`, `ForgeArchitectAgent`, `StatelessBehaviorAuditor`, `DesktopWpfSpecialist`, `DocLedgerAgent`, and `BugHunterAgent`. Offline mode can invoke selected repository tools locally without the SDK; it does not run cloud agents. Online execution requires an importable SDK, credentials, and offline mode disabled. The `--agents` test profile requires the SDK because it validates SDK configuration. Python orchestration uses `CoALAAgentMemory`; C# `ForgeAgentMemory` is separate in-game SDK state. Token compaction uses best-effort patterns: report observed fixture coverage and heuristic, input-specific token estimates; do not claim universal lossless error retention or guaranteed reduction, and consult original output for exact evidence.

Run the autonomous CLI through `tools\Run-CalradiaForge-Agents.bat`, which uses the repository `.venv` Python interpreter. Run project Python and agent tests through `tools\Run-CalradiaForge-Python-Checks.bat`; run .NET tests through their repository `.bat` launchers. Do not launch test `.exe` or `.dll` files directly. For the current subagent roster and route/status behavior, follow `docs/AUTONOMOUS_AGENTS.md` and `.agents/skills/multi-agent-orchestration/SKILL.md`.


## Cross-Agent Compatibility (Codex & Multi-Agent)
- At the end of each repository-changing request, objective, or plan, commit all intentional changes attributable to it; do not include unrelated pre-existing work. Intermediate steps need no separate commits. Standing user authorization requires a normal upstream push at objective completion without asking again, unless the current objective explicitly requests no push/local-only delivery. Follow .agents/rules/git_sync_workflow.md.
- For OpenAI Codex CLI and universal multi-agent specifications, see `AGENTS.md` and `CODEX.md`.

### Mandatory Packaging and Git Completion Gates
- At release, milestone, or significant code/feature completion, generate the three current-version ZIPs through the canonical packaging workflow, require its archive audit, and verify each SHA-256 against the generated manifest. An explicit no-ZIP instruction takes precedence; documentation-only maintenance does not independently trigger packaging. Follow `AGENTS.md` §3 and `.agents/rules/auto_packaging.md`.
- Commit only intentional changes scoped to the completed objective; the standing authorization requires a normal upstream push unless the current objective explicitly says local-only/no-push. After pushing, verify the remote branch equals the exact pushed SHA and inspect the applicable GitHub Actions, check runs, and commit statuses for that SHA. Follow `AGENTS.md` Rule E and `.agents/rules/git_sync_workflow.md`.

## Specialist Skill Routing

Use the specialist that matches the task after the `calradia-forge-dev-workflow` gateway:
- Crash and lifecycle triage: `.agents/skills/debugging-master/SKILL.md`.
- Measured runtime cost, allocations, and time-slicing: `.agents/skills/performance-hunter/SKILL.md`.
- Tactical combat behavior-tree proposals: `.agents/skills/game-ai-behavior-trees/SKILL.md`.
- Campaign callback cadence and simulation scheduling: `.agents/skills/discrete-event-simulation/SKILL.md`.
- Autonomous-agent routing and output compaction: `.agents/skills/multi-agent-orchestration/SKILL.md`.
- Gauntlet/WPF presentation, accessibility, and F10 integration: `.agents/skills/game-ui-design/SKILL.md`.

## SDK onboarding and knowledge consolidation

For SDK packages, .NET templates and static content, follow
`.agents/rules/developer_onboarding.md` and the `calradia-forge-dotnet` onboarding
reference. Keep package/fixture evidence separate from public publication and live
engine loading or rendering; if a check cannot be observed, report it as pending or
unverified. Consumer modules target net472, resolve licensed TaleWorlds references
locally, and must not redistribute TaleWorlds assemblies or the Forge SDK runtime
already supplied by Forge. ForgeWeave, hooks and whole-method replacement have
distinct contracts; shared-target metadata is a review signal, not proof of a
conflict. At objective completion, consolidate validated lessons in project
specialists and mirror shared instructions in AGENTS.md, CODEX.md and GEMINI.md.
Preserve upstream snapshots and unrelated staged work.
For periodic work that can safely be deferred, use `ForgeTimeSlicer.ShouldProcess`
with a stable entity ID; this does not reduce the cost of scanning the source
collection or guarantee even bucket sizes. Measure the complete callback before
claiming an allocation budget or zero-allocation behavior.
- For Calradia Forge Gauntlet changes, F10 hotkey behavior, Resource Browser import, and live game review order, follow `.agents/rules/calradia_forge_ui.md`, `.agents/rules/bannerlord_input_debug_agent.md`, and the relevant Bannerlord UI skills. F10 uses one rising-edge gate across `IsKeyPressed`, `IsKeyDown`, and `IsKeyDownImmediate`; keep other hotkeys on `IsKeyPressed`. Close the Modding Kit before launching Bannerlord. Before each source correction, close Bannerlord/ModKit and end/reset Ordenador; keep them closed through edits/builds and between live checks. For Campaign Rule Builder, a visible active row is not proof that `Add rule` added the intended rule or that it was saved; event changes edit the selected working rule live. Verify an actual count/ID change after Add and verify Save draft separately.

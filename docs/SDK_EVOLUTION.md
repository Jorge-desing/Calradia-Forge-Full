# SDK evolution and developer onboarding

Calradia Forge's beginner path generates native module files before the game starts.
The advanced path exposes cooperative events and explicitly requested hooks. These
capabilities have different lifecycle and safety contracts; they are not interchangeable.

## Capability matrix and provenance

The initial roadmap audit used historical HEAD `59789ad04db7f6ff6c7864f5a6a11d190ff4e406`
with SDK contract 12. The independently developed hook/API 13 work, including
Finalizer/Transpiler changes and protected revisions 108–110, was later committed at
HEAD `d8c0def6ab3604ded8600bacd81db50cd581bd32`. That work is now the current stable
baseline and is not attributed to this onboarding objective. This objective keeps
product version 25.2.0 and does not change `ForgeApi.Version`.

| Capability | Source | Evidence boundary |
| --- | --- | --- |
| ForgeWeave | `src/CalradiaForge.Core/ForgeWeaveEngine.cs` | Cooperative dispatch of adapter events; no universal method interception. |
| Prefix/Postfix hooks | `src/CalradiaForge.Core/ForgeHookService.cs` | Present in baseline HEAD; explicit registration/application and host gates. Concurrent native modification is not certified. |
| Finalizer/Transpiler | API 13 at committed HEAD `d8c0def` | Independent hook work now in the stable baseline; this onboarding objective does not implement or independently validate it. |
| Whole-method replacement | `src/CalradiaForge.Sdk/ForgeDetour.cs` | Experimental native writer; serial fixtures do not prove safe concurrent writes. |
| `ForgePatchDiagnostics` | `src/CalradiaForge.Core/ForgePatchDiagnostics.cs`, `Models.cs`, `ExternalPatchRuntimeInspector.cs` | Bounded Forge-owned records plus an optional bounded runtime diagnostic signal when an already-loaded `0Harmony` assembly exposes the expected public static `HarmonyLib.Harmony.GetAllPatchedMethods()` and `GetPatchInfo(MethodBase)` query surface. Not a release/mod compatibility test. No Harmony build or distribution dependency; shared targets are review signals, not a conflict verdict. |
| Troop/item builders | SDK builders and `ForgeNoviceHub` | Native XML generation; engine loading is a separate gate. |
| Gauntlet generation | `ForgeUiContracts` and `GauntletComposer` | Static prefab/ViewModel/localization generation, not a replacement renderer. |
| DLL hot reload | No supported module runtime | Deferred. The assembly workbench and simulated command catalog do not establish runtime reload support. |
| Behavior trees | Combat scaffold only | No validated tree runtime; a reproducible tactical use case is required first. |

The supplied [interactive analysis](sdk-evolution-analysis.html) and modified Downloads
copy are preserved at the user's request. This HTML is reference material, not the
maintained technical source of truth; the source-backed matrix on this page governs
current capability claims.

## NuGet and IDE onboarding

Prepare `CalradiaForge.Sdk` and the project template as versioned local NuGet packages.
Use the freshly generated SDK feed for smoke testing instead of a global package cache.
The generated module targets `net472`, declares its Calradia Forge dependency and uses
an explicit local `GameBin` for licensed engine references. Forge supplies the SDK at
runtime; consumer modules must not ship their own second SDK assembly or game DLLs.

The same .NET template can be installed for the CLI, Visual Studio and Rider. Verify
CLI generation and compilation independently from actual IDE UI integration. See
[Microsoft custom templates](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
and [Rider custom templates](https://www.jetbrains.com/help/rider/Install_custom_project_templates.html).
Local package creation is not publication to nuget.org. Public publication requires
an authorized destination and credentials, and must use the tested package hashes.

From a Git checkout with the project Python environment prepared, use:

```bat
tools\Pack-CalradiaForge-Developer-Templates.bat --no-pause
tools\Test-CalradiaForge-Developer-Onboarding.bat --no-pause
```

The pack launcher prints a unique local feed/report under
`artifacts/sdk-evolution/onboarding/`. The smoke launcher packages again into its
own feed, installs into an isolated template hive, generates a temporary module,
restores with an isolated package cache and compiles using local `GamePath`.
`--portable-only` verifies packaging/generation/restore without claiming a game build;
`--game-path "<Bannerlord root>"` selects the licensed installation explicitly.
The portable GitHub workflow uses this mode and does not publish packages.

To install the tested template for normal development, use its actual package path
from the report: `dotnet new install <CalradiaForge.Mod.Template.25.2.0.nupkg>`.
Create a module with `dotnet new calradiaforge-mod -n MyForgeMod` and restore using
the reported local SDK feed. Set `BANNERLORD_GAME_PATH` or the MSBuild `GamePath`
property before compiling. No public feed is assumed to contain this package version.

The pack runner records the committed HEAD snapshot and hashes of the explicitly
overlaid SDK project/builders and README package asset. In the final report, API 13
comes from committed HEAD; this onboarding objective did not add public SDK API.
The report is evidence for the listed source composition and local packages, not for
an uncommitted public contract or publication to a public feed.

## Static content and showcase

Use one XML-building implementation for beginner generators and SDK builders.
Check item components, equipment references, NPCCharacter IDs and manifest file
registrations against the installed target game's Native data. Retain source hashes
in the generator validation report without distributing proprietary XML inputs.
The ContentShowcase verifier prints the current installed hashes for both XSDs and
the Native `weapons.xml` used by the sample `horse_whip` comparison. The documented
hashes remain the authoring snapshot; a differing local hash is a review signal, not
proof of corruption or a validation failure by itself.

The [content showcase](../examples/CalradiaForge.ContentShowcase/README.md) contains
a troop, equipment item and static read-only page. Its generation/build BAT passed
deterministic output, installed-game XSDs, references and local compilation.
Opening its page does not create content dynamically or execute tests. Generated
assets must be deterministic and passive; localized labels are separate from technical IDs.
Validate prefab/widget bindings before reviewing the real renderer in a safe menu.

## Interoperability and runtime research

`ForgePatchDiagnostics` returns bounded Forge-owned hook and replacement records, counts,
notes, freshness and an `ExternalRuntime` observation. Its optional reflection observer
checks for the expected public static `HarmonyLib.Harmony.GetAllPatchedMethods()` and
`GetPatchInfo(MethodBase)` query surface on an already-loaded assembly named `0Harmony`.
This is a bounded runtime diagnostic signal, not a release or mod-compatibility test. The
project does not reference, load, or distribute Harmony. An unavailable, unsupported,
incomplete, or empty external observation says nothing about other patch backends. Shared
targets are review signals, not proof of conflict. Forge does not reorder or revert third-party code.
A method identity or owner label is evidence, not authentication.
Hook/ForgeWeave exceptions can be attributed only at Forge-owned callback boundaries;
an exact source line requires matching symbols. Reports do not catch every native crash.

### Migration from Harmony Atlas diagnostics

The Harmony-specific diagnostics surface is retired in favor of Forge-owned patch
diagnostics: the public `HarmonyDiagnostics`/Harmony-specific DTO surface,
`SessionReport.Harmony`, and the `harmony` protocol action are replaced by
`ForgePatchDiagnostics`, `SessionReport.PatchDiagnostics`, and `patch-diagnostics`.
This changes Core/report/protocol contracts: `ForgeProtocol.Version` is 2 while
`ForgeProtocol.EnvelopeVersion` remains 1. It is not an SDK contract change and does not
change `ForgeApi.Version`.

Measure traversal separately from selected work. ForgeTimeSlicer uses stable IDs but
ProcessBatch scans the source collection. A one-of-24 processing gate does not remove
that scan. Portable benchmarks are harness measurements, not in-game frame latency.
No optimization is accepted on theoretical savings alone.

Investigate explicit scalar configuration/data reload separately from native resource
caches and assembly replacement. .NET Framework unloads assemblies through their
AppDomain, not individually; see [Microsoft assembly loading](https://learn.microsoft.com/en-us/dotnet/standard/assembly/load-unload).
Do not advertise simulated `cf.reload_prefabs` entries as an actual game command.

`src/CalradiaForge.Sdk/ModSettings.cs` provides registration, cached reads and explicit
save results; it does not expose a reload operation. Editing a settings file is not
evidence that an existing consumer sees new values. A later reload prototype should
read bounded scalar data, validate a complete candidate, swap it on the owning thread
and report rejection while preserving the previous state. That design does not reload
entities, assemblies or Gauntlet resource caches; it is research, not a delivered API.

## Validation and knowledge handoff

Run every build/test through BAT launchers. Record source, package and live engine
evidence separately; hosted portable CI has no licensed TaleWorlds installation.
Check package contents, manifests, determinism and isolated template generation.
The initial measured BAT results and evidence limits are recorded in Rev115 and
Rev116; the follow-up corrections, current checks and knowledge handoff are appended
through Rev129. The local NuGet/template smoke is separate from the canonical distribution ZIP
audit and hash manifest; neither package is described as publicly published.

Teach project-specific procedures through `calradia-forge-dotnet` and its references,
link the onboarding rule from all three root guides, and keep specialized instructions
in the existing skill. Preserve upstream snapshots, mirror shared invariants and
validate modified skills with `tools/Validate-CalradiaForge-Skills.bat`.

Protected revisions append to the existing chain; never rewrite earlier records to
hide incomplete checks. Commit this objective's edits separately from the captured
initial index. Packaging and live engine gates remain required for their respective claims.

### Evidence recorded during implementation

- `tools\Validate-CalradiaForge-Onboarding-Knowledge.bat` passed root instruction
  parity, local documentation links, Ruff and nine modified skill validations.
- Historical initial focused fixture run: `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause`
  passed 14 isolated diagnostic cases against the earlier adapter implementation. This
  fake query runtime is not a live Harmony coexistence test; this count is not the final
  result for the later hardened implementation.
- `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` passed Ruff, 20 archive
  and asset cases, five image cases, five tool-audit cases and source sprite checks.
- `tests\CalradiaForge.AssetPipeline.Tests.bat --no-pause` passed 23/23 cases,
  including source-template `v1.0.0`/placeholder/Forge-dependency validation and the
  current product-version requirement for real module manifests.
- The latest isolated onboarding validation passed through
  `tools\Test-CalradiaForge-Developer-Onboarding.bat --no-pause`. Current evidence is
  `artifacts/sdk-evolution/onboarding/20261002T033001Z-00c3d2e2/source-package-report.json`.
  It records repository HEAD `d940b1ccfbcb88d82a264ccd0cf931fb80489071`, stable and
  worktree SDK contract 13, `workingTreeApiChangesIncluded: false`, target frameworks
  `net472;net8.0`, and product version 25.2.0. The report excludes the pending
  workspace change to `src/CalradiaForge.Sdk/ForgeTimeSlicer.cs` from the package snapshot.
- This run packed `CalradiaForge.Sdk.25.2.0.nupkg` (SHA-256
  `bf39fdd302ae8b456697aaaef9f13b335fa766715247a2d271f0cf08dae4e0ed`) and
  `CalradiaForge.Mod.Template.25.2.0.nupkg` (SHA-256
  `5dd97b521c851889f3c0c2cbabdacb0df4cd5db5a8c01791d4954ce1c4e66521`). Both are
  local artifacts under
  `artifacts/sdk-evolution/onboarding/20261002T033001Z-00c3d2e2/feed/`; they were
  not published to a public package feed. The report records the SDK contract from HEAD,
  overlays for the current SDK project and three builders, and the current README package
  asset. Their SHA-256 values are SDK project overlay
  `4e84aae7b49028ce23177ced3f49aeed2f39f437f23ba5ee17d600f781266223`,
  `ForgeTroopBuilder.cs` `21030d195be815452e5cf121756272b522adac34b28620f5bb22ff9de8ef3ebc`,
  `ForgeItemBuilder.cs` `14a69238d4223d219f691cd577dc47dfa292bfed2602f96b803ed7d11ee45265`,
  `ForgeNoviceHub.cs` `0f0d4cc5cae85548e662384f764821adb7e2f6346dbdb4d5f2ee1b25c40b8be1`,
  and SDK README package asset `eaa3e654ccddc9cf34f169b83e1d614cda10139f35afb3a78fec7d1138498321`.
- The latest run passed SDK and template packaging, isolated template-hive installation,
  module generation, manifest/dependency validation and restore from the produced local
  SDK feed. The generated module compiled against the local licensed GameBin for `net472`
  with 0 warnings and 0 errors. Its restore selected the SDK DLL as a compile asset and
  no SDK runtime DLL; the build output contained neither the SDK DLL nor TaleWorlds DLLs.
  Template packing used only copied source inputs under the ignored per-run snapshot, not
  the shared template checkout's `bin/obj`.
- The previous same-day report `20261002T003054Z-ecaf86a0` remains historical evidence.
  It records the same repository HEAD and SDK contracts but predates the exclusion of
  the pending `ForgeTimeSlicer.cs` workspace change. Its package hashes were SDK
  `05037336dec31a54773f4d51b3c8c6edc1e4e0ff1f1a86753b7ae9dad9859c9e` and template
  `aa57e9d4cc0d9ac56172fc5e13e469fe0ce2b5fc9ff2e1849a581237a6af87b4`, under
  `artifacts/sdk-evolution/onboarding/20261002T003054Z-ecaf86a0/feed/`. Its source hashes
  were SDK project overlay `4e84aae7b49028ce23177ced3f49aeed2f39f437f23ba5ee17d600f781266223`,
  `ForgeTroopBuilder.cs` `21030d195be815452e5cf121756272b522adac34b28620f5bb22ff9de8ef3ebc`,
  `ForgeItemBuilder.cs` `14a69238d4223d219f691cd577dc47dfa292bfed2602f96b803ed7d11ee45265`,
  `ForgeNoviceHub.cs` `9fb58a0b86657e6ccc3d57371e7c8c7146c04a7a8ff6d7a6d77934cb8e2884a2`,
  and SDK README package asset `eaa3e654ccddc9cf34f169b83e1d614cda10139f35afb3a78fec7d1138498321`.
- The earlier contract 13 report `20261001T184300Z-5a8bfd73` remains historical evidence.
  It records committed HEAD `d8c0def6ab3604ded8600bacd81db50cd581bd32`; the contract 13
  hook work was already committed independently, and the onboarding objective did not
  change that API. Its package hashes were SDK
  `78c6362f07f9a6049a63b05febc375904f650530c0ac02c397c12f823e81307e` and template
  `95d3f897362504fc01d63403f343d197ad827208e4a8d981487ac193c61b11f7`, under
  `artifacts/sdk-evolution/onboarding/20261001T184300Z-5a8bfd73/feed/`. Its source hashes
  were SDK project overlay `4e84aae7b49028ce23177ced3f49aeed2f39f437f23ba5ee17d600f781266223`,
  `ForgeTroopBuilder.cs` `21030d195be815452e5cf121756272b522adac34b28620f5bb22ff9de8ef3ebc`,
  `ForgeItemBuilder.cs` `d06d5d99893eae3164592c08cae42813fbe593ac8356971f152f553809ddabfa`,
  `ForgeNoviceHub.cs` `738dc197daa15080efd25639cb790c24687d0d94ba37c33446c2fce10e3df17e`,
  and SDK README package asset `eaa3e654ccddc9cf34f169b83e1d614cda10139f35afb3a78fec7d1138498321`.
  The later report above supersedes it for current package provenance. Passing this
  CLI/package smoke does not verify Visual Studio or Rider UI integration, loading in
  Bannerlord, or live in-game behavior.
- The prior contract 12 report `20261001T182942Z-385d88e6` and the earlier
  `20261001T181729Z-7da8eac8` / `20261001T181755Z-35fc8d32` runs remain as historical
  evidence; they are superseded for current package provenance by the isolated
  contract 13 report above. Passing this CLI/package smoke does not verify Visual
  Studio or Rider UI integration, loading in Bannerlord, or live in-game behavior.
- `tools\Append-CalradiaForge-Improvement-Record.bat --verify` verified 113 existing
  records. This does not mean this objective's annex has been appended.

- `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` passed deterministic
  generation, Items/NPCCharacters XSDs, manifest, native mesh/reference and static page
  checks, plus a clean `net472` module build, Core 405/405 and ForgeWeave 73/73.
  The ignored evidence is `artifacts/ContentShowcase-20261001-rerun1.log`.
- Five-sample time-slicer medians were 1.945 ms (128 entities × 128 repetitions),
  0.9962 ms (2,048 × 8) and 0.2476 ms (16,384 × 1), with zero measured synchronous
  thread allocations in this fixture. Repetition counts differ: these are harness
  measurements, not comparative speedups or Bannerlord frame timings. No optimization
  was applied on their basis.

- The integrated BAT pipeline passed Core 405/405, ForgeWeave 73/73, Desktop 65/65
  and 294 WPF render cases with 182 layout passes. The repeat recorded 17,341 ms
  in the render harness, not application latency. Evidence:
  `artifacts/sdk-evolution/package-final.log`. Archive auditing then rejected the
  consumer template's independent module version; the corrected audit remains a
  separate gate and these successful suites do not certify the archives.
- The isolated Harmony diagnostics BAT passed 11/11 against the combined source;
  deterministic owner ordering and all four patch kinds are covered separately by
  the integrated Core regression. The stateless acceptance BAT passed all four gates.

Final archives, scoped commit, IDE UI and native engine loading/rendering remain
separate completion gates.

### Security hardening follow-up

- Historical security-hardening follow-up: the isolated
  `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause` passed 21/21 after the
  optional adapter was restricted to the supplied `0Harmony`
  assembly identity, public members and exact query signatures. The fake fixture
  verifies that decoy assemblies, private members, and invalid overloads are not
  invoked; it does not establish live Harmony coexistence.
- The adapter distinguishes `NotRequested` from a completed `NotLoaded` scan. A
  partial assembly scan remains `Incomplete`. Enumeration/output caps do not bound
  CPU, allocations, or side effects inside synchronous external calls and public
  getters; those execute in-process and are not sandboxed.

### Final objective verification evidence — 2026-10-01

- Final focused `tests\CalradiaForge.PatchDiagnostics.Tests.bat --no-pause` passed
  27/27 cases. The earlier 14-case initial run and 21-case hardening follow-up above
  remain historical records of separate working-tree states; they are not cumulative
  counts or substitutes for this final run.
- Final integrated `tools\Run-CalradiaForge-Tests.bat --no-pause` completed with clean
  `net472` and `net8.0` builds (0 warnings, 0 errors), Core 403/403, ForgeWeave 73/73,
  Desktop 65/65, and 295 WPF render cases with 308 layout/render calls. Render-harness
  counts and timings are not observed application latency.
- `tools\Test-CalradiaForge-Developer-Onboarding.bat --no-pause` and
  `tools\Test-CalradiaForge-ContentShowcase.bat --no-pause` passed their local template
  and static showcase workflows. These checks do not establish Visual Studio/Rider UI
  integration, public package publication, or loading/rendering in Bannerlord.
- Output and enumeration caps bound Forge-produced diagnostic records, but cannot hard-
  bound the runtime cost or allocations of in-process reflection, `MethodBase.GetParameters()`,
  or synchronous external getters/enumerators. Such calls are not sandboxed. No live
  Harmony runtime, Bannerlord session, campaign, or battle was used for this evidence.

### Verified knowledge and showcase-contract follow-up — 2026-10-02

- The content-showcase generator now verifies that prefab `@Property` bindings have matching `[DataSourceProperty]` members and that localization keys referenced by the generated ViewModel and native content XML exist in both English and Spanish catalogs. Negative contract fixtures cover a missing annotation and missing keys in either locale. This is a static artifact check; it does not establish Gauntlet rendering or loading in Bannerlord.
- The debugging and simulation guidance now distinguishes engine-thread affinity from blanket claims of memory corruption, labels simulation examples as illustrative, and routes runtime claims through the local assemblies and a reproducible test. The time-slicing guidance was corrected to match `ForgeTimeSlicer`'s deterministic ID hash, uneven buckets, and full-source traversal. Unmeasured hero-count/frame-drop claims are identified as hypotheses.
- The showcase BAT passed with its binding/localization contract regressions, clean `net472` module build, Core 411/411, and ForgeWeave 73/73. Skill validation for the updated debugging and simulation guidance passed. These are source and fixture checks; live Bannerlord rendering remains unverified.
- Canonical distribution and integrity validation are rerun after this append. No game or Modding Kit process is launched.

### Evidence correction — diagnostics and campaign timing — 2026-10-02

- Forge `.cfcrash` files are JSON text reports. The report formatter now uses the existing Newtonsoft.Json runtime reference so Windows paths, quotes, newlines, tabs and other control characters remain valid JSON. The Core BAT now invokes the F10 edge-gate and serialization regressions; this is a formatter test, not a live F10 or Bannerlord check.
- Historical 25.0.0–25.2.0 validation notes are preserved as run records. Their phrases “modulo-24 time-slicing anti-lag” and “zero GC allocation hot paths” are not evidence that the complete campaign callback was measured. The synthetic `ProcessBatch` harness does not measure `Hero.AllAliveHeroes` traversal, the campaign callback's allocations, or in-game latency.
- Agent documentation now matches the configured `BugHunterAgent` tools and the Core BAT's non-pausing flag. The crash analysis description is bounded to `.cfcrash` exception text and metadata-only handling for `.dmp`/`.sav`.
- The protected bilingual follow-up is recorded in [Rev125](CalradiaForge-Registro-Mejoras-Rev125.docx); its integrity entry chains from Rev124 without rewriting earlier records.

### Source-backed knowledge follow-up — 2026-10-02

- `ForgeAgentMemory` is a bounded C# game-runtime store whose semantic, episodic and procedural tiers are conceptually analogous to parts of CoALA; it is not the full CoALA language-agent framework. Python orchestration's `CoALAAgentMemory` is a separate API. The Desktop inspector uses illustrative sample profiles and labels them as such; current SDK limits are 2,048 agent IDs, 128 semantic entries per agent, 512 episodic entries (128 per type), and 128 procedural entries per agent.
- `ForgeNoviceHub.GenerateCombatAiComponentScaffold` now rejects malformed or reserved C# type identifiers and escapes quotes, backslashes, control characters and Unicode surrogate code units in generated sound literals. Regressions cover those input boundaries. Generated code still requires compilation against the target licensed game assemblies and an authorized engine review before it can be claimed as a working combat integration.
- `ForgeTradeSimulator` remains a formula-based illustrative calculator, not an engine-connected market/workshop model. `ForgeTimeSlicer` selects eligible work but scans the source collection; no complete campaign callback timing/allocation or in-game frame benefit is established.
- UI guidance now treats graphical dashboards, demo seeding, dossiers, mottos and Split Deck prominence as task-specific choices. Gauntlet hot reload via `Ctrl + ~` / `ui.toggle_debug_mode` remains an unverified version-dependent report, not a supported project gate.
- Empty behavior-owned `SyncData` only shows that the behavior adds no custom serialized fields; it does not guarantee whole-save or mod-set compatibility. Static regression, harness, and live Bannerlord evidence remain separate.

### Diagnostic-state and bounded-memory telemetry correction — 2026-10-02

- `ExternalPatchRuntimeInspector` reports `Truncated` only when a configured output or collection boundary actually clips results. A failed query, iterator failure, or skipped assembly can make the report `Incomplete` without claiming that collected output was truncated. Its isolated BAT regression suite passed 28/28; fixtures do not prove behavior of an arbitrary loaded third-party runtime.
- `AgentCognitiveMemoryBehavior` increments its semantic and episodic telemetry counters only after the bounded SDK memory store accepts the write. A regression fills the global agent quota, verifies rejected writes, and checks that counters do not rise for them. The integrated Core suite passed 415/415.
- `ForgeTimeSlicer` maps null and empty IDs to bucket zero. Callers should supply stable, non-empty entity IDs for useful deterministic distribution; the helper does not avoid traversing the source collection. Tests cover `GetBucket` and `ShouldProcess`, without changing runtime behavior or claiming a performance benefit.
- The Desktop agent-memory inspector is a CoALA-inspired illustrative sample, not the C# SDK registry and not a live memory snapshot. Render tests now assert the `Sample` evidence status and explicit sample-only wording rather than the obsolete verified-runtime expectation.
- Integrated BAT evidence on this working tree: clean `net472`/`net8.0`/Desktop builds, Core 415/415, Patch Diagnostics 28/28, ForgeWeave 73/73, Desktop 65/65, and 295 WPF render cases with 308 layout/render passes. Render timings are harness timings. No Bannerlord process, campaign, battle, third-party runtime, IDE extension, or public NuGet publication was verified.

### Final objective verification evidence — 2026-10-02 (Registry Rev127)

- The final `tools\Run-CalradiaForge-Tests.bat --no-pause` run passed with clean `net472`, `net8.0`, and Desktop builds (zero warnings/errors), Core 415/415, Patch Diagnostics 30/30, ForgeWeave 73/73, Desktop 65/65, and 295 WPF render cases with 320 layout/render passes. The 20,903 ms measurement is harness time only.
- `tools\Verify-CalradiaForge-StatelessBehavior.bat` passed 4/4 acceptance checks. `tools\Run-CalradiaForge-Python-Checks.bat --ci --no-pause` passed Ruff 0.16.9, 25 asset/archive tests, five image tests, 18 agent audit cases, 15 offline orchestrator cases, three CLI launcher cases, and Gauntlet/sprite checks. Its `--ledger` run verified 42/42 EN/ES pairs.
- All 29 modified skill directories passed the maintained `quick_validate.py` launcher. The onboarding knowledge BAT verified shared-guide parity and links, Ruff, and nine skill validators. The protected ledger was verified at 126 records before the new append; Rev127 is appended and verified separately.
- The onboarding smoke `20261003T012628Z-f3bebd08` packed and isolated-installed SDK/template 25.2.0 packages, generated a consumer module, restored it from the local feed, and compiled `net472` against local licensed GameBin references with zero warnings/errors. The report says stable SDK contract 13 and `workingTreeApiChangesIncluded: false`; it is not a public package publication or in-game test. Package hashes: SDK `2f4aa6863129bd8ea67ae6e7b8b71de643975d962c0e007e02f38ece5f98718c`; template `f9b3a74577e9ae5b9caa7134b37dab8214111396a876fad8e57fd499e2115a64`.
- The content-showcase BAT passed deterministic output, Native Items/NPCCharacters schema checks, manifest/reference validation, and a clean generated `net472` module build. These static checks do not prove Gauntlet rendering or module loading in Bannerlord.
- No game or Modding Kit was launched. Live Bannerlord loading/rendering, live Harmony coexistence, public NuGet publication, and Visual Studio/Rider marketplace integration remain unverified. This objective does not claim a runtime performance gain.

### Retained evidence reconciliation — 2026-10-02 (Registry Rev128)

- The latest retained integrated log, `artifacts/sdk-evolution/post-final-reviewed-20261002.log`, records Core 410/410, Patch Diagnostics 28/28, ForgeWeave 73/73, Desktop 65/65, and 295 WPF render cases with 308 layout/render passes. Its render duration is 24,476 ms of harness time.
- Rev127 reported Core 415/415, Patch Diagnostics 30/30, 320 passes, and 20,903 ms. No retained log matching that exact set was found, so those values remain reported but cannot be independently reproduced from this checkout. The absence of a matching log does not establish that the run did not occur.
- The `ProcessBatch` benchmark is synthetic and does not measure a complete campaign callback or latency inside Bannerlord.
- The content-showcase verifier now rejects incorrect or escaping locale paths, missing referenced string files, a non-focusable close button, and an incorrect close-label binding. Its BAT regression run validates source contracts, schemas, deterministic output, and the generated module build; it is not live Gauntlet rendering evidence.

### Final objective verification — 2026-10-02 (Registry Rev129)

- The latest integrated BAT run passed clean `net472`, `net8.0`, and Desktop builds with zero warnings/errors; Core 416/416, ForgeWeave 73/73, Desktop 65/65, and 295 WPF render cases with 320 layout/render passes. Its 19,288 ms measurement is from the harness, not the open application's latency.
- The dedicated Patch Diagnostics BAT passed 30/30. The onboarding smoke test generated, restored, and built a temporary `net472` consumer from isolated local SDK/template packages with zero warnings against licensed local GameBin references. The static content showcase and stateless behavior acceptance BATs passed; all 29 modified skills and the shared-guide onboarding knowledge audit validated.
- The base Python `--ci` profile passed; `--ledger` verified 42/42 maintained English/Spanish technical-document pairs. This bilingual appendix is separately tracked in the protected record. Optional Antigravity dependency setup was attempted but pip failed with `InvalidChunkLength`, so the optional dependency-backed profile remains unverified. No Bannerlord or Modding Kit was launched, and the TPAC deep parser reported that its local library/fixture was unavailable.
- Harmony inspection remains optional and read-only at the Forge call boundary: it queries a compatible, already-loaded `0Harmony` assembly by reflection and never loads or mutates Harmony. The external synchronous calls are not sandboxed or internally time-bounded, and these fixtures do not prove compatibility with real third-party mods. Product version remains 25.2.0, SDK API 13; no public package publication or IDE marketplace integration is claimed.

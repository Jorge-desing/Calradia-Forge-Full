# SDK packages, templates and generated content

Read when preparing a versioned SDK package, developer template, static content
generator, or claims about the extension framework. See the bilingual
[SDK evolution guide](../../../../docs/SDK_EVOLUTION.md) for commands and validation evidence.

## Stable input and isolated verification

Capture HEAD and the initial index before changes. Treat API additions in a parallel
working tree as pending until validated; do not discard or silently commit them.
Use the actual project TFMs and release version for package metadata. A NuGet
package built locally is not proof that the same version exists on a public feed.

Install templates into an isolated hive, generate into a temporary module directory,
restore from the newly produced SDK feed with an isolated cache, and verify
project.assets.json. Consumer output must omit the SDK runtime assembly already
provided by Forge and all proprietary engine binaries.

## Content and UI evidence

Canonical content is built through ForgeTroopBuilder and ForgeItemBuilder. The
beginner hub should delegate to the same builders instead of keeping a divergent
XML string implementation. Do not switch weapon XML shape based on a guess:
inspect Native ModuleData, record source file/hash/version and check ID references.

Generated prefab, ViewModel, localization and registration remain native Gauntlet
artifacts. Verify escaping, deterministic outputs, ownership, bindings and manifest
paths before engine loading. Keep live renderer evidence distinct from fixture checks.

## Runtime boundaries

ForgeWeave is cooperative. Hook callbacks execute in-process on the invoking thread;
read-only metadata cannot certify coexistence of arbitrary transpilers or raw detours.
Diagnose observed owners/targets without invoking, reordering or reverting third-party code.
Report reflection failure and incomplete captures as inconclusive.

For time slicing, ForgeTimeSlicer.ShouldProcess normalizes the hour and uses a stable
StringId hash. ProcessBatch still scans its source collection: a smaller selected
bucket is not evidence of O(N/24) traversal. Measure allocations and traversal separately.
Reloading scalar configuration is separate from unloading/replacing an assembly.

## Knowledge maintenance

Link the exact current source, BAT result and scope for a lesson; identify worktree
versus committed status and pending live gates. Historical counts and timings are
not invariant acceptance thresholds. Mirror shared rules across the root guides;
keep specialized procedures here rather than duplicating them in every skill.

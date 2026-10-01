# Rev107 — Distribution completion gate

**Date:** 1 October 2026
**Version:** Calradia Forge 25.2.0, unchanged
**Scope:** Agent instructions, development skill and distribution rules.

## Observed problem

The previous hook-engine delivery completed source and CI verification without generating current distribution ZIPs. Modular rules required milestone packaging, while the development skill's description and lifecycle diagram incorrectly described packaging as request-only. Root instructions lacked an explicit distribution completion gate.

## Correction

AGENTS.md and CODEX.md now directly require the three current-version ZIPs at release, milestone or significant code/feature completion and on explicit request, including unchanged versions. The development skill and packaging rules use the same trigger. Explicit no-ZIP instructions for the current request take precedence; documentation-only maintenance does not independently trigger packaging.

Delivery requires successful canonical packaging, archive audit and independently matching SHA-256 digests, with absolute output/evidence links. Missing prerequisites or files remain incomplete. Generated archives stay in ignored artifacts and are never committed. Packaging does not authorize version bumps, publication, remote pushes, installation or game launch. The root script exclusion is clarified to retain only the existing app-host-free Desktop runtime launcher exception, not development/test scripts. Evidence filenames follow the pipeline's actual version-without-dots suffix.

## Validation and limits

The development skill passed its BAT structural validator. The canonical packaging pipeline is invoked for this explicit request with -Quick, reusing existing atlas and documentation rather than regenerating assets, while retaining build, BAT test and archive-audit gates. Final delivery reports its actual result and hashes. Previous protected records are unchanged; no live game import or render verification is inferred from packaging.

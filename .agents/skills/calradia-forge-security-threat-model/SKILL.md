---
name: calradia-forge-security-threat-model
description: Threat-model Calradia Forge architecture and code paths using the repository's current trust boundaries, runtime targets, IPC, and dependency rules.
---

# Calradia Forge security threat model

Use this skill when the user asks for a threat model or a security architecture review of Calradia Forge. Start with the current source and the relevant parts of [system design](../../../docs/SYSTEM_DESIGN.md), [SDK](../../../docs/SDK.md), [dependency framework review](../../../docs/DEPENDENCY_FRAMEWORK_REVIEW.md), and [architecture rules](../../rules/calradia_forge_architecture.md). Follow references when they define the path under review. Treat documentation as a guide to verify against current code, not proof that an implementation still matches it.

## Establish the actual scope

- Name the feature, data, entry points, actors, and deployment surface in scope. Separate the Bannerlord module, shared Core/SDK, standalone Desktop, Python development tools, and optional agent runtime; they are distinct products and execution contexts.
- Trace input to sensitive operation and output, including the caller, validation, authorization or state gate, filesystem or process access, transport, error handling, and retained evidence. Cite source paths and relevant symbols or line numbers for material findings.
- Describe attacker capabilities and assumptions explicitly. Consider another process running as the same Windows user where the named pipe is in scope; its current-user ACL does not establish isolation between processes owned by that user.
- Separate demonstrated behavior from design intent, test-fixture evidence, and unverified runtime behavior. Do not turn absence of a finding into a security certification.

## Repository trust boundaries to verify

- `CalradiaForge.Mod` is the Bannerlord game module targeting `net472` and operating on the game thread. TaleWorlds assemblies are local engine inputs, not distributable Forge dependencies.
- `CalradiaForge.Sdk` targets `net472;net8.0` without engine references. `CalradiaForge.Core` targets both frameworks, with engine-specific references and its MonoMod backend restricted to `net472`. `CalradiaForge.Desktop` is a separate `net8.0-windows` WPF process and must not reference TaleWorlds assemblies directly or transitively. Verify project files and conditional references before reporting a boundary violation.
- Desktop communicates with the game through the local duplex named pipe `CalradiaForge-{BannerlordProcessId}`. The documented ACL permits the current Windows user; the JSON request is limited to 64 KiB and the documented desktop response limit is 32 MiB. Check actual framing, parsing, authorization, bounds, cancellation, and error paths. Do not describe the pipe as a network service or as isolation from same-user processes.
- Built-in analyzers inspect bounded metadata and files without loading or executing analyzed target assemblies. Verify that the specific path under review preserves those limits and execution boundary; do not generalize a property of one analyzer to every extension or tool.
- SDK extensions and synchronous callbacks execute in-process. ForgeWeave applies declared context/access and write gates, isolates failures, and has bounded evidence, but Forge cannot forcibly terminate arbitrary extension code and is not a sandbox.
- Harmony/`0Harmony` is not a Forge dependency. Under Rule F, the optional observer may inspect only the exact already-loaded runtime through verified public query members; it must not load Harmony or patch, unpatch, or reorder third-party code. Its observations cannot prove compatibility or coexistence, detect every backend, attribute a conflict from a shared target, or bound synchronous work inside third-party queries.
- Python repository utilities and optional Google Antigravity agents are development tooling, not dependencies of the mod, Core, SDK, or Desktop and not part of mod packages. The ordinary Python profile is separate from the explicit optional agent profile. Verify their actual entry points, network use, credentials, and execution before treating these tools as runtime attack surface.

## Analyze and report

1. Inventory valuable data and operations, including user files, campaign state, local settings, credentials, generated artifacts, code execution, and state-changing game actions where relevant.
2. Draw the in-scope trust boundaries and identify who can send data across each one.
3. Walk concrete abuse cases from entry point to impact. Check validation and canonicalization, authorization, resource bounds, link/reparse handling, archive/XML/document/image parsing, temporary-file behavior, subprocesses, secrets in logs, and failure recovery when those areas are present.
4. For each finding, give severity with rationale, preconditions, affected code, evidence, impact, current controls, a minimal actionable mitigation, and remaining uncertainty. Distinguish confirmed defects from plausible risks and design questions.
5. Include relevant non-goals and evidence limits. In particular, fixture checks do not establish live Bannerlord behavior, parser preflight does not establish visual rendering, and a shared hook target alone is not proof of a third-party conflict.

Prefer a concise threat table and a short list of assumptions and open questions. Do not invent an attack surface or claim a mitigation that source evidence does not support. Keep proprietary game DLLs, user saves, secrets, and generated artifacts out of proposed commits.

## Persisted deliverables

Return findings in the response by default. If the user requests a repository document, write the English source as `docs/SECURITY_THREAT_MODEL.md` and its conceptual Spanish counterpart as `docs/SECURITY_THREAT_MODEL.es.md` in the same change. Follow the documentation workflow and ledger rules that apply to the requested scope; do not append to the immutable improvements ledger unless the user requests that work. Do not create an unpaired technical document under `docs/`.

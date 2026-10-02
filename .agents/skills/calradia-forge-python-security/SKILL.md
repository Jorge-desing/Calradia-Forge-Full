---
name: calradia-forge-python-security
description: Review or harden security-sensitive Python tooling in Calradia Forge, including repository utilities, tests, and optional agent code.
---

# Calradia Forge Python security

Use this skill for an explicit security review or hardening request involving Python in this repository. Keep the scope to Python entry points and their Python callers, dependencies, inputs, outputs, and subprocesses. For a cross-product architecture threat model, use [calradia-forge-security-threat-model](../calradia-forge-security-threat-model/SKILL.md). Route C# and MSBuild changes through [calradia-forge-dotnet](../calradia-forge-dotnet/SKILL.md); do not apply Python-specific fixes to the .NET products.

Read [Python tooling](../../../docs/PYTHON-TOOLING.md), the relevant source and dependency manifest, and the current task's applicable repository rules before recommending changes. Repository tools use Python 3.12 in `.venv`. `requirements-tools.txt` and `requirements-dev.txt` describe the normal tool profile; `agents/requirements.txt` is a separate optional profile enabled explicitly with `tools/Setup-CalradiaForge-Python.bat --agents`. These are development dependencies, not runtime dependencies of the Bannerlord module, Core, SDK, or WPF Desktop. Verify the active setup rather than assuming optional packages are installed.

## Review workflow

1. Identify the requested entry points and callers. Trace untrusted values from command-line arguments, environment variables, files, archives, XML/documents/images, IPC or remote agent responses to filesystem writes, process execution, deserialization, and logs.
2. Inspect the actual code and pinned manifests. Check path normalization and containment, symlink/reparse behavior where applicable, file and archive size/count limits, temporary-file cleanup, parser configuration, subprocess argument construction and environment, network access, secret handling, and exception/logging paths. Only report a category when it exists in the path under review.
3. Check YAML loading for safe APIs when parsing untrusted or repository-controlled metadata; do not assume PyYAML is used safely merely because it is installed. Check that formats capable of object execution or arbitrary code are not introduced without a justified, explicit design.
4. Inspect agent code separately from ordinary tools. Verify where external model/API calls occur, how credentials are sourced, whether prompts or file contents can leave the machine, what data is logged, and whether the optional agent profile is being kept out of the default tooling dependencies.
5. Report evidence with file paths, symbols or line numbers, exploit preconditions, impact, and a minimal fix. Label uncertain or untested behavior clearly. Avoid broad claims such as “secure” or “sandboxed.”

## Change constraints

- Make the smallest fix that closes a demonstrated issue while preserving CLI contracts, repository policy, generated-output locations, and reproducibility. Do not add a dependency, broaden network access, install globally, or enable the optional agent profile unless the task requires it.
- Do not run autofix or reformat unrelated Python files during a security review. Preserve unrelated working-tree changes and stage only files belonging to the task.
- Keep generated reports, caches, archives, logs, and other outputs under their documented or ignored locations; never commit credentials, user data, game saves, engine DLLs, `bin/`, `obj/`, `.venv/`, or Python caches.
- When validation is requested, use the repository launchers and the existing Python 3.12 environment described in `docs/PYTHON-TOOLING.md`. The skill metadata validator is `tools/Validate-CalradiaForge-Skills.bat <skill-directory>`; the read-only lint command is `.venv\Scripts\ruff.exe check`. Do not silently fall back to global Python or install packages to make a check pass.
- Follow the root `AGENTS.md` Git workflow at objective completion: preserve the initial worktree baseline, scope commits to intentional task changes, and perform its authorized push/check review when applicable. Report inaccessible external checks as unverified.

## Evidence limits

Static inspection and unit fixtures do not prove the absence of vulnerabilities in all inputs or dependencies. A successful Python check does not establish native Bannerlord behavior or security of the `net472` game process. Keep Python tooling findings separate from game-runtime findings and state what was not exercised.

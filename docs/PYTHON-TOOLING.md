# Python Tooling

Calradia Forge uses Python for repository validation, asset preparation, documentation utilities, and optional Antigravity agents. These packages are development tools; they are not dependencies of the Bannerlord module, the SDK, Core, or the WPF application, and they are not part of mod packages.

## Supported environment

Use Python 3.12 in the repository-local `.venv`. The virtual environment is ignored by Git and keeps project tooling separate from the system Python installation. Install or refresh it explicitly from the repository root:

```bat
tools\Setup-CalradiaForge-Python.bat
```

The setup launcher creates or reuses `.venv` and installs the development profile. It does not change the global Python installation. Project test and validation launchers expect setup to have completed; they do not install dependencies automatically. If Python 3.12 or a required package is missing, run setup and then rerun the requested `.bat` launcher.

## Dependency profiles

| Manifest | Contents and purpose |
| --- | --- |
| [`requirements-tools.txt`](../requirements-tools.txt) | Pinned libraries used by repository tools, including PyYAML for skill metadata validation, Pillow for image processing, and `lxml`/`python-docx` for document workflows. |
| [`requirements-dev.txt`](../requirements-dev.txt) | Development profile that includes the tools profile and pinned Ruff for lint checks. |
| [`agents/requirements.txt`](../agents/requirements.txt) | Separate optional runtime dependencies for Google Antigravity agents. |

The default setup installs `requirements-dev.txt`. To include the optional agent runtime in the same isolated environment, use:

```bat
tools\Setup-CalradiaForge-Python.bat --agents
```

The `--agents` option is explicit; ordinary project setup does not install the Antigravity runtime. Keep agent-only packages in `agents/requirements.txt` rather than adding them to the general tool profile.


## Skill metadata validation

Run the repository launcher with the skill directory to validate its `SKILL.md` frontmatter using the Codex Skill Creator validator and the project environment:

```bat
tools\Validate-CalradiaForge-Skills.bat .agents\skills\calradia-forge-desktop
```

The launcher locates `quick_validate.py` in the active Codex Skill Creator installation. PyYAML is provided by the project's tools profile, so the validator can parse YAML frontmatter without installing packages globally. If the validator script itself is unavailable, install or restore the Skill Creator skill in Codex and rerun the launcher.

## Ruff

Ruff is a development dependency. Run its read-only lint check with:

```bat
.venv\Scripts\ruff.exe check
```

The repository configuration limits the initial gate to maintained Python entry points and tests. Use `check` only: do not run autofix, `format`, or commands that rewrite files as part of validation.

## Tests and checks

Run project Python tests and validators through their `.bat` launchers. This ensures they use the configured toolchain and repository options. Complete the setup step first; launchers must fail with a clear setup hint instead of silently falling back to a global or older Python installation. The test runners do not bootstrap or upgrade the environment.

CI selects Python 3.12 and installs the repository tool profile before running Python-based checks. The agent-test job explicitly prepares the optional profile and calls `tools\Run-CalradiaForge-Python-Checks.bat --ci --agents --no-pause`; ordinary local checks do not require Antigravity. Neither profile is copied into release ZIPs or loaded by the mod at runtime.

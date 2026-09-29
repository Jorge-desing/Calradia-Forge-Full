---
name: git-sync-workflow
description: Enforce structured git commits, preflight validation, and synchronization with GitHub upon request or concluding releases.
trigger: always_on
---

# Git Synchronization & Push Workflow

Whenever the user requests updating and pushing changes to GitHub, or when concluding a major milestone, version release, or significant round of features/fixes, you MUST follow this structured workflow.

---

## 1. Trigger Conditions
1. **Explicit User Request:** Whenever the user requests "actualiza y sube cambios a github", "haz git push", "sincroniza con el repositorio remoto", or similar commands.
2. **Milestone / Release Finalization:** After concluding major updates, executing `tools/package.ps1`, and generating release verification artifacts.

---

## 2. Mandatory Pre-Push Verification Gate
Before staging or committing changes, ensure that:
1. **Clean Solution Build:**
   ```powershell
   dotnet build CalradiaForge.sln -c Release -v:minimal
   ```
   Must succeed with 0 errors.
2. **Stateless Behavior Gate (Mandatory):**
   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify_stateless_behavior.ps1
   ```
   Must pass 4/4 acceptance criteria.
3. **Distribution & Secret Safety:**
   - Never commit sensitive keys, API tokens, `.env` files, or private credentials.
   - Never commit proprietary game engine binaries (`TaleWorlds.*.dll`), user save files (`*.sav`), or temporary debug dumps (`*.cfcrash`, `*.log`).

---

## 3. Git Staging & Commit Conventions
1. **Inspect Working Tree:**
   ```powershell
   git status --porcelain
   ```
   Verify all modified and untracked files. Distinguish intentional project changes (source, UI, tests, docs, assets, scripts) from external debris (build outputs, logs, pycache).

1b. **⚠️ AUTORÍA DE SESIÓN — Solo Comprometer Cambios Propios (CRÍTICO):**
   - **SOLO** stagear y commitear los archivos que el agente **actual** modificó o creó en la **sesión presente**.
   - Los cambios preexistentes en el working tree (de sesiones anteriores, subagents, stash pops, o trabajo acumulado sin commitear) **NO** deben incluirse a menos que el usuario lo pida explícitamente.
   - **Cómo identificar los cambios propios:** Rastrear qué herramientas (`write_to_file`, `replace_file_content`, `run_command`) usé yo en esta sesión y qué archivos tocaron. Si aparecen archivos en `git status` que no recuerdo haber modificado, son de otra sesión.
   - **Señal de alerta:** Si `git status --porcelain` muestra decenas de archivos inesperados, pausar y preguntar al usuario cuáles quiere incluir antes de hacer `git add` masivo.
   - La Rule E de `AGENTS.md` ("stage ALL intentional workspace modifications") se refiere a los cambios **propios de esta sesión** — no autoriza a apropiar el trabajo de otros agentes o sesiones anteriores.

2. **Comprehensive Project Staging & Exclusion of External Garbage:**
   - **Full Project Changes Included:** Always stage all intentional project modifications, features, fixes, documentation, ledger DOCX/annexes, tests, and assets. Never arbitrarily exclude valid repository changes under the assumption they are "external".
   - **Strict Exclusion of External Garbage (Zero External Spillover):** NEVER stage or commit:
     1. Build artifacts, intermediate outputs, and caches (`bin/`, `obj/`, `artifacts/`, `.vs/`, `.idea/`, `__pycache__/`, `*.pyc`).
     2. Temporary debug logs, crash dumps, and traces (`*.log`, `*.cfcrash`).
     3. Game engine binaries (`TaleWorlds.*.dll`) or user game saves (`*.sav`).
     4. Private credentials, secrets, or API keys (`.env`, tokens).
     5. Operating system metadata (`Thumbs.db`, `desktop.ini`, `.DS_Store`).
   - **Clean Staging:** Stage all verified project paths cleanly by path or pattern, confirming that no external garbage or cached binaries are included.
3. **Structured Commit Message:**
   Write clear, semantic commit messages in Spanish or English matching repository conventions:
   - Concise imperative subject line (e.g. `feat: ...`, `fix: ...`, `refactor: ...`, `chore: ...`).
   - Detailed body listing the specific components modified, bugs resolved, architectural invariants maintained, and verification evidence.

---

## 4. Push to Remote & Reporting
1. **Push Command:**
   ```powershell
   git push origin <branch>
   ```
   (Typically `git push origin main` or the active upstream tracking branch).
2. **Delivery Response:**
   Clearly state in your summary response:
   - The commit hash and message.
   - The remote repository and target branch.
   - The list of key files and subsystems synchronized.

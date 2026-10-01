---
name: git-sync-workflow
description: Require one scoped Git commit when each user request, objective, or plan is complete; push only on request.
trigger: always_on
---

# Chat-Scoped Git Commit & Push Workflow

Whenever a user-requested request, objective, or plan in the current chat intentionally creates or changes repository files, it MUST end with one scoped commit containing all intentional repository changes attributable to that request, objective, or plan. Intermediate steps within the same objective do not require separate commits. This applies to small fixes and documentation/rule changes as well as releases. Push only when the user explicitly asks for synchronization or publishing.

---

## 1. Trigger Conditions
1. **Request / Objective / Plan Completion:** At completion of each coherent user request, objective, or plan that changes repository files, prepare and create a scoped commit. Do not split intermediate steps of the same objective into separate commits.
2. **Explicit Push Request:** Push only when the user requests "actualiza y sube cambios a github", "haz git push", "sincroniza con el repositorio remoto", or similar.
3. **Milestone / Release Finalization:** Use the release verification workflow before committing a major release; do not infer permission to push from a release milestone.

---

## 2. Verification Before Commit or Push
Before committing, run the checks appropriate to the changed scope and report their actual results. For source/runtime changes, use the project build and relevant test launchers; for tests, invoke them through the repository `.bat` launcher when one exists. Do not launch test `.exe` or `.dll` files directly. A failed or unavailable check must be reported honestly; never label the change verified when it is not.

Before pushing a release or a change whose workflow requires full validation, ensure that:
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

1b. **Chat-Scoped Ownership (Critical):**
   - Capture the initial `git status --porcelain` and relevant diffs before editing. Track the files and hunks changed for this request/objective/plan, including changes made by delegated agents.
   - Stage and commit every intentional repository change attributable to this objective. Never include unrelated pre-existing edits, changes from another task/chat, or generated test output merely because they appear in the working tree.
   - If a task-owned edit shares a file with pre-existing work, stage only the task-owned hunks where practical. Do not discard, rewrite, or silently absorb the pre-existing work to make a commit easy.
   - If ownership cannot be separated confidently, do not perform a broad `git add`; preserve the working tree and explain the specific ambiguity so the user can resolve it.

2. **Comprehensive Objective Staging & Exclusion of External Garbage:**
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

4. **Commit Completion:**
   - Verify the staged diff and `git status --porcelain` immediately before committing; use explicit paths or reviewed hunks, never an unreviewed `git add -A` in a dirty workspace.
   - Create the commit before reporting the request/objective/plan complete, then record its short hash and subject in the response.
   - If the objective made no repository changes, no empty commit is required. External application actions that do not change repository files are outside this commit rule.

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

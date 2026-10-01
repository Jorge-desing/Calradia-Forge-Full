---
name: git-sync-workflow
description: Require a scoped commit at objective completion, push by default unless the user opts out, and verify the exact remote commit and its checks after every push.
trigger: always_on
---

# Chat-Scoped Git Commit & Push Workflow

Whenever a user-requested request, objective, or plan in the current chat intentionally creates or changes repository files, it MUST end with one scoped commit containing all intentional repository changes attributable to that request, objective, or plan. Intermediate steps within the same objective do not require separate commits. This applies to small fixes and documentation/rule changes as well as releases. Standing user authorization requires a normal push to the configured upstream after every completed repository-changing objective, unless the user explicitly requests local-only delivery or no push for that objective.

---

## 1. Trigger Conditions
1. **Request / Objective / Plan Completion:** At completion of each coherent user request, objective, or plan that changes repository files, prepare and create a scoped commit. Do not split intermediate steps of the same objective into separate commits.
2. **Default Push:** Push the complete scoped objective commit to the configured upstream without asking again. Honor a newer explicit no-push/local-only instruction. Use a normal fast-forward push; publication and force-push require separate authorization.
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

3. **Mandatory Post-Push Review:**
   - Record the full SHA that was pushed. Verify the destination branch points to that SHA using `git ls-remote` or the GitHub API; a successful push alone does not establish build or test success.
   - Inspect GitHub Actions runs, check runs, and commit statuses for that exact SHA. Do not substitute green checks from an earlier commit or another branch. Use the GitHub connector when available, otherwise authenticated GitHub API/CLI access without exposing credentials.
   - Wait for applicable checks to finish during the active request, using bounded waits and progress updates. If a check fails, retrieve its job logs, identify the failing phase, correct task-related defects, rerun the relevant local BAT validation, and push the correction when synchronization is already authorized. Review the replacement SHA again.
   - Check that the complete intended diff was delivered and that no caches, secrets, proprietary assemblies or generated test captures entered the commit. Recheck the working tree for intentional changes still awaiting delivery.
   - Report commit/branch synchronization and remote validation separately, with links to the matching runs. Missing, queued, cancelled, inaccessible or still-running checks are **pending/unavailable**, not passing. Explain external blockers without claiming completion of unverified checks.
   - Historical failed runs remain immutable. Do not rewrite Git history, force-push, delete failed evidence, disable tests or weaken assertions merely to remove red indicators. New successful checks establish recovery for the new SHA.
   - Do not create another commit solely to record successful remote checks; report their evidence in the response to avoid an endless commit/check cycle. A separately requested permanent evidence record is a new scoped documentation change and its push must also be reviewed.

## Standing push authorization and overrides

- The user grants standing authorization for commit and push at objective completion. Apply this default to code, tests, assets, documentation, rules and skills. An explicit no-push instruction for the current objective overrides the default until the user changes it. Do not ask again for normal delivery or task-related CI repairs.
- The authorization also covers task-related CI correction commits and their pushes until the exact-SHA checks finish, subject to the user's latest scope. It does not authorize unrelated working-tree changes, release publication, force-push or changes to repository protection.
- Before pushing, inspect the complete outgoing commit range, not only HEAD. Preserve concurrent edits and stage only owned changes. If authorized delivery cannot be separated from unrelated committed history, report the concrete conflict rather than rewriting history.
- After pushing, verify the exact remote SHA and inspect Actions, check runs and statuses. Do not finish while applicable checks are queued or running; follow the post-push review below and report genuine external blockers explicitly.

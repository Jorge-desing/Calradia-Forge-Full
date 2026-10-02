---
name: calradia-forge-gh-fix-ci
description: Diagnose and repair failing GitHub Actions or check runs for Calradia Forge, then verify the exact pushed commit. Use for requested CI repair, not ordinary local debugging.
---

# Calradia Forge GitHub CI Repair

Use this skill when the user asks to diagnose or fix failing GitHub checks for this repository.

## Workflow

1. Confirm the repository, current branch, target PR or commit, and the exact SHA whose checks failed. Check `gh auth status` without printing credentials. Use the GitHub connector if available; otherwise use the authenticated GitHub CLI/API. Never request or display an auth token.
2. Inspect the failed run's job summaries and logs for that SHA. Identify the failing step and distinguish a task-related code or workflow defect from an external runner, service, or unrelated failure. Do not infer the cause from a red badge alone.
3. Read the applicable repository guidance and choose the narrowest relevant local validation. For code changes, use the prescribed project build, stateless audit, and subsystem BAT launchers as applicable. Treat unavailable game UI or engine behavior as unverified; a green CI run does not prove live behavior.
4. Preserve the working-tree baseline and concurrent work. Stage only files owned by this repair; never use broad staging in a dirty checkout. If a task-owned change shares a file with unrelated edits, stage only separable hunks. If ownership cannot be separated, stop and report the specific conflict.
5. Apply the standing authorization in `.agents/rules/git_sync_workflow.md`: create a scoped commit and normal push at objective completion unless the user explicitly requested local-only delivery. It does not authorize unrelated changes, force-push, release publication, or changing repository protections.
6. After each authorized push, verify that the upstream branch points to the exact pushed SHA. Inspect Actions runs, check runs, and commit statuses for that SHA, wait for applicable checks with bounded waits, and report their direct links. If a task-related check fails, inspect its logs, make a scoped correction, run relevant local BAT validation, push under the same authorization, and review the replacement SHA. Do not report queued, running, cancelled, missing, or inaccessible checks as passing.

## Scope and evidence

- Do not repair failures that are unrelated to this request merely because they appear in the same workflow or checkout.
- Do not weaken assertions, disable checks, rewrite history, or remove failed-run evidence to obtain a green result.
- Do not post PR comments, review replies, or resolve review threads as part of CI repair unless the user explicitly authorizes those GitHub actions.
- Report separately what passed locally, what was pushed, whether the remote branch matches, and what the exact-SHA checks show. Link to the matching GitHub run or check; state blockers as pending or unavailable.

# Verified lessons from recent commits

Read when changing persistence, failure tests, CI or delivery gates. Commit evidence does not prove that a later working tree or live game has passed validation.

- **3f7170c — atomic replacement:** `src/CalradiaForge.Sdk/AtomicFileReplacement.cs` preserves `File.Replace`. Retry only `IOException.HResult == 0x80070497` (Win32 1175), while both file names exist, for at most four attempts with 20/40/60 ms waits. Never retry 1176/1177 or substitute delete/move. Existence alone does not establish content integrity.
- **4c5b16c — failure assertions:** `tests/CalradiaForge.Tests/SdkFeaturesTests.cs` captures the exception and asserts occurrence and reference identity after `catch`. An assertion only inside `catch` silently passes if nothing throws.
- **4c5b16c — downloads:** `tools/Setup-CalradiaForge-Python.bat` uses `pip --timeout 120 --retries 5`. These bounded transport retries do not prove complete response-body delivery or successful optional Antigravity setup.
- **6f9f4eb — CI migration:** `.github/workflows/ci.yml` selects checkout v5, setup-dotnet v5 and setup-python v6 for Node.js 24. These are that commit's selected versions, not a permanent claim about the latest upstream releases. Review all affected workflows and annotations at the exact pushed SHA; never disable checks to hide failures.
- **fafc99c — remote gate:** after an authorized push, verify the remote SHA and await applicable Actions, check runs and statuses for that SHA. Correct task-related failures and review the replacement SHA. Local success or successful upload is not remote validation. Avoid evidence-only commit loops and preserve historical failed runs.
- **3470169 and 6963aa3 — distribution:** significant milestones require three audited current-version ZIPs with independently matching SHA-256, even without a version bump and regardless of conversation language. Honor an explicit no-ZIP instruction for the current request. Documentation-only maintenance does not independently require packaging. Archives, staging and reports stay ignored; packaging does not authorize publication or installation. Normal objective pushes use the standing user authorization in the Git rule; an explicit no-push override still applies.

Use filenames emitted by the pipeline: version `25.2.0` currently produces `package-audit-2520.json` and `package-sha256-2520.txt`. Recheck this convention when changing the pipeline.

## Maintaining evidence

Inspect the relevant commit diff and current implementation before updating knowledge; a commit message alone is insufficient. Distinguish committed behavior, ongoing working-tree features and pending live checks. Record actual case counts, viewport dimensions, layout passes and timing from the current BAT run. Historical metrics are baselines, not universal thresholds. Fixtures do not prove concurrent native-write safety, editor imports or in-game rendering.

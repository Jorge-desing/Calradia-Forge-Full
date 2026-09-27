# Persisting the Codex Computer Use compatibility layer

Codex stores Computer Use in a versioned runtime directory. An official update can create a new `cua_node/<runtime-id>/bin/node_modules/@oai/sky/bin/windows` folder, so a `version.dll` installed beside an older helper may not be used by the new helper.

## Managed persistence

Run `Manage-CodexCaptureCompatPersistence.bat Install` from this directory. It copies the tested `dist/version.dll` and its maintenance script to `%LOCALAPPDATA%\OpenAI\Codex\CodexCaptureCompat`, records their SHA-256 hashes, then registers and starts the visible, user-level Task Scheduler task `CodexCaptureCompat-RuntimeWatcher`. The task runs without elevation at user logon and watches only `%LOCALAPPDATA%\OpenAI\Codex\runtimes\cua_node`; filesystem events trigger a scan, with a 15-second fallback scan.

The watcher recognizes only direct runtime-version folders with the exact Codex helper layout and an existing `codex-computer-use.exe`. It skips reparse points. It installs the managed DLL when the target has no `version.dll`; it replaces a different DLL only when the adjacent install record proves that the current file is already owned by this project. Replaced DLLs and records are copied to the managed `backups` folder and checked against SHA-256. An unowned or mismatched DLL is left untouched and reported in `maintenance.jsonl`.

When an official update changes the helper bytes but keeps the same runtime path, the watcher may reconcile its installation only when the adjacent record has the expected schema, owner marker, `managedBy` value, exact helper path, and valid SHA-256 fields. If `version.dll` is present, its bytes must match the DLL hash in that record before replacement; if it is absent, the valid same-path ownership record permits restoration from the staged project payload. Any present DLL whose bytes do not match the recorded hash is treated as unexpected and preserved. A changed helper hash alone is not evidence that an installation record or DLL is owned.

Candidate paths are rejected if traversal would pass through a junction or another reparse point; the watcher does not follow such links, including links in the runtime root, runtime-version directory, or nested helper path. The scheduled task uses a bounded recovery policy of three restart attempts, spaced one minute apart, if the watcher exits unexpectedly.

The watcher never edits Codex's application files, changes `codex-computer-use.exe`, elevates privileges, or terminates Codex or the helper. If a newly created helper starts before the watcher places the DLL, the current process cannot load it retroactively; it will take effect the next time that helper starts. After an official update, check `Manage-CodexCaptureCompatPersistence.bat Status` and verify Computer Use with a real capture before treating the new runtime as validated.

Use `Manage-CodexCaptureCompatPersistence.bat Uninstall` to remove only the owned scheduled task. The staged DLL, script, install records, and backups remain available. The task can be restored with `Install`. To remove a DLL from a specific runtime, use the existing `Install-CodexCaptureCompat.bat` uninstaller with that exact helper path.

The fixture suite is `tests\Test-CodexCaptureCompatPersistence.bat`; the project build/test entry point also calls it through `tools\Build-Test-CodexCaptureCompat.bat`. Tests cover new-runtime discovery, idempotency, exact-path scope, foreign-file protection, managed upgrades, and verified backups. Fixture passes do not verify a future Codex runtime or a live capture.

# Computer Use keyboard repair — 2026-10-01

The current per-user runtime is `1b30f7d4d73226ca`, with `@oai/sky` 0.7.5. The earlier Ctrl alias correction was reapplied using the explicitly recognized package profile and a verified original backup. The native helper independently constructed both key-down and key-up INPUT records with `wScan = 0`; this defect remained after the alias repair.

The local compatibility proxy now intercepts SendInput only when the mapped x64 PE profile, import slot and exact constructor anchors match the audited helper. Missing scan codes are mapped using the foreground thread's keyboard layout. Caller records, Unicode/mouse inputs, existing scan codes, key-up flags, return counts and Win32 error behavior are preserved. Unknown helper profiles do not enable this keyboard correction. Public APIs, IPC, MSIX and WindowsApps were unchanged.

The installed ownership record retained a staging path after the runtime was promoted. A narrowly pinned, recoverable migration now accepts only the observed runtime, owner, helper/DLL hashes and exact relative paths without reparse points. It backs up the record and DLL before rebinding the record. Other moved or unknown installations remain untouched; Status is read-only.

## Validation

- `CodexCaptureCompat/capture-compat/build.bat`: build and native parser, keyboard adapter, error preservation, capture lifetime and callback dispatch tests passed.
- `CodexCaptureCompat/capture-compat/tests/Test-CodexCaptureCompatPersistence.bat`: ownership, unknown/corrupt preservation, read-only Status, transaction recovery, backup, reparse and staging-promotion guard cases passed.
- `Manage-CodexCaptureCompatKeyAliases.bat Status`: Applied, with verified original backup.
- Managed installation BAT: verified replacement and healthy, owned watcher. The installed record now names the final runtime path; source and installed DLL hashes match.
- Fixture launched only through `tests/Start-CodexCaptureCompatFixture.bat`: F10 down/up `0x44/0x44`, Alt+OEM3 `0x27/0x27`, Alt+OEM5 `0x29/0x29`, Ctrl+K accelerator and harmless control click observed. Ctrl, Alt and Shift were released after each chord. Screenshot capture still worked; the process loaded both the local proxy and system version.dll.

The fixture was closed and Computer Use reset afterward. Codex Desktop, Explorer and unrelated applications were not closed. These observations prove the isolated input path, not Bannerlord's polling or hook execution. A new live game check remains pending; OEM key names are layout-dependent and must not be globally remapped.

## Exact hashes

| Item | SHA-256 |
| --- | --- |
| Current helper | `ABDD75DF576B3CBCC7ED170DE1B4F27A65C81E25768A9B0B46D682FB586FB483` |
| Original adapter backup | `617D8E6E18FDDE25F06D4CBA2C84C994E076E05F30A8C55D09E918401C48B171` |
| Patched adapter | `F40CEF88BF48349EA769FE6A10D290D2B25F1623D504C37EE11BA30A10CD4517` |
| Previous native proxy backup | `52CA7BFD990F99344F233E136D6F9B03BB793D906B8493E2A4BD368843DFBE84` |
| Installed native proxy | `965164B41DC83144C159CADE60ACB560BA9D3D89327AB389EED778741F2BE3D4` |

Backups remain under `%LOCALAPPDATA%\OpenAI\Codex\CodexCaptureCompat`; generated binaries and test logs are not repository sources. Unknown future runtimes require a new verified profile rather than forcing these anchors.

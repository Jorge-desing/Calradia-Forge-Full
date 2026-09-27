# Rev018 — Import authorization and Resource Browser access retry

**Date:** 2026-09-23  
**Release line:** Calradia Forge 22.0.0  
**Scope:** Standalone experimental `BannerlordFbxImporter`; this entry records operator authorization and tool observations only.

## Operator clarification and authorization

The operator clarified that they had switched away from the Editor to write a message. They then explicitly authorized continuing import actions without a per-import limit. This is authorization to proceed; it is not evidence that a file was selected, submitted, imported, or verified.

## Resource Browser and automation observations

The visible Resource Browser remained at `Modules > CalradiaForge > Assets`. The attempted UI Automation / Computer Use interaction did not open or expose the import file selector. Coordinate-based actions were rejected while another window overlapped the target, so they did not establish a successful menu or picker interaction. The Remote Desktop Commander attempt did not return a response to the ping/read operation during this attempt.

No file was selected or submitted. No import settings or final import action were observed, no output resource was verified, and no files in `Assets` or `AssetSources` were changed.

## Evidence and validation limits

`Test-BannerlordFbxImporter.bat` reported 52 passed, 0 failed, and 1 skipped. The skipped fixture concerned symlink creation being denied by Windows. These automated results do not validate live Editor controls or a real import.

TaleWorlds' texture documentation describes texture assets and their use through the Asset Browser ([official texture asset guide](https://moddocs.bannerlord.com/asset-management/asset-types/textures/)). That general workflow does not prove that the current import selector accepts PNG or that an imported resource was generated. The selector's filters and resulting resource type remain unobserved.

## Status

The operator has authorized continued import actions without a numeric limit, but the live import workflow remains uncalibrated because the selector and its controls were not reached. No import is claimed as successful. The next useful evidence is a visible, uniquely identified import selector in the Resource Browser and observation of its accepted file filter and settings; only a resulting resource visible with its name and type can establish output verification.

This entry is a new append-only record. It does not modify prior revisions, README files, DOCX records, module assets, or release packages.

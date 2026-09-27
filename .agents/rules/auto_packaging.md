---
name: auto-packaging
description: Enforce automatic generation of distribution .zip files at the end of major updates.
trigger: always_on
---

# Auto-Packaging Workflow

Whenever a major update is performed, a new version is reached, or a significant batch of features is completed, you MUST automatically package the project for distribution.

1. **Trigger Condition:** Upon completing an update or feature set, before concluding the goal.
2. **Action:** Execute the workspace's packaging scripts (e.g., `tools\package.ps1`).
3. **Delivery:** Clearly state in your summary response that the ZIP files were generated and provide their exact output paths.

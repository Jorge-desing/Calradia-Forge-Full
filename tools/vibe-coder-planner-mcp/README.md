# Vibe Coder Planner MCP bridge for Codex

This local stdio MCP bridge lets Codex read projects and tasks from Vibe Coder Planner and create or update project tasks through the documented REST API.

## Authentication

Set the Windows user environment variable `VIBECODERPLANNER_API_KEY` to a Vibe Planner API token. The token is not stored in this folder or in Codex configuration. After setting it, fully restart Codex so its MCP process inherits the updated environment.

## Tools

- `vibe_list_projects` — read project records.
- `vibe_get_project` — read one project.
- `vibe_list_project_tasks` — read a project's tasks.
- `vibe_sync_current_workspace` — find the workspace's Vibe project by name or create a project record and save its ID under `.vibe-coder-planner/project.json`.
- `vibe_create_project_task` — create a task without generating an AI plan.
- `vibe_update_project_task` — update a task title or description.
- `vibe_set_project_task_status` — set a task status.

The bridge exposes no delete operation and no AI plan-generation operation. Syncing a project record does not upload local files or link a GitHub repository.

## Configuration

Codex launches `server.mjs` over stdio. Its global MCP configuration passes through `VIBECODERPLANNER_API_KEY` and sets `VIBE_WORKSPACE_ROOT` to the current workspace. The project metadata sent during first sync is in `workspace-project.json`.

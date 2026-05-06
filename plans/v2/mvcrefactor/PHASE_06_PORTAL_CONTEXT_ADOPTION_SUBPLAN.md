# Phase 6 Portal Context Adoption Subplan

This subplan covers the first adoption step for the route-first portal context resolver.

## Goals

- Remove duplicated actor-id parsing in low-risk WebApp code.
- Keep route parameters and BLL contracts unchanged.
- Avoid mapper-heavy or workflow-heavy route/context rewrites.

## Scope

- `WorkspaceResolver`
- `Management/ProfileController`
- `Management/UsersController`

## Deferred Scope

- MVC mapper refactors.
- Resident/unit lease workflow controller refactors.
- Moving protected areas into `Portal`.
- Route shortcut migration.
- Cookie fallback behavior.

## Build Checkpoint

After this slice, the project owner should run:

```powershell
dotnet build mamarrproject.sln -nologo
```


# Phase 1 Supporting Architecture Subplan

This subplan covers the first safe implementation slice after inventory.

## Goals

- Preserve current behavior and routes.
- Keep BLL API-ready and MVC-neutral.
- Add canonical BLL DTO and mapper scaffolding without forcing service rewrites.
- Start moving protected MVC controllers to `IAppBLL`.

## Tasks

1. Add canonical BLL DTOs for simple entity shapes where DAL canonical DTOs already exist.
2. Add BLL-to-DAL mappers implementing `Base.Contracts.IBaseMapper` for those canonical DTO pairs.
3. Keep existing static projection mappers for dashboard/workspace/read models.
4. Refactor low-risk MVC controllers to inject `IAppBLL` instead of individual BLL service interfaces.
5. Do not change routes, views, API controllers, Admin scaffolded controllers, tests, Base projects, DAL repositories, migrations, or schema.

## First Controller Candidates

- `WebApp/Areas/Management/Controllers/DashboardController.cs`
- `WebApp/Areas/Management/Controllers/ProfileController.cs`
- `WebApp/Areas/Management/Controllers/CustomersController.cs`
- `WebApp/Areas/Management/Controllers/ResidentsController.cs`

## Build Checkpoint

After this slice, the project owner should run:

```powershell
dotnet build mamarrproject.sln -nologo
```

The implementation agent must not run the build unless explicitly allowed.


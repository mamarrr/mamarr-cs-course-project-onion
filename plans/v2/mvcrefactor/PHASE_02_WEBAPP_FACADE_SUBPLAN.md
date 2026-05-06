# Phase 2 WebApp Facade Subplan

This subplan covers the second safe implementation slice.

## Goals

- Keep current routes, views, and behavior intact.
- Make protected MVC controllers depend on `IAppBLL` instead of individual BLL service interfaces.
- Keep WebApp-local services such as identity, chrome, navigation, and mappers as WebApp dependencies.
- Avoid API controller, Admin controller, Base, DAL, test, schema, and migration changes.

## Completed Scope

- Management controllers now call BLL services through `IAppBLL`.
- Customer controllers now call BLL services through `IAppBLL`.
- Property controllers now call BLL services through `IAppBLL`.
- Unit controllers now call BLL services through `IAppBLL`.
- Resident controllers now call BLL services through `IAppBLL`.
- `WorkspaceResolver` now uses `IAppBLL.WorkspaceCatalog`.

## Deferred Scope

- Area reorganization into `Public`, `Portal`, and `Admin`.
- Portal route shape changes.
- Context resolver/accessor redesign.
- Removing MVC mapper classes.
- Moving command construction out of workflow-heavy controllers.
- API controller cleanup.
- Admin scaffold cleanup.

## Build Checkpoint

After this slice, the project owner should run:

```powershell
dotnet build mamarrproject.sln -nologo
```


# Phase 5 Portal Context Resolver Subplan

This subplan covers the route-first context resolver skeleton.

## Goals

- Add a WebApp-level portal route context resolver.
- Centralize actor id and route-value extraction.
- Keep authorization decisions in BLL services.
- Preserve current protected routes and areas.
- Use the resolver in one low-risk controller first.

## Scope

- `PortalRouteContext`
- `PortalContextKind`
- `ICurrentPortalContextResolver`
- `CurrentPortalContextResolver`
- DI registration
- `Management/DashboardController` as the first consumer

## Deferred Scope

- Moving protected controllers into `Portal`.
- Rewriting all controller command mapping to use route context.
- BLL context contract changes.
- Cookie fallback behavior.
- API controller cleanup.

## Build Checkpoint

After this slice, the project owner should run:

```powershell
dotnet build mamarrproject.sln -nologo
```


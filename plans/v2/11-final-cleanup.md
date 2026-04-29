# Mamarr CS Course Project Refactor Agent Plan

This file is one implementation slice of the larger refactor plan. It is written to be handed to an AI coding agent as a focused task.

Global constraints for every slice:

- Preserve existing user-facing behavior.
- Do not add new features.
- Do not implement ticket, vendor, scheduled work, or work-log functionality.
- Do not change API routes unless explicitly required and documented.
- Do not intentionally change JSON response shapes.
- Do not intentionally change MVC page behavior.
- Do not introduce FluentResults into DAL repository interfaces or UOW methods.
- Repositories return DAL DTOs, nullable DAL DTOs, booleans, lists, IDs, or throw unexpected infrastructure/programming exceptions.
- Use the existing `BaseRepository` for custom EF repositories whenever possible, because it already contains regular CRUD operations and built-in IDOR/parent-scope restrictions for entities that have a parent management company or customer.
- Custom repositories should inherit from `BaseRepository` when the entity can use the generic CRUD/mapping behavior. Add only query-specific or workflow-specific methods on top of it.
- Do not reimplement regular CRUD methods such as add, update, remove, find, or list in custom repositories unless the current `BaseRepository` cannot support that entity/use case.
- BLL service boundaries return `FluentResults.Result` or `FluentResults.Result<T>`.
- `App.DTO` remains API-only and must not be referenced by BLL.
- BLL must not reference `App.DAL.EF` or `AppDbContext`.
- Controllers must not use repositories or `AppDbContext`.
- Dependency registration helper methods live in `WebApp/Helpers`, not in `App.BLL` or `App.DAL.EF`.
- Build the solution after the slice changes and fix compile errors caused by the slice.

# Final Cleanup Phase

## Goal

Remove obsolete code and normalize structure after all vertical slices are complete.

This phase should not change behavior.

## Scope

Cleanup only:

- Old BLL service interfaces that were moved to `App.BLL.Contracts`.
- Old BLL service implementations that were replaced.
- Old direct `AppDbContext` usage in BLL.
- Old direct repository/UOW usage in controllers, if any remains.
- Unused DTOs created during intermediate refactor attempts.
- Unused mappers.
- Duplicate DI registrations in `Program.cs` and `WebApp/Helpers/DependencyInjectionHelpers.cs`.
- Obsolete namespaces/imports.
- Project references that violate target dependency direction.

Do not add new features.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/Program.cs`
- `WebApp/Helpers/DependencyInjectionHelpers.cs`
- All `WebApp/ApiControllers/**`
- All regular MVC controllers under `WebApp/Controllers/**` and areas if present
- `App.BLL/**/*.cs`
- `App.BLL.Contracts/**/*.cs`
- `App.Contracts/**/*.cs`
- `App.DAL.EF/**/*.cs`
- Project files:
  - `App.BLL/App.BLL.csproj`
  - `App.DAL.EF/App.DAL.EF.csproj`
  - `App.Contracts/App.Contracts.csproj`
  - `App.DTO/App.DTO.csproj`
  - `WebApp/WebApp.csproj`

## Cleanup checks

## Check 1: BLL does not reference App.DAL.EF

Search:

```bash
grep -R "App.DAL.EF" App.BLL -n
grep -R "AppDbContext" App.BLL -n
```

Expected result:

- No production BLL service should use `App.DAL.EF`.
- No BLL service should inject `AppDbContext`.

If any remain, refactor them through `IAppUOW` or mark them explicitly as out of current implemented scope if they belong to excluded ticket/vendor/work features.

## Check 2: BLL does not reference App.DTO

Search:

```bash
grep -R "App.DTO" App.BLL App.BLL.Contracts -n
```

Expected result:

- No BLL or BLL contracts reference `App.DTO`.

## Check 3: Controllers do not use DAL directly

Search:

```bash
grep -R "AppDbContext" WebApp/Controllers WebApp/ApiControllers -n
grep -R "IAppUOW" WebApp/Controllers WebApp/ApiControllers -n
grep -R "Repository" WebApp/Controllers WebApp/ApiControllers -n
```

Expected result:

- Controllers should not use `AppDbContext`.
- Controllers should not use repositories or UOW directly.
- Controllers should call BLL services.

Exception:

- `Program.cs` and `WebApp/Helpers` can reference DAL concrete types because WebApp is the composition root.

## Check 4: Repositories do not return domain entities

Inspect repository interfaces in:

```text
App.Contracts/DAL/**
```

Expected result:

- Repository methods return DAL DTOs, nullable DAL DTOs, booleans, lists, IDs, or `Task`.
- Repository interfaces do not return domain entity types.
- Repository interfaces do not return FluentResults.

## Check 5: FluentResults usage is correct

Expected:

- `App.BLL.Contracts` references FluentResults.
- BLL service methods return `Result` or `Result<T>`.
- WebApp maps FluentResults to HTTP or MVC ModelState.
- DAL does not use FluentResults in repository/UOW contracts.

## Check 6: DI registration is centralized in WebApp helpers

Expected:

- `WebApp/Helpers/DependencyInjectionHelpers.cs` contains:
  - `AddAppDalEf`
  - `AddAppBll`
  - `AddWebAppMappers`
- `App.DAL.EF/DependencyInjection.cs` should not exist unless it existed for another reason and is not used for this architecture.
- `App.BLL/DependencyInjection.cs` should not exist unless it existed for another reason and is not used for this architecture.
- `Program.cs` should use helper methods instead of long lists of registrations.

## Check 7: Excluded features remain excluded

Ensure the refactor did not implement or expand:

- Ticket functionality.
- Vendor functionality.
- Scheduled work functionality.
- Work-log functionality.

Existing incomplete code may remain untouched if it existed before, but this plan should not add new user-facing behavior for those areas.

## Check 8: Remove obsolete code carefully

Remove files only when all references are gone.

Before deleting a file:

1. Search for its class/interface name.
2. Confirm no active controller/service uses it.
3. Confirm the replacement exists.
4. Build after deletion.

Do not delete domain entities, migrations, seed data, or DTOs used by currently active API responses.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by cleanup.

## Stop condition

Stop when the solution builds and dependency direction rules are satisfied.

Do not make behavior changes during cleanup.

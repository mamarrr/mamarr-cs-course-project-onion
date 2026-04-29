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

# Slice 4: Unit Profile / Unit Workspace

## Goal

Refactor unit profile and unit workspace functionality while preserving current behavior.

## Scope

Refactor only:

- Unit profile read/update/delete if implemented.
- Units listed under a property.
- Create unit under a property if implemented.
- Unit dashboard/workspace context.
- Unit access checks.
- API and MVC controllers serving unit flows.

Do not refactor resident or lease assignment workflows beyond read-only summaries already shown in unit dashboard.

## Property-unit flows included in this slice

The current codebase has some unit list/create flows under property controllers. These are part of the Unit slice, not the Property slice. Include these in this plan:

- `WebApp/ApiControllers/Property/PropertyUnitsController.cs`
- `WebApp/Areas/Property/Controllers/UnitsController.cs`, if present
- Property-area unit ViewModels/views, if they currently depend on old unit workspace services
- Unit list/create flows under a property

These files should be refactored only for unit list/create behavior. Do not change unrelated property profile/workspace behavior that was completed in the Property slice.

## Lease/resident workflow deferral

`UnitTenantsController` may be located under `WebApp/ApiControllers/Unit/**`, but lease/resident assignment workflows are **not** part of this slice.

For `UnitTenantsController`, only replace duplicated unit-context resolution with the new Unit BLL/access service if needed for compilation or consistency. Do **not** refactor or redesign:

- `ILeaseAssignmentService`
- `ILeaseLookupService`
- lease create/update/delete commands
- resident search/assignment workflows
- lease business rules

Those remain on the old implementation until the Lease Assignments slice.

## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

The `UnitRepository` implementation should follow the same pattern as the current `CustomerRepository` and `PropertyRepository`: inherit from `BaseRepository`, inject the corresponding DAL mapper, pass the mapper to the base constructor, and add only custom query/workflow methods that are not already provided by the base repository. All Domain ↔ DAL DTO conversion must go through mapper classes, not inline repeated mapping logic spread across controllers or services.

## Files to inspect first

- `WebApp/ApiControllers/Unit/**`
- `WebApp/ApiControllers/Property/PropertyUnitsController.cs`
- `WebApp/Areas/Property/Controllers/UnitsController.cs`, if present
- Unit MVC controllers/views/ViewModels, if present
- Property-area unit ViewModels/views, if present
- Current unit DTOs in `App.DTO`
- Current BLL services under:
  - `App.BLL/UnitWorkspace/**`
  - `App.BLL/Properties/**`
  - `App.BLL.Contracts/Properties/**`
- `App.Domain/**/Unit.cs`
- `App.Domain/**/Property.cs`
- `App.Domain/**/Lease.cs`
- `App.DAL.EF/AppDbContext.cs`
- Existing property/customer repositories/services
- `IAppUOW` / `AppUOW`

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Units/**`
- `App.DAL.EF/Repositories/UnitRepository.cs`
- `App.DAL.EF/Mappers/Units/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Units/**`
- `App.BLL/Units/**`
- `App.BLL/Mappers/Units/**`
- `WebApp/Mappers/Api/Units/**`
- `WebApp/Mappers/Mvc/Units/**`, if MVC exists
- `WebApp/ApiControllers/Unit/**`
- `WebApp/ApiControllers/Property/PropertyUnitsController.cs`
- `WebApp/Areas/Property/Controllers/UnitsController.cs`, if present
- Unit MVC controllers/ViewModels, if present
- Property-area unit ViewModels/views, if present
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify resident/lease/management/onboarding behavior except for compilation. Do not refactor property profile/workspace behavior outside the property-unit list/create flows listed above.

## Current behavior inventory

Before editing, identify:

- Unit API routes.
- Property-unit API routes, especially routes in `PropertyUnitsController`.
- Unit MVC routes, if present.
- Property-area unit MVC routes, especially `Areas/Property/Controllers/UnitsController.cs` if present.
- Existing request/response DTOs.
- Existing ViewModels.
- Existing unit dashboard response shape.
- Existing unit create/update validation.
- Existing not-found/forbidden/duplicate behavior.
- Existing slug/unit number behavior.
- Any active lease summaries currently shown.

Preserve all behavior.

## DAL contracts

Create:

```text
App.Contracts/DAL/Units/
  IUnitRepository.cs
  UnitDalDto.cs
  UnitProfileDalDto.cs
  UnitDashboardDalDto.cs
  UnitListItemDalDto.cs
  UnitCreateDalDto.cs
  UnitUpdateDalDto.cs
```

Suggested DTOs:

```csharp
public sealed class UnitDalDto
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string UnitNr { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public bool IsActive { get; init; }
}
```

```csharp
public sealed class UnitProfileDalDto
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string UnitNr { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

Adjust fields to current domain and existing API responses. In the current domain/API naming, prefer existing unit field names such as:

- `UnitNr`
- `FloorNr`
- `SizeM2`
- `Notes`
- `Slug`
- `IsActive`
- `CreatedAt`
- `PropertyId`

Do not introduce new public names like `Name`, `Number`, `Floor`, or `Size` if the existing DTOs/controllers use `UnitNr`, `FloorNr`, or `SizeM2`.

## Repository interface

```csharp
public interface IUnitRepository : IBaseRepository<UnitDalDto>
{
    Task<UnitProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken = default);

    Task<UnitDashboardDalDto?> GetDashboardAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitListItemDalDto>> AllByPropertyAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default);

    Task<bool> UnitSlugExistsForPropertyAsync(
        Guid propertyId,
        string slug,
        Guid? exceptUnitId = null,
        CancellationToken cancellationToken = default);

    Task<UnitDalDto> AddAsync(
        UnitCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UnitUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid unitId,
        Guid propertyId,
        CancellationToken cancellationToken = default);
}
```

Adjust for current behavior and the actual generic signature of `IBaseRepository` in this codebase.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/UnitRepository.cs
App.DAL.EF/Mappers/Units/UnitDalMapper.cs
```

Rules:

- `UnitRepository` should inherit from `BaseRepository<UnitDalDto, Unit, AppDbContext>` or the exact generic signature used by the current base repository.
- Inject `UnitDalMapper` and pass it to the `BaseRepository` constructor, following the same style as `CustomerRepository` and `PropertyRepository`.
- Use mapper methods for Domain ↔ DAL DTO conversion. Do not duplicate mapping logic inline across methods.
- Scope unit queries by company/customer/property/unit slugs.
- Include only current dashboard data.
- Return empty list for no units.
- Do not return FluentResults.
- Do not return domain entities.

## UOW changes

Add:

```csharp
IUnitRepository Units { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create:

```text
App.BLL.Contracts/Units/
  Services/
    IUnitProfileService.cs
    IUnitWorkspaceService.cs
    IUnitAccessService.cs
  Queries/
    GetUnitProfileQuery.cs
    GetUnitDashboardQuery.cs
    GetPropertyUnitsQuery.cs
  Commands/
    CreateUnitCommand.cs
    UpdateUnitCommand.cs
    DeleteUnitCommand.cs
  Models/
    UnitProfileModel.cs
    UnitDashboardModel.cs
    UnitListItemModel.cs
```

Service examples:

```csharp
public interface IUnitProfileService
{
    Task<Result<UnitProfileModel>> GetAsync(
        GetUnitProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> UpdateAsync(
        UpdateUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeleteUnitCommand command,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IUnitWorkspaceService
{
    Task<Result<UnitDashboardModel>> GetDashboardAsync(
        GetUnitDashboardQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<UnitListItemModel>>> GetPropertyUnitsAsync(
        GetPropertyUnitsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> CreateAsync(
        CreateUnitCommand command,
        CancellationToken cancellationToken = default);
}
```

## BLL implementation

Create or refactor:

```text
App.BLL/Units/UnitProfileService.cs
App.BLL/Units/UnitWorkspaceService.cs
App.BLL/Units/UnitAccessService.cs
App.BLL/Mappers/Units/UnitBllMapper.cs
```

Rules:

- Use `IAppUOW`.
- Do not use `AppDbContext`.
- Move controller-level context chains into BLL service/access service.
- Convert missing company/customer/property/unit to `NotFoundError` where current behavior does.
- Convert access failures to `ForbiddenError`.
- Convert duplicate unit slug/UnitNr to `ConflictError` if current behavior does.
- Call `_uow.SaveChangesAsync` once for successful create/update/delete.
- Do not introduce lease assignment changes.

## WebApp API mappers

Create:

```text
WebApp/Mappers/Api/Units/UnitApiMapper.cs
```

If property-unit list/create endpoints currently use property-specific response DTOs, either reuse `UnitApiMapper` for those endpoints or create a small mapper under `WebApp/Mappers/Api/Properties/` only for adapting property-unit responses. Do not create duplicate business mapping logic.

Responsibilities:

- Route/user -> unit queries.
- API request DTO -> create/update commands.
- BLL model -> existing API response DTO.

## Controllers

Refactor:

```text
WebApp/ApiControllers/Unit/*
WebApp/ApiControllers/Property/PropertyUnitsController.cs
```

For MVC, also refactor property-area unit controllers if present:

```text
WebApp/Areas/Property/Controllers/UnitsController.cs
```

Rules:

- Preserve route/auth attributes.
- Remove manual chain:
  - resolve company
  - resolve customer
  - resolve property
  - resolve unit
  - map each failure manually
- Use one BLL service call where possible.
- Use `ToActionResult`.
- For `PropertyUnitsController`, replace old `IPropertyUnitService` / old unit workspace dependencies with the new Unit BLL contracts while preserving existing property-unit routes and response shapes.
- For `UnitTenantsController`, do not refactor lease assignment behavior in this slice. Only replace duplicated unit-context resolution if needed.

## MVC controllers, if present

Refactor unit MVC controller/ViewModels using the same pattern. Include `WebApp/Areas/Property/Controllers/UnitsController.cs` and property-area unit ViewModels/views if they currently serve unit list/create flows under a property.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register unit BLL services.
- Register WebApp unit mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when unit profile/workspace is refactored and builds.

This includes:

- Unit profile/dashboard controllers.
- Units listed under a property.
- Create unit under a property, if currently implemented.
- `PropertyUnitsController` API flow.
- Property-area MVC unit list/create flow, if currently implemented.
- No lease assignment/resident workflow redesign.

Do not start Resident or Lease slice.

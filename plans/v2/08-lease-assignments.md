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

# Slice 6: Lease Assignments

## Goal

Refactor currently implemented lease assignment functionality while preserving existing behavior.

## Scope

Refactor only existing lease-related functionality:

- Assign resident to unit, if currently implemented.
- End lease, if currently implemented.
- Update lease dates/roles, if currently implemented.
- Read unit/resident lease lists, if currently implemented.
- Validate overlapping leases using current business rules.
- Lease-related API and MVC controllers.

Do not add new lease features.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- Controllers/actions related to lease assignments:
  - `WebApp/ApiControllers/**` lease-related files
  - MVC lease assignment controllers/views, if present
- Current BLL services under:
  - `App.BLL/LeaseAssignments/**`
  - any unit/resident services that manage leases
- Current lease DTOs in `App.DTO`
- Current lease ViewModels in `WebApp`
- `App.Domain/**/Lease.cs`
- `App.Domain/**/LeaseRole.cs`
- `App.Domain/**/Unit.cs`
- `App.Domain/**/Resident.cs`
- `App.DAL.EF/AppDbContext.cs`
- Existing `IUnitRepository`
- Existing `IResidentRepository`
- Existing lookup repository/status role access
- `IAppUOW` / `AppUOW`

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Leases/**`
- `App.DAL.EF/Repositories/LeaseRepository.cs`
- `App.DAL.EF/Mappers/Leases/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Leases/**`
- `App.BLL/Leases/**`
- `App.BLL/Mappers/Leases/**`
- Lease-related WebApp API/MVC mappers
- Lease-related controllers/ViewModels
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify ticket/vendor/work functionality.

## Current behavior inventory

Before editing, identify:

- Current lease-related routes.
- Request/response DTO shapes.
- ViewModel shapes.
- Existing overlapping lease rules.
- Existing access rules.
- Existing lease role resolution.
- Existing not-found/forbidden/conflict/validation behavior.
- Existing transaction behavior, if any.

Preserve all behavior.

## DAL contracts

Create:

```text
App.Contracts/DAL/Leases/
  ILeaseRepository.cs
  LeaseDalDto.cs
  LeaseCreateDalDto.cs
  LeaseUpdateDalDto.cs
  LeaseAssignmentDalDto.cs
  LeaseRoleDalDto.cs
  ActiveLeaseDalDto.cs
```

Suggested DTOs:

```csharp
public sealed class LeaseDalDto
{
    public Guid Id { get; init; }
    public Guid UnitId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid? LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
}
```

Adjust date types to match the current domain.

## Repository interface

```csharp
public interface ILeaseRepository
{
    Task<IReadOnlyList<LeaseDalDto>> ActiveByUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseDalDto>> ActiveByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseDalDto>> AllByUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseDalDto>> AllByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);

    Task<LeaseDalDto?> FindAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingLeaseAsync(
        Guid unitId,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? exceptLeaseId = null,
        CancellationToken cancellationToken = default);

    Task<LeaseDalDto> AddAsync(
        LeaseCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        LeaseUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> EndLeaseAsync(
        Guid leaseId,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
```

Adjust to current methods and date types.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/LeaseRepository.cs
App.DAL.EF/Mappers/Leases/LeaseDalMapper.cs
```

Rules:

- Do not return domain entities.
- Do not use FluentResults.
- Keep overlap logic equivalent to current behavior.
- Keep tenant scoping when queries are reached through unit/resident context.
- Do not call `SaveChangesAsync` inside repository unless existing base pattern requires it.

## UOW changes

Add:

```csharp
ILeaseRepository Leases { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create:

```text
App.BLL.Contracts/Leases/
  Services/
    ILeaseAssignmentService.cs
    ILeaseLookupService.cs
  Queries/
    GetUnitLeasesQuery.cs
    GetResidentLeasesQuery.cs
  Commands/
    AssignResidentToUnitCommand.cs
    EndLeaseCommand.cs
    UpdateLeaseCommand.cs
  Models/
    LeaseModel.cs
    ActiveLeaseModel.cs
    LeaseAssignmentModel.cs
```

Service:

```csharp
public interface ILeaseAssignmentService
{
    Task<Result<LeaseAssignmentModel>> AssignResidentToUnitAsync(
        AssignResidentToUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> EndLeaseAsync(
        EndLeaseCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> UpdateLeaseAsync(
        UpdateLeaseCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LeaseModel>>> GetUnitLeasesAsync(
        GetUnitLeasesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LeaseModel>>> GetResidentLeasesAsync(
        GetResidentLeasesQuery query,
        CancellationToken cancellationToken = default);
}
```

Only include methods that are currently implemented.

## BLL implementation

Create or refactor:

```text
App.BLL/Leases/LeaseAssignmentService.cs
App.BLL/Leases/LeaseLookupService.cs
App.BLL/Mappers/Leases/LeaseBllMapper.cs
```

Rules:

- Use `IAppUOW`.
- Do not use `AppDbContext`.
- Unit must belong to resolved company/customer/property context.
- Resident must belong to correct company/customer context.
- Preserve existing no-overlap rule.
- Resolve lease role through lookup/repository, not hardcoded seeded GUIDs.
- Return `ConflictError` for overlap if current behavior treats it as a conflict.
- Return `BusinessRuleError` or `ValidationAppError` for invalid date ranges according to current behavior.
- Use UOW transaction methods if assignment writes multiple records.
- Call `_uow.SaveChangesAsync` once for successful command.

## WebApp mappers

Create lease API and MVC mappers only for existing endpoints/pages:

```text
WebApp/Mappers/Api/Leases/LeaseApiMapper.cs
WebApp/Mappers/Mvc/Leases/LeaseViewModelMapper.cs
```

## Controllers

Refactor lease-related API/MVC controllers.

Rules:

- Preserve routes/auth.
- Controller maps input to BLL command/query.
- Controller calls one BLL service method.
- Controller maps result to response/ViewModel.
- Controller uses `ToActionResult` for APIs.
- No repositories or `AppDbContext`.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register lease BLL services.
- Register lease WebApp mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when existing lease assignment functionality is refactored and builds.

Do not start Management/Onboarding.

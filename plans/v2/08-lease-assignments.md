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

This slice must also remove the current build break caused by the old lease assignment code referencing the removed/obsolete Unit and Resident workspace namespaces.

## Scope

Refactor only existing lease-related functionality:

- Assign resident to unit, if currently implemented.
- End lease, if currently implemented.
- Update lease dates/roles, if currently implemented.
- Read unit/resident lease lists, if currently implemented.
- Validate overlapping leases using current business rules.
- Lease-related API and MVC controllers.
- Replace the old `App.BLL/LeaseAssignments/**` services/contracts with the new lease contracts/services.
- Remove all old lease dependencies on `App.BLL.ResidentWorkspace.*`, `App.BLL.UnitWorkspace.*`, `ResidentDashboardContext`, and `UnitDashboardContext`.

Do not add new lease features.

## Current build-break context after Unit and Resident slices

The Unit and Resident slices have already moved their BLL models/services into the new contracts structure:

```text
App.BLL.Contracts.Units
App.BLL.Contracts.Residents
```

The old lease assignment code still references deleted/obsolete namespaces and context types:

```text
App.BLL.ResidentWorkspace.*
App.BLL.UnitWorkspace.*
ResidentDashboardContext
UnitDashboardContext
```

Plan 08 must remove these old dependencies. Do **not** recreate the old `ResidentWorkspace` or `UnitWorkspace` namespaces just to make the solution compile.

Instead, refactor lease assignment code to use the new BLL contract models, queries, and commands from:

```text
App.BLL.Contracts.Residents
App.BLL.Contracts.Units
App.BLL.Contracts.Leases
```

Plan 08 is not complete until no code references:

```text
App.BLL.ResidentWorkspace
App.BLL.UnitWorkspace
ResidentDashboardContext
UnitDashboardContext
App.DAL.EF from BLL lease services
```

## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

Lease repository implementation should follow the same established pattern as the already refactored customer/property/unit/resident repositories:

```csharp
public interface ILeaseRepository : IBaseRepository<LeaseDalDto>
{
    // Custom lease queries/workflow methods only.
}
```

```csharp
public sealed class LeaseRepository
    : BaseRepository<LeaseDalDto, Lease, AppDbContext>, ILeaseRepository
{
    private readonly AppDbContext _dbContext;
    private readonly LeaseDalMapper _mapper;

    public LeaseRepository(AppDbContext dbContext, LeaseDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    // Custom lease queries/workflows here.
}
```

Adjust generic type parameters to match the actual current `BaseRepository`/`IBaseRepository` signatures in the codebase.

## Files to inspect first

- Current build errors in `App.BLL/LeaseAssignments/**`.
- Current BLL services under:
  - `App.BLL/LeaseAssignments/**`
  - `App.BLL/Units/**`
  - `App.BLL/Residents/**`
- New BLL contracts under:
  - `App.BLL.Contracts/Units/**`
  - `App.BLL.Contracts/Residents/**`
- Current Unit/Resident context/access models and services:
  - `App.BLL.Contracts/Units/Models/UnitWorkspaceModel.cs`
  - `App.BLL.Contracts/Residents/Models/ResidentWorkspaceModel.cs`
  - `App.BLL.Contracts/Units/Services/IUnitAccessService.cs`
  - `App.BLL.Contracts/Residents/Services/IResidentAccessService.cs`
- Controllers/actions related to lease assignments:
  - `WebApp/ApiControllers/Resident/ResidentUnitsController.cs`
  - `WebApp/ApiControllers/Unit/UnitTenantsController.cs`
  - `WebApp/Areas/Resident/Controllers/UnitsController.cs`, if present/implemented
  - `WebApp/Areas/Unit/Controllers/TenantsController.cs`, if present/implemented
  - Any other lease-related API/MVC controllers found by search
- Current lease DTOs in `App.DTO`.
- Current lease ViewModels in `WebApp`.
- Current lease-related WebApp mappers, if any.
- `App.Domain/**/Lease.cs`.
- `App.Domain/**/LeaseRole.cs`.
- `App.Domain/**/Unit.cs`.
- `App.Domain/**/Resident.cs`.
- `App.DAL.EF/AppDbContext.cs`.
- Existing `IUnitRepository` and `UnitRepository`.
- Existing `IResidentRepository` and `ResidentRepository`.
- Existing lookup repository/status/role access.
- `IAppUOW` / `AppUOW`.

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Leases/**`
- `App.DAL.EF/Repositories/LeaseRepository.cs`
- `App.DAL.EF/Mappers/Leases/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Leases/**`
- `App.BLL/Leases/**`
- `App.BLL/Mappers/Leases/**`
- `App.BLL/LeaseAssignments/**` only to migrate, delete, move, or exclude old code after replacement
- Lease-related WebApp API/MVC mappers
- `WebApp/ApiControllers/Resident/ResidentUnitsController.cs`
- `WebApp/ApiControllers/Unit/UnitTenantsController.cs`
- `WebApp/Areas/Resident/Controllers/UnitsController.cs`, if implemented
- `WebApp/Areas/Unit/Controllers/TenantsController.cs`, if implemented
- Other lease-related controllers/ViewModels found during inventory
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify ticket/vendor/work functionality.

## Current behavior inventory

Before editing, identify and preserve:

- Current lease-related routes.
- Request/response DTO shapes.
- ViewModel shapes.
- Existing resident-to-unit lease flows.
- Existing unit-to-resident/tenant lease flows.
- Existing overlapping lease rules.
- Existing access rules.
- Existing lease role resolution.
- Existing not-found/forbidden/conflict/validation behavior.
- Existing transaction behavior, if any.
- Existing search/list behavior used by lease assignment modals/forms.

Preserve all behavior. The refactor should only change internal architecture.

## Old LeaseAssignments replacement rule

Do not keep old `App.BLL.LeaseAssignments` services compiling against deleted Unit/Resident workspace types.

During this slice, replace the old lease assignment contracts and implementations with new lease contracts under:

```text
App.BLL.Contracts/Leases
```

and new implementations under:

```text
App.BLL/Leases
```

After the new lease services and WebApp controllers are wired:

- old `App.BLL/LeaseAssignments/ILeaseAssignmentService.cs` should be deleted, moved, or excluded from compilation;
- old `App.BLL/LeaseAssignments/ILeaseLookupService.cs` should be deleted, moved, or excluded from compilation;
- old `App.BLL/LeaseAssignments/LeaseAssignmentService.cs` should be deleted, moved, or excluded from compilation;
- old `App.BLL/LeaseAssignments/LeaseLookupService.cs` should be deleted, moved, or excluded from compilation.

Do not add compatibility shims for `ResidentDashboardContext` or `UnitDashboardContext` as the final solution. If a temporary local shim is used while coding, remove it before completing this slice.

## Context handling rule

Do not pass old dashboard context classes into lease services.

Lease commands/queries should carry the required context values directly. Use the current new Unit/Resident workspace/access models to populate those values from controllers or mapper code.

Typical context values needed by lease commands/queries:

```text
AppUserId
ManagementCompanyId
CompanySlug
CustomerId, if needed
CustomerSlug, if needed
PropertyId, if needed
PropertySlug, if needed
UnitId, for unit-based lease flows
UnitSlug, for unit-based lease flows
ResidentId, for resident-based lease flows
ResidentIdCode or ResidentSlug, for resident-based lease flows if currently used by routes
```

Controllers should resolve unit/resident context through the new Unit/Resident access services, then map that context into lease commands/queries.

If the current controller already receives resolved `UnitWorkspaceModel` or `ResidentWorkspaceModel`, pass only the needed values into lease commands/queries. Do not pass old `UnitDashboardContext` or `ResidentDashboardContext` types.

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

Add additional DAL DTOs only if current behavior needs them:

```text
ResidentLeaseDalDto
UnitLeaseDalDto
LeasePropertySearchItemDalDto
LeaseUnitOptionDalDto
LeaseResidentSearchItemDalDto
LeaseRoleOptionDalDto
```

Suggested base DTO:

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

Adjust fields, names, and date types to match the current domain and API behavior.

## Repository interface

Use `IBaseRepository<LeaseDalDto>` if the current base interface signature supports it.

```csharp
public interface ILeaseRepository : IBaseRepository<LeaseDalDto>
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

    Task<LeaseDalDto?> FirstByIdForUnitAsync(
        Guid leaseId,
        Guid unitId,
        CancellationToken cancellationToken = default);

    Task<LeaseDalDto?> FirstByIdForResidentAsync(
        Guid leaseId,
        Guid residentId,
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

Adjust method names and signatures to current behavior. Do not create methods for functionality that is not currently implemented.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/LeaseRepository.cs
App.DAL.EF/Mappers/Leases/LeaseDalMapper.cs
```

Rules:

- `LeaseRepository` should inherit from `BaseRepository` where possible.
- `LeaseRepository` must use `LeaseDalMapper` for domain/DAL DTO mapping.
- Do not return domain entities.
- Do not use FluentResults.
- Keep overlap logic equivalent to current behavior.
- Keep tenant scoping when queries are reached through unit/resident context.
- Do not call `SaveChangesAsync` inside repository unless the existing base pattern requires it.

## UOW changes

Add:

```csharp
ILeaseRepository Leases { get; }
```

Implement in `AppUOW` using the same lazy repository pattern as the already refactored repositories.

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
    GetUnitLeaseQuery.cs
    GetResidentLeaseQuery.cs
    SearchLeasePropertiesQuery.cs
    GetLeaseUnitsForPropertyQuery.cs
    SearchLeaseResidentsQuery.cs
  Commands/
    CreateLeaseFromResidentCommand.cs
    CreateLeaseFromUnitCommand.cs
    UpdateLeaseFromResidentCommand.cs
    UpdateLeaseFromUnitCommand.cs
    DeleteLeaseFromResidentCommand.cs
    DeleteLeaseFromUnitCommand.cs
    EndLeaseCommand.cs
  Models/
    LeaseModel.cs
    ResidentLeaseModel.cs
    UnitLeaseModel.cs
    LeaseCommandModel.cs
    ActiveLeaseModel.cs
    LeaseAssignmentModel.cs
    LeasePropertySearchItemModel.cs
    LeaseUnitOptionModel.cs
    LeaseResidentSearchItemModel.cs
    LeaseRoleOptionModel.cs
```

Only create contracts/models that are needed by currently implemented endpoints/pages.

### Service contract guidance

The app currently has lease flows from both resident context and unit context. Model the new service contracts around those existing flows instead of passing old dashboard context classes.

Example shape:

```csharp
public interface ILeaseAssignmentService
{
    Task<Result<IReadOnlyList<ResidentLeaseModel>>> GetResidentLeasesAsync(
        GetResidentLeasesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<UnitLeaseModel>>> GetUnitLeasesAsync(
        GetUnitLeasesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> GetLeaseForResidentAsync(
        GetResidentLeaseQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseModel>> GetLeaseForUnitAsync(
        GetUnitLeaseQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> CreateFromResidentAsync(
        CreateLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> CreateFromUnitAsync(
        CreateLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> UpdateFromResidentAsync(
        UpdateLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<LeaseCommandModel>> UpdateFromUnitAsync(
        UpdateLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFromResidentAsync(
        DeleteLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteFromUnitAsync(
        DeleteLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default);
}
```

Lookup service example:

```csharp
public interface ILeaseLookupService
{
    Task<Result<IReadOnlyList<LeasePropertySearchItemModel>>> SearchPropertiesAsync(
        SearchLeasePropertiesQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LeaseUnitOptionModel>>> ListUnitsForPropertyAsync(
        GetLeaseUnitsForPropertyQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LeaseResidentSearchItemModel>>> SearchResidentsAsync(
        SearchLeaseResidentsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LeaseRoleOptionModel>>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);
}
```

Adjust method names and return models to preserve the current API/MVC behavior. Do not add endpoints or features that do not already exist.

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
- Do not reference `App.DAL.EF`.
- Do not reference `App.BLL.ResidentWorkspace` or `App.BLL.UnitWorkspace`.
- Do not use `ResidentDashboardContext` or `UnitDashboardContext`.
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

Mapper responsibilities:

- New unit/resident workspace/access models -> lease queries/commands.
- API request DTOs -> lease commands.
- Lease BLL models -> existing API response DTOs.
- Lease BLL models -> existing MVC ViewModels.

## Controllers

Refactor lease-related API/MVC controllers.

Required controllers to review/refactor:

```text
WebApp/ApiControllers/Resident/ResidentUnitsController.cs
WebApp/ApiControllers/Unit/UnitTenantsController.cs
WebApp/Areas/Resident/Controllers/UnitsController.cs, if implemented
WebApp/Areas/Unit/Controllers/TenantsController.cs, if implemented
```

Rules:

- Preserve routes/auth.
- Resolve resident/unit context using the new Resident/Unit access services.
- Map resolved context into lease query/command objects.
- Controller calls one BLL lease service method where possible.
- Controller maps result to response/ViewModel.
- Controller uses `ToActionResult` for APIs.
- No repositories or `AppDbContext`.
- No old `ResidentDashboardContext` or `UnitDashboardContext`.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register lease BLL services from `App.BLL/Leases`.
- Register lease WebApp mappers.
- Remove old `App.BLL.LeaseAssignments` service registrations after replacement.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when existing lease assignment functionality is refactored and builds.

This slice is not complete until:

```text
No code references App.BLL.ResidentWorkspace.
No code references App.BLL.UnitWorkspace.
No code references ResidentDashboardContext.
No code references UnitDashboardContext.
No BLL lease service references App.DAL.EF or AppDbContext.
No old App.BLL/LeaseAssignments files remain compiled against obsolete types.
Existing lease-related routes and response shapes are preserved.
```

Do not start Management/Onboarding.

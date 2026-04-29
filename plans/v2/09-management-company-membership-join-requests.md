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

# Slice 7: Management Company / Membership / Join Requests

## Goal

Refactor management company, company membership, company roles, and join request functionality while preserving current behavior.

This slice must account for `ManagementCompanyJoinRequestStatus` being a DB-backed lookup entity.

This slice should replace the old singular `App.BLL/ManagementCompany/**` implementation with the new plural `App.BLL/ManagementCompanies/**` architecture.

## Precondition

Plan 08 Lease Assignments is complete.

Do not modify lease assignment, resident workspace, unit workspace, or lease-related controllers in this slice unless required only for compilation.

Before starting, verify the solution does not have active legacy references from previous slices, especially:

```text
App.BLL.LeaseAssignments
App.BLL.ResidentWorkspace
App.BLL.UnitWorkspace
ResidentDashboardContext
UnitDashboardContext
```

If any of those remain, fix them as Plan 08 cleanup before starting Plan 09.

## Scope

Refactor existing functionality only:

- Management company profile.
- Management company dashboard/context if it depends on old management BLL services.
- Company membership/users.
- Company roles.
- Join request listing/review/approval/rejection if currently implemented.
- Management company access checks.
- Management API and MVC controllers related to profile, dashboard, users/membership, roles, and join requests.

Do not add new membership features or new join request UI/API behavior.

Do not fully refactor onboarding in this slice. Full onboarding cleanup remains Plan 10.

## Existing management infrastructure note

Some management-company DAL/BLL pieces already exist from earlier slices.

Do not create duplicate files/classes with the same responsibility. Extend or refactor existing files where appropriate.

Inspect and reuse/extend existing files such as:

```text
App.Contracts/DAL/ManagementCompanies/IManagementCompanyRepository.cs
App.Contracts/DAL/ManagementCompanies/ManagementCompanyDalDto.cs
App.DAL.EF/Repositories/ManagementCompanyRepository.cs
App.BLL.Contracts/ManagementCompanies/ManagementCompanyJoinRequestStatusCodes.cs
```

Create new files only for missing responsibilities, such as:

```text
App.Contracts/DAL/ManagementCompanies/IManagementCompanyJoinRequestRepository.cs
App.Contracts/DAL/ManagementCompanies/ManagementCompanyJoinRequestDalDto.cs
App.Contracts/DAL/ManagementCompanies/ManagementCompanyJoinRequestCreateDalDto.cs
App.DAL.EF/Repositories/ManagementCompanyJoinRequestRepository.cs
App.DAL.EF/Mappers/ManagementCompanies/ManagementCompanyJoinRequestDalMapper.cs
App.BLL.Contracts/ManagementCompanies/** missing contracts/models
App.BLL/ManagementCompanies/** missing service implementations
WebApp/Mappers/Api/ManagementCompanies/** missing WebApp mappers
```

## Old ManagementCompany replacement rule

Current old management BLL code may exist under the singular folder:

```text
App.BLL/ManagementCompany/Access
App.BLL/ManagementCompany/Membership
App.BLL/ManagementCompany/Profiles
```

During this slice, migrate currently implemented behavior to the new structure:

```text
App.BLL.Contracts/ManagementCompanies/**
App.BLL/ManagementCompanies/**
App.BLL/Mappers/ManagementCompanies/**
```

After the new services are wired, old `App.BLL.ManagementCompany` services should be deleted, moved, or excluded from compilation if they still depend on `App.DAL.EF` or `AppDbContext`.

Plan 09 is not complete until management BLL services no longer reference:

```text
App.DAL.EF
AppDbContext
old App.BLL.ManagementCompany service contracts that bypass App.BLL.Contracts
```

Do not create temporary compatibility shims as the final solution.

## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

Management repositories should follow the same established pattern as the already refactored customer/property/unit/resident/lease repositories:

```csharp
public interface IManagementCompanyRepository : IBaseRepository<ManagementCompanyDalDto>
{
    // Custom management-company queries only.
}
```

```csharp
public interface IManagementCompanyJoinRequestRepository : IBaseRepository<ManagementCompanyJoinRequestDalDto>
{
    // Custom join-request queries/workflow methods only.
}
```

Implementation shape:

```csharp
public sealed class ManagementCompanyRepository
    : BaseRepository<ManagementCompanyDalDto, ManagementCompany, AppDbContext>, IManagementCompanyRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ManagementCompanyDalMapper _mapper;

    public ManagementCompanyRepository(
        AppDbContext dbContext,
        ManagementCompanyDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    // Custom management-company queries here.
}
```

```csharp
public sealed class ManagementCompanyJoinRequestRepository
    : BaseRepository<ManagementCompanyJoinRequestDalDto, ManagementCompanyJoinRequest, AppDbContext>, IManagementCompanyJoinRequestRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ManagementCompanyJoinRequestDalMapper _mapper;

    public ManagementCompanyJoinRequestRepository(
        AppDbContext dbContext,
        ManagementCompanyJoinRequestDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    // Custom join-request queries/workflows here.
}
```

Adjust generic type parameters to match the actual current `BaseRepository`/`IBaseRepository` signatures in the codebase.

## Files to inspect first

- Current BLL services under:
  - `App.BLL/ManagementCompany/**`
  - `App.BLL/ManagementCompanies/**`, if already partially created
  - `App.BLL/Onboarding/**`, only where join requests are shared or must compile
- New/existing BLL contracts under:
  - `App.BLL.Contracts/ManagementCompanies/**`
- Management-related API controllers:
  - `WebApp/ApiControllers/Management/**`
  - Pay special attention to profile, membership/users, roles, and join-request endpoints if present.
- Management MVC controllers/views/ViewModels:
  - `WebApp/Areas/Management/Controllers/ProfileController.cs`, if present
  - `WebApp/Areas/Management/Controllers/UsersController.cs`, if present
  - `WebApp/Areas/Management/Controllers/DashboardController.cs`, if it uses old management services
  - Management join-request controllers/pages if present
- Do not refactor `Management/CustomersController` or `Management/ResidentsController` unless they still reference old management BLL services or require small compile fixes.
- Current management DTOs in `App.DTO`.
- Current management ViewModels in `WebApp`.
- `App.Domain/**/ManagementCompany.cs`.
- `App.Domain/**/ManagementCompanyUser.cs`.
- `App.Domain/**/ManagementCompanyRole.cs`.
- `App.Domain/**/ManagementCompanyJoinRequest.cs`.
- `App.Domain/**/ManagementCompanyJoinRequestStatus.cs`.
- `App.Domain/**/Identity/AppUser.cs`.
- `App.DAL.EF/AppDbContext.cs`.
- `App.DAL.EF/Seeding/**`.
- Existing lookup repository/status code constants.
- `IAppUOW` / `AppUOW`.
- `WebApp/Helpers/DependencyInjectionHelpers.cs`.

## Allowed files/folders to create or modify

- `App.Contracts/DAL/ManagementCompanies/**`
- `App.DAL.EF/Repositories/ManagementCompanyRepository.cs`
- `App.DAL.EF/Repositories/ManagementCompanyJoinRequestRepository.cs`
- `App.DAL.EF/Mappers/ManagementCompanies/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/ManagementCompanies/**`
- `App.BLL/ManagementCompanies/**`
- `App.BLL/Mappers/ManagementCompanies/**`
- `App.BLL/ManagementCompany/**` only to migrate, delete, move, or exclude old code after replacement
- `WebApp/Mappers/Api/ManagementCompanies/**`
- `WebApp/Mappers/Mvc/ManagementCompanies/**`, if MVC exists
- Management profile, dashboard, users/membership, roles, and join-request API/MVC controllers/ViewModels
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify onboarding except shared interface adjustments required for compilation. Full onboarding refactor is the next slice.

Do not modify customer, resident, unit, lease, ticket, vendor, or work functionality except for small compile fixes caused directly by this slice.

## Controller scope rule

This slice is **not** a general refactor of every file under `WebApp/ApiControllers/Management` or `WebApp/Areas/Management`.

Refactor only management company profile, dashboard, users/membership, roles, and join-request review flows.

Primary MVC/API targets:

```text
WebApp/Areas/Management/Controllers/ProfileController.cs
WebApp/Areas/Management/Controllers/UsersController.cs
WebApp/Areas/Management/Controllers/DashboardController.cs, if it uses old management services
management join-request endpoints/controllers, if present
management profile/membership API controllers, if present
```

Do not refactor these unless they still reference old management BLL services or require small compile fixes:

```text
WebApp/ApiControllers/Management/CustomersController.cs
WebApp/ApiControllers/Management/ResidentsController.cs
WebApp/Areas/Management/Controllers/CustomersController.cs
WebApp/Areas/Management/Controllers/ResidentsController.cs
```

## Onboarding boundary rule

Join-request repository and status-code handling may be introduced in this slice because management review/approval depends on it.

Do not fully refactor onboarding controllers/services in Plan 09.

If onboarding join-request creation must compile against new shared contracts, make the minimum compatibility adjustment only. Full onboarding cleanup remains Plan 10.

Do not redesign onboarding redirects, workspace selection, or account onboarding state here.

## Current behavior inventory

Before editing, identify and preserve:

- Management API routes.
- Management MVC routes, if present.
- Existing request/response DTOs.
- Existing ViewModels.
- Current company access logic.
- Current company dashboard behavior.
- Current company membership/users behavior.
- Current company role behavior.
- Current join request behavior.
- Current status values/codes for join requests.
- Existing approval/rejection behavior.
- Existing not-found/forbidden/conflict/validation behavior.
- Existing transaction behavior for approve/reject/profile updates, if any.

Preserve all behavior. The refactor should only change internal architecture.

## DAL contracts

Create or extend:

```text
App.Contracts/DAL/ManagementCompanies/
  IManagementCompanyRepository.cs
  IManagementCompanyJoinRequestRepository.cs
  ManagementCompanyDalDto.cs
  ManagementCompanyProfileDalDto.cs
  ManagementCompanyUserDalDto.cs
  ManagementCompanyJoinRequestDalDto.cs
  ManagementCompanyJoinRequestCreateDalDto.cs
```

Use generic `LookupDalDto` for `ManagementCompanyJoinRequestStatus` unless current behavior requires a specific DTO.

Do not duplicate existing management-company repository or DTO files. Extend current files where they already exist.

## Management company repository

Use `IBaseRepository<ManagementCompanyDalDto>` if the current base interface signature supports it.

Suggested methods:

```csharp
public interface IManagementCompanyRepository : IBaseRepository<ManagementCompanyDalDto>
{
    Task<ManagementCompanyDalDto?> FirstBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyProfileDalDto?> FirstProfileBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<bool> UserBelongsToCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> UserHasCompanyRoleAsync(
        Guid appUserId,
        Guid managementCompanyId,
        string roleCode,
        CancellationToken cancellationToken = default);

    Task<string?> FindActiveUserRoleCodeAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyUserDalDto>> MembersByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(
        ManagementCompanyProfileDalDto dto,
        CancellationToken cancellationToken = default);
}
```

Adjust method names and signatures to match the current domain and already implemented repository methods.

## Join request repository

Use `IBaseRepository<ManagementCompanyJoinRequestDalDto>` if the current base interface signature supports it.

Suggested methods:

```csharp
public interface IManagementCompanyJoinRequestRepository : IBaseRepository<ManagementCompanyJoinRequestDalDto>
{
    Task<IReadOnlyList<ManagementCompanyJoinRequestDalDto>> PendingByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyJoinRequestDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyJoinRequestDalDto?> FindByIdAndCompanyAsync(
        Guid requestId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingRequestAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyJoinRequestDalDto> AddAsync(
        ManagementCompanyJoinRequestCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(
        Guid requestId,
        string statusCode,
        Guid resolvedByAppUserId,
        DateTime resolvedAt,
        CancellationToken cancellationToken = default);
}
```

Rules:

- Do not hardcode seeded GUIDs.
- Use status code lookup, either through `ILookupRepository` or query joins.
- Prefer status codes for business decisions and status labels for display.
- Only `PENDING` requests can be approved/rejected.
- Return DAL DTOs, booleans, lists, nullable DTOs, or throw unexpected infrastructure/programming exceptions.
- Do not return FluentResults from repositories.

## DAL implementation

Create or refactor:

```text
App.DAL.EF/Repositories/ManagementCompanyRepository.cs
App.DAL.EF/Repositories/ManagementCompanyJoinRequestRepository.cs
App.DAL.EF/Mappers/ManagementCompanies/ManagementCompanyDalMapper.cs
App.DAL.EF/Mappers/ManagementCompanies/ManagementCompanyJoinRequestDalMapper.cs
```

Rules:

- Repositories should inherit from `BaseRepository` where possible.
- Repositories must use DAL mappers for domain/DAL DTO mapping.
- Return DAL DTOs only.
- Do not use FluentResults.
- Do not expose domain entities.
- Do not expose EF transaction types.
- Do not call `SaveChangesAsync` inside repositories unless the existing base pattern requires it.
- Query `ManagementCompanyJoinRequestStatus` by code or via lookup repository.
- Include `StatusId`, `StatusCode`, and `StatusLabel` in join request DTOs when currently displayed/needed.

## UOW changes

Ensure `IAppUOW` exposes:

```csharp
IManagementCompanyRepository ManagementCompanies { get; }
IManagementCompanyJoinRequestRepository ManagementCompanyJoinRequests { get; }
```

Implement missing repositories in `AppUOW` using the same lazy repository pattern as the already refactored repositories.

Do not duplicate existing `ManagementCompanies` registration if it already exists.

## BLL contracts

Create or extend:

```text
App.BLL.Contracts/ManagementCompanies/
  Services/
    IManagementCompanyProfileService.cs
    ICompanyMembershipService.cs
    ICompanyJoinRequestService.cs
    IManagementCompanyAccessService.cs
  Queries/
    GetManagementCompanyProfileQuery.cs
    GetCompanyMembersQuery.cs
    GetJoinRequestsQuery.cs
  Commands/
    UpdateManagementCompanyProfileCommand.cs
    CreateJoinRequestCommand.cs
    ApproveJoinRequestCommand.cs
    RejectJoinRequestCommand.cs
  Models/
    ManagementCompanyProfileModel.cs
    ManagementCompanyUserModel.cs
    ManagementCompanyJoinRequestModel.cs
  ManagementCompanyJoinRequestStatusCodes.cs
```

Do not duplicate `ManagementCompanyJoinRequestStatusCodes.cs` if it already exists. Extend or reuse it.

Status code constants:

```csharp
public static class ManagementCompanyJoinRequestStatusCodes
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}
```

Only create commands/queries/models that are needed by currently implemented endpoints/pages.

## BLL implementation

Create or refactor:

```text
App.BLL/ManagementCompanies/ManagementCompanyProfileService.cs
App.BLL/ManagementCompanies/CompanyMembershipService.cs
App.BLL/ManagementCompanies/CompanyJoinRequestService.cs
App.BLL/ManagementCompanies/ManagementCompanyAccessService.cs
App.BLL/Mappers/ManagementCompanies/ManagementCompanyBllMapper.cs
App.BLL/Mappers/ManagementCompanies/ManagementCompanyJoinRequestBllMapper.cs
```

Rules:

- Use `IAppUOW`.
- Do not use `AppDbContext`.
- Do not reference `App.DAL.EF`.
- Do not reference old singular `App.BLL.ManagementCompany` service contracts from new services.
- Use status code constants, not GUIDs.
- Return `ConflictError` for duplicate/pending request conflicts if current behavior does.
- Return `BusinessRuleError` or `ConflictError` for invalid status transition according to current behavior.
- Return `ForbiddenError` for permission failures.
- Call `_uow.SaveChangesAsync` once per successful command.
- Use UOW transaction methods for approve/reject if membership and request status are updated together.
- Preserve existing role checks and access rules.

## WebApp mappers

Create WebApp mappers only for existing endpoints/pages:

```text
WebApp/Mappers/Api/ManagementCompanies/ManagementCompanyApiMapper.cs
WebApp/Mappers/Api/ManagementCompanies/ManagementCompanyJoinRequestApiMapper.cs
WebApp/Mappers/Mvc/ManagementCompanies/ManagementCompanyViewModelMapper.cs, if MVC management pages need it
WebApp/Mappers/Mvc/ManagementCompanies/ManagementCompanyJoinRequestViewModelMapper.cs, if MVC join-request pages need it
```

Mapper responsibilities:

- Route/user -> management queries/commands.
- API request DTOs -> management commands.
- BLL models -> existing API response DTOs.
- BLL models -> existing MVC ViewModels.

## Controllers

Refactor management-related API/MVC controllers in scope.

Rules:

- Preserve routes/auth.
- Controller maps request/route/user to command/query.
- Controller calls one BLL service method where possible.
- Controller maps result to response/ViewModel.
- Controller uses `ToActionResult` for APIs.
- Controller does not query status entities or membership directly.
- Controller does not use repositories.
- Controller does not use `AppDbContext`.
- Do not refactor management customer/resident listing controllers unless required only for compilation.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register management BLL services from `App.BLL/ManagementCompanies`.
- Register management WebApp mappers.
- Remove old `App.BLL.ManagementCompany` service registrations after replacement.
- Do not remove registrations still needed by onboarding until Plan 10 unless they have been replaced safely.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when management company/membership/join request functionality is refactored and builds.

This slice is not complete until:

```text
No management BLL service references App.DAL.EF.
No management BLL service references AppDbContext.
No new service references old App.BLL.ManagementCompany contracts.
No old App.BLL/ManagementCompany files remain compiled against AppDbContext/App.DAL.EF.
ManagementCompanyJoinRequestStatus is handled by code/lookup, not hardcoded seeded GUIDs.
Existing management profile/membership/join-request routes and response shapes are preserved.
Management customer/resident listing flows are not unnecessarily refactored.
```

Do not start Onboarding.

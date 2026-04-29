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

Refactor management company, company membership, and join request functionality while preserving current behavior.

This slice must account for `ManagementCompanyJoinRequestStatus` being a DB-backed lookup entity.

## Scope

Refactor existing functionality only:

- Management company profile.
- Company membership.
- Company roles.
- Join request creation/approval/rejection if currently implemented.
- Management company access checks.
- Management API and MVC controllers.

Do not add new membership features or new join request UI/API behavior.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/ApiControllers/Management/**`
- Management MVC controllers/views/ViewModels, if present
- Current management DTOs in `App.DTO`
- Current BLL services under:
  - `App.BLL/ManagementCompany/**`
  - `App.BLL/Onboarding/**`, if join requests are shared
- `App.Domain/**/ManagementCompany.cs`
- `App.Domain/**/ManagementCompanyUser.cs`
- `App.Domain/**/ManagementCompanyRole.cs`
- `App.Domain/**/ManagementCompanyJoinRequest.cs`
- `App.Domain/**/ManagementCompanyJoinRequestStatus.cs`
- `App.Domain/**/Identity/AppUser.cs`
- `App.DAL.EF/AppDbContext.cs`
- `App.DAL.EF/Seeding/**`
- Existing lookup repository/status code constants
- `IAppUOW` / `AppUOW`

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
- `WebApp/Mappers/Api/ManagementCompanies/**`
- `WebApp/Mappers/Mvc/ManagementCompanies/**`, if MVC exists
- `WebApp/ApiControllers/Management/**`
- Management MVC controllers/ViewModels, if present
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify onboarding except shared interface adjustments required for compilation. Full onboarding refactor is the next slice.

## Current behavior inventory

Before editing, identify:

- Management API routes.
- Management MVC routes, if present.
- Existing request/response DTOs.
- Existing ViewModels.
- Current company access logic.
- Current company membership behavior.
- Current join request behavior.
- Current status values/codes for join requests.
- Existing approval/rejection behavior.
- Existing not-found/forbidden/conflict/validation behavior.

Preserve all behavior.

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

## Management company repository

Suggested methods:

```csharp
public interface IManagementCompanyRepository
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

    Task<IReadOnlyList<ManagementCompanyUserDalDto>> MembersByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(
        ManagementCompanyProfileDalDto dto,
        CancellationToken cancellationToken = default);
}
```

Adjust to current role/domain model.

## Join request repository

Suggested methods:

```csharp
public interface IManagementCompanyJoinRequestRepository
{
    Task<IReadOnlyList<ManagementCompanyJoinRequestDalDto>> PendingByCompanyAsync(
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

## DAL implementation

Create or refactor:

```text
App.DAL.EF/Repositories/ManagementCompanyRepository.cs
App.DAL.EF/Repositories/ManagementCompanyJoinRequestRepository.cs
App.DAL.EF/Mappers/ManagementCompanies/ManagementCompanyDalMapper.cs
App.DAL.EF/Mappers/ManagementCompanies/ManagementCompanyJoinRequestDalMapper.cs
```

Rules:

- Return DAL DTOs only.
- Do not use FluentResults.
- Do not expose domain entities.
- Do not expose EF transaction types.
- Query `ManagementCompanyJoinRequestStatus` by code or via lookup repository.
- Include `StatusId`, `StatusCode`, and `StatusLabel` in join request DTOs when currently displayed/needed.

## UOW changes

Add:

```csharp
IManagementCompanyRepository ManagementCompanies { get; }
IManagementCompanyJoinRequestRepository ManagementCompanyJoinRequests { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create:

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

Status code constants:

```csharp
public static class ManagementCompanyJoinRequestStatusCodes
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}
```

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
- Use status code constants, not GUIDs.
- Return `ConflictError` for duplicate/pending request conflicts if current behavior does.
- Return `BusinessRuleError` or `ConflictError` for invalid status transition according to current behavior.
- Return `ForbiddenError` for permission failures.
- Call `_uow.SaveChangesAsync` once per successful command.
- Use UOW transaction methods for approve/reject if membership and request status are updated together.

## WebApp mappers

Create:

```text
WebApp/Mappers/Api/ManagementCompanies/ManagementCompanyApiMapper.cs
WebApp/Mappers/Api/ManagementCompanies/ManagementCompanyJoinRequestApiMapper.cs
```

MVC mappers only if MVC management pages exist.

## Controllers

Refactor:

```text
WebApp/ApiControllers/Management/*
```

Rules:

- Preserve routes/auth.
- Controller maps request/route/user to command/query.
- Controller calls BLL service.
- Controller uses `ToActionResult`.
- Controller does not query status entities or membership directly.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register management BLL services.
- Register management WebApp mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when management company/membership/join request functionality is refactored and builds.

Do not start Onboarding.

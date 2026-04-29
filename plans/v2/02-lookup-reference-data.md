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

# Lookup Reference Data Slice

## Goal

Introduce the generic lookup repository/service pattern for lookup/reference entities already used by the implemented application.

This slice exists because `ManagementCompanyJoinRequestStatus` is now a database-backed lookup entity and join-request/onboarding flows need stable lookup-by-code behavior.

Do not add new lookup APIs or UI features unless such endpoints already exist and need only internal refactoring.

## Scope

Implement or prepare lookup access for currently active lookup/reference data:

- `ContactType`
- `CustomerRepresentativeRole`
- `LeaseRole`
- `ManagementCompanyRole`
- `ManagementCompanyJoinRequestStatus`
- `PropertyType`

Other lookup entities may use the same pattern only if they already exist and are actively used by currently implemented functionality.

Do not add ticket/vendor/work lookup handling.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `App.Contracts/ILookUpEntity.cs`
- `App.Domain/**` lookup/reference entity files
- `App.Domain/**/ManagementCompanyJoinRequestStatus.cs`
- `App.Domain/**/ManagementCompanyJoinRequest.cs`
- `App.DAL.EF/AppDbContext.cs`
- `App.DAL.EF/Seeding/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- Existing services/controllers that read lookup data
- Management company join request and onboarding services/controllers

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Lookups/**`
- `App.Contracts/Common/**`, if lookup contracts are kept there
- `App.DAL.EF/Repositories/LookupRepository.cs`
- `App.DAL.EF/Mappers/LookupDalMapper.cs`
- `App.BLL.Contracts/Lookups/**`
- `App.BLL/Lookups/**`
- `App.BLL/Mappers/Lookups/**`
- `WebApp/Mappers/Api/Lookups/**`, only if an existing API response needs it
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify unrelated feature controllers.

## DAL DTOs

Create:

```text
App.Contracts/DAL/Lookups/LookupDalDto.cs
```

Shape:

```csharp
public sealed class LookupDalDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
```

For `ManagementCompanyJoinRequestStatus`, generic `LookupDalDto` is enough unless existing UI/API needs additional fields.

## DAL repository contract

Create:

```text
App.Contracts/DAL/Lookups/ILookupRepository.cs
```

Recommended shape:

```csharp
public interface ILookupRepository
{
    Task<LookupDalDto?> FindManagementCompanyJoinRequestStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyJoinRequestStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindManagementCompanyRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindLeaseRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindPropertyTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindContactTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);
}
```

Alternative acceptable shape:

```csharp
Task<LookupDalDto?> FindByCodeAsync<TLookup>(
    string code,
    CancellationToken cancellationToken = default)
    where TLookup : class, ILookUpEntity;
```

Use the simpler implementation that fits the existing generic repository/base mapper setup.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/LookupRepository.cs
App.DAL.EF/Mappers/LookupDalMapper.cs
```

Repository rules:

- Return `LookupDalDto?` for lookup-by-code.
- Return empty list for list methods when no rows exist.
- Do not return FluentResults.
- Do not hardcode seeded GUIDs.
- Use stable codes such as `PENDING`, `APPROVED`, and `REJECTED`.

## UOW registration

Add to `IAppUOW`:

```csharp
ILookupRepository Lookups { get; }
```

Add lazy or constructor-backed implementation to `AppUOW`.

## BLL constants for join request status codes

Create:

```text
App.BLL.Contracts/ManagementCompanies/ManagementCompanyJoinRequestStatusCodes.cs
```

or:

```text
App.BLL.Contracts/Common/LookupCodes.cs
```

Shape:

```csharp
public static class ManagementCompanyJoinRequestStatusCodes
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}
```

Use these constants in BLL. Do not hardcode seeded GUIDs.

## Optional BLL lookup service

Create only if current controllers/services need lookup lists directly:

```text
App.BLL.Contracts/Lookups/Services/ILookupService.cs
App.BLL.Contracts/Lookups/Models/LookupModel.cs
App.BLL/Lookups/LookupService.cs
App.BLL/Mappers/Lookups/LookupBllMapper.cs
```

Service shape:

```csharp
public interface ILookupService
{
    Task<Result<IReadOnlyList<LookupModel>>> GetManagementCompanyJoinRequestStatusesAsync(
        CancellationToken cancellationToken = default);
}
```

If no current controller needs a generic lookup service, skip the BLL service and only implement DAL lookup access for feature services.

## WebApp changes

Register `ILookupRepository` through `AddAppDalEf()` in `WebApp/Helpers/DependencyInjectionHelpers.cs` if repositories are registered individually.

If repositories are exposed only through `IAppUOW`, no separate registration is needed.

Do not add new lookup endpoints unless such endpoints already exist and this slice is refactoring them.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when lookup access is available through UOW and status code constants exist for BLL use.

Do not refactor management join request or onboarding behavior in this slice unless absolutely needed for compilation.

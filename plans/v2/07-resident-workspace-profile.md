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

# Slice 5: Resident Workspace / Resident Profile

## Goal

Refactor resident workspace/profile functionality while preserving current behavior.

## Scope

Refactor only currently implemented resident functionality:

- Resident profile read/update/delete if implemented.
- Resident list/workspace.
- Resident contacts if currently implemented.
- Resident-user links if currently implemented.
- Read-only resident lease summaries if currently displayed.
- Resident API and MVC controllers.

Do not introduce new resident features.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/ApiControllers/Resident/**`
- Resident MVC controllers/views/ViewModels, if present
- Current resident DTOs in `App.DTO`
- Current BLL services under:
  - `App.BLL/ResidentWorkspace/**`
- Related services under `App.BLL/UnitWorkspace/**` or `LeaseAssignments/**`, if resident summaries depend on them
- `App.Domain/**/Resident.cs`
- `App.Domain/**/ResidentContact.cs`
- `App.Domain/**/ResidentUser.cs`
- `App.Domain/**/Contact.cs`
- `App.Domain/**/Lease.cs`
- `App.DAL.EF/AppDbContext.cs`
- Existing customer/property/unit repositories/services
- `IAppUOW` / `AppUOW`

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Residents/**`
- `App.Contracts/DAL/Contacts/**`
- `App.DAL.EF/Repositories/ResidentRepository.cs`
- `App.DAL.EF/Repositories/ContactRepository.cs`
- `App.DAL.EF/Mappers/Residents/**`
- `App.DAL.EF/Mappers/Contacts/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Residents/**`
- `App.BLL/Residents/**`
- `App.BLL/Mappers/Residents/**`
- `WebApp/Mappers/Api/Residents/**`
- `WebApp/Mappers/Mvc/Residents/**`, if MVC exists
- `WebApp/ApiControllers/Resident/**`
- Resident MVC controllers/ViewModels, if present
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify lease assignment behavior except read-only data needed for current resident displays.

## Current behavior inventory

Before editing, identify:

- Resident API routes.
- Resident MVC routes, if present.
- Existing request/response DTOs.
- Existing ViewModels.
- Existing resident profile response shape.
- Existing resident contact behavior.
- Existing resident-user link behavior.
- Existing access rules.
- Existing not-found/forbidden/duplicate/validation behavior.

Preserve all behavior.

## DAL contracts

Create:

```text
App.Contracts/DAL/Residents/
  IResidentRepository.cs
  ResidentDalDto.cs
  ResidentProfileDalDto.cs
  ResidentListItemDalDto.cs
  ResidentCreateDalDto.cs
  ResidentUpdateDalDto.cs
  ResidentContactDalDto.cs
  ResidentLeaseSummaryDalDto.cs

App.Contracts/DAL/Contacts/
  IContactRepository.cs
  ContactDalDto.cs
  ContactCreateDalDto.cs
  ContactUpdateDalDto.cs
```

Optional repositories:

- `IResidentContactRepository`
- `IResidentUserRepository`

Create optional repositories only if current behavior has complex direct queries. Otherwise manage link entities through `IResidentRepository` and `IContactRepository`.

## Repository interface

Suggested `IResidentRepository`:

```csharp
public interface IResidentRepository
{
    Task<ResidentProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string customerSlug,
        string residentSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentListItemDalDto>> AllByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsForCustomerAsync(
        Guid customerId,
        string slug,
        Guid? exceptResidentId = null,
        CancellationToken cancellationToken = default);

    Task<ResidentDalDto> AddAsync(
        ResidentCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ResidentUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid residentId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentContactDalDto>> ContactsByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentLeaseSummaryDalDto>> LeaseSummariesByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);
}
```

Suggested `IContactRepository`:

```csharp
public interface IContactRepository
{
    Task<ContactDalDto?> FindAsync(
        Guid contactId,
        CancellationToken cancellationToken = default);

    Task<ContactDalDto> AddAsync(
        ContactCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ContactUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid contactId,
        CancellationToken cancellationToken = default);
}
```

Adjust to current domain model.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/ResidentRepository.cs
App.DAL.EF/Repositories/ContactRepository.cs
App.DAL.EF/Mappers/Residents/ResidentDalMapper.cs
App.DAL.EF/Mappers/Contacts/ContactDalMapper.cs
```

Rules:

- Scope resident queries to company/customer.
- Include contacts/lease summaries only when current response needs them.
- Do not use FluentResults.
- Do not return domain entities.
- Do not add new resident contact capabilities beyond current behavior.

## UOW changes

Add:

```csharp
IResidentRepository Residents { get; }
IContactRepository Contacts { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create:

```text
App.BLL.Contracts/Residents/
  Services/
    IResidentProfileService.cs
    IResidentWorkspaceService.cs
    IResidentAccessService.cs
  Queries/
    GetResidentProfileQuery.cs
    GetResidentsQuery.cs
  Commands/
    CreateResidentCommand.cs
    UpdateResidentProfileCommand.cs
    DeleteResidentCommand.cs
    AddResidentContactCommand.cs
    RemoveResidentContactCommand.cs
  Models/
    ResidentProfileModel.cs
    ResidentListItemModel.cs
    ResidentContactModel.cs
    ResidentLeaseSummaryModel.cs
```

Only create contact commands if current UI/API supports adding/removing resident contacts.

## BLL implementation

Create or refactor:

```text
App.BLL/Residents/ResidentProfileService.cs
App.BLL/Residents/ResidentWorkspaceService.cs
App.BLL/Residents/ResidentAccessService.cs
App.BLL/Mappers/Residents/ResidentBllMapper.cs
```

Rules:

- Use `IAppUOW`.
- Do not use `AppDbContext`.
- Keep access decisions in BLL.
- Convert missing entities to `NotFoundError`.
- Convert access failures to `ForbiddenError`.
- Convert duplicate resident slug/person identity conflicts to `ConflictError` if current behavior does.
- Call `_uow.SaveChangesAsync` once for successful create/update/delete.
- Use transactions only if current operation writes multiple related records.

## WebApp API mappers

Create:

```text
WebApp/Mappers/Api/Residents/ResidentApiMapper.cs
```

Responsibilities:

- Route/user -> queries.
- API request DTO -> commands.
- BLL models -> existing API response DTOs.

## Controllers

Refactor:

```text
WebApp/ApiControllers/Resident/*
```

Rules:

- Preserve routes/auth.
- Use BLL service + mapper + `ToActionResult`.
- No repositories.
- No `AppDbContext`.
- No manual business validation except MVC/API model binding validation already present.

## MVC controllers, if present

Refactor resident MVC controller/ViewModels using the same pattern.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register resident BLL services.
- Register resident WebApp mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when resident functionality is refactored and builds.

Do not start Lease Assignment slice.

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

# Slice 3: Property Profile / Property Workspace

## Goal

Refactor property profile and property workspace functionality while preserving current behavior.

## Scope

Refactor only:

- Property profile read/update/delete if implemented.
- Properties listed under a customer.
- Create property under a customer if implemented.
- Property dashboard/workspace context.
- Property access checks.
- API and MVC controllers serving property flows.

Do not refactor unit functionality yet, except where property endpoints currently list units and the behavior already exists. If unit logic is substantial, leave it for the Unit slice.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/ApiControllers/Property/**`
- Property MVC controllers/views/ViewModels, if present
- Current property DTOs in `App.DTO`
- Current BLL services under:
  - `App.BLL/PropertyWorkspace/**`
  - `App.BLL/CustomerWorkspace/**`, if it handles property list/create
- `App.Domain/**/Property.cs`
- `App.Domain/**/PropertyType.cs`
- `App.Domain/**/Customer.cs`
- `App.DAL.EF/AppDbContext.cs`
- Existing `ICustomerRepository`
- Existing `IManagementCompanyRepository`
- `IAppUOW` / `AppUOW`

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Properties/**`
- `App.DAL.EF/Repositories/PropertyRepository.cs`
- `App.DAL.EF/Mappers/Properties/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Properties/**`
- `App.BLL/Properties/**`
- `App.BLL/Mappers/Properties/**`
- `WebApp/Mappers/Api/Properties/**`
- `WebApp/Mappers/Mvc/Properties/**`, if MVC exists
- `WebApp/ApiControllers/Property/**`
- Property MVC controllers/ViewModels, if present
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify unit/resident/lease/management/onboarding behavior except for compilation.

## Current behavior inventory

Before editing, identify:

- Property API routes.
- Property MVC routes, if present.
- Existing request/response DTOs.
- Existing ViewModels.
- Existing property dashboard context shape.
- Existing property create/update validation.
- Existing not-found/forbidden/duplicate behavior.
- Existing slug generation/validation behavior.
- Existing property type lookup behavior.

Preserve all behavior.

## DAL contracts

Create:

```text
App.Contracts/DAL/Properties/
  IPropertyRepository.cs
  PropertyDalDto.cs
  PropertyProfileDalDto.cs
  PropertyListItemDalDto.cs
  PropertyCreateDalDto.cs
  PropertyUpdateDalDto.cs
```

Suggested DTOs:

```csharp
public sealed class PropertyDalDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public bool IsActive { get; init; }
}
```

```csharp
public sealed class PropertyProfileDalDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? Address { get; init; }
    public Guid? PropertyTypeId { get; init; }
    public string? PropertyTypeCode { get; init; }
    public string? PropertyTypeLabel { get; init; }
    public bool IsActive { get; init; }
}
```

Adjust fields to match current domain and responses.

## Repository interface

```csharp
public interface IPropertyRepository
{
    Task<PropertyProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyListItemDalDto>> AllByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsForCustomerAsync(
        Guid customerId,
        string slug,
        Guid? exceptPropertyId = null,
        CancellationToken cancellationToken = default);

    Task<PropertyDalDto> AddAsync(
        PropertyCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PropertyUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid propertyId,
        Guid customerId,
        CancellationToken cancellationToken = default);
}
```

Adjust for current delete behavior.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/PropertyRepository.cs
App.DAL.EF/Mappers/Properties/PropertyDalMapper.cs
```

Rules:

- Scope property queries by company/customer/property slugs.
- Include property type data only when needed for current responses.
- Return empty list for no properties.
- Do not return FluentResults.
- Do not call `SaveChangesAsync` inside repository unless required by existing base pattern.

## UOW changes

Add:

```csharp
IPropertyRepository Properties { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create:

```text
App.BLL.Contracts/Properties/
  Services/
    IPropertyProfileService.cs
    IPropertyWorkspaceService.cs
  Queries/
    GetPropertyProfileQuery.cs
    GetPropertyWorkspaceQuery.cs
  Commands/
    CreatePropertyCommand.cs
    UpdatePropertyProfileCommand.cs
    DeletePropertyCommand.cs
  Models/
    PropertyProfileModel.cs
    PropertyDashboardModel.cs
    PropertyListItemModel.cs
```

Service examples:

```csharp
public interface IPropertyProfileService
{
    Task<Result<PropertyProfileModel>> GetAsync(
        GetPropertyProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> UpdateAsync(
        UpdatePropertyProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeletePropertyCommand command,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IPropertyWorkspaceService
{
    Task<Result<PropertyDashboardModel>> GetDashboardAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PropertyListItemModel>>> GetCustomerPropertiesAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> CreateAsync(
        CreatePropertyCommand command,
        CancellationToken cancellationToken = default);
}
```

Split interfaces differently only if current functionality demands it.

## BLL implementation

Create or refactor:

```text
App.BLL/Properties/PropertyProfileService.cs
App.BLL/Properties/PropertyWorkspaceService.cs
App.BLL/Mappers/Properties/PropertyBllMapper.cs
```

Rules:

- Use `IAppUOW`.
- Do not use `AppDbContext`.
- Resolve customer/company access using existing BLL access services or repositories.
- Convert missing company/customer/property to `NotFoundError` where current behavior does.
- Convert access failures to `ForbiddenError`.
- Convert duplicate slugs/registry-type conflicts to `ConflictError` if current behavior does.
- Call `_uow.SaveChangesAsync` once per successful create/update/delete command.
- Use transactions only if multiple writes must be atomic.

## WebApp API mappers

Create:

```text
WebApp/Mappers/Api/Properties/PropertyApiMapper.cs
```

Responsibilities:

- Route/user -> property queries.
- API request DTO -> create/update commands.
- BLL model -> existing API response DTO.

## Controllers

Refactor:

```text
WebApp/ApiControllers/Property/*
```

Rules:

- Preserve route/auth attributes.
- Remove direct access-chain orchestration if BLL now handles it.
- Use service + mapper + `ToActionResult`.

## MVC controllers, if present

Refactor property MVC controller/ViewModels using the same pattern.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register property BLL services.
- Register WebApp property mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when property profile/workspace is refactored and builds.

Do not start Unit slice.

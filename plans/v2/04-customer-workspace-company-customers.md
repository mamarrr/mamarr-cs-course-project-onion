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

# Slice 2: Customer Workspace / Company Customers

## Goal

Refactor customer workspace and company customer list/create functionality while preserving existing behavior.

This slice follows Customer Profile and may reuse `ICustomerRepository`.

## Scope

Refactor only:

- Customer dashboard/workspace context.
- Customer list under a management company.
- Create customer under a company, if currently implemented.
- Customer access resolution used by customer workspace.
- API and MVC controllers serving these flows.

Do not refactor property profile/workspace yet.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/ApiControllers/Customer/CustomerDashboardController.cs`
- `WebApp/ApiControllers/Customer/CustomerPropertiesController.cs`, if it resolves customer workspace context
- Any MVC customer dashboard/list/create controllers/views
- Current BLL services under:
  - `App.BLL/CustomerWorkspace/Workspace/**`
  - `App.BLL/CustomerWorkspace/**`
  - `App.BLL/ManagementCompany/**`, if customer listing depends on company access
- Existing customer access service/interfaces
- Existing customer creation DTOs/ViewModels
- `App.Domain/**/Customer.cs`
- `App.Domain/**/ManagementCompany.cs`
- `App.DAL.EF/AppDbContext.cs`
- Existing `ICustomerRepository` from Customer Profile slice
- Existing `IAppUOW` and `AppUOW`

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Customers/**`
- `App.Contracts/DAL/ManagementCompanies/**`
- `App.DAL.EF/Repositories/CustomerRepository.cs`
- `App.DAL.EF/Repositories/ManagementCompanyRepository.cs`
- `App.DAL.EF/Mappers/Customers/**`
- `App.DAL.EF/Mappers/ManagementCompanies/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Customers/**`
- `App.BLL/Customers/**`
- `App.BLL/Mappers/Customers/**`
- `WebApp/Mappers/Api/Customers/**`
- `WebApp/Mappers/Mvc/Customers/**`, if MVC exists
- `WebApp/ApiControllers/Customer/**`, only controllers in this slice
- Related customer MVC controllers/ViewModels, if present
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify property/unit/resident/lease/management/onboarding behavior except shared access interfaces required for compilation.

## Current behavior inventory

Before editing, identify:

- Customer dashboard routes and response shape.
- Customer list routes and response shape.
- Customer creation route/request/response behavior.
- Existing customer access checks.
- How company slug is resolved.
- Existing duplicate slug/name/registry behavior.
- Existing redirect behavior for MVC, if applicable.

Preserve all behavior.

## DAL contracts

Extend or create:

```text
App.Contracts/DAL/Customers/
  CustomerListItemDalDto.cs
  CustomerCreateDalDto.cs
  CustomerWorkspaceDalDto.cs

App.Contracts/DAL/ManagementCompanies/
  IManagementCompanyRepository.cs
  ManagementCompanyDalDto.cs
```

Suggested customer DTOs:

```csharp
public sealed class CustomerListItemDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public bool IsActive { get; init; }
}
```

```csharp
public sealed class CustomerCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; } = true;
}
```

Suggested management company DTO:

```csharp
public sealed class ManagementCompanyDalDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public bool IsActive { get; init; }
}
```

## Repository methods

Add to `ICustomerRepository`:

```csharp
Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanySlugAsync(
    string companySlug,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanyIdAsync(
    Guid managementCompanyId,
    CancellationToken cancellationToken = default);

Task<CustomerDalDto> AddAsync(
    CustomerCreateDalDto dto,
    CancellationToken cancellationToken = default);

Task<bool> CustomerSlugExistsInCompanyAsync(
    Guid managementCompanyId,
    string slug,
    CancellationToken cancellationToken = default);
```

Create `IManagementCompanyRepository` if it does not already exist:

```csharp
public interface IManagementCompanyRepository
{
    Task<ManagementCompanyDalDto?> FirstBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<bool> UserBelongsToCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
```

Adjust method names to current domain/user membership structure.

## DAL implementation

Create or extend:

```text
App.DAL.EF/Repositories/CustomerRepository.cs
App.DAL.EF/Repositories/ManagementCompanyRepository.cs
App.DAL.EF/Mappers/Customers/CustomerDalMapper.cs
App.DAL.EF/Mappers/ManagementCompanies/ManagementCompanyDalMapper.cs
```

Rules:

- Preserve tenant scoping.
- Return empty lists when no customers exist.
- Do not return domain entities.
- Do not use FluentResults.

## UOW changes

Add to `IAppUOW` if missing:

```csharp
ICustomerRepository Customers { get; }
IManagementCompanyRepository ManagementCompanies { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create or extend:

```text
App.BLL.Contracts/Customers/
  Services/
    ICustomerAccessService.cs
    ICompanyCustomerService.cs
    ICustomerWorkspaceService.cs
  Queries/
    GetCustomerWorkspaceQuery.cs
    GetCompanyCustomersQuery.cs
  Commands/
    CreateCustomerCommand.cs
  Models/
    CustomerWorkspaceModel.cs
    CustomerListItemModel.cs
    CompanyCustomerModel.cs
```

Recommended service split:

```csharp
public interface ICustomerAccessService
{
    Task<Result<CustomerWorkspaceModel>> ResolveCustomerWorkspaceAsync(
        GetCustomerWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface ICompanyCustomerService
{
    Task<Result<IReadOnlyList<CustomerListItemModel>>> GetCompanyCustomersAsync(
        GetCompanyCustomersQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyCustomerModel>> CreateCustomerAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default);
}
```

`ICustomerWorkspaceService` can focus on dashboard/workspace data if current code distinguishes it from access.

## BLL implementation

Create or refactor:

```text
App.BLL/Customers/CustomerAccessService.cs
App.BLL/Customers/CompanyCustomerService.cs
App.BLL/Customers/CustomerWorkspaceService.cs
App.BLL/Mappers/Customers/CustomerWorkspaceBllMapper.cs
```

Rules:

- Move current `AppDbContext` queries into repositories.
- Keep access decisions in BLL.
- Use `Result.Fail(new NotFoundError(...))` when company/customer context is missing.
- Use `Result.Fail(new ForbiddenError(...))` when user is not allowed.
- Use `Result.Fail(new ConflictError(...))` for duplicate customer slug/registry code if current behavior treats it as conflict.
- Call `_uow.SaveChangesAsync` once for successful customer creation.
- Use transactions only if the current create workflow writes multiple related entities atomically.

## WebApp API mappers

Create or extend:

```text
WebApp/Mappers/Api/Customers/CustomerWorkspaceApiMapper.cs
WebApp/Mappers/Api/Customers/CompanyCustomerApiMapper.cs
```

Responsibilities:

- Route/user -> BLL queries.
- API create request -> `CreateCustomerCommand`.
- BLL models -> existing API response DTOs.

## Controllers

Refactor only customer workspace/company customer controllers.

Target:

- Controller calls one BLL service method per endpoint.
- Controller maps request/route/user to BLL command/query.
- Controller maps BLL model to API DTO/ViewModel.
- Controller uses `ToActionResult`.
- Controller does not resolve company/customer access chains manually.

Keep route/auth attributes unchanged.

## MVC controllers, if present

- Map ViewModel -> command.
- Map BLL model -> ViewModel.
- Convert FluentResults errors to current MVC behavior.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register new BLL services.
- Register WebApp mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when customer workspace/company customer functionality is refactored and builds.

Do not start Property slice.

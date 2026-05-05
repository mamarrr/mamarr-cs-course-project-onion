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

# Slice 1: Customer Profile

## Goal

Refactor the Customer Profile vertical slice across DAL, BLL, and WebApp while preserving current behavior.

This slice should prove the architecture with a focused read/update/delete workflow.

## Scope

Refactor only customer profile functionality:

- Customer profile read.
- Customer profile update.
- Customer profile delete, if currently implemented.
- Duplicate registry code handling.
- Access checks already used by customer profile flow.
- API controller and regular MVC controller only if they currently serve customer profile behavior.

Do not refactor customer workspace/list/create functionality in this slice. That belongs to the next slice.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/ApiControllers/Customer/CustomerProfileController.cs`
- Any MVC customer profile controller/view files, if present
- Current customer profile request/response DTOs in `App.DTO`
- Current customer profile ViewModels in `WebApp`
- Current BLL customer profile service and interface, likely under:
  - `App.BLL/CustomerWorkspace/Profiles/**`
  - or similarly named customer profile folders
- Current customer access service/interface
- `App.Domain/**/Customer.cs`
- Related domain entities used by customer deletion/update
- `App.DAL.EF/AppDbContext.cs`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- Existing mapper/base repository classes

## Allowed files/folders to create or modify

- `App.Contracts/DAL/Customers/**`
- `App.DAL.EF/Repositories/CustomerRepository.cs`
- `App.DAL.EF/Mappers/Customers/**`
- `App.Contracts/IAppUow.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.BLL.Contracts/Customers/**`
- `App.BLL/Customers/CustomerProfileService.cs`
- `App.BLL/Mappers/Customers/**`
- Existing customer profile BLL files, only to migrate or remove after replacement
- `WebApp/Mappers/Api/Customers/**`
- `WebApp/Mappers/Mvc/Customers/**`, only if MVC profile exists
- `WebApp/ApiControllers/Customer/CustomerProfileController.cs`
- Customer profile MVC controller/ViewModel files, only if present
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Do not modify property/unit/resident/lease/management/onboarding functionality except shared access code necessary to compile.

## Current behavior inventory

Before editing, record in comments or notes for yourself:

- Customer profile routes.
- Authorization attributes.
- Request DTO fields.
- Response DTO fields.
- Existing success status codes.
- Existing not-found behavior.
- Existing forbidden behavior.
- Existing duplicate registry code behavior.
- Existing validation behavior.
- Existing delete behavior and cascade/manual delete behavior.

Preserve all of these unless a change is unavoidable and explicitly documented in code comments or commit notes.

## DAL contracts

Create:

```text
App.Contracts/DAL/Customers/
  ICustomerRepository.cs
  CustomerDalDto.cs
  CustomerProfileDalDto.cs
  CustomerUpdateDalDto.cs
```

Recommended DTOs:

```csharp
public sealed class CustomerDalDto
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
public sealed class CustomerProfileDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

```csharp
public sealed class CustomerUpdateDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

Repository interface:

```csharp
public interface ICustomerRepository
{
    Task<CustomerProfileDalDto?> FirstProfileByCompanyAndSlugAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<CustomerProfileDalDto?> FindProfileAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> RegistryCodeExistsInCompanyAsync(
        Guid managementCompanyId,
        string registryCode,
        Guid? exceptCustomerId = null,
        CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(
        CustomerUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
```

Adjust method names/fields to match current domain and behavior.

Repository methods should not return FluentResults.

## DAL implementation

Create:

```text
App.DAL.EF/Repositories/CustomerRepository.cs
App.DAL.EF/Mappers/Customers/CustomerDalMapper.cs
```

Repository rules:

- Include only navigation data required for the current customer profile response.
- Keep tenant/company scoping in repository queries.
- Return `null` or `false` for not found.
- Do not return domain entities to BLL.
- Do not call `SaveChangesAsync` inside repository methods unless the existing base repository pattern already does so. Prefer UOW save boundary.
- Do not introduce new cascade behavior.

## UOW changes

Add to `IAppUOW`:

```csharp
ICustomerRepository Customers { get; }
```

Implement in `AppUOW`.

## BLL contracts

Create:

```text
App.BLL.Contracts/Customers/
  Services/
    ICustomerProfileService.cs
  Queries/
    GetCustomerProfileQuery.cs
  Commands/
    UpdateCustomerProfileCommand.cs
    DeleteCustomerCommand.cs
  Models/
    CustomerProfileModel.cs
```

Service:

```csharp
using FluentResults;

public interface ICustomerProfileService
{
    Task<Result<CustomerProfileModel>> GetAsync(
        GetCustomerProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerProfileModel>> UpdateAsync(
        UpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeleteCustomerCommand command,
        CancellationToken cancellationToken = default);
}
```

Query:

```csharp
public sealed class GetCustomerProfileQuery
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }
}
```

Command:

```csharp
public sealed class UpdateCustomerProfileCommand
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }

    public string Name { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

Delete command:

```csharp
public sealed class DeleteCustomerCommand
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }
}
```

Model:

```csharp
public sealed class CustomerProfileModel
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

Adjust fields to match current DTOs/responses.

## BLL implementation

Create or replace:

```text
App.BLL/Customers/CustomerProfileService.cs
App.BLL/Mappers/Customers/CustomerProfileBllMapper.cs
```

Rules:

- Depend on `IAppUOW`, not `AppDbContext`.
- Depend on BLL contracts and DAL contracts.
- Use existing access service logic if available, but move it toward BLL contracts if needed.
- Return `Result.Fail(new NotFoundError(...))` for missing profile.
- Return `Result.Fail(new ForbiddenError(...))` for failed access checks.
- Return `Result.Fail(new ConflictError(...))` for duplicate registry code if that is current behavior.
- Return `Result.Fail(new ValidationAppError(...))` for BLL validation failures if current service has them.
- Call `_uow.SaveChangesAsync(cancellationToken)` once for successful update/delete command.
- Use `_uow.BeginTransactionAsync`, `CommitTransactionAsync`, and `RollbackTransactionAsync` only if the current update/delete operation touches multiple related objects and must be atomic.
- Do not start a transaction for simple read-only queries.

## WebApp API mapper

Create:

```text
WebApp/Mappers/Api/Customers/CustomerProfileApiMapper.cs
```

Responsibilities:

- API request DTO -> `UpdateCustomerProfileCommand`
- Route values + user principal -> `GetCustomerProfileQuery`
- Route values + user principal -> `DeleteCustomerCommand`
- `CustomerProfileModel` -> existing API response DTO

If the project has a current way to get user ID, reuse it.

## API controller refactor

Refactor:

```text
WebApp/ApiControllers/Customer/CustomerProfileController.cs
```

Target:

- Controller injects `ICustomerProfileService`.
- Controller injects WebApp API mapper.
- Controller does not inject `AppDbContext`.
- Controller does not inject repositories.
- Controller does not manually implement business validation.
- Controller delegates result conversion to `ToActionResult`.

Example:

```csharp
[HttpPut]
public async Task<ActionResult<CustomerProfileResponseDto>> UpdateProfile(
    string companySlug,
    string customerSlug,
    UpdateCustomerProfileRequestDto dto,
    CancellationToken cancellationToken)
{
    var command = _mapper.ToCommand(companySlug, customerSlug, dto, User);
    var result = await _customerProfileService.UpdateAsync(command, cancellationToken);

    return result.ToActionResult(_mapper.ToResponseDto);
}
```

Keep all route attributes and auth attributes unchanged.

## MVC controller refactor, only if present

If a regular MVC customer profile controller exists:

- Use BLL service.
- Map ViewModel -> BLL command.
- Map BLL model -> ViewModel.
- Convert FluentResults errors into `ModelState` or redirect behavior matching current behavior.
- Do not use `App.DTO` for MVC ViewModels.

## DI registration

In `WebApp/Helpers/DependencyInjectionHelpers.cs`, register:

- `ICustomerProfileService` -> `CustomerProfileService`
- `CustomerProfileApiMapper`
- MVC mapper if present

Repository registration may not be needed if repository is only reached through `IAppUOW`.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when Customer Profile uses the new DAL/BLL/WebApp pattern and the solution builds.

Do not start Customer Workspace or Company Customers in this slice.

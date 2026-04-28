# Mamarr CS Course Project Refactor Plan

## Purpose

This plan describes a controlled, vertical-slice refactor of the existing application architecture.

The goal is **not** to add new user-facing functionality. The goal is to improve the internal architecture while preserving the current behavior of the application.

The refactor will be done one vertical slice at a time across these layers:

1. DAL layer: repositories, Unit of Work, DAL DTOs, DAL mappers.
2. BLL layer: services, service contracts, BLL commands/queries/models, BLL mappers.
3. Web layer: regular MVC controllers, API controllers, API DTO mapping, MVC ViewModels.

Ticket, vendor, scheduled work, and work-log functionality are intentionally excluded from this plan because they are not fully implemented yet and this refactor should not introduce new features.

---

# 1. Confirmed Architecture Decisions

## Decision 1: Repositories return DAL DTOs

Repositories should return DAL DTOs, not domain entities.

```text
Repository -> DAL DTO -> BLL mapper -> BLL model -> Web mapper -> API DTO / ViewModel
```

This keeps the BLL independent from EF/domain persistence details.

## Decision 2: Transactions are exposed through Unit of Work

Transaction handling should live behind the Unit of Work abstraction.

BLL services can start and commit/rollback transactions through `IAppUOW`, but the EF-specific transaction implementation stays inside `App.DAL.EF`.

## Decision 3: BLL returns FluentResults

BLL services should return `FluentResults.Result` or `FluentResults.Result<T>`.

Expected business/application outcomes should be represented as result failures, not thrown exceptions.

Examples:

```text
Not found      -> Result.Fail(new NotFoundError(...))
Forbidden      -> Result.Fail(new ForbiddenError(...))
Conflict       -> Result.Fail(new ConflictError(...))
Validation     -> Result.Fail(new ValidationAppError(...))
Business rule  -> Result.Fail(new BusinessRuleError(...))
```

Unexpected bugs or infrastructure failures may still throw exceptions and be handled by global exception middleware.

## Decision 4: App.DTO is API-only

`App.DTO` should only contain API-facing DTOs.

BLL services should not accept or return `App.DTO` types. Instead, BLL uses its own commands, queries, and models from `App.BLL.Contracts`.

---

# 2. Target Project Dependency Direction

Final intended dependency direction:

```text
WebApp
  -> App.DTO
  -> App.BLL.Contracts
  -> App.BLL
  -> App.Contracts

App.BLL
  -> App.BLL.Contracts
  -> App.Contracts
  -> App.Resources, if needed

App.BLL.Contracts
  -> FluentResults
  -> Base.Contracts, if needed

App.DAL.EF
  -> App.Contracts
  -> App.Domain
  -> Base.DAL.EF

App.Contracts
  -> Base.DAL.Contracts
  -> Base.Contracts

App.DTO
  -> Base.Contracts, if needed

App.Domain
  -> Base.Domain
  -> Base.Contracts
```

## Dependency rules

```text
WebApp can reference BLL contracts and API DTOs.
WebApp should not use AppDbContext directly outside composition/startup/seeding.
Controllers should not use repositories directly.
BLL should not reference App.DAL.EF.
BLL should not use AppDbContext.
BLL should not use App.DTO.
DAL should not reference BLL.
DAL repositories should return DAL DTOs.
Domain entities should not be returned to BLL or controllers.
```

---

# 3. Final Layer Responsibilities

## 3.1 Domain layer

Owns:

```text
Entities
Entity relationships
Domain-level base interfaces
```

Does not own:

```text
API DTOs
BLL models
Repository DTOs
EF query logic
Service workflows
HTTP status decisions
```

## 3.2 DAL contracts layer: App.Contracts

Owns:

```text
Repository interfaces
Unit of Work interface
DAL DTOs
DAL create/update DTOs
Lookup repository contracts
Transaction abstraction
```

Example structure:

```text
App.Contracts/
  DAL/
    Customers/
      ICustomerRepository.cs
      CustomerDalDto.cs
      CustomerProfileDalDto.cs
      CustomerListItemDalDto.cs
      CustomerCreateDalDto.cs
      CustomerUpdateDalDto.cs
```

## 3.3 DAL implementation layer: App.DAL.EF

Owns:

```text
AppDbContext
EF repository implementations
Domain <-> DAL DTO mappers
EF transaction implementation
Migrations
Seeding
```

## 3.4 BLL contracts layer: App.BLL.Contracts

Owns:

```text
Service interfaces
BLL commands
BLL queries
BLL models
FluentResults error classes
Current-user/context abstractions, if needed by services/controllers
```

Example structure:

```text
App.BLL.Contracts/
  Customers/
    Services/
      ICustomerProfileService.cs
    Commands/
      UpdateCustomerProfileCommand.cs
      DeleteCustomerCommand.cs
    Queries/
      GetCustomerProfileQuery.cs
    Models/
      CustomerProfileModel.cs
```

## 3.5 BLL implementation layer: App.BLL

Owns:

```text
Service implementations
Business workflows
Access checks
Validation orchestration
DAL DTO -> BLL model mappers
BLL command -> DAL create/update DTO mappers
SaveChanges/transaction orchestration through UOW
```

## 3.6 API DTO layer: App.DTO

Owns:

```text
Versioned API request DTOs
Versioned API response DTOs
Swagger-facing contracts
```

`App.DTO` is API-only and should not be referenced from BLL.

## 3.7 WebApp layer

Owns:

```text
API controllers
MVC controllers
Razor ViewModels
API DTO <-> BLL command/model mappers
BLL model <-> MVC ViewModel mappers
HTTP result mapping from FluentResults
```

---

# 4. DTO Boundaries

Because DTOs are required between all layers, use three separate groups.

## 4.1 DAL DTOs

Location:

```text
App.Contracts/DAL/...
```

Used between:

```text
App.DAL.EF <-> App.BLL
```

Example:

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

DAL DTOs are persistence-facing. They can include IDs and fields needed by BLL, but they should not expose EF navigation graphs.

## 4.2 BLL commands, queries, and models

Location:

```text
App.BLL.Contracts/...
```

Used between:

```text
WebApp <-> App.BLL
```

Example query:

```csharp
public sealed class GetCustomerProfileQuery
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }
}
```

Example command:

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

Example model:

```csharp
public sealed class CustomerProfileModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

## 4.3 API DTOs

Location:

```text
App.DTO/v1/...
```

Used by:

```text
API controllers
Swagger
External clients
```

Example request DTO:

```csharp
public sealed class UpdateCustomerProfileRequestDto
{
    public string Name { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

Example response DTO:

```csharp
public sealed class CustomerProfileResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string? RegistryCode { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
```

## 4.4 MVC ViewModels

Location:

```text
WebApp/.../ViewModels/...
```

Used by:

```text
MVC controllers
Razor views
```

ViewModels should not be used by BLL or DAL.

---

# 5. Mapper Boundaries

Use mappers at every boundary.

## 5.1 DAL mappers

Location:

```text
App.DAL.EF/Mappers/...
```

Responsibility:

```text
Domain entity <-> DAL DTO
```

Examples:

```text
Customer entity -> CustomerProfileDalDto
CustomerCreateDalDto -> Customer entity
CustomerUpdateDalDto -> existing Customer entity
```

## 5.2 BLL mappers

Location:

```text
App.BLL/Mappers/...
```

Responsibility:

```text
DAL DTO -> BLL model
BLL command -> DAL create/update DTO
```

Examples:

```text
CustomerProfileDalDto -> CustomerProfileModel
UpdateCustomerProfileCommand -> CustomerUpdateDalDto
```

## 5.3 WebApp API mappers

Location:

```text
WebApp/Mappers/Api/...
```

Responsibility:

```text
API request DTO -> BLL command
BLL model -> API response DTO
```

## 5.4 WebApp MVC mappers

Location:

```text
WebApp/Mappers/Mvc/...
```

Responsibility:

```text
MVC form ViewModel -> BLL command
BLL model -> MVC ViewModel
```

---

# 6. FluentResults Strategy

Install FluentResults in:

```text
App.BLL.Contracts
App.BLL
```

BLL service interfaces should return:

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

## 6.1 Custom error classes

Location:

```text
App.BLL.Contracts/Common/Errors/
```

Recommended error classes:

```text
NotFoundError
ForbiddenError
UnauthorizedError
ValidationAppError
ConflictError
BusinessRuleError
UnexpectedAppError
```

Example:

```csharp
using FluentResults;

public sealed class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
        Metadata["ErrorType"] = "NotFound";
    }
}
```

Example conflict error:

```csharp
using FluentResults;

public sealed class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Conflict";
    }
}
```

Example validation error:

```csharp
using FluentResults;

public sealed class ValidationAppError : Error
{
    public IReadOnlyList<ValidationFailureModel> Failures { get; }

    public ValidationAppError(
        string message,
        IReadOnlyList<ValidationFailureModel> failures) : base(message)
    {
        Failures = failures;
        Metadata["ErrorType"] = "Validation";
    }
}
```

## 6.2 WebApp FluentResults-to-HTTP mapping

Create:

```text
WebApp/Infrastructure/Results/FluentResultHttpExtensions.cs
```

Example:

```csharp
public static class FluentResultHttpExtensions
{
    public static ActionResult<TResponse> ToActionResult<TModel, TResponse>(
        this Result<TModel> result,
        Func<TModel, TResponse> map)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(map(result.Value));
        }

        var firstError = result.Errors.FirstOrDefault();

        return firstError switch
        {
            NotFoundError => new NotFoundObjectResult(ToProblem(result)),
            ForbiddenError => new ObjectResult(ToProblem(result)) { StatusCode = StatusCodes.Status403Forbidden },
            UnauthorizedError => new UnauthorizedObjectResult(ToProblem(result)),
            ConflictError => new ConflictObjectResult(ToProblem(result)),
            ValidationAppError => new BadRequestObjectResult(ToProblem(result)),
            BusinessRuleError => new BadRequestObjectResult(ToProblem(result)),
            _ => new ObjectResult(ToProblem(result)) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        var firstError = result.Errors.FirstOrDefault();

        return firstError switch
        {
            NotFoundError => new NotFoundObjectResult(ToProblem(result)),
            ForbiddenError => new ObjectResult(ToProblem(result)) { StatusCode = StatusCodes.Status403Forbidden },
            UnauthorizedError => new UnauthorizedObjectResult(ToProblem(result)),
            ConflictError => new ConflictObjectResult(ToProblem(result)),
            ValidationAppError => new BadRequestObjectResult(ToProblem(result)),
            BusinessRuleError => new BadRequestObjectResult(ToProblem(result)),
            _ => new ObjectResult(ToProblem(result)) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }

    private static object ToProblem(IResultBase result)
    {
        return new
        {
            errors = result.Errors.Select(e => e.Message).ToArray()
        };
    }
}
```

The exact response shape should match the existing API behavior as closely as possible.

---

# 7. Unit of Work and Transaction Strategy

## 7.1 Base UOW contract

Update `IBaseUOW` to support cancellation tokens and return the number of affected records.

Target:

```csharp
namespace Base.DAL.Contracts;

public interface IBaseUOW
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## 7.2 Transaction abstraction

Create the transaction abstraction in either:

```text
Base.DAL.Contracts/IAppTransaction.cs
```

or:

```text
App.Contracts/Common/IAppTransaction.cs
```

Recommended shape:

```csharp
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

Then extend `IAppUOW`:

```csharp
public interface IAppUOW : IBaseUOW
{
    Task<IAppTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    IManagementCompanyRepository ManagementCompanies { get; }
    IManagementCompanyJoinRequestRepository ManagementCompanyJoinRequests { get; }

    ICustomerRepository Customers { get; }
    IPropertyRepository Properties { get; }
    IUnitRepository Units { get; }
    IResidentRepository Residents { get; }
    ILeaseRepository Leases { get; }
    IContactRepository Contacts { get; }
}
```

## 7.3 EF implementation

In `App.DAL.EF`, create:

```text
EfAppTransaction.cs
AppUOW.cs
```

`EfAppTransaction` should wrap EF Core's `IDbContextTransaction`.

BLL services should use transactions only for workflows that need multiple repository operations to succeed or fail together.

Example:

```csharp
await using var tx = await _uow.BeginTransactionAsync(cancellationToken);

var customer = await _uow.Customers.FirstProfileByCompanyAndSlugAsync(
    command.CompanySlug,
    command.CustomerSlug,
    cancellationToken);

if (customer is null)
{
    return Result.Fail(new NotFoundError("Customer was not found."));
}

await _uow.Customers.UpdateProfileAsync(updateDto, cancellationToken);
await _uow.SaveChangesAsync(cancellationToken);

await tx.CommitAsync(cancellationToken);

return Result.Ok(model);
```

---

# 8. Foundation Phase

This phase should be completed before the first business vertical slice.

It should not change application behavior.

## 8.1 Foundation tasks

```text
1. Create App.BLL.Contracts project.
2. Add FluentResults package to App.BLL.Contracts.
3. Add FluentResults package to App.BLL if needed.
4. Fix Base.DAL.Contracts namespaces.
5. Update IBaseUOW to support CancellationToken and return int.
6. Add IAppTransaction.
7. Expand IAppUOW with BeginTransactionAsync.
8. Create FluentResults error classes in App.BLL.Contracts.
9. Create WebApp FluentResults-to-HTTP extension.
10. Create DAL/BLL/WebApp mapper base interfaces.
11. Add AddAppDalEf() DI extension.
12. Add AddAppBll() DI extension.
13. Keep old services/controllers working during this foundation phase.
```

## 8.2 Suggested foundation folders

```text
App.BLL.Contracts/
  Common/
    Errors/
      NotFoundError.cs
      ForbiddenError.cs
      UnauthorizedError.cs
      ConflictError.cs
      ValidationAppError.cs
      BusinessRuleError.cs
      UnexpectedAppError.cs
    ValidationFailureModel.cs

Base.DAL.Contracts/
  IBaseUOW.cs
  IAppTransaction.cs

App.Contracts/
  Common/
    IAppUOW.cs

WebApp/
  Infrastructure/
    Results/
      FluentResultHttpExtensions.cs
```

## 8.3 Foundation definition of done

```text
Solution builds.
Existing app behavior still works.
No business functionality has changed.
No controllers are refactored yet except for harmless namespace/import changes.
Old BLL services still work.
App.BLL.Contracts exists.
FluentResults types are available in BLL contracts.
UOW transaction abstraction exists.
```

---

# 9. Final Repository List

## 9.1 Must-have custom repositories

```text
IManagementCompanyRepository
IManagementCompanyJoinRequestRepository
ICustomerRepository
IPropertyRepository
IUnitRepository
IResidentRepository
ILeaseRepository
IContactRepository
```

## 9.2 Optional custom repositories

Create these only if the slice needs complex queries or direct behavior:

```text
IManagementCompanyUserRepository
ICustomerRepresentativeRepository
IResidentContactRepository
IResidentUserRepository
```

Otherwise, manage those through parent services/repositories.

## 9.3 Generic lookup repository

Use for lookup/reference entities:

```text
ContactType
CustomerRepresentativeRole
LeaseRole
ManagementCompanyRole
PropertyType
```

If other lookup entities already exist in the codebase and are actively used by the currently implemented functionality, they can also use the same generic lookup pattern. Do not introduce lookup handling for features that are not currently implemented.

---

# 10. Final Service Module List

The current BLL feature-folder structure should be evolved, not thrown away.

Target BLL modules:

```text
App.BLL/
  Common/
    Access/
    Mapping/
    Validation/
    Slugging/

  ManagementCompanies/
    ManagementCompanyProfileService.cs
    CompanyMembershipService.cs
    CompanyJoinRequestService.cs
    ManagementCompanyAccessService.cs

  Customers/
    CustomerProfileService.cs
    CustomerWorkspaceService.cs
    CompanyCustomerService.cs
    CustomerAccessService.cs

  Properties/
    PropertyProfileService.cs
    PropertyWorkspaceService.cs

  Units/
    UnitProfileService.cs
    UnitWorkspaceService.cs
    UnitAccessService.cs

  Residents/
    ResidentProfileService.cs
    ResidentWorkspaceService.cs
    ResidentAccessService.cs

  Leases/
    LeaseAssignmentService.cs
    LeaseLookupService.cs

  Onboarding/
    AccountOnboardingService.cs
    ContextSelectionService.cs
    WorkspaceCatalogService.cs
```

---

# 11. Vertical Slice Workflow

Every vertical slice should follow the same order.

```text
1. Inventory current behavior.
2. Identify routes/controllers/views affected.
3. Define DAL DTOs.
4. Define repository interface.
5. Implement EF repository.
6. Implement DAL mapper.
7. Add repository to IAppUOW/AppUOW.
8. Define BLL commands/queries/models in App.BLL.Contracts.
9. Define BLL service interface in App.BLL.Contracts.
10. Implement BLL service using IAppUOW.
11. Implement BLL mapper.
12. Update API DTOs only if required to preserve existing API shape.
13. Add WebApp API mapper.
14. Refactor API controller.
15. Add/update MVC ViewModels if regular controller exists.
16. Add WebApp MVC mapper.
17. Refactor regular MVC controller.
18. Add tests/manual regression checks.
19. Remove old direct DbContext/service code for that slice.
```

---

# 12. Definition of Done Per Vertical Slice

A slice is complete when:

```text
No controller in that slice uses AppDbContext.
No controller in that slice uses repositories.
No BLL service in that slice uses AppDbContext.
BLL service depends on App.BLL.Contracts and App.Contracts.
Repository returns DAL DTOs.
BLL returns FluentResults Result / Result<T>.
API controller maps App.DTO <-> BLL command/model.
Regular MVC controller maps ViewModel <-> BLL command/model.
Existing route paths remain unchanged.
Existing status codes remain unchanged unless intentionally documented.
Existing JSON response shape remains unchanged unless intentionally documented.
Existing UI behavior remains unchanged.
No ticket/vendor/scheduled-work/work-log functionality is introduced.
```

---

# 13. Slice Order

## Slice 0: Architecture Foundation

Purpose:

```text
Prepare shared contracts, FluentResults, UOW transactions, DI extension methods, and mapper interfaces.
```

Deliverables:

```text
App.BLL.Contracts project
FluentResults package
Common BLL errors
UOW transaction abstraction
Fixed Base.DAL.Contracts namespace
AddAppDalEf()
AddAppBll()
FluentResultHttpExtensions
Mapper base interfaces
```

This is a standalone plan and should be done before all business slices.

---

## Slice 1: Customer Profile

This should be the first real vertical slice.

Why:

```text
It touches read/update/delete.
It has business validation.
It has duplicate checks.
It has access checks.
It proves the architecture without touching the whole app.
```

### DAL contracts

```text
App.Contracts/DAL/Customers/
  ICustomerRepository.cs
  CustomerDalDto.cs
  CustomerProfileDalDto.cs
  CustomerUpdateDalDto.cs
```

Repository methods:

```csharp
Task<CustomerProfileDalDto?> FirstProfileByCompanyAndSlugAsync(
    string companySlug,
    string customerSlug,
    CancellationToken cancellationToken = default);

Task<bool> RegistryCodeExistsInCompanyAsync(
    Guid managementCompanyId,
    string registryCode,
    Guid? exceptCustomerId = null,
    CancellationToken cancellationToken = default);

Task UpdateProfileAsync(
    CustomerUpdateDalDto dto,
    CancellationToken cancellationToken = default);

Task DeleteAsync(
    Guid customerId,
    CancellationToken cancellationToken = default);
```

### DAL implementation

```text
App.DAL.EF/Repositories/CustomerRepository.cs
App.DAL.EF/Mappers/CustomerDalMapper.cs
```

### BLL contracts

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

### BLL implementation

```text
App.BLL/Customers/CustomerProfileService.cs
App.BLL/Mappers/Customers/CustomerProfileBllMapper.cs
```

### WebApp

```text
WebApp/Mappers/Api/Customers/CustomerProfileApiMapper.cs
WebApp/ApiControllers/Customer/CustomerProfileController.cs
```

Controller target shape:

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

---

## Slice 2: Customer Workspace / Company Customers

Purpose:

```text
Customer list
Customer dashboard context
Create customer under company
Customer access resolution
```

### DAL contracts

```text
IManagementCompanyRepository
ICustomerRepository
```

DTOs:

```text
ManagementCompanyDalDto
CustomerListItemDalDto
CustomerCreateDalDto
CustomerWorkspaceDalDto
```

Repository methods:

```csharp
Task<ManagementCompanyDalDto?> FirstBySlugAsync(
    string companySlug,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanySlugAsync(
    string companySlug,
    CancellationToken cancellationToken = default);

Task<CustomerDalDto> AddAsync(
    CustomerCreateDalDto dto,
    CancellationToken cancellationToken = default);

Task<bool> CustomerSlugExistsInCompanyAsync(
    Guid managementCompanyId,
    string slug,
    CancellationToken cancellationToken = default);
```

### BLL contracts

```text
ICustomerAccessService
ICompanyCustomerService
ICustomerWorkspaceService
```

Models:

```text
CustomerWorkspaceModel
CustomerListItemModel
CompanyCustomerModel
```

Commands/queries:

```text
GetCustomerWorkspaceQuery
GetCompanyCustomersQuery
CreateCustomerCommand
```

### Controllers

Refactor:

```text
CustomerDashboardController
CustomerPropertiesController, if it currently resolves customer workspace context
Regular MVC customer dashboard/controllers, if present
```

---

## Slice 3: Property Profile / Property Workspace

Purpose:

```text
Property profile
Property dashboard
Properties under customer
Create/update/delete property
```

### DAL

```text
IPropertyRepository
ICustomerRepository
IManagementCompanyRepository
```

DTOs:

```text
PropertyDalDto
PropertyProfileDalDto
PropertyListItemDalDto
PropertyCreateDalDto
PropertyUpdateDalDto
```

Repository methods:

```csharp
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
```

### BLL

```text
IPropertyProfileService
IPropertyWorkspaceService
```

Models:

```text
PropertyProfileModel
PropertyDashboardModel
PropertyListItemModel
```

Commands/queries:

```text
GetPropertyProfileQuery
CreatePropertyCommand
UpdatePropertyProfileCommand
DeletePropertyCommand
```

### Controllers

Refactor:

```text
WebApp/ApiControllers/Property/*
Property MVC controllers/views
```

---

## Slice 4: Unit Profile / Unit Workspace

Purpose:

```text
Units under property
Unit dashboard
Unit profile
Create/update/delete unit
```

### DAL

```text
IUnitRepository
IPropertyRepository
ILeaseRepository
```

DTOs:

```text
UnitDalDto
UnitProfileDalDto
UnitDashboardDalDto
UnitListItemDalDto
UnitCreateDalDto
UnitUpdateDalDto
```

Repository methods:

```csharp
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
```

### BLL

```text
IUnitProfileService
IUnitWorkspaceService
IUnitAccessService
```

Models:

```text
UnitProfileModel
UnitDashboardModel
UnitListItemModel
```

Commands/queries:

```text
GetUnitProfileQuery
GetUnitDashboardQuery
CreateUnitCommand
UpdateUnitCommand
DeleteUnitCommand
```

### Controllers

Refactor:

```text
WebApp/ApiControllers/Unit/*
Unit MVC controllers/views
```

This slice should eliminate controller-level chains like:

```text
resolve company
resolve customer
resolve property
resolve unit
map each failure to HTTP manually
```

The controller should call one service method and convert the FluentResults result.

---

## Slice 5: Resident Workspace / Resident Profile

Purpose:

```text
Resident profile
Resident contacts
Resident list
Resident-user links, if currently implemented
Resident lease summaries
```

### DAL

```text
IResidentRepository
IContactRepository
ILeaseRepository
IUnitRepository
```

Optional:

```text
IResidentContactRepository
IResidentUserRepository
```

DTOs:

```text
ResidentDalDto
ResidentProfileDalDto
ResidentListItemDalDto
ResidentContactDalDto
ResidentLeaseSummaryDalDto
ResidentCreateDalDto
ResidentUpdateDalDto
```

### BLL

```text
IResidentProfileService
IResidentWorkspaceService
IResidentAccessService
```

Models:

```text
ResidentProfileModel
ResidentListItemModel
ResidentContactModel
ResidentLeaseSummaryModel
```

Commands/queries:

```text
GetResidentProfileQuery
CreateResidentCommand
UpdateResidentProfileCommand
AddResidentContactCommand
RemoveResidentContactCommand
```

### Controllers

Refactor:

```text
WebApp/ApiControllers/Resident/*
Resident MVC controllers/views
```

---

## Slice 6: Lease Assignments

Purpose:

```text
Assign residents to units
End leases
Update lease periods/roles
Validate overlapping active leases
```

### DAL

```text
ILeaseRepository
IUnitRepository
IResidentRepository
```

DTOs:

```text
LeaseDalDto
LeaseCreateDalDto
LeaseUpdateDalDto
LeaseAssignmentDalDto
LeaseRoleDalDto
ActiveLeaseDalDto
```

Repository methods:

```csharp
Task<IReadOnlyList<LeaseDalDto>> ActiveByUnitAsync(
    Guid unitId,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<LeaseDalDto>> ActiveByResidentAsync(
    Guid residentId,
    CancellationToken cancellationToken = default);

Task<bool> HasOverlappingLeaseAsync(
    Guid unitId,
    DateOnly startDate,
    DateOnly? endDate,
    Guid? exceptLeaseId = null,
    CancellationToken cancellationToken = default);

Task AddAsync(
    LeaseCreateDalDto dto,
    CancellationToken cancellationToken = default);

Task UpdateAsync(
    LeaseUpdateDalDto dto,
    CancellationToken cancellationToken = default);
```

### BLL

```text
ILeaseAssignmentService
ILeaseLookupService
```

Commands/queries:

```text
AssignResidentToUnitCommand
EndLeaseCommand
UpdateLeaseCommand
GetUnitLeasesQuery
GetResidentLeasesQuery
```

Business rules:

```text
Unit must belong to resolved company/customer/property context.
Resident must belong to correct company/customer context.
No overlapping lease unless explicitly allowed.
Lease role must exist.
End date cannot be before start date.
```

### Controllers

Refactor lease-related API and MVC controllers.

---

## Slice 7: Management Company / Membership / Join Requests

Purpose:

```text
Management company profile
Company membership
Company roles
Join requests
Company access
```

### DAL

```text
IManagementCompanyRepository
IManagementCompanyJoinRequestRepository
```

Optional:

```text
IManagementCompanyUserRepository
```

DTOs:

```text
ManagementCompanyDalDto
ManagementCompanyProfileDalDto
ManagementCompanyUserDalDto
ManagementCompanyJoinRequestDalDto
ManagementCompanyJoinRequestCreateDalDto
```

### BLL

```text
IManagementCompanyProfileService
ICompanyMembershipService
ICompanyJoinRequestService
IManagementCompanyAccessService
```

Commands/queries:

```text
GetManagementCompanyProfileQuery
UpdateManagementCompanyProfileCommand
CreateJoinRequestCommand
ApproveJoinRequestCommand
RejectJoinRequestCommand
GetCompanyMembersQuery
```

### Controllers

Refactor:

```text
WebApp/ApiControllers/Management/*
Management MVC controllers/views
```

This slice touches identity/membership logic, so it is intentionally later than customer/property/unit.

---

## Slice 8: Onboarding

Purpose:

```text
Account onboarding
Company selection
Workspace selection
Join request creation
Initial redirect/context flow
```

Keep onboarding as a use-case module.

### DAL

Uses:

```text
IManagementCompanyRepository
IManagementCompanyJoinRequestRepository
ICustomerRepository
IResidentRepository
```

### BLL

```text
IAccountOnboardingService
IContextSelectionService
IWorkspaceCatalogService
ICompanyJoinRequestService
```

Commands/queries:

```text
GetOnboardingStateQuery
SelectWorkspaceCommand
CreateCompanyJoinRequestCommand
CompleteAccountOnboardingCommand
```

### Controllers

Refactor:

```text
WebApp/Controllers/OnboardingController.cs
WebApp/ApiControllers/Onboarding/*
```

Regular MVC onboarding controllers should build ViewModels from BLL models only.

---

# 14. Lookup Slice

This can be done early or alongside the slices that need lookup data.

Purpose:

```text
Provide roles, contact types, property types, and other lookup/reference data already used by currently implemented functionality.
```

Do not add lookup APIs or lookup services for functionality that is not currently implemented.

## DAL

```text
ILookupRepository<TLookupDalDto>
```

Lookup DTO:

```csharp
public sealed class LookupDalDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
```

## BLL

```text
ILookupService
```

Model:

```csharp
public sealed class LookupModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
```

## API DTO

```text
LookupResponseDto
```

---

# 15. Controller Refactor Rules

The existing controller feature grouping should stay.

Target API controller pattern:

```csharp
[HttpGet]
public async Task<ActionResult<CustomerProfileResponseDto>> GetProfile(
    string companySlug,
    string customerSlug,
    CancellationToken cancellationToken)
{
    var query = _mapper.ToQuery(companySlug, customerSlug, User);

    var result = await _customerProfileService.GetAsync(
        query,
        cancellationToken);

    return result.ToActionResult(_mapper.ToResponseDto);
}
```

Controllers should do:

```text
Read route/body/query values.
Get current user id.
Map API DTO/ViewModel to BLL command/query.
Call one BLL service method.
Map BLL model to API DTO/ViewModel.
Return result.
```

Controllers should not do:

```text
Use AppDbContext.
Use repositories.
Perform business validation.
Perform tenant access logic directly.
Start transactions.
Call SaveChangesAsync.
Manually delete related data.
Contain repeated if NotFound/Forbidden/Conflict blocks everywhere.
```

---

# 16. Regular MVC Controller Rules

For regular MVC controllers:

```text
GET action:
  Build BLL query.
  Call BLL service.
  Map BLL model to ViewModel.
  Return View(viewModel).

POST action:
  Validate ModelState.
  Map ViewModel to BLL command.
  Call BLL service.
  If success, redirect.
  If failure, map FluentResults error to ModelState/ViewModel.
```

MVC ViewModels stay in `WebApp`, not `App.DTO` and not `App.BLL.Contracts`.

---

# 17. DI Registration Plan

## 17.1 App.DAL.EF

Create:

```text
App.DAL.EF/DependencyInjection.cs
```

Example:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddAppDalEf(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddScoped<IAppUOW, AppUOW>();

        return services;
    }
}
```

## 17.2 App.BLL

Create:

```text
App.BLL/DependencyInjection.cs
```

Example:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddAppBll(this IServiceCollection services)
    {
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<ICustomerWorkspaceService, CustomerWorkspaceService>();
        services.AddScoped<IPropertyProfileService, PropertyProfileService>();
        services.AddScoped<IUnitProfileService, UnitProfileService>();
        services.AddScoped<IResidentProfileService, ResidentProfileService>();
        services.AddScoped<ILeaseAssignmentService, LeaseAssignmentService>();

        return services;
    }
}
```

## 17.3 WebApp

Eventually `Program.cs` should move from many individual service registrations to:

```csharp
builder.Services.AddAppDalEf(connectionString);
builder.Services.AddAppBll();
builder.Services.AddWebAppMappers();
```

---

# 18. Testing Plan

For each vertical slice, test at multiple levels.

## 18.1 DAL tests

```text
Repository returns only tenant-scoped data.
Repository does not leak data from another company/customer.
Create works.
Update works.
Delete works.
Lookup queries work.
```

## 18.2 BLL tests

```text
Success path.
Not found returns NotFoundError.
Forbidden returns ForbiddenError.
Duplicate returns ConflictError.
Validation failure returns ValidationAppError.
Business rule violation returns BusinessRuleError.
SaveChangesAsync called once for successful command.
Transaction commits on success.
Transaction rolls back/disposes on failure.
```

## 18.3 Controller/API tests or manual checks

```text
Same route.
Same auth behavior.
Same status codes.
Same JSON response shape.
Same validation response shape if possible.
Same Swagger DTO shape.
```

## 18.4 MVC regression checks

```text
Page opens.
Forms submit.
Validation displays correctly.
Redirects still work.
Breadcrumbs/navigation still work.
```

---

# 19. Commit Strategy

Use small commits that match architecture layers and slices.

Recommended pattern:

```text
refactor-foundation: add App.BLL.Contracts and FluentResults errors
refactor-foundation: add UOW transaction abstraction
refactor-dal-customer: add customer repository and DAL DTOs
refactor-bll-customer: move customer profile service to new contracts
refactor-web-customer: thin customer profile API controller
refactor-mvc-customer: update customer profile MVC controller and viewmodels
```

Avoid vague commits:

```text
big architecture refactor
cleanup
fix everything
mass changes
```

---

# 20. Recommended Split into Multiple Plans

This master plan should be split into smaller implementation plans.

## Plan A: Foundation Architecture Plan

Includes:

```text
App.BLL.Contracts
FluentResults
Common error types
UOW transactions
Base namespace fixes
DI extension methods
Mapper base interfaces
Result-to-HTTP mapping
```

## Plan B: Customer Vertical Slice Plan

Includes:

```text
Customer Profile
Customer Workspace
Company Customers
Customer API controllers
Customer MVC controllers
```

This can be split further into:

```text
B1 Customer Profile
B2 Customer Workspace / Company Customers
```

## Plan C: Property + Unit Vertical Slice Plan

Includes:

```text
Property Profile
Property Workspace
Unit Profile
Unit Workspace
Property API/MVC
Unit API/MVC
```

## Plan D: Resident + Lease Plan

Includes:

```text
Resident Profile
Resident Workspace
Resident contacts
Lease assignments
Lease validation
```

## Plan E: Management + Onboarding Plan

Includes:

```text
Management company profile
Membership
Join requests
Onboarding
Workspace selection
```

---

# 21. Recommended Execution Order

```text
1. Plan A  — Foundation architecture
2. Plan B1 — Customer Profile
3. Plan B2 — Customer Workspace / Company Customers
4. Plan C1 — Property Profile / Workspace
5. Plan C2 — Unit Profile / Workspace
6. Plan D1 — Resident Profile / Workspace
7. Plan D2 — Lease Assignments
8. Plan E1 — Management Company / Membership / Join Requests
9. Plan E2 — Onboarding
10. Final cleanup — remove old services, old mappings, unused DTOs, unused registrations
```

---

# 22. Explicit Out of Scope

The following are out of scope for this refactor plan:

```text
Adding new user-facing features.
Implementing ticket functionality.
Implementing vendor functionality.
Implementing scheduled work functionality.
Implementing work-log functionality.
Changing API routes unless absolutely necessary.
Changing response shapes unless explicitly documented.
Changing UI behavior unless required by the refactor and documented.
Large-scale redesign of the domain model.
```

---

# 23. Final Guiding Principle

The final architecture should follow this rule:

```text
Controller = HTTP/MVC coordination only
BLL service = business/use-case logic
Repository = data access/query/persistence logic
UOW = save and transaction boundary
Mapper = translation between layer-specific DTO/model types
```

The refactor is successful when the codebase is cleaner internally, but the user-facing behavior remains the same.

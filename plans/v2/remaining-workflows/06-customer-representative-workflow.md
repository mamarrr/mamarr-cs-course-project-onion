# Customer Representative Workflow Plan

## Purpose

Implement management of customer representatives, linking a customer to a resident with representative role and validity dates.

## Current state

`CustomerRepresentative` exists and is used indirectly for customer context resolution, but has no dedicated service or MVC workflow.

## End state

The completed workflow supports:

- list representatives for customer
- assign resident as representative
- choose role
- edit validity and notes
- end assignment
- delete erroneous assignment when safe

## Domain plan

- No domain changes expected.
- Consider index `(CustomerId, ResidentId, CustomerRepresentativeRoleId, ValidFrom)`.
- Enforce overlapping assignments in BLL/repository rather than hard DB uniqueness.

## DTO strategy

Canonical DTO pair:

```text
CustomerRepresentativeBllDto / CustomerRepresentativeDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid CustomerId;
    public Guid ResidentId;
    public Guid CustomerRepresentativeRoleId;
    public DateOnly ValidFrom;
    public DateOnly? ValidTo;
    public string? Notes;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
ICustomerRepresentativeRepository : IBaseRepository<CustomerRepresentativeDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByCustomerAsync(customerId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(customerRepresentativeId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(customerRepresentativeId, managementCompanyId);
Task /* ... */ OverlappingAssignmentExistsAsync(customerId, residentId, roleId, validFrom, validTo, exceptId);
```

Repository implementation requirements:

- Place implementation under `App.DAL.EF/Repositories`.
- Inherit from `BaseRepository<TDalDto, TDomain, AppDbContext>` for persisted entities.
- Apply tenant scoping in every read and mutation.
- Use `AsNoTracking()` for reads.
- Use `AsTracking()` for updates.
- Override `UpdateAsync` where `LangStr`, parent filtering, or relationship-safe updates are needed.
- Return DAL DTOs or DAL projection DTOs only.

## UOW wiring

Update:

```text
App.DAL.Contracts/IAppUow.cs
App.DAL.EF/AppUOW.cs
```

Steps:

1. Add repository property to `IAppUOW`.
2. Add mapper field to `AppUOW`.
3. Add private lazy repository field.
4. Add lazy property implementation.

## BLL plan

Service contract:

```text
ICustomerRepresentativeService : IBaseService<CustomerRepresentativeBllDto>
```

ICustomerService methods:

```csharp
Task<Result> ListForCustomerAsync(CustomerRoute route);
Task<Result> GetFormAsync(CustomerRoute route);
Task<Result> AddAsync(CustomerRoute route, CustomerRepresentativeBllDto dto);
Task<Result> UpdateAsync(CustomerRepresentativeRoute route, CustomerRepresentativeBllDto dto);
Task<Result> EndAssignmentAsync(CustomerRepresentativeRoute route, DateOnly validTo);
Task<Result> DeleteAsync(CustomerRepresentativeRoute route);
```

Service implementation requirements:

- Implement inside `CustomerService` or an internal helper composed by `CustomerService`.
- If an internal helper is used, it may inherit `BaseService<CustomerRepresentativeBllDto, CustomerRepresentativeDalDto, ICustomerRepresentativeRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Customer must belong to company.
- Resident must belong to same company.
- Representative role must exist.
- ValidTo cannot be before ValidFrom.
- Overlapping active assignment for same customer/resident/role is blocked.
- Prefer ending assignments over deleting historical links.

## AppBLL wiring

Update:

```text
App.BLL.Contracts/IAppBLL.cs
App.BLL/AppBLL.cs
```

Steps:

1. Add service property to `IAppBLL`.
2. Add private lazy service field to `AppBLL`.
3. Instantiate service with UOW and dependent services.
4. Avoid circular dependencies.

## WebApp MVC plan

- Add CustomerRepresentativesController.
- Add index/form/end/delete ViewModels.
- Add Index, Create, Edit, End, Delete views.

Controller rules:

- Controllers are thin adapters.
- Controllers build route models from URL values and current user ID.
- Controllers map ViewModels to canonical BLL DTOs.
- Controllers do not duplicate tenant/RBAC/lifecycle rules.
- POST actions use PRG on success.
- Add BLL validation failures to `ModelState`.

## Razor views

Add views under the relevant Management area folder.

Typical view set:

```text
Index.cshtml
Details.cshtml
Create.cshtml
Edit.cshtml
Delete.cshtml
```

Nested workflows may use fewer views if embedded into parent pages.

## Navigation

- Entry point: Customer Details -> Representatives.

## Localization

Add resource entries for:

- page titles,
- form labels,
- validation messages,
- success messages,
- delete confirmations,
- lifecycle blocking reasons where applicable.

Add English and Estonian entries together.

## Definition of done

- Representatives can be listed/assigned/updated/ended/deleted when safe.
- Same-company and overlap rules enforced.
- No API controller.

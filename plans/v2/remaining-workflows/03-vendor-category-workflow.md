# Vendor Category Assignment Workflow Plan

## Purpose

Implement the workflow for assigning ticket categories to vendors so the system knows which work categories a vendor supports.

## Current state

`VendorTicketCategory` exists as a link entity between Vendor and TicketCategory, but there is no service or UI workflow.

## End state

The completed workflow supports:

- view vendor category assignments
- assign category to vendor
- remove assignment
- edit assignment notes
- use assignments for ticket/scheduled work vendor filtering

## Domain plan

- No domain changes expected.
- Confirm unique `(VendorId, TicketCategoryId)` constraint.
- Do not add IsActive unless assignment history is required.

## DTO strategy

Canonical DTO pair:

```text
VendorTicketCategoryBllDto / VendorTicketCategoryDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid VendorId;
    public Guid TicketCategoryId;
    public string? Notes;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IVendorTicketCategoryRepository : IBaseRepository<VendorTicketCategoryDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByVendorAsync(vendorId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(vendorTicketCategoryId, managementCompanyId);
Task /* ... */ ExistsAsync(vendorId, ticketCategoryId);
Task /* ... */ ExistsInCompanyAsync(vendorTicketCategoryId, managementCompanyId);
Task /* ... */ DeleteAssignmentAsync(vendorId, ticketCategoryId, managementCompanyId);
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
IVendorCategoryService : IBaseService<VendorTicketCategoryBllDto>
```

IVendorService methods:

```csharp
Task<Result> GetAssignmentsAsync(VendorRoute route);
Task<Result> AssignCategoryAsync(VendorRoute route, VendorTicketCategoryBllDto dto);
Task<Result> UpdateAssignmentAsync(VendorCategoryAssignmentRoute route, VendorTicketCategoryBllDto dto);
Task<Result> RemoveCategoryAsync(VendorCategoryRoute route);
```

Service implementation requirements:

- Implement inside `VendorService` or an internal helper composed by `VendorService`.
- If an internal helper is used, it may inherit `BaseService<VendorTicketCategoryBllDto, VendorTicketCategoryDalDto, IVendorTicketCategoryRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Vendor must belong to company.
- Ticket category must exist.
- Duplicate assignment rejected with ConflictError.
- Assign/remove allowed for OWNER, MANAGER, SUPPORT.
- Notes optional and trimmed.

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

- Add VendorCategoriesController.
- Add VendorCategoryAssignmentViewModel and VendorCategoryFormViewModel.
- Add Index, Assign, Edit views.
- Can be embedded in Vendor Details.

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

- Entry point: Vendor Details -> Categories.

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

- Assignment service and repository exist.
- Duplicate assignment blocked.
- Vendor/category tenant validity enforced.
- No API controller.

# Vendor Workflow Plan

## Purpose

Implement real vendor management for maintenance providers. Vendors should no longer be only ticket selector options.

## Current state

`Vendor` exists and repository plumbing exists, including ticket options and company existence checks. There is no `IVendorService` exposed through `IAppBLL`.

## End state

The completed workflow supports:

- view company vendors
- create vendor
- edit vendor
- view vendor profile
- delete vendor when safe
- navigate to contacts and category assignments

## Domain plan

- No domain changes expected.
- Confirm unique registry code per management company.
- Confirm relationships to VendorContact, VendorTicketCategory, Ticket, and ScheduledWork.

## DTO strategy

Canonical DTO pair:

```text
VendorBllDto / VendorDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid ManagementCompanyId;
    public string Name;
    public string RegistryCode;
    public string Notes;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IVendorRepository : IBaseRepository<VendorDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByCompanyAsync(managementCompanyId);
Task /* ... */ FindProfileAsync(vendorId, managementCompanyId);
Task /* ... */ RegistryCodeExistsInCompanyAsync(managementCompanyId, registryCode, exceptVendorId);
Task /* ... */ HasDeleteDependenciesAsync(vendorId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(vendorId, managementCompanyId);
Task /* ... */ OptionsForTicketAsync(managementCompanyId, categoryId);
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
IVendorService : IBaseService<VendorBllDto>
```

Service methods:

```csharp
Task<Result> ListForCompanyAsync(ManagementCompanyRoute route);
Task<Result> GetProfileAsync(VendorRoute route);
Task<Result> CreateAsync(ManagementCompanyRoute route, VendorBllDto dto);
Task<Result> CreateAndGetProfileAsync(...);
Task<Result> UpdateAsync(VendorRoute route, VendorBllDto dto);
Task<Result> UpdateAndGetProfileAsync(...);
Task<Result> DeleteAsync(VendorRoute route, string confirmationRegistryCode);
```

Service implementation requirements:

- Place implementation under `App.BLL/Services`.
- Inherit from `BaseService<TBllDto, TDalDto, TRepository, IAppUOW>` where there is a persisted entity.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Read: OWNER, MANAGER, FINANCE, SUPPORT.
- Create/update: OWNER, MANAGER, SUPPORT.
- Delete: OWNER, MANAGER.
- Name, RegistryCode, Notes required.
- RegistryCode unique per company.
- Delete blocked by tickets, scheduled work, contacts, or categories.

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

- Add VendorsController in Management area.
- Add VendorIndexViewModel, VendorFormViewModel, VendorDetailsViewModel, VendorDeleteViewModel.
- Add Index, Details, Create, Edit, Delete views.
- Details links to contacts, categories, filtered tickets, scheduled work later.

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

- Add Vendors link in management company navigation or dashboard.

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

- VendorService is wired into IAppBLL.
- Repository supports list/profile/duplicate/dependency checks.
- MVC controller and views exist.
- No API controller.

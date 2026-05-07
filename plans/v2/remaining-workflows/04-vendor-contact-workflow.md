# Vendor Contact Workflow Plan

## Purpose

Implement vendor contact management using shared Contact and VendorContact link entity.

## Current state

`VendorContact` exists with validity, confirmation, primary flag, full name, role title, contact, and vendor relationships. No service or MVC workflow exists.

## End state

The completed workflow supports:

- view contacts for vendor
- attach existing contact
- create contact and attach it
- edit vendor-contact metadata
- set primary
- confirm/unconfirm
- remove link

## Domain plan

- No domain changes expected.
- Confirm filtered unique index: one primary vendor contact per vendor.

## DTO strategy

Canonical DTO pair:

```text
VendorContactBllDto / VendorContactDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid VendorId;
    public Guid ContactId;
    public DateOnly ValidFrom;
    public DateOnly? ValidTo;
    public bool Confirmed;
    public bool IsPrimary;
    public string? FullName;
    public string? RoleTitle;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IVendorContactRepository : IBaseRepository<VendorContactDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByVendorAsync(vendorId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(vendorContactId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(vendorContactId, managementCompanyId);
Task /* ... */ HasPrimaryAsync(vendorId, managementCompanyId, exceptVendorContactId);
Task /* ... */ ClearPrimaryAsync(vendorId, managementCompanyId, exceptVendorContactId);
Task /* ... */ ContactLinkedToVendorAsync(vendorId, contactId, exceptVendorContactId);
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
IVendorContactService : IBaseService<VendorContactBllDto>
```

Service methods:

```csharp
Task<Result> ListForVendorAsync(VendorRoute route);
Task<Result> AddAsync(VendorRoute route, VendorContactBllDto dto, ContactBllDto? newContact);
Task<Result> UpdateAsync(VendorContactRoute route, VendorContactBllDto dto);
Task<Result> SetPrimaryAsync(VendorContactRoute route);
Task<Result> ConfirmAsync(VendorContactRoute route);
Task<Result> UnconfirmAsync(VendorContactRoute route);
Task<Result> RemoveAsync(VendorContactRoute route);
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

- Vendor must belong to company.
- Existing contact must belong to company.
- New contact uses contact base validation.
- ValidTo cannot be before ValidFrom.
- Only one primary contact per vendor.
- Setting primary runs in transaction: clear old, set new, save.
- Prevent duplicate vendor/contact link unless historical windows are supported.

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

- Add VendorContactsController.
- Add VendorContactsIndexViewModel, VendorContactFormViewModel, VendorContactDeleteViewModel.
- Add Index, Create, Edit, Delete views.
- Use shared contact fields partial.

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

- Entry point: Vendor Details -> Contacts.

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

- Vendor contacts can be created/edited/confirmed/set primary/removed.
- One-primary rule enforced.
- No API controller.

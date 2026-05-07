# Contact Base Workflow Plan

## Purpose

Implement a reusable tenant-scoped contact workflow that supports resident contacts and vendor contacts without duplicating contact creation, update, validation, and uniqueness logic.

## Current state

`Contact` exists as a tenant-scoped domain entity with contact value, contact type, management company, optional notes, and links to vendor/resident contacts. The repository is currently minimal.

## End state

The completed workflow supports:

- list company contacts for selectors
- create a contact in a company
- update a contact
- delete a contact if not linked
- validate contact type
- enforce unique contact value per company/contact type

## Domain plan

- No domain changes expected.
- Confirm unique `(ManagementCompanyId, ContactTypeId, ContactValue)`.
- Confirm ContactType and ManagementCompany foreign keys.

## DTO strategy

Canonical DTO pair:

```text
ContactBllDto / ContactDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid ManagementCompanyId;
    public Guid ContactTypeId;
    public string ContactValue;
    public string? Notes;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IContactRepository : IBaseRepository<ContactDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ FindInCompanyAsync(contactId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(contactId, managementCompanyId);
Task /* ... */ OptionsByCompanyAsync(managementCompanyId);
Task /* ... */ DuplicateValueExistsAsync(managementCompanyId, contactTypeId, contactValue, exceptContactId);
Task /* ... */ HasDependenciesAsync(contactId, managementCompanyId);
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
IContactService : IBaseService<ContactBllDto>
```

Service methods:

```csharp
Task<Result> ListForCompanyAsync(ManagementCompanyRoute route);
Task<Result> CreateAsync(ManagementCompanyRoute route, ContactBllDto dto);
Task<Result> UpdateAsync(ContactRoute route, ContactBllDto dto);
Task<Result> DeleteAsync(ContactRoute route);
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

- AppUserId required.
- Company slug required.
- ContactTypeId must exist.
- ContactValue required and trimmed.
- Duplicate company/type/value rejected with ConflictError.
- Delete blocked if linked to resident or vendor contact.

## BLL composition boundary

Do not expose Contact as a first-class `IAppBLL` facade.

```text
Keep:
IContactService
ContactService

Do not add:
IAppBLL.Contacts
```

`IContactService` is a public reusable BLL service contract and `ContactService` is a public reusable BLL service implementation, but parent domain services own the WebApp/API-facing workflows.

Future vendor/resident contact workflows should compose it internally:

```csharp
private readonly IContactService _contacts;

public VendorService(IAppUOW uow, ...)
    : base(...)
{
    _contacts = new ContactService(uow);
}
```

## WebApp MVC plan

- Standalone Contacts UI is optional initially.
- Add ContactFormViewModel and shared _ContactFields.cshtml partial.
- Use contact fields from VendorContacts and ResidentContacts forms.
- If standalone UI is added: ContactsController with Index/Create/Edit/Delete.

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

- No top-level link initially unless a contact directory is needed.

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

- ContactService exists as a reusable BLL service but is not exposed through IAppBLL.
- Contact repository has tenant-safe methods.
- Vendor/resident contact workflows can reuse contact creation.
- No API controller.

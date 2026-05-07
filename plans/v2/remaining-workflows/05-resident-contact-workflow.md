# Resident Contact Workflow Plan

## Purpose

Implement resident contact management using shared Contact and ResidentContact link entity.

## Current state

`ResidentContact` exists with validity, confirmation, primary flag, resident, and contact relationships. No service or MVC workflow exists.

## End state

The completed workflow supports:

- view resident contacts
- attach existing contact
- create contact and attach it
- edit validity/primary/confirmed fields
- set primary
- confirm/unconfirm
- remove link

## Domain plan

- No domain changes expected.
- Confirm filtered unique index: one primary resident contact per resident.

## DTO strategy

Canonical DTO pair:

```text
ResidentContactBllDto / ResidentContactDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid ResidentId;
    public Guid ContactId;
    public DateOnly ValidFrom;
    public DateOnly? ValidTo;
    public bool Confirmed;
    public bool IsPrimary;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IResidentContactRepository : IBaseRepository<ResidentContactDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByResidentAsync(residentId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(residentContactId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(residentContactId, managementCompanyId);
Task /* ... */ HasPrimaryAsync(residentId, managementCompanyId, exceptResidentContactId);
Task /* ... */ ClearPrimaryAsync(residentId, managementCompanyId, exceptResidentContactId);
Task /* ... */ ContactLinkedToResidentAsync(residentId, contactId, exceptResidentContactId);
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
IResidentContactService : IBaseService<ResidentContactBllDto>
```

Service methods:

```csharp
Task<Result> ListForResidentAsync(ResidentRoute route);
Task<Result> AddAsync(ResidentRoute route, ResidentContactBllDto dto, ContactBllDto? newContact);
Task<Result> UpdateAsync(ResidentContactRoute route, ResidentContactBllDto dto);
Task<Result> SetPrimaryAsync(ResidentContactRoute route);
Task<Result> ConfirmAsync(ResidentContactRoute route);
Task<Result> UnconfirmAsync(ResidentContactRoute route);
Task<Result> RemoveAsync(ResidentContactRoute route);
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

- Management users OWNER, MANAGER, SUPPORT can create/update.
- Delete only OWNER, MANAGER.
- Resident user may manage own contact if active resident context exists.
- Contact must belong to same company.
- ValidTo cannot be before ValidFrom.
- Only one primary contact per resident.

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

- Add ResidentContactsController.
- Add ResidentContactsIndexViewModel, ResidentContactFormViewModel, ResidentContactDeleteViewModel.
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

- Entry point: Resident Details -> Contacts.

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

- Resident contacts can be managed end to end.
- Resident/contact tenant ownership enforced.
- No API controller.

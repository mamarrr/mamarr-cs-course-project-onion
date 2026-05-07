# Resident User Link Workflow Plan

## Purpose

Implement management of links between authenticated app users and resident records. This controls resident self-service access.

## Current state

`ResidentUser` exists with AppUserId, ResidentId, ValidFrom, ValidTo, and CreatedAt. No dedicated service or MVC workflow exists.

## End state

The completed workflow supports:

- list user links for resident
- link app user to resident
- edit validity dates
- revoke link by setting ValidTo
- delete erroneous links where safe

## Domain plan

- No domain changes expected.
- Confirm unique ResidentId/AppUserId pair if only one lifetime link is allowed.
- Recommended first pass: one row per resident/user pair with validity window.
- Resident ID code is unique per management company: `(ManagementCompanyId, IdCode)`.
- Public self-link by ID code has no management-company route, so the BLL must query active residents by ID code across management companies.
- If multiple active resident rows match the same ID code across different management companies, create/reuse links for all active matching rows under the current policy.
- Do not silently choose one arbitrary row.

## Resident ID code persistence note

Current uniqueness rule:

```csharp
builder.Entity<Resident>()
    .HasIndex(e => new { e.ManagementCompanyId, e.IdCode })
    .IsUnique()
    .HasDatabaseName("ux_resident_company_id_code");
```

Decision:

```text
Keep the unique index.
Do not use an EF alternate key for the resident ID-code workflow.
```

Do not reintroduce:

```csharp
builder.Entity<Resident>()
    .HasAlternateKey(e => new { e.ManagementCompanyId, e.IdCode })
    .HasName("uq_resident_mcompany_idcode");
```

Reason:

```text
ResidentUser and other relationships use ResidentId.
The self-link workflow only needs uniqueness and lookup by IdCode.
A unique index is enough for that.
```

If a future entity explicitly uses `{ ManagementCompanyId, IdCode }` as a foreign-key principal with `HasPrincipalKey`, then an alternate key can be reconsidered. That is not needed now.

## DTO strategy

Canonical DTO pair:

```text
ResidentUserBllDto / ResidentUserDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid ResidentId;
    public Guid AppUserId;
    public DateOnly ValidFrom;
    public DateOnly? ValidTo;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IResidentUserRepository : IBaseRepository<ResidentUserDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByResidentAsync(residentId, managementCompanyId);
Task /* ... */ AllByAppUserAsync(appUserId);
Task /* ... */ FindInCompanyAsync(residentUserId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(residentUserId, managementCompanyId);
Task /* ... */ ActiveLinkExistsAsync(residentId, appUserId, exceptResidentUserId);
Task /* ... */ UserExistsAsync(appUserId);
Task /* ... */ FindUserIdByEmailAsync(email);
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
IResidentUserService : IBaseService<ResidentUserBllDto>
```

IResidentService methods:

```csharp
Task<Result> ListForResidentAsync(ResidentRoute route);
Task<Result> LinkUserAsync(ResidentRoute route, ResidentUserBllDto dto);
Task<Result> LinkUserByEmailAsync(ResidentRoute route, ResidentUserLinkByEmailModel model);
Task<Result> UpdateLinkAsync(ResidentUserRoute route, ResidentUserBllDto dto);
Task<Result> RevokeLinkAsync(ResidentUserRoute route, DateOnly validTo);
Task<Result> DeleteLinkAsync(ResidentUserRoute route);
```

Service implementation requirements:

- Implement inside `ResidentService` or an internal helper composed by `ResidentService`.
- If an internal helper is used, it may inherit `BaseService<ResidentUserBllDto, ResidentUserDalDto, IResidentUserRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- OWNER, MANAGER, SUPPORT can link/update/revoke.
- Delete only OWNER, MANAGER.
- Resident must belong to company.
- App user must exist.
- ValidTo cannot be before ValidFrom.
- Active duplicate link rejected.
- Prefer revoke over delete once used for access history.

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

- Add ResidentUsersController.
- Add index/link/edit/revoke/delete ViewModels.
- Add Index, Link, Edit, Revoke, Delete views.

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

- Entry point: Resident Details -> Linked users.

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

- Resident user access can be managed.
- Active duplicate links blocked.
- No API controller.

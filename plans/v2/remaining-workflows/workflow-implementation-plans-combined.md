<!-- 00-architecture-guidance.md -->



# Workflow Implementation Architecture Guidance

## Purpose

This is the master plan for implementing the missing domain workflows end to end. End to end means domain/persistence alignment, DAL contracts and repositories, BLL contracts and services, canonical DTOs, mappers, UOW/AppBLL wiring, MVC ViewModels, Management-area controllers, Razor views, navigation integration, validation, authorization, and localization hooks.

API controllers are intentionally out of scope for this phase.

## Parent facade placement for subworkflows

Do not create public BLL service contracts for subworkflows by default.

Use this placement:

```text
Vendor category assignments:
  public methods on IVendorService / IAppBLL.Vendors
  optional internal helper only if VendorService becomes too large

Vendor contacts:
  public methods on IVendorService / IAppBLL.Vendors
  IVendorContactRepository remains separate

Resident contacts:
  public methods on IResidentService / IAppBLL.Residents
  IResidentContactRepository remains separate

Customer representatives:
  public methods on ICustomerService / IAppBLL.Customers
  optional internal helper only if CustomerService becomes too large

Resident user links:
  public methods on IResidentService / IAppBLL.Residents
  includes management-side link/revoke and public onboarding self-link by ID code

Scheduled work:
  public methods on ITicketService / IAppBLL.Tickets
  optional internal helper only if TicketService becomes too large

Work logs:
  public methods on ITicketService / IAppBLL.Tickets
  optional internal helper only if TicketService becomes too large
```

Do not add these public contracts by default:

```text
IVendorCategoryService
IVendorContactService
IResidentContactService
ICustomerRepresentativeService
IResidentUserService
IScheduledWorkService
IWorkLogService
```


## Scope

Workflow plans included:

1. Contact base workflow
2. Vendor workflow
3. Vendor category assignment workflow
4. Vendor contact workflow
5. Resident contact workflow
6. Customer representative workflow
7. Resident user link workflow
8. Scheduled work workflow
9. Work log workflow
10. Ticket lifecycle integration workflow

Excluded for now:

- API controllers
- public API DTOs
- tests while the current project override says not to write tests
- explicit Admin scaffolding beyond existing admin surfaces

## Architectural baseline

Follow the current branch structure:

```text
App.Domain       Domain entities and identity entities
App.DAL.DTO      Canonical DAL DTOs and narrow projection DTOs
App.DAL.Contracts Repository contracts and IAppUOW
App.DAL.EF       EF repositories, DbContext mappings, EF mappers, migrations, seeding
App.BLL.DTO      Canonical BLL DTOs, route models, page/form/list/detail models
App.BLL.Contracts Service contracts and IAppBLL facade
App.BLL          BLL services, BLL mappers, workflow validation, tenant/RBAC/business rules
WebApp           MVC ViewModels, Management-area controllers, Razor views, navigation, TempData
```

## Contact service boundary

`IContactService` and `ContactService` are public reusable BLL components for shared contact creation, validation, duplicate checks, listing, updates, and dependency-safe deletion.

Do not expose contacts as a first-class `IAppBLL` facade:

```text
Do not add:
  IAppBLL.Contacts

Use instead:
  VendorService composes ContactService internally for vendor contact workflows.
  ResidentService composes ContactService internally for resident contact workflows.
```

Parent domain facades expose contact workflows to WebApp/API. `ContactService` remains reusable infrastructure inside those parent services.

## Layer rules

### Domain

- Keep entities free from MVC/API dependencies.
- Do not put workflow decisions in entities.
- Change domain only when a field, relationship, index, or persistence rule requires it.
- Persisted multilingual business data uses `LangStr`.

### DAL

- Persisted entity repositories should inherit `BaseRepository<TDalDto, TDomain, AppDbContext>`.
- Repository interfaces should inherit `IBaseRepository<TDalDto>`.
- Repositories apply tenant-safe filters in SQL.
- Use `AsNoTracking()` for reads and `AsTracking()` for updates.
- Override `UpdateAsync` when updating `LangStr`, enforcing tenant parent filtering, or avoiding relationship overwrites.
- DAL returns DAL DTOs and DAL projection DTOs only.

### BLL

- Persisted workflow services should inherit `BaseService<TBllDto, TDalDto, TRepository, IAppUOW>`.
- BLL owns tenant resolution, RBAC, IDOR protection, validation, duplicate checks, lifecycle transitions, transactions, and error mapping.
- BLL services return `FluentResults.Result` / `Result<T>`.
- BLL services do not expose EF or domain entities.
- Prefer canonical DTO + route model.

### WebApp MVC

- Controllers are thin adapters.
- Controllers build BLL route models from URL values and current user identity.
- Controllers map ViewModels to canonical BLL DTOs.
- Controllers must not duplicate tenant checks, role checks, lifecycle checks, or cross-tenant ownership checks.
- Views are strongly typed.
- Avoid `ViewBag` and `ViewData`.
- Use PRG after successful POST.
- Use `TempData` for flash messages.
- Do not add API controllers in this phase.

## Canonical DTO policy

Avoid DTO explosion. Use one canonical BLL DTO and one canonical DAL DTO per persisted workflow entity. Add projection/page models only for list/profile/detail/form needs.

Canonical BLL DTOs expected:

```text
ContactBllDto
VendorBllDto
VendorTicketCategoryBllDto
VendorContactBllDto
ResidentContactBllDto
CustomerRepresentativeBllDto
ResidentUserBllDto
ScheduledWorkBllDto
WorkLogBllDto
```

Do not create `Create*Dto`, `Update*Dto`, `Delete*Dto`, or API DTOs by default. Use canonical DTOs, route models, and WebApp ViewModels.

## Resident ID code uniqueness rule

Resident ID code is unique inside one management company, not globally.

Current persistence rule:

```csharp
builder.Entity<Resident>()
    .HasIndex(e => new { e.ManagementCompanyId, e.IdCode })
    .IsUnique()
    .HasDatabaseName("ux_resident_company_id_code");
```

Resident self-link by ID code in Public onboarding has no management-company route. Therefore the BLL must query active residents by ID code across management companies.

Recommended current policy:

```text
If one active resident row matches the ID code:
  create/reuse one ResidentUser link.

If multiple active resident rows match the same ID code across different management companies:
  create/reuse links for all active matching rows.

If no active resident row matches:
  return NotFound/Validation error.

Never silently choose an arbitrary resident row.
```

This policy matches the current business rule that no management-company approval is required. It grants resident-context access only for the matched resident records and never management-company access.

### Alternate key / unique index decision

The project has replaced natural-key alternate keys with unique indexes.

Use this rule:

```text
Use unique indexes for schema/business uniqueness.
Use EF alternate keys only when another entity actually targets the natural key via HasPrincipalKey.
```

For the resident self-link workflow, keep only the unique index on `{ ManagementCompanyId, IdCode }`.

Do not reintroduce:

```csharp
builder.Entity<Resident>()
    .HasAlternateKey(e => new { e.ManagementCompanyId, e.IdCode });
```

Normal relationships use `ResidentId`, so an alternate key is not needed for this workflow.


## Route model guidance

Keep route models in `App.BLL.DTO/Common/Routes/RouteRequestModels.cs` unless they become too large.

Recommended route additions:

```csharp
public sealed class VendorRoute : ManagementCompanyRoute { public Guid VendorId { get; set; } }
public sealed class VendorCategoryRoute : VendorRoute { public Guid TicketCategoryId { get; set; } }
public sealed class VendorContactRoute : VendorRoute { public Guid VendorContactId { get; set; } }
public sealed class ResidentContactRoute : ResidentRoute { public Guid ResidentContactId { get; set; } }
public sealed class CustomerRepresentativeRoute : CustomerRoute { public Guid CustomerRepresentativeId { get; set; } }
public sealed class ResidentUserRoute : ResidentRoute { public Guid ResidentUserId { get; set; } }
public sealed class ScheduledWorkRoute : TicketRoute { public Guid ScheduledWorkId { get; set; } }
public sealed class WorkLogRoute : ScheduledWorkRoute { public Guid WorkLogId { get; set; } }
```

## Repository method naming

Use consistent names:

```text
AllByCompanyAsync
AllByVendorAsync
AllByResidentAsync
AllByCustomerAsync
AllByTicketAsync
AllByScheduledWorkAsync
FindProfileAsync
FindDetailsAsync
FindInCompanyAsync
ExistsInCompanyAsync
HasDeleteDependenciesAsync
Duplicate...ExistsAsync
Options...Async
```

## Service method naming

Use consistent names:

```text
ListForCompanyAsync
ListForVendorAsync
ListForResidentAsync
ListForCustomerAsync
ListForTicketAsync
GetProfileAsync
GetDetailsAsync
GetFormAsync
CreateAsync
CreateAndGetProfileAsync
UpdateAsync
UpdateAndGetProfileAsync
DeleteAsync
AssignCategoryAsync
RemoveCategoryAsync
SetPrimaryAsync
ConfirmAsync
RevokeLinkAsync
ScheduleAsync
StartWorkAsync
CompleteWorkAsync
CancelAsync
```


Customer/resident/ticket subworkflow method placement:

```text
ICustomerService
  ListRepresentativesAsync
  GetRepresentativeFormAsync
  AddRepresentativeAsync
  UpdateRepresentativeAsync
  EndRepresentativeAssignmentAsync
  DeleteErroneousRepresentativeAssignmentAsync

IResidentService
  ListUserLinksAsync
  LinkUserAsync
  LinkUserByEmailAsync
  UpdateUserLinkAsync
  RevokeUserAccessAsync
  DeleteErroneousUserLinkAsync
  SelfLinkByIdCodeAsync

ITicketService
  ListScheduledWorkForTicketAsync
  GetScheduledWorkDetailsAsync
  GetScheduleCreateFormAsync
  GetScheduleEditFormAsync
  ScheduleWorkAsync
  UpdateScheduleAsync
  StartWorkAsync
  CompleteWorkAsync
  CancelWorkAsync
  DeleteScheduledWorkAsync
  ListWorkLogsForScheduledWorkAsync
  AddWorkLogAsync
  UpdateWorkLogAsync
  DeleteWorkLogAsync
  GetTransitionAvailabilityAsync
```

## UOW and AppBLL wiring

For each repository:

1. Add property to `IAppUOW`.
2. Add mapper field to `AppUOW`.
3. Add private repository field to `AppUOW`.
4. Add lazy property implementation.

For each service:

1. Add property to `IAppBLL`.
2. Add private service field to `AppBLL`.
3. Add lazy property implementation.
4. Avoid circular service dependencies.

Exception: reusable helper services such as `ContactService` may have public contracts and implementations without becoming first-class `IAppBLL` properties. Compose them inside the parent domain service that owns the user-facing workflow.

## Authorization baseline

Tenant boundary is management company.

| Operation type | Allowed roles |
|---|---|
| Read/list/details | `OWNER`, `MANAGER`, `FINANCE`, `SUPPORT` |
| Operational create/update | `OWNER`, `MANAGER`, `SUPPORT` |
| Delete/irreversible actions | `OWNER`, `MANAGER` |
| Cost visibility | `OWNER`, `MANAGER`, `FINANCE` |

Resident self-service exceptions:

- resident users may manage their own contact details,
- resident users may view own resident-linked records only when explicitly supported,
- resident users must never gain management-company-wide access.

Every service should validate `AppUserId`, resolve company by slug, verify active role/context, and query child entities by parent plus company.

## Validation and errors

Use existing BLL error types:

```text
UnauthorizedError
ForbiddenError
NotFoundError
ValidationAppError
ConflictError
BusinessRuleError
```

Use `ConflictError` for uniqueness violations and `BusinessRuleError` for lifecycle/dependency blocks.

## Localization and LangStr

- Persisted multilingual fields use `LangStr` in domain and string values in DTOs.
- Repositories should call `SetTranslation(...)` when preserving translations matters.
- MVC labels/messages should use resources.
- Add English and Estonian resource entries together.

## MVC layout guidance

Recommended controllers:

```text
WebApp/Areas/Management/Controllers/
  VendorsController.cs
  VendorCategoriesController.cs
  VendorContactsController.cs
  ResidentContactsController.cs
  CustomerRepresentativesController.cs
  ResidentUsersController.cs
  ScheduledWorksController.cs
  WorkLogsController.cs
```

Recommended ViewModel layout:

```text
WebApp/ViewModels/Management/<Workflow>/
  <Workflow>IndexViewModel.cs
  <Workflow>FormViewModel.cs
  <Workflow>DeleteViewModel.cs
```

Recommended views:

```text
WebApp/Areas/Management/Views/<Workflow>/
  Index.cshtml
  Details.cshtml
  Create.cshtml
  Edit.cshtml
  Delete.cshtml
```

Nested workflows can use fewer pages and be embedded into parent profile/details pages.

## Navigation integration

- Company/dashboard: Vendors
- Vendor details: Contacts and category assignments
- Resident details: Contacts and linked users
- Customer details: Representatives
- Ticket details: Scheduled work and work logs

## Definition of done per workflow

A workflow is complete when:

- domain and DbContext are aligned,
- DAL DTO, mapper, repository contract, repository implementation, and UOW wiring exist,
- BLL DTO, mapper, service contract, service implementation, and AppBLL wiring exist,
- tenant/RBAC checks are centralized in BLL,
- validation and duplicate/dependency rules exist,
- MVC ViewModels, Management controller actions, and Razor views exist,
- parent pages contain links or embedded summaries,
- user-visible text is localized or prepared for resources,
- no API controllers were added.





<!-- 01-contact-base-workflow.md -->



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





<!-- 02-vendor-workflow.md -->



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





<!-- 03-vendor-category-workflow.md -->



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





<!-- 04-vendor-contact-workflow.md -->



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





<!-- 05-resident-contact-workflow.md -->



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





<!-- 06-customer-representative-workflow.md -->



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





<!-- 07-resident-user-link-workflow.md -->



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





<!-- 08-scheduled-work-workflow.md -->



# Scheduled Work Workflow Plan

## Purpose

Implement scheduled work as the operational execution workflow between tickets and vendor work.

## Current state

`ScheduledWork` exists with planned/actual start/end, notes, vendor, ticket, work status, and work logs. No service or MVC workflow exists.

## End state

The completed workflow supports:

- list scheduled work for ticket
- schedule vendor work
- update schedule details
- start work
- complete work
- cancel work
- delete when no logs exist
- use scheduled work as ticket transition prerequisite

## Domain plan

- No domain changes expected.
- Confirm WorkStatus lookup seed codes: PLANNED, IN_PROGRESS, COMPLETED, CANCELLED.
- Confirm indexes for vendor schedule and ticket schedule queries.

## DTO strategy

Canonical DTO pair:

```text
ScheduledWorkBllDto / ScheduledWorkDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid VendorId;
    public Guid TicketId;
    public Guid WorkStatusId;
    public DateTime ScheduledStart;
    public DateTime? ScheduledEnd;
    public DateTime? RealStart;
    public DateTime? RealEnd;
    public string? Notes;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IScheduledWorkRepository : IBaseRepository<ScheduledWorkDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByCompanyAsync(managementCompanyId, filter);
Task /* ... */ AllByTicketAsync(ticketId, managementCompanyId);
Task /* ... */ FindDetailsAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ ExistsForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ HasWorkLogsAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ VendorBelongsToTicketCompanyAsync(vendorId, ticketId, managementCompanyId);
Task /* ... */ VendorSupportsTicketCategoryAsync(vendorId, ticketId);
Task /* ... */ AnyStartedForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ AnyCompletedForTicketAsync(ticketId, managementCompanyId);
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
IScheduledWorkService : IBaseService<ScheduledWorkBllDto>
```

ITicketService methods:

```csharp
Task<Result> ListForTicketAsync(TicketRoute route);
Task<Result> GetDetailsAsync(ScheduledWorkRoute route);
Task<Result> GetCreateFormAsync(TicketRoute route);
Task<Result> GetEditFormAsync(ScheduledWorkRoute route);
Task<Result> ScheduleAsync(TicketRoute route, ScheduledWorkBllDto dto);
Task<Result> UpdateScheduleAsync(ScheduledWorkRoute route, ScheduledWorkBllDto dto);
Task<Result> StartWorkAsync(ScheduledWorkRoute route, DateTime realStart);
Task<Result> CompleteWorkAsync(ScheduledWorkRoute route, DateTime realEnd);
Task<Result> CancelAsync(ScheduledWorkRoute route);
Task<Result> DeleteAsync(ScheduledWorkRoute route);
```

Service implementation requirements:

- Implement inside `TicketService` or an internal helper composed by `TicketService`.
- If an internal helper is used, it may inherit `BaseService<ScheduledWorkBllDto, ScheduledWorkDalDto, IScheduledWorkRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Ticket must belong to company.
- Vendor must belong to company.
- Vendor should support ticket category if ticket has category.
- Work status must exist.
- Scheduling allowed for OWNER, MANAGER, SUPPORT.
- ScheduledEnd cannot be before ScheduledStart.
- RealEnd cannot be before RealStart.
- Delete blocked if work logs exist.

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

- Add ScheduledWorksController.
- Add index/form/details/action/delete ViewModels.
- Add Index, Details, Create, Edit, Delete views.
- Ticket details embeds scheduled work summary.

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

- Entry point: Ticket Details -> Scheduled work.

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

- Tickets can have scheduled work records.
- Start/complete/cancel actions exist.
- Ticket lifecycle can check scheduled work.
- No API controller.





<!-- 09-work-log-workflow.md -->



# Work Log Workflow Plan

## Purpose

Implement work logs for scheduled work. Work logs capture actual labor, time, costs, and descriptions.

## Current state

`WorkLog` exists with created time, work start/end, hours, material cost, labor cost, description, app user, and scheduled work. No service or MVC workflow exists.

## End state

The completed workflow supports:

- list work logs for scheduled work
- add work logs
- edit work logs
- delete where allowed
- view labor/material totals
- use work logs as ticket completion prerequisite

## Domain plan

- No domain changes expected.
- Confirm numeric ranges are appropriate.
- Confirm Description uses LangStr?.

## DTO strategy

Canonical DTO pair:

```text
WorkLogBllDto / WorkLogDalDto
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid ScheduledWorkId;
    public Guid AppUserId;
    public DateTime? WorkStart;
    public DateTime? WorkEnd;
    public decimal? Hours;
    public decimal? MaterialCost;
    public decimal? LaborCost;
    public string? Description;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
IWorkLogRepository : IBaseRepository<WorkLogDalDto>
```

Repository methods to add or confirm:

```csharp
Task /* ... */ AllByScheduledWorkAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ FindInCompanyAsync(workLogId, managementCompanyId);
Task /* ... */ ExistsInCompanyAsync(workLogId, managementCompanyId);
Task /* ... */ ExistsForScheduledWorkAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ ExistsForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ TotalsForScheduledWorkAsync(scheduledWorkId, managementCompanyId);
Task /* ... */ TotalsForTicketAsync(ticketId, managementCompanyId);
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
IWorkLogService : IBaseService<WorkLogBllDto>
```

ITicketService methods:

```csharp
Task<Result> ListForScheduledWorkAsync(ScheduledWorkRoute route);
Task<Result> AddAsync(ScheduledWorkRoute route, WorkLogBllDto dto);
Task<Result> UpdateAsync(WorkLogRoute route, WorkLogBllDto dto);
Task<Result> DeleteAsync(WorkLogRoute route);
```

Service implementation requirements:

- Implement inside `TicketService` or an internal helper composed by `TicketService`.
- If an internal helper is used, it may inherit `BaseService<WorkLogBllDto, WorkLogDalDto, IWorkLogRepository, IAppUOW>`.
- Do not expose the helper through `IAppBLL`.
- Resolve current company/parent context from route.
- Check active user role or resident/customer context.
- Validate IDs through tenant-safe repository methods.
- Normalize strings before persistence.
- Return existing BLL error types: `UnauthorizedError`, `ForbiddenError`, `NotFoundError`, `ValidationAppError`, `ConflictError`, `BusinessRuleError`.
- Save changes through UOW.

## Business rules

- Scheduled work must belong to company.
- Add/update roles: OWNER, MANAGER, SUPPORT.
- Cost visibility: OWNER, MANAGER, FINANCE.
- Do not trust dto.AppUserId; set from current actor.
- Hours/costs non-negative.
- WorkEnd cannot be before WorkStart.
- At least one meaningful field required.
- Block edit/delete after ticket closed unless explicit manager override.

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

- Add WorkLogsController.
- Add index/form/delete ViewModels.
- Add Index, Create, Edit, Delete views.
- Scheduled work details embeds log summary and totals.

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

- Entry point: Scheduled Work Details -> Work logs.

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

- Work logs can be added/edited/deleted where allowed.
- Totals available.
- Ticket completion can require work logs.
- No API controller.





<!-- 10-ticket-lifecycle-integration-workflow.md -->



# Ticket Lifecycle Integration Workflow Plan

## Purpose

Tighten ticket lifecycle transitions so they depend on real workflow records: vendor assignment, scheduled work, work progress, and work logs.

## Current state

Ticket service supports basic CRUD, details, search, and AdvanceStatusAsync. It currently validates basic vendor/due date guards but does not require ScheduledWork or WorkLog records.

## End state

The completed workflow supports:

- CREATED -> ASSIGNED requires vendor
- ASSIGNED -> SCHEDULED requires scheduled work
- SCHEDULED -> IN_PROGRESS requires actual start
- IN_PROGRESS -> COMPLETED requires completed scheduled work and work logs
- COMPLETED -> CLOSED remains resolution verification

## Domain plan

- No domain changes expected.
- Confirm status codes: CREATED, ASSIGNED, SCHEDULED, IN_PROGRESS, COMPLETED, CLOSED.

## DTO strategy

Canonical DTO pair:

```text
TicketTransitionAvailabilityModel; no new persisted canonical DTO.
```

Canonical fields:

```csharp
public class CanonicalDto : BaseEntity
{
    public Guid TicketId;
    public string CurrentStatusCode;
    public string? NextStatusCode;
    public string? NextStatusLabel;
    public bool CanAdvance;
    public IReadOnlyList<string> BlockingReasons;
}
```

Projection/page models are allowed for list, profile, details, form, totals, or selector pages. Do not add create/update/delete DTOs unless the canonical DTO cannot represent the operation cleanly.

## DAL plan

Repository contract:

```text
Use ITicketRepository plus IScheduledWorkRepository and IWorkLogRepository.
```

Repository methods to add or confirm:

```csharp
Task /* ... */ TicketRepository.GetTransitionStateAsync(ticketId, managementCompanyId) optional;
Task /* ... */ ScheduledWorks.ExistsForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ ScheduledWorks.AnyStartedForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ ScheduledWorks.AnyCompletedForTicketAsync(ticketId, managementCompanyId);
Task /* ... */ WorkLogs.ExistsForTicketAsync(ticketId, managementCompanyId);
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
Extend ITicketService
```

Service methods:

```csharp
Task<Result> GetTransitionAvailabilityAsync(TicketRoute route);
Task<Result> AdvanceStatusAsync(TicketRoute route) with stronger prerequisites;
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

- CREATED -> ASSIGNED requires vendor.
- ASSIGNED -> SCHEDULED requires scheduled work.
- SCHEDULED -> IN_PROGRESS requires started scheduled work.
- IN_PROGRESS -> COMPLETED requires completed scheduled work and work logs.
- COMPLETED -> CLOSED allowed for management roles until verification workflow is added.
- Blocking reasons returned through BusinessRuleError or availability model.

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

- Update Ticket Details ViewModel/page to show transition availability and blocking reasons.
- Update AdvanceStatus POST to show blocking reasons.
- Add links from ticket details to schedule work and work logs.
- ScheduledWorkService start/complete can update ticket status when prerequisites are met.

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

- Entry point: Ticket Details -> lifecycle actions, scheduled work, work logs.

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

- Ticket advancement enforces real workflow prerequisites.
- Ticket details explains why transition is blocked.
- Scheduled work and work logs are linked.
- No API controller.

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

## More rules

- If you want to build the solution/project, ask the user to do it manually and the user will give you build results
- When a phase is done, ask the user to git commit and give a recommended git message
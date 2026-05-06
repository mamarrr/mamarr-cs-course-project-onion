# BLL Akaver-Style Structure Refactor Plan v2

Repository: `mamarrr/mamarr-cs-course-project`  
Branch context: latest inspected `dev` branch  
Reference architecture: `mamarrr/akaver-hw-demo`  
Plan file: `BLL_AKAVER_STRUCTURE_REFACTOR_PLAN_v3.md`

---

## 1. Purpose

This is a BLL-focused refactor plan.

The goal is to clean up the current BLL layer after the DAL refactor and after `BaseService` was changed toward `FluentResults`.

The main priorities are:

1. Reduce `IAppBLL` surface area by grouping exposed services domain-first.
2. Use `BaseService` as the standard foundation for aggregate-backed domain services.
3. Use canonical BLL DTOs by default for CRUD/simple entity operations and remove custom command/query DTOs when they only duplicate canonical DTOs.
4. Keep workflow behavior explicit inside domain services instead of exposing separate workflow services through `IAppBLL`.
5. Keep BLL API-ready: no MVC, WebApp, `HttpContext`, cookies, route data, ViewModels, or API DTO dependencies.

This plan is not a WebApp implementation plan, but it includes a small future WebApp cleanup note about removing simple `WebApp/Mappers` indirection.

---

## 2. Current branch state summary

The current `IAppBLL` exposes many granular services directly:

```text
AccountOnboarding
OnboardingCompanyJoinRequests
WorkspaceContexts
WorkspaceCatalog
WorkspaceRedirect
ContextSelection
ManagementCompanyProfiles
CompanyMembershipAdmin
CompanyCustomers
CustomerAccess
CustomerProfiles
CustomerWorkspaces
PropertyProfiles
PropertyWorkspaces
ResidentAccess
ResidentProfiles
ResidentWorkspaces
UnitAccess
UnitProfiles
UnitWorkspaces
LeaseAssignments
LeaseLookups
ManagementTickets
```

This surface is functional, but it is too wide and too workflow/UI-sliced.

The BLL currently has:

```text
App.BLL.Contracts
App.BLL.DTO
App.BLL
AppBLL composition root
FluentResults-based BLL contracts
typed BLL errors
canonical BLL DTOs
BLL mappers
AppDeleteGuard
domain/workspace/profile/access/workflow services
```

The latest `BaseService` is a generic CRUD service foundation that depends on:

```text
TBLLEntity
TDALEntity
TRepository : IBaseRepository<TKey, TDALEntity>
TUOW : IBaseUOW
IBaseMapper<TBLLEntity, TDALEntity>
```

and returns `FluentResults`.

This is now the intended base for aggregate-backed domain services.

---

## 3. Scope

### In scope

```text
App.BLL.Contracts
App.BLL.DTO
App.BLL
AppBLL facade
BLL service contracts
BLL service implementations
BLL DTO organization
BLL mapper organization
BLL service boundaries
BaseService inheritance from App.BLL services
BLL result/error consistency
Delete guard usage from BLL services
```

### Out of scope

```text
WebApp controller refactor
WebApp route/area/layout/navigation refactor
API controller refactor
API DTO creation
tests
large DAL refactor
database schema changes
Admin area refactor
```

### Permission gates

Do not make non-trivial changes to these without explicit approval:

```text
Base projects
DAL projects
database schema
API controllers
tests
ManagementCompany.DeleteCascadeAsync behavior
```

Small BaseService cleanup may be proposed because this plan depends on BaseService, but the change must be explained before touching Base projects.

---

## 4. Architectural target

The target is Akaver-like at the high level, not a mechanical copy.

Target shape:

```text
IAppBLL facade exposes a small set of domain services.
Every aggregate-backed public domain service inherits BaseService.
AppBLL composes domain services.
BLL services depend on IAppUOW, not AppDbContext.
BLL contracts expose BLL DTOs/models/results, not DAL DTOs.
Canonical BLL DTOs map to canonical DAL DTOs through IBaseMapper mappers.
BLL returns FluentResults / typed errors.
Repositories remain DAL-only and do not return FluentResults.
Workflow methods live inside the relevant domain service.
Internal helper/policy classes do not need BaseService.
```

---

## 5. Target dependency rules

### Allowed

```text
App.BLL.Contracts -> App.BLL.DTO
App.BLL.Contracts -> Base.BLL.Contracts
App.BLL.Contracts -> FluentResults

App.BLL -> App.BLL.Contracts
App.BLL -> App.BLL.DTO
App.BLL -> App.DAL.Contracts
App.BLL -> Base.BLL
App.BLL -> Base.Contracts
App.BLL -> FluentResults
App.BLL -> App.Resources
```

### Forbidden

```text
App.BLL -> App.DAL.EF
App.BLL -> WebApp
App.BLL -> App.DTO/v1
App.BLL -> MVC
App.BLL -> HttpContext
App.BLL -> cookies
App.BLL -> route data
App.BLL -> TempData / ViewData / ViewBag
App.BLL.Contracts -> App.DAL.DTO
App.BLL.Contracts -> WebApp
App.BLL.DTO -> WebApp
Repositories -> App.BLL.DTO
Repositories -> FluentResults
```

---

## 6. BaseService rule

### 6.1 Required rule

Every public domain BLL service exposed from `IAppBLL` must either:

1. inherit `BaseService` using its natural primary aggregate, or
2. be explicitly documented as a pure orchestration exception.

The preferred rule is:

```text
Every aggregate-backed public domain BLL service inherits BaseService.
Pure orchestration services may be documented exceptions.
Internal helpers/policies are not public domain services and do not need BaseService.
```

### 6.2 Why

The project now wants `BaseService` to be the standard CRUD foundation.

This means domain services should not reimplement simple canonical CRUD by hand when:

```text
canonical BLL DTO exists
canonical DAL DTO exists
IBaseMapper exists
IBaseRepository-backed repository exists
the operation is normal CRUD
```

### 6.3 What BaseService should provide

`BaseService` should provide:

```text
AllAsync
FindAsync
Add
UpdateAsync
Remove
RemoveAsync
canonical BLL DTO <-> DAL DTO mapping
generic not-found handling
FluentResults wrapping for base CRUD
```

Concrete services add:

```text
access checks
business validation
delete guard checks
workflow transitions
dashboard/profile/read models
search/filter methods
aggregate-specific commands
```

### 6.4 What should not inherit BaseService

These are not public domain services and do not need `BaseService`:

```text
AppDeleteGuard
CustomerAccessPolicy
PropertyAccessPolicy
ResidentAccessPolicy
UnitAccessPolicy
MembershipPolicy
SlugGenerator
internal validators
internal builders
private helper classes
small pure utility classes
```

### 6.5 Orchestration exceptions

Some public services may not have a natural aggregate.

Potential exceptions:

```text
WorkspaceService
OnboardingService
```

These should not be forced to inherit fake CRUD unless the team explicitly wants maximum consistency over semantic correctness.

If an exposed service does not inherit `BaseService`, document:

```text
why it is orchestration-only
why no natural primary aggregate exists
which repositories/services it coordinates
why inheriting BaseService would be misleading
```

### 6.6 Strict alternative

If the team wants strict consistency, then every `IAppBLL` service must inherit `BaseService`, even orchestration services, using the closest aggregate.

Example compromise:

```csharp
public class WorkspaceService
    : BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>,
      IWorkspaceService
{
}
```

This is allowed only as a documented compromise. It is less ideal because the service does not really represent management-company CRUD.

---

## 7. Target IAppBLL surface

### 7.1 Current problem

The current `IAppBLL` exposes implementation details and workflow slices directly.

Examples:

```text
CustomerAccess
CustomerProfiles
CustomerWorkspaces
PropertyProfiles
PropertyWorkspaces
ResidentAccess
ResidentProfiles
ResidentWorkspaces
UnitAccess
UnitProfiles
UnitWorkspaces
LeaseAssignments
LeaseLookups
WorkspaceCatalog
WorkspaceRedirect
WorkspaceContexts
ContextSelection
AccountOnboarding
OnboardingCompanyJoinRequests
```

These should be hidden behind domain services.

### 7.2 Target facade

Target `IAppBLL`:

```csharp
public interface IAppBLL : IBaseBLL
{
    ICustomerService Customers { get; }
    IPropertyService Properties { get; }
    IUnitService Units { get; }
    IResidentService Residents { get; }
    ILeaseService Leases { get; }
    ITicketService Tickets { get; }
    IManagementCompanyService ManagementCompanies { get; }
    ICompanyMembershipService Memberships { get; }
    IOnboardingService Onboarding { get; }
    IWorkspaceService Workspaces { get; }
}
```

Optional only if justified:

```csharp
ILookupService Lookups { get; }
IVendorService Vendors { get; }
IContactService Contacts { get; }
```

### 7.3 Target principle

Expose by domain/application capability:

```text
Customers
Properties
Units
Residents
Leases
Tickets
ManagementCompanies
Memberships
Onboarding
Workspaces
```

Do not expose by UI page, workflow helper, or technical role:

```text
CustomerProfiles
CustomerWorkspaces
CustomerAccess
UnitProfiles
UnitAccess
WorkspaceRedirect
ContextSelection
```

---

## 8. Target services and BaseService usage

## 8.1 CustomerService

Target exposed contract:

```text
ICustomerService
```

Target inheritance:

```csharp
public class CustomerService
    : BaseService<CustomerBllDto, CustomerDalDto, ICustomerRepository, IAppUOW>,
      ICustomerService
{
}
```

Owns:

```text
canonical customer CRUD
company customer list/create
customer profile get/update/delete
customer workspace/dashboard reads
customer access validation
customer delete guard usage
customer property links when customer is natural parent
```

Should absorb/wrap:

```text
ICompanyCustomerService
ICustomerAccessService
ICustomerProfileService
ICustomerWorkspaceService
```

---

## 8.2 PropertyService

Target exposed contract:

```text
IPropertyService
```

Target inheritance:

```csharp
public class PropertyService
    : BaseService<PropertyBllDto, PropertyDalDto, IPropertyRepository, IAppUOW>,
      IPropertyService
{
}
```

Owns:

```text
canonical property CRUD
property list/create under customer
property profile get/update/delete
property dashboard/workspace reads
property access validation
property delete guard usage
property type options if property-owned
```

Should absorb/wrap:

```text
IPropertyProfileService
IPropertyWorkspaceService
```

---

## 8.3 UnitService

Target exposed contract:

```text
IUnitService
```

Target inheritance:

```csharp
public class UnitService
    : BaseService<UnitBllDto, UnitDalDto, IUnitRepository, IAppUOW>,
      IUnitService
{
}
```

Owns:

```text
canonical unit CRUD
unit list/create under property
unit profile get/update/delete
unit dashboard/workspace reads
unit access validation
unit delete guard usage
unit tenant/lease summaries
```

Should absorb/wrap:

```text
IUnitAccessService
IUnitProfileService
IUnitWorkspaceService
```

---

## 8.4 ResidentService

Target exposed contract:

```text
IResidentService
```

Target inheritance:

```csharp
public class ResidentService
    : BaseService<ResidentBllDto, ResidentDalDto, IResidentRepository, IAppUOW>,
      IResidentService
{
}
```

Owns:

```text
canonical resident CRUD
resident list/search
resident profile get/update/delete
resident dashboard/workspace reads
resident access validation
resident lease/contact summaries
```

Should absorb/wrap:

```text
IResidentAccessService
IResidentProfileService
IResidentWorkspaceService
```

---

## 8.5 LeaseService

Target exposed contract:

```text
ILeaseService
```

Target inheritance:

```csharp
public class LeaseService
    : BaseService<LeaseBllDto, LeaseDalDto, ILeaseRepository, IAppUOW>,
      ILeaseService
{
}
```

Owns:

```text
canonical lease CRUD where valid
lease assignment workflow
lease lookup/options
lease overlap validation
resident lease views
unit lease views
lease-scoped deletes
```

Should absorb/wrap:

```text
ILeaseAssignmentService
ILeaseLookupService
```

Keep lease workflow DTOs where they express real workflow.

---

## 8.6 TicketService

Target exposed contract:

```text
ITicketService
```

Target inheritance:

```csharp
public class TicketService
    : BaseService<TicketBllDto, TicketDalDto, ITicketRepository, IAppUOW>,
      ITicketService
{
}
```

Owns:

```text
canonical ticket CRUD where valid
ticket list/search
ticket details
ticket edit model
ticket create/update workflow
ticket status transitions
ticket delete guard usage
ticket option lookups needed for ticket forms
```

Should absorb/wrap:

```text
IManagementTicketService
```

Expose as `Tickets`, not `ManagementTickets`, because tickets are a domain capability and future API controllers should not inherit UI naming.

---

## 8.7 ManagementCompanyService

Target exposed contract:

```text
IManagementCompanyService
```

Target inheritance:

```csharp
public class ManagementCompanyService
    : BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>,
      IManagementCompanyService
{
}
```

Owns:

```text
management company CRUD/profile
management company access validation
management company profile update
ManagementCompany.DeleteCascadeAsync exception exposure if needed
```

Should absorb/wrap:

```text
IManagementCompanyProfileService
IManagementCompanyAccessService
```

Do not remove `ManagementCompany.DeleteCascadeAsync`.

---

## 8.8 CompanyMembershipService

Target exposed contract:

```text
ICompanyMembershipService
```

Preferred inheritance if a natural membership aggregate exists:

```csharp
public class CompanyMembershipService
    : BaseService<CompanyMembershipBllDto, CompanyMembershipDalDto, ICompanyMembershipRepository, IAppUOW>,
      ICompanyMembershipService
{
}
```

If no natural membership aggregate/repository exists, document this as an orchestration exception.

Owns:

```text
company membership administration
user role options
role assignment rules
access request review
join request review
ownership transfer
membership authorization
membership command/query operations
```

Should absorb/wrap:

```text
ICompanyMembershipAdminService
ICompanyMembershipAuthorizationService
ICompanyMembershipCommandService
ICompanyMembershipQueryService
ICompanyOwnershipTransferService
ICompanyRoleOptionsService
ICompanyAccessRequestReviewService
```

Do not fake-inherit from `ManagementCompanyBllDto` just because memberships belong to a company, unless the team explicitly approves that compromise.

---

## 8.9 OnboardingService

Target exposed contract:

```text
IOnboardingService
```

Onboarding is a coordination/application service, not clearly one aggregate.

Preferred approach:

```text
OnboardingService is a documented orchestration exception.
ManagementCompany creation delegates to ManagementCompanyService.
Join request creation delegates to CompanyMembershipService or a JoinRequest domain service.
Workspace state delegates to WorkspaceService.
```

If the team insists that every `IAppBLL` service must inherit `BaseService`, choose one of these documented compromises:

```text
OnboardingService inherits BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>
```

or split onboarding so there is no heavy `OnboardingService` facade.

Owns/coordinates:

```text
registration/login-related onboarding commands
create management company during onboarding
create join request during onboarding
complete onboarding
onboarding state
```

Should absorb/wrap:

```text
IAccountOnboardingService
IOnboardingCompanyJoinRequestService
```

BLL must not depend on ASP.NET Identity UI, `HttpContext`, cookies, or MVC.

---

## 8.10 WorkspaceService

Target exposed contract:

```text
IWorkspaceService
```

Workspace is mostly a read/application context service, not a CRUD aggregate.

Preferred approach:

```text
WorkspaceService is a documented orchestration/read-model exception.
```

If the team insists that every `IAppBLL` service must inherit `BaseService`, choose a documented compromise:

```csharp
public class WorkspaceService
    : BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>,
      IWorkspaceService
{
}
```

Owns:

```text
workspace catalog
workspace context resolution
workspace redirect target
context selection authorization
workspace option models
```

Should absorb/wrap:

```text
IWorkspaceContextService
IWorkspaceCatalogService
IWorkspaceRedirectService
IContextSelectionService
```

BLL workspace methods must remain API-ready:

```text
no cookies
no route data
no HttpContext
no MVC redirect result
only explicit input DTOs/models
```

---

## 8.11 Optional smaller services

If exposed, these should inherit `BaseService`:

```csharp
public class ContactService
    : BaseService<ContactBllDto, ContactDalDto, IContactRepository, IAppUOW>,
      IContactService
{
}

public class VendorService
    : BaseService<VendorBllDto, VendorDalDto, IVendorRepository, IAppUOW>,
      IVendorService
{
}
```

Only expose from `IAppBLL` if they are real app-level capabilities and not only support data inside another service.

---

## 9. Internal service split

Reducing `IAppBLL` surface area does not require deleting all current services immediately.

Recommended transitional pattern:

```text
Public BLL contract:
  ICustomerService

Implementation:
  CustomerService : BaseService<...>, ICustomerService
    delegates to current CustomerAccessService
    delegates to current CustomerProfileService
    delegates to current CustomerWorkspaceService
    delegates to current CompanyCustomerService
```

Later, if the code becomes simpler, merge the delegated services into the domain service.

Internal helpers may stay public classes temporarily, but they should not remain exposed through `IAppBLL`.

---

## 10. DTO cleanup strategy

## 10.1 Canonical DTO first rule

Use canonical BLL DTOs as much as possible when the operation is normal CRUD or simple entity state transfer.

Do not create or keep custom command/query/model DTOs when they only duplicate a canonical BLL DTO and add no meaningful workflow, validation, projection, filtering, or business context.

A custom DTO is justified only when it clearly improves the architecture, such as for:

```text
workflow operations
multi-aggregate operations
filtered/search queries
dashboard/read projections
option/dropdown models
access/context resolution
delete confirmation or dependency review
status transitions
onboarding or membership workflows
```

If a custom DTO exists only because a specific page/action originally had its own DTO, prefer replacing it with:

```text
canonical BLL DTO + explicit method parameters
```

or:

```text
canonical BLL DTO + reusable context/scope model
```

This rule is intended to prevent DTO overengineering while still preserving useful workflow/read-model DTOs.

## 10.2 DTO categories

Classify every `App.BLL.DTO` type as exactly one of these:

```text
Canonical BLL DTO
Workflow command/request
Query/filter request
Read/projection model
Options/dropdown model
Error
Constant/helper
Redundant candidate
```

## 10.3 Canonical BLL DTOs

Canonical BLL DTOs are the default for simple CRUD.

They should:

```text
inherit from BaseEntity
live under App.BLL.DTO.*
map to canonical DAL DTOs through IBaseMapper mappers
represent simple entity state
not contain MVC/API concerns
not contain route/cookie/http concerns
```

Expected canonical DTOs:

```text
ContactBllDto
CustomerBllDto
PropertyBllDto
UnitBllDto
ResidentBllDto
LeaseBllDto
TicketBllDto
VendorBllDto
ManagementCompanyBllDto
CompanyMembershipBllDto if real aggregate exists
ManagementCompanyJoinRequestBllDto if real aggregate exists
```

## 10.4 Commands/queries to audit aggressively

Likely replacement candidates if they only duplicate canonical DTOs plus actor/scope fields and add no real workflow/projection value:

```text
CreateCustomerCommand
UpdateCustomerProfileCommand
CreatePropertyCommand
UpdatePropertyProfileCommand
CreateResidentCommand
UpdateResidentProfileCommand
CreateUnitCommand
UpdateUnitCommand
```

They may stay only if they express a real use case beyond CRUD, such as:

```text
confirmation text
actor/user id
scope slug/id
business workflow state
special validation input
non-entity fields
multi-aggregate operation
```

If the command is only:

```text
entity fields + user id + company slug
```

prefer:

```text
CanonicalBllDto + explicit method parameters
```

or:

```text
CanonicalBllDto + reusable scope/context model
```

## 10.5 Commands/queries likely worth keeping

Keep specialized DTOs where they represent real workflows:

```text
Lease assignment commands
Ticket workflow/status commands
Membership administration commands
Ownership transfer commands
Join request review commands
Onboarding account/company/join-request commands
Workspace redirect/context-selection queries
Filtered ticket/lease search queries
Dashboard/profile/list projection models
Delete commands that carry confirmation text or actor context
```

---

## 11. Method signature guidelines

## 11.1 Simple CRUD

Simple CRUD should use canonical BLL DTOs and inherit from `BaseService`.

Example:

```csharp
Task<Result<IEnumerable<CustomerBllDto>>> AllAsync(
    Guid parentId,
    CancellationToken cancellationToken = default);

Task<Result<CustomerBllDto>> FindAsync(
    Guid id,
    Guid parentId,
    CancellationToken cancellationToken = default);

Result<Guid> Add(CustomerBllDto dto);

Task<Result<CustomerBllDto>> UpdateAsync(
    CustomerBllDto dto,
    Guid parentId,
    CancellationToken cancellationToken = default);

Task<Result> RemoveAsync(
    Guid id,
    Guid parentId,
    CancellationToken cancellationToken = default);
```

## 11.2 Domain read/profile/workspace methods

Use explicit request DTOs only if they improve clarity.

Acceptable:

```csharp
Task<Result<CustomerProfileModel>> GetProfileAsync(
    GetCustomerProfileQuery query,
    CancellationToken cancellationToken = default);
```

Also acceptable:

```csharp
Task<Result<CustomerProfileModel>> GetProfileAsync(
    Guid appUserId,
    string companySlug,
    string customerSlug,
    CancellationToken cancellationToken = default);
```

Prefer request DTOs when the parameter count grows beyond 3 meaningful fields.

## 11.3 Workflow methods

Use named commands/requests for workflows:

```csharp
Task<Result<LeaseAssignmentModel>> AssignLeaseAsync(
    AssignLeaseCommand command,
    CancellationToken cancellationToken = default);

Task<Result<TicketDetailsModel>> UpdateStatusAsync(
    UpdateTicketStatusCommand command,
    CancellationToken cancellationToken = default);
```

Workflow methods still live in the domain service that inherits `BaseService`.

## 11.4 Delete methods

Simple delete:

```csharp
Task<Result> RemoveAsync(Guid id, Guid parentId, CancellationToken cancellationToken = default);
```

Delete with confirmation/business context:

```csharp
Task<Result> DeleteAsync(DeleteCustomerCommand command, CancellationToken cancellationToken = default);
```

Keep delete commands when they contain confirmation text, actor id, dependency-review data, or workflow-specific data.

---

## 12. BaseService cleanup before broad adoption

Before applying `BaseService` widely, decide whether to make these small Base changes:

```text
Change FindAsync from Result<T?> to Result<T> if not-found is failure.
Add mapper null checks instead of using !.
Keep generic BaseService errors in Base; app services may wrap/override with typed errors.
Prefer RemoveAsync(id, parentId) over Remove(entity) for app service usage.
```

Do not make Base depend on App-specific errors like `NotFoundError`.

---

## 13. Result and error rules

### 13.1 BLL result standard

All BLL service methods should return:

```text
Result
Result<T>
```

Known exception:

```text
IBaseBLL.SaveChangesAsync may remain Task<int> unless a separate Base change is approved.
```

If the team wants an absolute rule that all BLL methods return `FluentResults`, propose a separate Base change:

```csharp
Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default);
```

Do not include that change unless approved.

### 13.2 Typed errors

App-level services should use typed app errors for normal outcomes:

```text
ValidationAppError
NotFoundError
ForbiddenError
UnauthorizedError
ConflictError
BusinessRuleError
UnexpectedAppError
```

BaseService may return generic `Error` unless Base owns typed base errors.

### 13.3 Repository errors

Repositories must not return `FluentResults`.

Repositories return:

```text
DTO
nullable DTO
collections
bool facts
ids
void/Task for persistence commands
```

BLL translates repository outcomes into business/application results.

---

## 14. AppBLL composition plan

### 14.1 Target AppBLL composition

`AppBLL` should expose fewer domain services.

Example:

```csharp
public class AppBLL : BaseBLL<IAppUOW>, IAppBLL
{
    private ICustomerService? _customers;
    private IPropertyService? _properties;
    private IUnitService? _units;
    private IResidentService? _residents;
    private ILeaseService? _leases;
    private ITicketService? _tickets;
    private IManagementCompanyService? _managementCompanies;
    private ICompanyMembershipService? _memberships;
    private IOnboardingService? _onboarding;
    private IWorkspaceService? _workspaces;

    public ICustomerService Customers =>
        _customers ??= new CustomerService(UOW.Customers, UOW, new CustomerBllDtoMapper(), DeleteGuard);

    public IPropertyService Properties =>
        _properties ??= new PropertyService(UOW.Properties, UOW, new PropertyBllDtoMapper(), DeleteGuard);

    // etc.
}
```

Exact constructor shapes can differ, but aggregate-backed services should receive:

```text
repository
UOW
canonical mapper
delete guard/access helpers if needed
```

### 14.2 Transitional composition

Use transitional wrappers if safer.

Example:

```text
CustomerService : BaseService<...>, ICustomerService
  wraps current CompanyCustomerService
  wraps current CustomerAccessService
  wraps current CustomerProfileService
  wraps current CustomerWorkspaceService
```

Then remove direct facade exposure of old services.

### 14.3 Avoid cycles

Watch for cycles such as:

```text
CustomerService -> PropertyService -> CustomerService
UnitService -> PropertyService -> UnitService
WorkspaceService -> OnboardingService -> WorkspaceService
```

If cycles appear, extract internal policies/helpers:

```text
CustomerAccessPolicy
PropertyAccessPolicy
MembershipPolicy
WorkspaceCatalogReader
```

---

## 15. Service boundary refactor phases

## Phase 0 — Baseline and guardrails

### Tasks

1. Confirm the solution builds.
2. Confirm current `BaseService` compiles with the new FluentResults signatures.
3. Confirm no App.BLL reference to `App.DAL.EF`.
4. Confirm BLL contracts do not expose DAL DTOs.
5. Record current `IAppBLL` surface.
6. Record which exposed services are aggregate-backed and which are orchestration-only.

### Acceptance criteria

```text
Build baseline known.
Current BLL service list documented.
BaseService adoption targets documented.
Orchestration exceptions identified.
```

---

## Phase 1 — BLL inventory

For every BLL contract method, classify:

```text
service
method
input type
output type
Result/Result<T> usage
CRUD / workflow / query / projection / access / delete
uses canonical BLL DTO?
uses command/query DTO?
can live in aggregate-backed BaseService-derived domain service?
future API-ready?
```

For every BLL DTO, classify:

```text
canonical DTO
workflow command
query/filter
projection model
option model
error
constant/helper
redundant candidate
```

### Acceptance criteria

```text
Every App.BLL.Contracts method classified.
Every App.BLL.DTO type classified.
Candidate DTO removals listed but not executed yet.
Candidate service merges/wrappers listed.
BaseService inheritance targets listed.
```

---

## Phase 2 — Stabilize BaseService usage rules

### Tasks

1. Decide whether to adjust `BaseService.FindAsync` from `Result<T?>` to `Result<T>`.
2. Decide whether to add mapper null checks.
3. Decide whether `Remove(entity)` remains in `IBaseService`.
4. Decide whether generic BaseService string errors are acceptable.
5. Document that every aggregate-backed public domain service inherits `BaseService`.
6. Document all pure orchestration exceptions.

### Acceptance criteria

```text
BaseService behavior is understood before broad adoption.
No repository returns FluentResults.
Every future IAppBLL service either inherits BaseService or has documented orchestration-exception status.
```

---

## Phase 3 — Create new domain-first contracts

Create or redesign:

```text
ICustomerService
IPropertyService
IUnitService
IResidentService
ILeaseService
ITicketService
IManagementCompanyService
ICompanyMembershipService
IOnboardingService
IWorkspaceService
```

Do not remove old contracts immediately.

### Acceptance criteria

```text
New contracts expose BLL DTOs/models/results only.
New contracts do not expose DAL DTOs.
New contracts are API-ready.
Aggregate-backed contracts are intended for BaseService-backed implementations.
```

---

## Phase 4 — Build BaseService-backed domain services

Create implementation services:

```text
CustomerService : BaseService<CustomerBllDto, CustomerDalDto, ICustomerRepository, IAppUOW>
PropertyService : BaseService<PropertyBllDto, PropertyDalDto, IPropertyRepository, IAppUOW>
UnitService : BaseService<UnitBllDto, UnitDalDto, IUnitRepository, IAppUOW>
ResidentService : BaseService<ResidentBllDto, ResidentDalDto, IResidentRepository, IAppUOW>
LeaseService : BaseService<LeaseBllDto, LeaseDalDto, ILeaseRepository, IAppUOW>
TicketService : BaseService<TicketBllDto, TicketDalDto, ITicketRepository, IAppUOW>
ManagementCompanyService : BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>
ContactService : BaseService<ContactBllDto, ContactDalDto, IContactRepository, IAppUOW> if exposed/needed
VendorService : BaseService<VendorBllDto, VendorDalDto, IVendorRepository, IAppUOW> if exposed/needed
```

For `CompanyMembershipService`, either:

```text
BaseService-backed using real CompanyMembership DTO/repository
```

or:

```text
documented orchestration exception
```

For `OnboardingService` and `WorkspaceService`, either:

```text
documented orchestration exception
```

or:

```text
documented compromise using closest aggregate
```

### Acceptance criteria

```text
Aggregate-backed services inherit BaseService.
Workflow methods are added on top of BaseService.
Old services can remain internally during transition.
No behavior intentionally changed.
```

---

## Phase 5 — Update IAppBLL facade

Replace granular properties with:

```text
Customers
Properties
Units
Residents
Leases
Tickets
ManagementCompanies
Memberships
Onboarding
Workspaces
```

### Acceptance criteria

```text
IAppBLL surface area reduced.
AppBLL composes domain services.
Old granular service properties removed from IAppBLL or marked obsolete during transition.
BLL facade is closer to Akaver style.
```

---

## Phase 6 — DTO audit and trivial command/query removal

For each command/query DTO, answer:

```text
Does it express a workflow?
Does it contain non-entity fields?
Does it contain actor/scope fields only?
Can actor/scope be method parameters or reusable context model?
Can entity fields be represented by canonical BLL DTO?
Would replacement harm readability/API-readiness?
```

Replace trivial CRUD wrappers with canonical BLL DTOs where appropriate.

Likely candidate groups:

```text
Customers simple create/update
Properties simple create/update
Residents simple create/update
Units simple create/update
```

Keep workflow-heavy DTOs.

### Acceptance criteria

```text
Trivial CRUD command/query DTOs removed or marked justified.
Canonical BLL DTO usage increased.
No useful workflow DTO removed.
No WebApp/API DTO introduced.
```

---

## Phase 7 — Canonical DTO and mapper cleanup

### Tasks

1. Ensure canonical BLL DTOs inherit from `BaseEntity`.
2. Ensure canonical BLL DTOs live in `App.BLL.DTO.*` namespaces.
3. Ensure canonical BLL DTO <-> DAL DTO mappers implement `IBaseMapper`.
4. Fix namespace confusion if DTO files use `App.BLL.Contracts.*` namespaces.
5. Remove duplicated mapping inside services where mapper exists.

### Acceptance criteria

```text
DTO namespaces are clean.
Canonical DTO mapping is consistent.
No DAL DTO leaks into BLL contracts.
BaseService-backed services use canonical mappers.
```

---

## Phase 8 — Internal cleanup of old services/contracts

After new domain-first services are used:

1. Remove old granular contracts from `App.BLL.Contracts` if no external callers need them.
2. Keep implementation helpers internal where practical.
3. Move helper services to internal/domain subfolders if useful.
4. Remove redundant DTOs and mappers.
5. Remove obsolete using statements and namespaces.

### Acceptance criteria

```text
App.BLL.Contracts contains domain-first public BLL contracts.
Implementation can still be internally split.
No dead service contracts remain.
No dead DTOs remain.
IAppBLL exposes only intended domain services.
```

---

## 16. Future WebApp mapper cleanup note

This is not part of the BLL refactor implementation, but it should be kept as a follow-up WebApp cleanup rule.

Recommended direction:

```text
Remove WebApp/Mappers where they only map ViewModel -> BLL DTO/command.
Controllers may construct BLL DTOs/commands directly from ViewModels.
Shared FluentResults-to-ModelState logic should move to MVC extensions/helpers.
Keep mapper-like helpers only when mapping is genuinely complex and reused in several controllers.
```

Example:

```csharp
var dto = new CustomerBllDto
{
    Id = vm.Id,
    Name = vm.Name,
    RegistryCode = vm.RegistryCode
};
```

Do not introduce mapper classes just to hide simple object construction.

---

## 17. Final BLL architecture audit

### Audit checklist

```text
IAppBLL exposes fewer domain-first services.
Every aggregate-backed IAppBLL service inherits BaseService.
Any non-BaseService exposed service is documented as a pure orchestration exception.
AppBLL composes services cleanly.
App.BLL has no App.DAL.EF reference.
App.BLL.Contracts has no DAL DTO exposure.
All BLL service methods return Result/Result<T>, except documented SaveChangesAsync exception if retained.
Repositories do not return FluentResults.
Canonical BLL DTOs inherit BaseEntity.
Canonical BLL DTOs map to canonical DAL DTOs through IBaseMapper mappers.
Trivial CRUD commands/queries are removed or justified.
Canonical BLL DTOs are used as much as possible where they make sense.
Custom DTOs are not kept when they are only overengineered duplicates of canonical DTOs.
Workflow DTOs remain where justified.
Workflow methods live inside domain services.
DeleteGuard remains in BLL.
ManagementCompany.DeleteCascadeAsync remains untouched.
BLL remains MVC/API/WebApp-neutral.
```

### Acceptance criteria

```text
Build succeeds.
BLL public surface is smaller.
BLL services are domain-first.
BaseService is the standard foundation for aggregate-backed services.
Future API controllers can call BLL without BLL redesign.
```

---

## 18. Concrete target IAppBLL before/after

### Before

```csharp
public interface IAppBLL : IBaseBLL
{
    IAccountOnboardingService AccountOnboarding { get; }
    IOnboardingCompanyJoinRequestService OnboardingCompanyJoinRequests { get; }
    IWorkspaceContextService WorkspaceContexts { get; }
    IWorkspaceCatalogService WorkspaceCatalog { get; }
    IWorkspaceRedirectService WorkspaceRedirect { get; }
    IContextSelectionService ContextSelection { get; }

    IManagementCompanyProfileService ManagementCompanyProfiles { get; }
    ICompanyMembershipAdminService CompanyMembershipAdmin { get; }

    ICompanyCustomerService CompanyCustomers { get; }
    ICustomerAccessService CustomerAccess { get; }
    ICustomerProfileService CustomerProfiles { get; }
    ICustomerWorkspaceService CustomerWorkspaces { get; }

    IPropertyProfileService PropertyProfiles { get; }
    IPropertyWorkspaceService PropertyWorkspaces { get; }

    IResidentAccessService ResidentAccess { get; }
    IResidentProfileService ResidentProfiles { get; }
    IResidentWorkspaceService ResidentWorkspaces { get; }

    IUnitAccessService UnitAccess { get; }
    IUnitProfileService UnitProfiles { get; }
    IUnitWorkspaceService UnitWorkspaces { get; }

    ILeaseAssignmentService LeaseAssignments { get; }
    ILeaseLookupService LeaseLookups { get; }

    IManagementTicketService ManagementTickets { get; }
}
```

### After

```csharp
public interface IAppBLL : IBaseBLL
{
    ICustomerService Customers { get; }
    IPropertyService Properties { get; }
    IUnitService Units { get; }
    IResidentService Residents { get; }
    ILeaseService Leases { get; }
    ITicketService Tickets { get; }
    IManagementCompanyService ManagementCompanies { get; }
    ICompanyMembershipService Memberships { get; }
    IOnboardingService Onboarding { get; }
    IWorkspaceService Workspaces { get; }
}
```

If additional domains deserve top-level exposure, add only with a clear reason.

---

## 19. Implementation prompt for coding agent

```text
Refactor only the BLL layer toward a cleaner Akaver-like architecture.

Primary goals:
1. Reduce IAppBLL surface area by grouping exposed services domain-first.
2. Every aggregate-backed public domain BLL service must inherit BaseService.
3. Pure orchestration services must be explicitly documented as exceptions.
4. Keep internal service split if useful, but expose fewer top-level facade properties.
5. Use canonical BLL DTOs by default for CRUD/simple entity operations; remove custom command/query DTOs when they only duplicate canonical DTOs and add no real workflow, projection, filtering, validation, or business-context value.

Scope:
- App.BLL.Contracts
- App.BLL.DTO
- App.BLL
- AppBLL
- BLL mappers
- BLL service boundaries
- BaseService inheritance from aggregate-backed services

Out of scope:
- WebApp refactor
- API controller refactor
- tests
- large DAL refactor
- database schema changes
- Admin refactor

Rules:
- BLL uses IAppUOW, not AppDbContext.
- Repositories do not return FluentResults.
- BLL service methods return Result/Result<T>.
- BLL contracts expose only BLL DTOs/models/results.
- No MVC/API/WebApp concepts in BLL.
- Canonical BLL DTOs inherit BaseEntity.
- Canonical BLL DTO <-> DAL DTO mapping uses IBaseMapper.
- BaseService is the standard CRUD foundation for aggregate-backed services.
- Workflow methods are added on top of BaseService in the relevant domain service.
- Internal helpers/policies do not need BaseService.
- Keep workflow DTOs for real workflows.
- Do not remove ManagementCompany.DeleteCascadeAsync.
- Ask permission before changing Base/DAL materially.
```

---

## 20. Definition of done

```text
IAppBLL exposes a small domain-first facade.
Every aggregate-backed exposed domain service inherits BaseService.
Any exposed service that does not inherit BaseService has documented orchestration-exception status.
AppBLL composes domain services.
Existing behavior is preserved.
DTO explosion is reduced where DTOs were only simple CRUD wrappers; canonical BLL DTOs are used by default whenever a custom DTO would be overengineering.
Workflow DTOs remain where justified.
Canonical BLL DTO usage is consistent and preferred wherever it makes sense.
BLL contracts are API-ready.
BLL has no WebApp/MVC/API dependencies.
BLL has no App.DAL.EF dependency.
Build succeeds.
```

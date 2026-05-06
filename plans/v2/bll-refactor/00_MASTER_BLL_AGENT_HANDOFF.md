# Master BLL Agent Handoff

Repository: `mamarrr/mamarr-cs-course-project`  
Branch: latest `dev` branch  
Reference style: `mamarrr/akaver-hw-demo` high-level BLL architecture  
Source plan: `BLL_AKAVER_STRUCTURE_REFACTOR_PLAN_v3.md`

---

## 1. Mission

Refactor only the BLL layer toward a cleaner Akaver-like architecture.

The main goals are:

1. Reduce `IAppBLL` surface area by grouping exposed services domain-first.
2. Use `BaseService` as the standard CRUD foundation for every aggregate-backed public domain service.
3. Use canonical BLL DTOs by default for CRUD/simple entity operations.
4. Remove custom command/query DTOs when they only duplicate canonical BLL DTOs and add no real workflow, projection, filtering, validation, or business-context value.
5. Keep workflow behavior explicit inside the relevant domain service instead of exposing separate workflow services through `IAppBLL`.
6. Keep one canonical repository-mutating method per normal CRUD operation, and build projection-returning convenience methods on top of it.
7. Keep BLL API-ready.

---

## 2. Current problem

Current `IAppBLL` exposes many granular services directly:

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

This is functional, but too wide and too workflow/UI-sliced.

Target direction:

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

---

## 3. Hard constraints

Do not refactor these unless explicitly assigned and approved:

```text
WebApp controllers
WebApp route/area/layout/navigation
API controllers
API DTOs
Tests
large DAL refactor
database schema
Admin area
ManagementCompany.DeleteCascadeAsync behavior
```

Do not make non-trivial changes to:

```text
Base projects
DAL projects
```

unless your phase explicitly asks you to propose a change and explain why.

---

## 4. Dependency rules

Allowed:

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

Forbidden:

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

## 5. BaseService / IBaseService readiness rule

Every public domain BLL service exposed from `IAppBLL` must either:

1. inherit `BaseService` using its natural primary aggregate, or
2. be explicitly documented as a pure orchestration exception.

Preferred rule:

```text
Every aggregate-backed public domain BLL service inherits BaseService.
Pure orchestration services may be documented exceptions.
Internal helpers/policies are not public domain services and do not need BaseService.
```

Before aggregate-backed services are implemented, `BaseService` and `IBaseService` must be verified as ready for this plan.

Required readiness shape:

```text
IBaseService.FindAsync returns Result<TEntity>, not Result<TEntity?>.
Not-found is a failed Result, not successful null.
All public IBaseService methods return Result/Result<T>.
Mapper nulls are handled as failed Results, not null-forgiving crashes.
UpdateAsync checks existence before repository update.
RemoveAsync checks existence before repository delete.
Public Add(entity) is removed from IBaseService.
BaseService exposes protected AddCore(entity), not public Add(entity).
BaseService may expose protected AddAndFindCoreAsync(entity, parentId, ct) as a convenience helper.
BaseService uses generic Base errors only.
BaseService does not depend on App-specific typed errors.
BaseService does not authorize users.
BaseService does not resolve route slugs.
BaseService does not perform tenant/membership/role checks.
BaseService does not run delete guards.
BaseService does not contain workflow/business rules.
```

BaseService should be used when:

```text
canonical BLL DTO exists
canonical DAL DTO exists
IBaseMapper exists
IBaseRepository-backed repository exists
the operation is normal CRUD
```

Important distinction:

```text
BaseService inherited public methods are mechanical read/update/delete primitives.
Create is not exposed publicly through IBaseService.
Domain services expose safe contextual create operations on top of protected BaseService add helpers.
```

Example:

```text
Inherited public primitive:
  UpdateAsync(CustomerBllDto dto, Guid parentId, CancellationToken ct)

Protected add helper:
  AddCore(CustomerBllDto dto)

Safe app operation:
  CreateAsync(CustomerRoute route, CustomerBllDto dto, CancellationToken ct)
```

The safe app operation resolves trusted scope, authorizes access, sets server-owned fields, validates business rules, and only then calls `AddCore` or `AddAndFindCoreAsync`.

Public `Add(TEntity entity)` must be removed from `IBaseService` because generic create is almost never tenant/actor-safe in this app. Keep protected add helpers inside `BaseService` so aggregate-backed domain services can still reuse mapping/repository add logic.

BaseService must not contain app-specific workflow rules. Concrete services add those on top.

---

## 6. Canonical DTO first rule

Use canonical BLL DTOs as much as possible when the operation is normal CRUD or simple entity state transfer.

Do not create or keep custom command/query/model DTOs when they only duplicate a canonical BLL DTO and add no meaningful workflow, validation, projection, filtering, or business context.

Custom DTOs are justified for:

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

If a custom DTO exists only because a specific page/action originally had its own DTO, prefer:

```text
canonical BLL DTO + explicit method parameters
```

or:

```text
canonical BLL DTO + reusable context/scope model
```

---

## 7. Trusted scope + canonical DTO rule

Canonical BLL DTOs represent entity state only.

Do not put actor identity, route slugs, cookies, permissions, or trusted tenant scope into canonical BLL DTOs.

For public app operations that require authorization or tenant safety, use:

```text
route request model + canonical BLL DTO
```

or, inside BLL after resolution:

```text
trusted scope model + canonical BLL DTO
```

Route slugs are untrusted external identifiers. BLL services may accept them as lookup input, but BLL must resolve them into trusted scope objects containing IDs, membership/role/capabilities, and parent-resource relationships before calling BaseService CRUD.

This turns current command DTOs such as:

```text
UserId + CompanySlug + CustomerSlug + duplicated entity fields
```

into:

```text
CustomerRoute + CustomerBllDto
```

or:

```text
CustomerScope + CustomerBllDto
```

BaseService handles mechanical CRUD only after the domain service has:

```text
resolved route/natural keys into trusted IDs
authorized actor access
validated business rules
set server-owned fields
checked duplicate business keys
called delete guards where needed
```

This rule preserves tenant safety while still using canonical DTOs as much as possible.


---

## 8. Canonical CRUD method rule

For each aggregate service, define one canonical repository-mutating method for each normal CRUD operation.

The canonical CRUD/mutation method should:

```text
use route/scope + canonical BLL DTO where context is needed
perform the repository-changing work
own validation, scope resolution, server-owned-field setup, and save/reload behavior for that mutation
prefer returning the canonical BLL DTO
```

If a caller needs a projection/read model after the mutation, add a separate composition method such as:

```text
CreateAndGetProfileAsync
UpdateAndGetProfileAsync
DeleteAndGetListAsync if actually needed
```

That projection-returning method must call the canonical CRUD/mutation method and then load/build the projection. It must not duplicate repository-changing logic.

Example preferred shape:

```csharp
Task<Result<UnitBllDto>> CreateAsync(
    PropertyRoute route,
    UnitBllDto dto,
    CancellationToken cancellationToken = default);

Task<Result<UnitProfileModel>> CreateAndGetProfileAsync(
    PropertyRoute route,
    UnitBllDto dto,
    CancellationToken cancellationToken = default);
```

`CreateAndGetProfileAsync` calls `CreateAsync`, then calls `GetProfileAsync` or another read/projection method.

Pure CRUD-shaped methods should prefer returning canonical BLL DTOs. Projection models may be returned by dedicated use-case methods, but they must compose the canonical CRUD method instead of duplicating the mutation.

Workflow operations are allowed to have separate methods when they represent real business actions rather than duplicate CRUD.


---

## 9. Result and error rules

All BLL service methods should return:

```text
Result
Result<T>
```

Known exception:

```text
IBaseBLL.SaveChangesAsync may remain Task<int> unless a separate Base change is approved.
```

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

Repositories must not return `FluentResults`.

---

## 10. Service targets

Aggregate-backed services should inherit `BaseService`:

```text
CustomerService
PropertyService
UnitService
ResidentService
LeaseService
TicketService
ManagementCompanyService
ContactService if exposed/needed
VendorService if exposed/needed
CompanyMembershipService if a real membership aggregate/repository exists
```

Potential documented orchestration exceptions:

```text
WorkspaceService
OnboardingService
CompanyMembershipService if no natural membership aggregate/repository exists
```

Internal helpers/policies do not need BaseService:

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
small pure utility classes
```

---

## 11. Agent workflow rules

Each agent should receive:

1. this master handoff,
2. exactly one phase file,
3. the current branch state,
4. permission boundaries.

Each agent should produce:

```text
summary of changes
files changed
build status
risks / TODOs
handoff notes for next phase
explicit list of assumptions
```

Agents must not start later phases early unless the phase file explicitly allows preparatory work.

---

## 12. Final definition of done

```text
IAppBLL exposes a small domain-first facade.
Every aggregate-backed exposed domain service inherits BaseService.
BaseService/IBaseService readiness requirements are implemented and verified before domain service implementation.
Any exposed service that does not inherit BaseService has documented orchestration-exception status.
AppBLL composes domain services.
Existing behavior is preserved.
DTO explosion is reduced where DTOs were only simple CRUD wrappers.
Canonical BLL DTOs are used by default whenever a custom DTO would be overengineering.
Each normal CRUD operation has one canonical repository-mutating method; projection-returning methods compose it instead of duplicating mutation logic.
Trusted route/scope models carry actor, tenant, route, parent-resource, and permission context separately from canonical DTOs.
Workflow DTOs remain where justified.
BLL contracts are API-ready.
BLL has no WebApp/MVC/API dependencies.
BLL has no App.DAL.EF dependency.
Build succeeds.
```

## More rules

- If you want to build the solution/project, ask the user to do it manually and the user will give you build results
- When a phase is done, ask the user to git commit and give a recommended git message

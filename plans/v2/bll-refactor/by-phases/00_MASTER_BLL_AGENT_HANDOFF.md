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
6. Keep BLL API-ready.

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

## 5. BaseService rule

Every public domain BLL service exposed from `IAppBLL` must either:

1. inherit `BaseService` using its natural primary aggregate, or
2. be explicitly documented as a pure orchestration exception.

Preferred rule:

```text
Every aggregate-backed public domain BLL service inherits BaseService.
Pure orchestration services may be documented exceptions.
Internal helpers/policies are not public domain services and do not need BaseService.
```

BaseService should be used when:

```text
canonical BLL DTO exists
canonical DAL DTO exists
IBaseMapper exists
IBaseRepository-backed repository exists
the operation is normal CRUD
```

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

## 7. Result and error rules

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

## 8. Service targets

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

## 9. Agent workflow rules

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

## 10. Final definition of done

```text
IAppBLL exposes a small domain-first facade.
Every aggregate-backed exposed domain service inherits BaseService.
Any exposed service that does not inherit BaseService has documented orchestration-exception status.
AppBLL composes domain services.
Existing behavior is preserved.
DTO explosion is reduced where DTOs were only simple CRUD wrappers.
Canonical BLL DTOs are used by default whenever a custom DTO would be overengineering.
Workflow DTOs remain where justified.
BLL contracts are API-ready.
BLL has no WebApp/MVC/API dependencies.
BLL has no App.DAL.EF dependency.
Build succeeds.
```

## 11. Rules for ai agents

- Do not try to build projects or the solution, ask the user to build the solution and give either the errors or 'OK' if build is okay
- If phase is complete or it is a good build point, then remind the user to git commit and give a recommended commit message, make sure the commits arent either too small or big.
- phase reports should be added to the phase-reports folder

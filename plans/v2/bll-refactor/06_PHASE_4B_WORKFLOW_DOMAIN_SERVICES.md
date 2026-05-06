# Phase 4B Agent Brief — Workflow-Heavy Domain Services

Give this file to the Phase 4B agent together with `00_MASTER_BLL_AGENT_HANDOFF.md`, the Phase 2 BaseService readiness report, the Phase 3 contract report, and the Phase 3.5 trusted scope/context report.

---

## Goal

Build or refactor workflow-heavy domain services while still using `BaseService` for aggregate-backed domains.

This phase covers:

```text
LeaseService
TicketService
ManagementCompanyService
CompanyMembershipService
OnboardingService
WorkspaceService
```

---

## Scope

In scope:

```text
App.BLL.Services.Leases
App.BLL.Services.Tickets
App.BLL.Services.ManagementCompanies
App.BLL.Services.Onboarding
workspace/context BLL services
membership BLL services
related contracts
related DTOs
trusted route/scope model usage
related mappers
IAppBLL facade entries
DI registrations
WebApp MVC callers that currently depend on old granular BLL services
```

Out of scope:

```text
Customer/Property/Unit/Resident core refactor unless needed for integration
REST API rewrite
tests
large DAL changes
database schema
ManagementCompany.DeleteCascadeAsync removal
```

REST API note:

```text
The old API controllers, App.DTO/v1 API DTOs, and WebApp API mappers were intentionally deleted during the 4A flattening work because they need a complete rewrite.
Phase 4B must not rebuild or partially migrate the old REST API surface.
Phase 4B should migrate MVC/WebApp callers only where they currently call old granular BLL services.
```

---

## Flattening directive

Phase 4B should follow the same final-shape strategy used for Phase 4A.

Do not stop at transitional wrapper services if the old services can be removed in the same pass. Build the grouped services as the actual BLL entrypoints, migrate callers, then delete the old granular service contracts/classes and DI registrations.

Target public facade:

```csharp
ILeaseService Leases { get; }
ITicketService Tickets { get; }
IManagementCompanyService ManagementCompanies { get; }
ICompanyMembershipService CompanyMemberships { get; }
IOnboardingService Onboarding { get; }
IWorkspaceService Workspaces { get; }
```

Remove or stop exposing the old facade entries after callers are migrated:

```text
LeaseAssignments
LeaseLookups
ManagementTickets
ManagementCompanyProfiles
CompanyMembershipAdmin
AccountOnboarding
OnboardingCompanyJoinRequests
WorkspaceContexts
WorkspaceCatalog
WorkspaceRedirect
ContextSelection
```

Delete old service contracts and implementations when their behavior is absorbed:

```text
ILeaseAssignmentService / LeaseAssignmentService
ILeaseLookupService / LeaseLookupService
IManagementTicketService / ManagementTicketService
IManagementCompanyProfileService / ManagementCompanyProfileService
ICompanyMembershipAdminService / CompanyMembershipAdminService
IAccountOnboardingService / AccountOnboardingService
IOnboardingCompanyJoinRequestService / OnboardingCompanyJoinRequestService
IWorkspaceContextService / WorkspaceContextService
IWorkspaceCatalogService / UserWorkspaceCatalogService
IWorkspaceRedirectService / WorkspaceRedirectService
IContextSelectionService
```

If one implementation class currently satisfies multiple granular interfaces, replace it with one grouped service contract and one grouped implementation rather than keeping adapter interfaces alive.

---

## Target inheritance

Aggregate-backed services:

```csharp
LeaseService
    : BaseService<LeaseBllDto, LeaseDalDto, ILeaseRepository, IAppUOW>,
      ILeaseService

TicketService
    : BaseService<TicketBllDto, TicketDalDto, ITicketRepository, IAppUOW>,
      ITicketService

ManagementCompanyService
    : BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>,
      IManagementCompanyService
```

CompanyMembershipService:

```text
Use BaseService only if a real CompanyMembershipBllDto, CompanyMembershipDalDto, mapper, and repository exist.
Otherwise document it as an orchestration exception.
```

OnboardingService and WorkspaceService:

```text
Preferred: documented orchestration exceptions.
Alternative: documented compromise using closest aggregate only if explicitly approved.
```

---

## Service ownership

### LeaseService

Absorb/wrap:

```text
ILeaseAssignmentService
ILeaseLookupService
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

Final 4B target:

```text
LeaseService is the only public BLL entrypoint for lease assignment, lease lookup/options, resident lease views, and unit lease views.
MVC callers should use `_bll.Leases`.
Old lease assignment/lookup services should not remain registered or exposed.
```

### TicketService

Absorb/wrap:

```text
IManagementTicketService
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
ticket options
```

Expose as `Tickets`, not `ManagementTickets`.

Final 4B target:

```text
TicketService is the only public BLL entrypoint for management ticket list/search, details, forms, create/update/delete, selector options, and status advancement.
MVC callers should use `_bll.Tickets`.
`ManagementTickets` should be removed from `IAppBLL`.
```

### ManagementCompanyService

Absorb/wrap:

```text
IManagementCompanyProfileService
IManagementCompanyAccessService if present
```

Owns:

```text
management company CRUD/profile
management company access validation
management company profile update
ManagementCompany.DeleteCascadeAsync exception exposure if needed
```

Do not remove `ManagementCompany.DeleteCascadeAsync`.

Final 4B target:

```text
ManagementCompanyService owns profile get/update/delete and management-area access checks that previously lived behind management company profile/access services.
MVC callers should use `_bll.ManagementCompanies`.
```

### CompanyMembershipService

Absorb/wrap:

```text
ICompanyMembershipAdminService
ICompanyMembershipAuthorizationService
ICompanyMembershipCommandService
ICompanyMembershipQueryService
ICompanyOwnershipTransferService
ICompanyRoleOptionsService
ICompanyAccessRequestReviewService
```

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

Final 4B target:

```text
CompanyMembershipService is a documented orchestration exception unless a real CompanyMembership BLL/DAL DTO and repository are introduced.
It should still be the only public BLL entrypoint for membership administration.
MVC callers should use `_bll.CompanyMemberships`.
Old admin/authorization/query/command/role/ownership/access-request service contracts should not remain registered or exposed after migration.
```

### OnboardingService

Absorb/wrap:

```text
IAccountOnboardingService
IOnboardingCompanyJoinRequestService
```

Owns/coordinates:

```text
registration/login-related onboarding commands
create management company during onboarding
create join request during onboarding
complete onboarding
onboarding state
```

Must not depend on ASP.NET Identity UI, HttpContext, cookies, or MVC.

Final 4B target:

```text
OnboardingService is a documented orchestration exception.
It should absorb account onboarding and company join request behavior.
MVC/public onboarding callers should use `_bll.Onboarding`.
Old account onboarding and join-request services should not remain registered or exposed.
```

### WorkspaceService

Absorb/wrap:

```text
IWorkspaceContextService
IWorkspaceCatalogService
IWorkspaceRedirectService
IContextSelectionService
```

Owns:

```text
workspace catalog
workspace context resolution
workspace redirect target
context selection authorization
workspace option models
```

Must remain API-ready:

```text
no cookies
no route data
no HttpContext
no MVC redirect result
only explicit input DTOs/models
```

Final 4B target:

```text
WorkspaceService is a documented orchestration exception.
It should absorb workspace context, catalog, redirect, and context selection behavior.
MVC/UI infrastructure callers should use `_bll.Workspaces`.
Old workspace context/catalog/redirect/context-selection services should not remain registered or exposed.
```

---

## BaseService usage constraints

Inherited BaseService methods are mechanical CRUD primitives.

Public create operations must be implemented on workflow/domain services as contextual `CreateAsync(route/scope, canonicalDto, ct)` methods where create is supported.

Do not expose raw `Add(dto)` as the normal public app create workflow. Public `Add` should no longer exist on `IBaseService`.

For create/update/delete methods that need actor, tenant, route, lifecycle, status, or permission checks, implement contextual wrappers that:

```text
resolve route request into trusted scope
authorize actor access
validate entity state and business rules
set server-owned fields
call BaseService CRUD or protected `AddCore`/`AddAndFindCoreAsync` when the operation is normal CRUD
save/reload where needed
return typed app errors for expected failures
```

Workflow methods may call repositories directly when BaseService is not the right primitive, but normal CRUD parts should still use BaseService where possible.

## Canonical CRUD/projection method rule

For `LeaseService`, `TicketService`, and `ManagementCompanyService`, implement one canonical repository-mutating method per normal CRUD operation when normal CRUD is valid for that aggregate.

The canonical method should prefer returning the canonical BLL DTO.

If a projection/read model is needed after mutation, add a separately named composition method such as:

```csharp
Task<Result<TicketDetailsModel>> CreateAndGetDetailsAsync(...);
Task<Result<TicketDetailsModel>> UpdateAndGetDetailsAsync(...);
```

The projection-returning method must call the canonical create/update method and then load/build the projection. It must not duplicate repository-changing logic.

Workflow methods are allowed to return workflow/projection models directly when they represent real business actions, such as:

```text
ticket status transition
lease assignment
membership role change
ownership transfer
join request review
```

Do not classify a method as workflow merely because a UI page wants a projection back.

---

## DTO rules

Keep workflow DTOs when justified:

```text
lease assignment commands
ticket workflow/status commands
membership commands
ownership transfer commands
join request review commands
onboarding commands
workspace context queries
filtered/search queries
```

Use canonical DTOs for simple CRUD entity payloads where possible.

For CRUD-like methods that also need actor/tenant/route context, use:

```text
Route request model + canonical BLL DTO
```

or internally:

```text
Trusted scope model + canonical BLL DTO
```

Keep workflow commands where the operation is genuinely not normal CRUD, especially status transitions, lease assignment, membership changes, onboarding, and workspace selection.

---

## Acceptance criteria

```text
Phase 2 has verified BaseService/IBaseService readiness.
Lease/Ticket/ManagementCompany services inherit BaseService.
Public create methods exist on domain services where create is supported.
Each normal CRUD operation has one canonical repository-mutating method that prefers returning the canonical BLL DTO.
Create methods call protected BaseService add helpers only after route/scope authorization and server-owned-field setup.
Projection-returning create/update methods, if needed, call the canonical CRUD method and then load/build the projection.
CompanyMembership inheritance or exception is documented.
Onboarding/Workspace exceptions or compromises are documented.
Workflow behavior is preserved.
CRUD-like create/update methods use route/scope + canonical DTO where possible.
Domain services resolve trusted scope before calling BaseService CRUD.
Domain services set server-owned fields before calling BaseService CRUD.
Workflow DTOs remain where justified.
Trivial CRUD wrapper DTOs are removed or marked justified.
WebApp MVC callers are migrated to the grouped facade services.
Old granular service contracts/classes for absorbed behavior are deleted.
Old granular DI registrations are removed.
IAppBLL exposes grouped Phase 4B services, not old granular Phase 4B entries.
REST API controllers/DTOs/mappers remain deleted and are not rebuilt in this phase.
No App.DAL.EF dependency introduced.
No WebApp/API dependency introduced.
Build status documented.
```

---

## Handoff to next agent

The next agent needs:

```text
new service class names/paths
documented orchestration exceptions
old services deleted, or explicit blocker if any could not be deleted
DTOs removed/kept
MVC callers migrated
facade/DI migration status
compile/build status
known unresolved issues
```

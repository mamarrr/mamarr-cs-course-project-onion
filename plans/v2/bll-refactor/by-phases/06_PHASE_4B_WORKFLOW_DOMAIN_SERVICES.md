# Phase 4B Agent Brief — Workflow-Heavy Domain Services

Give this file to the Phase 4B agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and prior phase reports.

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
related mappers
```

Out of scope:

```text
Customer/Property/Unit/Resident core refactor unless needed for integration
WebApp/API/tests
large DAL changes
database schema
ManagementCompany.DeleteCascadeAsync removal
```

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

Use canonical DTOs for simple CRUD where possible.

---

## Acceptance criteria

```text
Lease/Ticket/ManagementCompany services inherit BaseService.
CompanyMembership inheritance or exception is documented.
Onboarding/Workspace exceptions or compromises are documented.
Workflow behavior is preserved.
Workflow DTOs remain where justified.
Trivial CRUD wrapper DTOs are removed or marked justified.
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
old services still delegated to
old services ready for removal or not
DTOs removed/kept
compile/build status
known unresolved issues
```

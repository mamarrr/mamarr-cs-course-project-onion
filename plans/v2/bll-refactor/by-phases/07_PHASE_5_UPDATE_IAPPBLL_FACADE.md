# Phase 5 Agent Brief — Update IAppBLL Facade

Give this file to the Phase 5 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and prior phase reports.

---

## Goal

Replace the wide granular `IAppBLL` facade with a smaller domain-first facade.

---

## Scope

In scope:

```text
App.BLL.Contracts/IAppBLL.cs
App.BLL/AppBLL.cs
service composition
using statements
obsolete granular facade property removal
compile fixes in BLL caused by facade changes
```

Out of scope:

```text
WebApp controller migration unless explicitly assigned
API controllers
tests
DAL changes
domain service behavior changes
```

---

## Target IAppBLL

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

```text
Lookups
Vendors
Contacts
```

---

## Remove from IAppBLL

Remove or obsolete facade exposure of:

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

Old implementation classes may remain internally if domain services still delegate to them.

---

## Tasks

1. Update `IAppBLL`.
2. Update `AppBLL` lazy fields/properties.
3. Compose domain services.
4. Ensure aggregate-backed services receive repository, UOW, mapper, and needed helpers.
5. Keep orchestration exceptions documented.
6. Remove unused using statements.
7. Build and record compile errors.
8. Do not touch WebApp controllers unless explicitly assigned.

---

## Acceptance criteria

```text
IAppBLL exposes only domain-first services.
AppBLL composes domain services.
Old granular service properties are removed or marked obsolete during transition.
Aggregate-backed services are BaseService-backed.
Orchestration exceptions are documented.
No new architecture violations introduced.
Build status documented.
```

---

## Handoff to next agent

The next agent needs:

```text
new IAppBLL shape
new AppBLL composition pattern
compile errors caused by WebApp callers if any
old granular services still retained internally
old granular services ready for cleanup
```

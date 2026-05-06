# Phase 3 Agent Brief — Domain-First BLL Contracts

Give this file to the Phase 3 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and prior phase reports.

---

## Goal

Create or redesign domain-first BLL service contracts without yet deleting old granular contracts.

---

## Scope

In scope:

```text
App.BLL.Contracts
domain-first service interfaces
contract method grouping
Result/Result<T> signatures
BLL DTO/model usage
```

Out of scope:

```text
major implementation moves
deleting old contracts prematurely
WebApp/API/tests
DAL changes
```

---

## Target contracts

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

Optional only if justified:

```text
ILookupService
IVendorService
IContactService
```

---

## Contract rules

Each contract must:

```text
return Result or Result<T>
expose BLL DTOs/models only
not expose DAL DTOs
not expose WebApp/ViewModels/API DTOs
not expose HttpContext/cookies/route concepts
be future API-ready
use canonical BLL DTOs by default for simple CRUD
keep workflow DTOs only where justified
```

Aggregate-backed contracts should be compatible with `IBaseService<TKey, TEntity>` / `BaseService`.

---

## Suggested method grouping

For each domain service:

```text
canonical CRUD methods
domain list/search methods
profile/dashboard/read methods
access/context methods
workflow methods
delete methods
options/lookups if domain-owned
```

Do not create separate top-level facade services for profile/workspace/access if they belong naturally to the domain service.

---

## Deliverable

Update/create contract files under:

```text
App.BLL.Contracts/Customers/ICustomerService.cs
App.BLL.Contracts/Properties/IPropertyService.cs
App.BLL.Contracts/Units/IUnitService.cs
App.BLL.Contracts/Residents/IResidentService.cs
App.BLL.Contracts/Leases/ILeaseService.cs
App.BLL.Contracts/Tickets/ITicketService.cs
App.BLL.Contracts/ManagementCompanies/IManagementCompanyService.cs
App.BLL.Contracts/ManagementCompanies/ICompanyMembershipService.cs
App.BLL.Contracts/Onboarding/IOnboardingService.cs
App.BLL.Contracts/Onboarding/IWorkspaceService.cs
```

Exact folders may follow existing conventions.

---

## Acceptance criteria

```text
New domain-first contracts exist.
New contracts expose BLL DTOs/models/results only.
New contracts do not expose DAL DTOs.
New contracts are API-ready.
Aggregate-backed contracts are intended for BaseService-backed implementations.
Old granular contracts still exist for transition unless safely unused.
Build succeeds or compile errors are limited to expected unimplemented interfaces and documented.
```

---

## Handoff to next agent

The next agent needs:

```text
new contract names and paths
method signatures
old granular contracts mapped to new contracts
known compile gaps
orchestration exceptions
```

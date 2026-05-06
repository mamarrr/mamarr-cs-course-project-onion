# Phase 1 Agent Brief — Detailed BLL Inventory

Give this file to the Phase 1 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and the Phase 0 report.

---

## Goal

Create a detailed inventory of BLL contracts, services, DTOs, mappers, and BaseService adoption targets.

Do not perform the actual refactor yet, except tiny documentation-only cleanup.

---

## Scope

In scope:

```text
App.BLL.Contracts
App.BLL.DTO
App.BLL
mappers
service contracts
service methods
DTO categorization
BaseService candidate analysis
```

Out of scope:

```text
changing public contracts
renaming services
deleting DTOs
moving methods
WebApp/API/tests
```

---

## Tasks

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
could live in aggregate-backed BaseService-derived domain service?
future API-ready?
```

For every BLL DTO, classify:

```text
canonical DTO
workflow command/request
query/filter
projection/read model
options/dropdown model
error
constant/helper
redundant candidate
```

For every mapper, classify:

```text
canonical BLL DTO <-> DAL DTO mapper
workflow/read-model projection mapper
duplicate/unnecessary mapper
missing mapper
```

For every current granular service, classify target owner:

```text
CustomerService
PropertyService
UnitService
ResidentService
LeaseService
TicketService
ManagementCompanyService
CompanyMembershipService
OnboardingService
WorkspaceService
internal helper/policy
delete/remove candidate
```

---

## Deliverable

Create:

```text
plans/bll-refactor/PHASE_1_BLL_INVENTORY.md
```

Include tables/lists for:

```text
Contract method inventory
DTO inventory
Mapper inventory
Service-to-target-domain mapping
BaseService inheritance target list
Orchestration exception candidates
DTO removal/replacement candidates
Risks and unresolved questions
```

---

## Acceptance criteria

```text
Every App.BLL.Contracts method classified.
Every App.BLL.DTO type classified.
Every current service mapped to a target domain service or helper role.
Candidate DTO removals listed but not executed.
Candidate service merges/wrappers listed.
BaseService inheritance targets listed.
Orchestration exception candidates listed.
No behavior changed.
```

---

## Handoff to next agent

The next agent needs:

```text
BaseService adoption candidates
known BaseService issues
DTOs that look redundant
services requiring new domain contracts
orchestration exceptions that need documentation
```

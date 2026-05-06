# Phase 7 Agent Brief — Canonical DTO and Mapper Cleanup

Give this file to the Phase 7 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and prior phase reports.

---

## Goal

Ensure canonical BLL DTOs and BLL mappers are consistent, clean, and compatible with BaseService-backed domain services.

---

## Scope

In scope:

```text
App.BLL.DTO canonical DTOs
App.BLL mappers
mapper namespaces
DTO namespaces
BaseEntity inheritance
IBaseMapper implementation
duplicated mapping inside BLL services
```

Out of scope:

```text
WebApp mapper cleanup
API DTOs
DAL DTO redesign unless tiny safe fix approved
tests
```

---

## Tasks

1. Ensure canonical BLL DTOs inherit from `BaseEntity`.
2. Ensure canonical BLL DTOs live in `App.BLL.DTO.*` namespaces.
3. Fix namespace confusion if DTO files use `App.BLL.Contracts.*`.
4. Ensure canonical BLL DTO <-> DAL DTO mappers implement `IBaseMapper`.
5. Ensure BaseService-backed services receive the correct mapper.
6. Remove duplicated canonical mapping inside services where mapper exists.
7. Do not remove specialized projection mapping where it is actually useful.
8. Build and document issues.

---

## Expected canonical DTOs

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

---

## Acceptance criteria

```text
DTO namespaces are clean.
Canonical BLL DTOs inherit BaseEntity.
Canonical BLL DTO <-> DAL DTO mappers implement IBaseMapper.
BaseService-backed services use canonical mappers.
No DAL DTO leaks into BLL contracts.
No duplicated simple canonical mapping remains where mapper exists.
Build status documented.
```

---

## Handoff to next agent

The next agent needs:

```text
mapper files changed
DTO namespaces changed
services needing using fixes
build status
remaining mapping TODOs
```

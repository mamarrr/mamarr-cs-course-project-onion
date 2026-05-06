# Phase 6 Agent Brief — DTO Audit and Canonical DTO First Cleanup

Give this file to the Phase 6 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and prior phase reports.

---

## Goal

Reduce DTO overengineering by using canonical BLL DTOs wherever custom command/query DTOs add no real architectural value.

---

## Scope

In scope:

```text
App.BLL.DTO
App.BLL.Contracts method signatures where needed
App.BLL service method signatures where needed
BLL mappers where needed
DTO deletion/merge where safe
```

Out of scope:

```text
WebApp ViewModel refactor unless explicitly assigned
API DTOs
tests
large DAL changes
```

---

## Canonical DTO first rule

Use canonical BLL DTOs as much as possible when the operation is normal CRUD or simple entity state transfer.

Do not create or keep custom command/query/model DTOs when they only duplicate a canonical BLL DTO and add no meaningful workflow, validation, projection, filtering, or business context.

A custom DTO is justified for:

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

---

## Aggressive audit candidates

Audit these especially:

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

Replace if they only contain duplicated entity fields plus actor/route/scope data:

```text
entity fields + user id + company/customer/property slug/id
```

Prefer:

```text
Route request model + CanonicalBllDto
```

or internally:

```text
Trusted scope model + CanonicalBllDto
```

---

## Keep DTOs when justified

Keep specialized DTOs for:

```text
lease assignment commands
ticket workflow/status commands
membership administration commands
ownership transfer commands
join request review commands
onboarding account/company/join-request commands
workspace redirect/context-selection queries
filtered ticket/lease search queries
dashboard/profile/list projection models
delete commands with confirmation or actor context
```

---

## Tasks

1. Use Phase 1 inventory to list all DTOs.
2. For every command/query DTO, decide:
   - workflow or CRUD wrapper?
   - fields duplicate canonical DTO?
   - does it add validation/projection/filtering/business context?
   - can actor/scope be represented by a route request model or trusted scope model?
   - can a reusable route/scope model replace repeated scope fields?
3. Replace trivial CRUD wrappers with canonical BLL DTOs.
4. Keep justified workflow/read-model DTOs.
5. Remove dead DTO files.
6. Update contracts/services/mappers accordingly.
7. Build and document issues.

---

## Acceptance criteria

```text
Trivial CRUD command/query DTOs removed or marked justified.
Canonical BLL DTO usage increased.
CRUD-like command DTOs with `UserId + slugs + duplicated entity fields` are replaced by route/scope + canonical DTO where possible.
Custom DTOs are not kept when they are overengineered duplicates.
Workflow DTOs remain where justified.
No WebApp/API DTO introduced.
No DAL DTO leaks into BLL contracts.
Build status documented.
```

---

## Handoff to next agent

The next agent needs:

```text
DTOs removed
DTOs kept and why
method signatures changed
contracts/services affected
compile/build status
WebApp/API callers affected if any
```


---

## Create method impact

Because public `Add(entity)` is removed from `IBaseService`, CRUD-like create commands should be replaced with domain-level contextual create methods.

Convert this pattern:

```text
CreatePropertyCommand
  UserId
  CompanySlug
  CustomerSlug
  property fields duplicated from PropertyBllDto
```

to:

```text
CustomerRoute + PropertyBllDto
```

through:

```csharp
Task<Result<PropertyBllDto>> CreateAsync(
    CustomerRoute route,
    PropertyBllDto dto,
    CancellationToken cancellationToken = default);
```

The domain service then resolves trusted scope and calls protected `AddCore` or `AddAndFindCoreAsync`.

Do not add these route-aware create methods to BaseService.

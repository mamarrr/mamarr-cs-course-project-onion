# Phase 4A Agent Brief — Core BaseService-Backed Domain Services

Give this file to the Phase 4A agent together with `00_MASTER_BLL_AGENT_HANDOFF.md`, the Phase 2 BaseService readiness report, the Phase 3 contract report, and the Phase 3.5 trusted scope/context report.

---

## Goal

Build or refactor the core aggregate-backed domain services so they inherit `BaseService` and absorb/wrap current granular services.

This phase covers:

```text
CustomerService
PropertyService
UnitService
ResidentService
```

---

## Scope

In scope:

```text
App.BLL.Services.Customers
App.BLL.Services.Properties
App.BLL.Services.Units
App.BLL.Services.Residents
related contracts
related mappers
related canonical DTO use
trusted route/scope model usage
delete guard usage
access/profile/workspace method grouping
```

Out of scope:

```text
Leases
Tickets
ManagementCompanies
Memberships
Onboarding
Workspace
WebApp/API/tests
large DAL changes
```

---

## Target inheritance

```csharp
CustomerService
    : BaseService<CustomerBllDto, CustomerDalDto, ICustomerRepository, IAppUOW>,
      ICustomerService

PropertyService
    : BaseService<PropertyBllDto, PropertyDalDto, IPropertyRepository, IAppUOW>,
      IPropertyService

UnitService
    : BaseService<UnitBllDto, UnitDalDto, IUnitRepository, IAppUOW>,
      IUnitService

ResidentService
    : BaseService<ResidentBllDto, ResidentDalDto, IResidentRepository, IAppUOW>,
      IResidentService
```

Use exact generic overloads and repository property names from the actual codebase.

---

## Service ownership

### CustomerService

Absorb/wrap:

```text
ICompanyCustomerService
ICustomerAccessService
ICustomerProfileService
ICustomerWorkspaceService
```

Owns:

```text
canonical customer CRUD
company customer list/create
customer profile get/update/delete
customer workspace/dashboard reads
customer access validation
customer delete guard usage
```

### PropertyService

Absorb/wrap:

```text
IPropertyProfileService
IPropertyWorkspaceService
```

Owns:

```text
canonical property CRUD
property list/create under customer
property profile get/update/delete
property workspace/dashboard reads
property access validation
property delete guard usage
```

### UnitService

Absorb/wrap:

```text
IUnitAccessService
IUnitProfileService
IUnitWorkspaceService
```

Owns:

```text
canonical unit CRUD
unit list/create under property
unit profile get/update/delete
unit workspace/dashboard reads
unit access validation
unit delete guard usage
unit tenant/lease summaries
```

### ResidentService

Absorb/wrap:

```text
IResidentAccessService
IResidentProfileService
IResidentWorkspaceService
```

Owns:

```text
canonical resident CRUD
resident list/search
resident profile get/update/delete
resident workspace/dashboard reads
resident access validation
resident lease/contact summaries
```

---


## BaseService usage constraints

Inherited BaseService methods are mechanical CRUD primitives.

Public create operations must be implemented on domain services as contextual `CreateAsync(route/scope, canonicalDto, ct)` methods.

Do not expose raw `Add(dto)` as the normal public app create workflow. Public `Add` should no longer exist on `IBaseService`.

For create/update/delete methods that need actor, tenant, route, or permission checks, implement contextual wrappers that:

```text
resolve route request into trusted scope
authorize actor access
validate entity state and business rules
set server-owned fields
call BaseService CRUD or protected `AddCore`/`AddAndFindCoreAsync` for create
save/reload where needed
return typed app errors for expected failures
```


## DTO rules

Use canonical DTOs as entity payloads by default:

```text
CustomerBllDto
PropertyBllDto
UnitBllDto
ResidentBllDto
```

Remove or mark redundant command/query DTOs only when they merely duplicate canonical DTOs and add no workflow/projection value.

For CRUD-like methods that need actor/tenant/route context, use:

```text
Route request model + canonical BLL DTO
```

or internally:

```text
Trusted scope model + canonical BLL DTO
```

Do not use command DTOs that merely combine `UserId + slugs + duplicated entity fields`.

Do not delete workflow/read-model DTOs that are genuinely needed.

---

## Transitional strategy

It is acceptable to create new domain services that delegate to current granular services first.

Example:

```text
CustomerService : BaseService<...>, ICustomerService
  delegates to CompanyCustomerService
  delegates to CustomerAccessService
  delegates to CustomerProfileService
  delegates to CustomerWorkspaceService
```

Then remove direct exposure of old services later.

---

## Acceptance criteria

```text
Phase 2 has verified BaseService/IBaseService readiness.
Core domain services exist and inherit BaseService.
Public create methods exist on the domain services where create is supported.
Create methods call protected BaseService add helpers only after route/scope authorization and server-owned-field setup.
Create/update methods use canonical BLL DTOs as entity payloads.
Actor/tenant/route context is carried separately through route/scope models.
Domain services resolve trusted scope before calling BaseService CRUD.
Domain services set server-owned fields before calling BaseService CRUD.
Core domain contracts are implemented.
Existing behavior is preserved.
Canonical DTOs are used for simple CRUD.
Workflow/read-model DTOs are preserved when justified.
Delete guard remains used for blocked deletes.
No App.DAL.EF dependency introduced.
No WebApp/API dependency introduced.
Build status documented.
```

---

## Handoff to next agent

The next agent needs:

```text
new service class names/paths
constructor dependencies
old services still delegated to
old services ready for removal or not
DTOs removed/kept
compile/build status
known unresolved issues
```

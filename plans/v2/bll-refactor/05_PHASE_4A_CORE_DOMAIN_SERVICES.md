# Phase 4A Agent Brief — Core BaseService-Backed Domain Services

Give this file to the Phase 4A agent together with `00_MASTER_BLL_AGENT_HANDOFF.md` and prior phase reports.

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

## DTO rules

Use canonical DTOs by default:

```text
CustomerBllDto
PropertyBllDto
UnitBllDto
ResidentBllDto
```

Remove or mark redundant command/query DTOs only when they merely duplicate canonical DTOs and add no workflow/projection value.

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
Core domain services exist and inherit BaseService.
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

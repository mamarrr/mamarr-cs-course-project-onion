# Phase 3.5 Agent Brief — Trusted Scope and Context Model Standardization

Give this file to the Phase 3.5 agent together with `00_MASTER_BLL_AGENT_HANDOFF.md`, Phase 0 report, Phase 1 inventory, Phase 2 BaseService decisions, and Phase 3 contract report.

---

## Goal

Standardize how BLL carries actor, tenant, route, parent-resource, and permission context while still using canonical BLL DTOs as the entity payload.

This phase exists because canonical BLL DTOs are good for entity state, but public app operations also need trusted context before BaseService CRUD is safe.

---

## Core rule

Use three distinct concepts:

```text
Route request model
  untrusted external input: app user id + route slugs/natural keys

Trusted scope model
  BLL-resolved context: actor id + tenant/resource ids + membership/role/capabilities + parent relationships

Canonical BLL DTO
  entity state only
```

Do not put actor identity, route slugs, cookies, permissions, or trusted tenant scope into canonical BLL DTOs.

Do not trust tenant or parent IDs coming from the client inside a canonical DTO. Domain services must resolve trusted scope and set server-owned fields before calling BaseService CRUD.

---

## Scope

In scope:

```text
App.BLL.DTO/Common/Scopes or equivalent folder
route request models
trusted scope models
naming conventions for route/scope models
contract recommendations for route/scope + canonical DTO signatures
scope resolver interface recommendations if useful
documentation of which current command DTOs should become route/scope + canonical DTO
```

Out of scope:

```text
implementing all domain services
rewriting WebApp controllers
API controllers
tests
large DAL changes
schema changes
```

---

## Suggested route request models

Create only the models actually needed by the current contracts/services.

Possible examples:

```csharp
public sealed record CompanyRoute(
    Guid AppUserId,
    string CompanySlug);
```

```csharp
public sealed record CustomerRoute(
    Guid AppUserId,
    string CompanySlug,
    string CustomerSlug);
```

```csharp
public sealed record PropertyRoute(
    Guid AppUserId,
    string CompanySlug,
    string PropertySlug);
```

```csharp
public sealed record UnitRoute(
    Guid AppUserId,
    string CompanySlug,
    string UnitSlug);
```

```csharp
public sealed record ResidentRoute(
    Guid AppUserId,
    string CompanySlug,
    string ResidentIdCode);
```

```csharp
public sealed record TicketRoute(
    Guid AppUserId,
    string CompanySlug,
    Guid TicketId);
```

Use slugs/natural keys as lookup input only. They are not authority.

---

## Suggested trusted scope models

Create only the models actually needed by the current contracts/services.

Possible examples:

```csharp
public sealed record CompanyScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    string CompanySlug,
    CompanyMembershipContext Membership);
```

```csharp
public sealed record CustomerScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    string CompanySlug,
    string CustomerSlug,
    CompanyMembershipContext Membership);
```

```csharp
public sealed record PropertyScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    Guid PropertyId,
    string CompanySlug,
    string PropertySlug,
    CompanyMembershipContext Membership);
```

```csharp
public sealed record UnitScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    Guid PropertyId,
    Guid UnitId,
    string CompanySlug,
    string UnitSlug,
    CompanyMembershipContext Membership);
```

```csharp
public sealed record ResidentScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid ResidentId,
    string CompanySlug,
    string ResidentIdCode,
    CompanyMembershipContext? Membership);
```

Exact fields should match actual authorization and repository needs. Avoid adding fields just because they might be useful later.

---

## Optional scope resolver interfaces

If useful, define internal BLL resolver interfaces/classes.

Examples:

```csharp
internal interface ICustomerScopeResolver
{
    Task<Result<CustomerScope>> ResolveAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default);
}
```

```csharp
internal interface IPropertyScopeResolver
{
    Task<Result<PropertyScope>> ResolveAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default);
}
```

These should remain internal implementation details unless there is a strong reason to expose them.

Scope resolvers may replace or internalize current access services such as:

```text
CustomerAccessService
ResidentAccessService
UnitAccessService
ManagementCompanyAccessService
```

but do not break existing callers in this phase unless explicitly assigned.

---

## Contract signature guidance

For public domain service methods that are normal CRUD plus tenant/actor safety, prefer:

```csharp
Task<Result<CustomerBllDto>> CreateAsync(
    CompanyRoute route,
    CustomerBllDto dto,
    CancellationToken cancellationToken = default);
```

```csharp
Task<Result<CustomerBllDto>> UpdateAsync(
    CustomerRoute route,
    CustomerBllDto dto,
    CancellationToken cancellationToken = default);
```

```csharp
Task<Result> DeleteAsync(
    CustomerRoute route,
    DeleteConfirmation confirmation,
    CancellationToken cancellationToken = default);
```

For internal methods after scope has already been resolved:

```csharp
Task<Result<CustomerBllDto>> UpdateAsync(
    CustomerScope scope,
    CustomerBllDto dto,
    CancellationToken cancellationToken = default);
```

Use delete confirmation/request DTOs where confirmation text, dependency review, or workflow data is meaningful.

---

## DTO conversion rule

When auditing existing commands, convert this pattern:

```text
UserId + CompanySlug + CustomerSlug + duplicated canonical entity fields
```

to:

```text
CustomerRoute + CustomerBllDto
```

or, after resolution:

```text
CustomerScope + CustomerBllDto
```

Keep command DTOs when they represent true workflow:

```text
ticket status transition
lease assignment
membership role update
ownership transfer
onboarding completion
workspace selection
delete confirmation with business-specific fields
```

---

## Canonical CRUD/projection composition rule

Route/scope models should be used by the canonical CRUD/mutation method.

Example canonical mutation:

```csharp
Task<Result<UnitBllDto>> UpdateAsync(
    UnitRoute route,
    UnitBllDto dto,
    CancellationToken cancellationToken = default);
```

If a projection is needed after mutation, use a composition method:

```csharp
Task<Result<UnitProfileModel>> UpdateAndGetProfileAsync(
    UnitRoute route,
    UnitBllDto dto,
    CancellationToken cancellationToken = default);
```

The composition method calls `UpdateAsync(...)`, then loads the profile projection. It must not repeat repository mutation logic.

This keeps route/scope safety and canonical DTO usage without duplicating create/update workflows.

---

## Service implementation rule for later phases

Domain services must follow this order before calling BaseService CRUD:

```text
1. Validate route request shape.
2. Resolve route/natural keys into trusted scope.
3. Authorize actor using membership/role/capabilities.
4. Validate entity state and business rules.
5. Check duplicate business keys and cross-tenant references.
6. Generate/set server-owned fields such as slug, parent id, status, timestamps.
7. For create, call protected BaseService `AddCore` or `AddAndFindCoreAsync` from the domain service after scope has been resolved. For update/delete/read, call BaseService CRUD using canonical BLL DTO and trusted parent id. Do not treat inherited BaseService methods as complete authorization-safe workflows.
8. Save/reload where needed.
9. Return typed app errors for expected failures.
```

---

## Deliverable

Create or update:

```text
App.BLL.DTO/Common/Scopes/
plans/bll-refactor/PHASE_3_5_TRUSTED_SCOPE_CONTEXT.md
```

The report should include:

```text
route request models added
trusted scope models added
scope resolver recommendations
commands that should become route/scope + canonical DTO
commands that should stay workflow commands
contracts needing signature adjustments
risks and unresolved questions
```

---

## Acceptance criteria

```text
Route models exist for company/customer/property/unit/resident/ticket/lease contexts as needed.
Trusted scope models exist for resolved tenant/resource/member context as needed.
Canonical DTOs do not carry actor identity or route slugs.
Public BLL method guidance does not trust client-provided tenant IDs.
Scope resolvers or resolver recommendations are documented.
Phase 4A/4B agents have a clear route/scope + canonical DTO pattern to implement.
No WebApp/API dependency is introduced.
```

---

## Handoff to Phase 4A/4B agents

The next agents need:

```text
route request model names and paths
trusted scope model names and paths
which current command DTOs should be replaced
which workflow commands should stay
scope resolver recommendations
method signature patterns to implement
```


---

## BaseService boundary reminder

BaseService must not contain route-aware `CreateAsync` overloads such as:

```csharp
Task<Result<PropertyBllDto>> CreateAsync(
    CustomerRoute route,
    PropertyBllDto dto,
    CancellationToken cancellationToken = default);
```

That method belongs on `IPropertyService` / `PropertyService`.

BaseService may only provide generic protected helpers such as:

```csharp
protected Result<TKey> AddCore(TBLLEntity entity);
```

or optionally:

```csharp
protected Task<Result<TBLLEntity>> AddAndFindCoreAsync(
    TBLLEntity entity,
    TKey parentId = default!,
    CancellationToken cancellationToken = default);
```

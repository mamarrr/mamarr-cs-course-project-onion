# Changes in BLL Agent Refactor Plan Split v4

This update changes the plan so `BaseService.Add` does not remain a public mechanical primitive.

## Main architectural update

Public route-aware create methods such as:

```csharp
Task<Result<PropertyBllDto>> CreateAsync(
    CustomerRoute route,
    PropertyBllDto dto,
    CancellationToken cancellationToken = default);
```

belong on app-level domain services such as `IPropertyService`, not on `BaseService`.

`BaseService` should stay generic and app-agnostic. It should not know about `CustomerRoute`, `PropertyBllDto`, route slugs, tenant authorization, memberships, permissions, delete guards, or workflow rules.

## Target Base changes

- Remove `Result<TKey> Add(TEntity entity)` from `IBaseService`.
- Remove public `Add(TBLLEntity entity)` from `BaseService`.
- Add protected `AddCore(TBLLEntity entity)` to `BaseService`.
- Optionally add protected `AddAndFindCoreAsync(...)`.
- Domain services expose contextual `CreateAsync(route/scope, canonicalDto, ct)` methods.

## Files updated

- `00_MASTER_BLL_AGENT_HANDOFF.md`
  - Updated BaseService readiness rule.
  - Added target removal of public Add.
  - Added protected AddCore guidance.

- `03_PHASE_2_BASESERVICE_RULES.md`
  - Rewritten so Phase 2 explicitly implements the Base/IBase Add change.
  - Added target `IBaseService` shape.
  - Added target `AddCore` and optional `AddAndFindCoreAsync`.
  - Added domain create method rule.

- `04_PHASE_3_DOMAIN_FIRST_CONTRACTS.md`
  - Added rule that create methods belong on domain contracts.
  - Clarified that route-aware create methods must not be added to BaseService.

- `04B_PHASE_3_5_TRUSTED_SCOPE_CONTEXT.md`
  - Added BaseService boundary reminder for route-aware create.
  - Clarified create should call protected add helpers after scope resolution.

- `05_PHASE_4A_CORE_DOMAIN_SERVICES.md`
  - Added requirement for public domain create methods.
  - Added protected AddCore/AddAndFindCoreAsync usage.

- `06_PHASE_4B_WORKFLOW_DOMAIN_SERVICES.md`
  - Added same create/add-helper guidance for workflow-heavy services.

- `08_PHASE_6_DTO_AUDIT_CANONICAL_FIRST.md`
  - Added create method impact section.

- `10_PHASE_8_INTERNAL_CLEANUP_AND_FINAL_AUDIT.md`
  - Added final audit items for removal of public Add and domain create methods.

- `12_AGENT_EXECUTION_ORDER.md`
  - Updated Phase 2 description.

- `README.md`
  - Updated to v4.

## Files included unchanged

All original files remain included in the zip, even if they were not modified.

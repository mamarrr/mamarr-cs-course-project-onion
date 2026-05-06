# Changes in BLL Agent Refactor Plan Split v5

This update adds the canonical CRUD method rule.

## Main architectural update

For each aggregate service, define one canonical repository-mutating method for each normal CRUD operation.

The canonical method should:

```text
use route/scope + canonical BLL DTO where context is needed
perform the repository-changing work
prefer returning the canonical BLL DTO
```

If a caller needs a projection/read model after mutation, add a separate composition method such as:

```text
CreateAndGetProfileAsync
UpdateAndGetProfileAsync
CreateAndGetDetailsAsync
UpdateAndGetDetailsAsync
```

That projection-returning method must call the canonical CRUD/mutation method and then load/build the projection. It must not duplicate repository-changing logic.

## Example

Preferred:

```csharp
Task<Result<UnitBllDto>> CreateAsync(
    PropertyRoute route,
    UnitBllDto dto,
    CancellationToken cancellationToken = default);

Task<Result<UnitProfileModel>> CreateAndGetProfileAsync(
    PropertyRoute route,
    UnitBllDto dto,
    CancellationToken cancellationToken = default);
```

Avoid plain CRUD methods that mutate the repository and return a projection directly unless they are deliberately named/documented as projection-returning use-case methods.

## Files updated

- `00_MASTER_BLL_AGENT_HANDOFF.md`
  - Added canonical CRUD method rule.
  - Updated mission and definition of done.

- `03_PHASE_2_BASESERVICE_RULES.md`
  - Added canonical CRUD method impact.

- `04_PHASE_3_DOMAIN_FIRST_CONTRACTS.md`
  - Added contract rule for canonical CRUD methods and projection composition methods.

- `04B_PHASE_3_5_TRUSTED_SCOPE_CONTEXT.md`
  - Added route/scope-aware CRUD/projection composition guidance.

- `05_PHASE_4A_CORE_DOMAIN_SERVICES.md`
  - Added core service implementation rule for canonical CRUD methods and projection composition.

- `06_PHASE_4B_WORKFLOW_DOMAIN_SERVICES.md`
  - Added workflow-heavy service implementation rule for canonical CRUD methods and projection composition.

- `08_PHASE_6_DTO_AUDIT_CANONICAL_FIRST.md`
  - Added return-type/projection cleanup rule.

- `10_PHASE_8_INTERNAL_CLEANUP_AND_FINAL_AUDIT.md`
  - Added final audit items for canonical CRUD methods and projection composition.

- `12_AGENT_EXECUTION_ORDER.md`
  - Added dependency note for enforcing canonical CRUD method rule.

- `README.md`
  - Updated to v5.

## Files included unchanged

All original files remain included in the zip, even if they were not modified.

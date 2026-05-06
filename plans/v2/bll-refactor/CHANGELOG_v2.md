# Changes in BLL Agent Refactor Plan Split v2

This update adds the trusted route/scope context pattern to the existing BLL refactor agent pack.

## Main architectural update

The plan now explicitly distinguishes:

```text
Route request model
  untrusted external input: app user id + route slugs/natural keys

Trusted scope model
  BLL-resolved context: actor id + tenant/resource ids + membership/role/capabilities + parent relationships

Canonical BLL DTO
  entity state only
```

This lets the project keep the canonical DTO first rule without losing tenant/actor/route authorization safety.

## Files updated

- `00_MASTER_BLL_AGENT_HANDOFF.md`
  - Added trusted scope + canonical DTO rule.
  - Updated mission and final definition of done.
  - Clarified that route slugs are lookup inputs, not authorization proof.

- `04_PHASE_3_DOMAIN_FIRST_CONTRACTS.md`
  - Added route/scope + canonical DTO guidance for contract signatures.
  - Added handoff to the new Phase 3.5.

- `05_PHASE_4A_CORE_DOMAIN_SERVICES.md`
  - Added route/scope model usage to core service implementation.
  - Added acceptance criteria for resolving trusted scope before BaseService CRUD.

- `06_PHASE_4B_WORKFLOW_DOMAIN_SERVICES.md`
  - Added route/scope + canonical DTO pattern for workflow-heavy domain services.
  - Clarified when workflow commands should remain.

- `08_PHASE_6_DTO_AUDIT_CANONICAL_FIRST.md`
  - Updated DTO cleanup to replace CRUD-like commands with route/scope + canonical DTO.
  - Added guidance for commands that combine `UserId + slugs + duplicated entity fields`.

- `10_PHASE_8_INTERNAL_CLEANUP_AND_FINAL_AUDIT.md`
  - Added trusted scope/context audit criteria.

- `12_AGENT_EXECUTION_ORDER.md`
  - Added Phase 3.5 to the execution order.
  - Updated parallelization/dependency guidance.

- `README.md`
  - Updated file list and usage note.

## New file added

- `04B_PHASE_3_5_TRUSTED_SCOPE_CONTEXT.md`
  - New dedicated phase for route request models, trusted scope models, optional scope resolvers, and contract signature guidance.

## Files included unchanged

All original phase files are still included in the zip, even if unchanged.

# BLL Agent Refactor Plan Split v4

This folder contains a master handoff and phase-specific agent briefs.

Recommended usage: give every agent `00_MASTER_BLL_AGENT_HANDOFF.md` plus the single phase file they are implementing.

Important v4 update: Phase 2 now explicitly removes public `Add(entity)` from `IBaseService`, replaces public `BaseService.Add` with protected `AddCore`, and requires route/scope-aware create methods to live on domain services.

## Files

- `00_MASTER_BLL_AGENT_HANDOFF.md`
- `01_PHASE_0_BASELINE_AND_GUARDRAILS.md`
- `02_PHASE_1_BLL_INVENTORY.md`
- `03_PHASE_2_BASESERVICE_RULES.md`
- `04B_PHASE_3_5_TRUSTED_SCOPE_CONTEXT.md`
- `04_PHASE_3_DOMAIN_FIRST_CONTRACTS.md`
- `05_PHASE_4A_CORE_DOMAIN_SERVICES.md`
- `06_PHASE_4B_WORKFLOW_DOMAIN_SERVICES.md`
- `07_PHASE_5_UPDATE_IAPPBLL_FACADE.md`
- `08_PHASE_6_DTO_AUDIT_CANONICAL_FIRST.md`
- `09_PHASE_7_CANONICAL_DTO_MAPPER_CLEANUP.md`
- `10_PHASE_8_INTERNAL_CLEANUP_AND_FINAL_AUDIT.md`
- `11_FOLLOWUP_WEBAPP_MAPPER_CLEANUP_NOTE.md`
- `12_AGENT_EXECUTION_ORDER.md`
- `CHANGELOG_v2.md`
- `CHANGELOG_v3.md`

# Recommended Agent Execution Order

Give every agent:

1. `00_MASTER_BLL_AGENT_HANDOFF.md`
2. the relevant phase file
3. all previous phase reports
4. the latest branch state

---

## Sequential order

```text
Phase 0  -> Baseline and guardrails
Phase 1  -> Detailed BLL inventory
Phase 2  -> BaseService rules/cleanup decision
Phase 3  -> Domain-first contracts
Phase 4A -> Core domain services
Phase 4B -> Workflow-heavy domain services
Phase 5  -> Update IAppBLL facade
Phase 6  -> DTO audit and canonical DTO first cleanup
Phase 7  -> Canonical DTO and mapper cleanup
Phase 8  -> Internal cleanup and final BLL audit
```

---

## Parallelization guidance

Safe after Phase 3:

```text
Phase 4A and Phase 4B can run in parallel if they coordinate shared contracts and AppBLL changes carefully.
```

Recommended dependency:

```text
Phase 5 should wait until Phase 4A and Phase 4B have created the new domain services.
```

Phase 6 and Phase 7 can partially overlap, but this is riskier because DTO and mapper changes often touch the same files.

---

## Handoff format for every agent

Each agent should end with:

```text
Summary
Files changed
Build status
What was intentionally not changed
Assumptions made
Risks / TODOs
Next phase handoff notes
Questions requiring owner decision
```

---

## Stop-and-ask conditions

Agents must stop and ask before:

```text
changing Base projects materially
changing DAL projects materially
changing schema
removing ManagementCompany.DeleteCascadeAsync
adding API DTOs
refactoring API controllers
refactoring WebApp controllers
changing tests
making BLL depend on WebApp/MVC/API concepts
```

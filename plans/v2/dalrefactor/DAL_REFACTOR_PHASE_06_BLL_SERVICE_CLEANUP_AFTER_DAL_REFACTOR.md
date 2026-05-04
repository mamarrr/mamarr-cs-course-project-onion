# DAL Refactor Phase 6: BLL Service Cleanup After DAL Refactor

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goals

- Update BLL services to use the cleaned repository boundaries.
- Keep validation decisions in BLL.
- Ensure delete methods use the delete guard before deleting.
- Prepare for later use of `BaseService` where appropriate.

## Tasks

1. Update services that currently call old repository methods.
   - Replace calls to moved methods with calls to the owning repositories.
   - Example: lease services should use resident/property/unit repositories for existence checks.

2. Move validation flow to BLL where missing.
   - Repositories answer factual questions.
   - BLL decides whether to return validation, not-found, forbidden, or business-rule errors.

3. Use the correct repository for each factual predicate.
   - Resident checks through `UOW.Residents`.
   - Property checks through `UOW.Properties`.
   - Unit checks through `UOW.Units`.
   - Lease checks through `UOW.Leases`.
   - Lookup/role checks through `UOW.Lookups`.

4. Update delete flows.
   - BLL delete methods should call the delete guard before deleting.
   - If dependencies exist, return a blocked-delete business error.
   - If allowed, call the owning repository simple delete method.
   - Do not call old cascade delete repository methods.

5. Keep app-specific service methods that return `FluentResults` and validation errors.

6. Do not force domain-specific services into `BaseService` if they are workflow-heavy.

## BaseService usage guidance

Use `BaseService` only for simple CRUD-style BLL services.

Do not force these into `BaseService` yet:

- onboarding services
- lease assignment workflows
- ticket workflows
- profile services with access checks
- membership administration
- delete guard or delete policy services

Potential future simple candidates:

- lookup-like services
- simple admin CRUD screens
- small aggregate services without complex validation/access logic

## Out of scope

- Do not rewrite controllers unless required by changed BLL contracts.
- Do not introduce cascade delete orchestration.
- Do not change repository implementations unless required to fix BLL callers.
- Do not redesign all service contracts.

## Acceptance criteria

- BLL services call repository methods from the correct owning repositories.
- BLL delete methods use delete guard for converted entities.
- Old cascade delete repository methods are no longer called by converted BLL services.
- Validation and blocked-delete errors are returned from BLL.
- Build succeeds.

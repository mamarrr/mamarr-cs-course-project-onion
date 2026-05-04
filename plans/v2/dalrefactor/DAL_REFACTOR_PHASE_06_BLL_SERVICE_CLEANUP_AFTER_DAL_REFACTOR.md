# DAL Refactor Phase 6: BLL service cleanup after DAL refactor

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

### Goals

- Update BLL services to use the cleaned repository boundaries.
- Keep validation decisions in BLL.
- Prepare for later use of `BaseService` where appropriate.

### Tasks

1. Update services that currently call old repository methods.
2. Move validation flow to BLL where missing.
3. Use the correct repository for each factual predicate.
4. Keep app-specific service methods that return `FluentResults` and validation errors.
5. Do not force domain-specific services into `BaseService` if they are workflow-heavy.

### BaseService usage guidance

Use `BaseService` only for simple CRUD-style BLL services.

Do not force these into `BaseService` yet:

- onboarding services
- lease assignment workflows
- ticket workflows
- profile services with access checks
- membership administration
- delete orchestration

Potential future simple candidates:

- lookup-like services
- simple admin CRUD screens
- small aggregate services without complex validation/access logic

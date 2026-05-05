# Phase 3 BLL Result Error Subplan

This subplan covers the BLL result-style cleanup slice.

## Goals

- Preserve service contracts.
- Keep normal business outcomes in `FluentResults`.
- Replace plain string `Result.Fail(...)` outcomes with typed BLL errors where the outcome category is clear.
- Avoid API, MVC route, Base, DAL, test, schema, and migration changes.

## Scope

- `AccountOnboardingService`
- `WorkspaceRedirectService`
- `OnboardingCompanyJoinRequestService`

## Error Mapping

- Missing authenticated actor -> `UnauthorizedError`
- Unknown company -> `NotFoundError`
- Invalid role or empty input -> `ValidationAppError`
- Duplicate registry code, membership, or pending request -> `ConflictError`
- Missing seeded initial management role -> `BusinessRuleError`
- Unauthorized workspace selection -> `ForbiddenError`

## Build Checkpoint

After this slice, the project owner should run:

```powershell
dotnet build mamarrproject.sln -nologo
```


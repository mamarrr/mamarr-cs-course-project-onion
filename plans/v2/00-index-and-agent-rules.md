# Refactor Agent Plan Index

This folder contains separate implementation plans for the vertical-slice refactor of the Mamarr CS Course Project.

Use these files in order. Do not give an AI coding agent the whole refactor at once. Give the agent one file at a time, and require it to stop when that slice is complete.

## Execution order

1. `01-foundation-architecture.md`
2. `02-lookup-reference-data.md`
3. `03-customer-profile.md`
4. `04-customer-workspace-company-customers.md`
5. `05-property-profile-workspace.md`
6. `06-unit-profile-workspace.md`
7. `07-resident-workspace-profile.md`
8. `08-lease-assignments.md`
9. `09-management-company-membership-join-requests.md`
10. `10-onboarding.md`
11. `11-final-cleanup.md`

## Global architecture decisions

### Repositories return DAL DTOs

Repositories should return DAL DTOs, not domain entities.

```text
Repository -> DAL DTO -> BLL mapper -> BLL model -> Web mapper -> API DTO / ViewModel
```

### Repository inheritance and BaseRepository usage

Use the existing `BaseRepository` wherever possible for custom EF repositories. It already contains regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer.

Custom repositories should inherit from `BaseRepository` and add only entity/use-case-specific query methods or workflow methods that are not already covered by the base implementation. Repository interfaces should inherit from the existing `IBaseRepository` when normal CRUD operations are needed.

Do not reimplement generic CRUD behavior such as add, update, remove, find, or list unless `BaseRepository` cannot support the entity or use case. Match the actual generic parameters and mapper pattern already used in the codebase.

### Transactions are owned by Unit of Work

Transaction handling belongs directly to `IAppUOW` and `AppUOW`.

Do not create a separate transaction abstraction. `AppUOW` may privately hold EF Core transaction state, but `IDbContextTransaction` must not leave `App.DAL.EF`.

### FluentResults belongs at the BLL boundary

Use FluentResults for BLL service results and BLL-internal application flow where useful.

Do not use FluentResults in repository interfaces or UOW methods by default.

DAL communicates with BLL using DAL DTOs, nullable DTOs, booleans, lists, IDs, and normal exceptions for unexpected infrastructure failures.

Repositories should not decide business meaning such as `NotFound`, `Forbidden`, `Conflict`, or `Validation`.

### App.DTO is API-only

BLL must not accept or return `App.DTO` types. BLL uses commands, queries, and models from `App.BLL.Contracts`.

## Out of scope for every plan

- Adding new user-facing features.
- Implementing ticket functionality.
- Implementing vendor functionality.
- Implementing scheduled work functionality.
- Implementing work-log functionality.
- Large-scale domain redesign.
- Adding a test suite as part of this refactor plan.

## Agent operating rules

Before editing:

1. Inspect the files listed in the slice plan.
2. Identify existing routes, response DTOs, ViewModels, services, and behavior.
3. Keep changes limited to the slice.
4. Do not refactor unrelated modules.
5. Do not rename routes/controllers/actions unless the slice explicitly says so.
6. Do not change seeded IDs or domain data unless needed for compilation and existing behavior.

After editing:

1. Run `dotnet build`.
2. Fix compile errors caused by the slice.
3. Stop. Do not proceed to the next slice.

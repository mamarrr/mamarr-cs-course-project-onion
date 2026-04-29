# Mamarr CS Course Project Refactor Agent Plan

This file is one implementation slice of the larger refactor plan. It is written to be handed to an AI coding agent as a focused task.

Global constraints for every slice:

- Preserve existing user-facing behavior.
- Do not add new features.
- Do not implement ticket, vendor, scheduled work, or work-log functionality.
- Do not change API routes unless explicitly required and documented.
- Do not intentionally change JSON response shapes.
- Do not intentionally change MVC page behavior.
- Do not introduce FluentResults into DAL repository interfaces or UOW methods.
- Repositories return DAL DTOs, nullable DAL DTOs, booleans, lists, IDs, or throw unexpected infrastructure/programming exceptions.
- Use the existing `BaseRepository` for custom EF repositories whenever possible, because it already contains regular CRUD operations and built-in IDOR/parent-scope restrictions for entities that have a parent management company or customer.
- Custom repositories should inherit from `BaseRepository` when the entity can use the generic CRUD/mapping behavior. Add only query-specific or workflow-specific methods on top of it.
- Do not reimplement regular CRUD methods such as add, update, remove, find, or list in custom repositories unless the current `BaseRepository` cannot support that entity/use case.
- BLL service boundaries return `FluentResults.Result` or `FluentResults.Result<T>`.
- `App.DTO` remains API-only and must not be referenced by BLL.
- BLL must not reference `App.DAL.EF` or `AppDbContext`.
- Controllers must not use repositories or `AppDbContext`.
- Dependency registration helper methods live in `WebApp/Helpers`, not in `App.BLL` or `App.DAL.EF`.
- Build the solution after the slice changes and fix compile errors caused by the slice.

# Slice 8: Onboarding

## Goal

Refactor onboarding functionality while preserving current behavior.

Onboarding should remain a use-case module, not generic CRUD.

## Scope

Refactor existing onboarding flows only:

- Account onboarding state.
- Company selection.
- Workspace selection.
- Join request creation as part of onboarding, if currently implemented.
- Initial redirect/context flow.
- Regular MVC onboarding controller.
- Onboarding API controllers, if present.

Do not add new onboarding steps, screens, or endpoints.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/Controllers/OnboardingController.cs`
- `WebApp/ApiControllers/Onboarding/**`
- Onboarding views/ViewModels in `WebApp`
- Current BLL services under `App.BLL/Onboarding/**`
- Management company/join request services from previous slice
- Current onboarding DTOs in `App.DTO`, if API exists
- `App.Domain/**/ManagementCompany.cs`
- `App.Domain/**/ManagementCompanyJoinRequest.cs`
- `App.Domain/**/ManagementCompanyJoinRequestStatus.cs`
- `App.Domain/**/Customer.cs`
- `App.Domain/**/Resident.cs`
- `App.DAL.EF/AppDbContext.cs`
- `IAppUOW` / `AppUOW`

## Allowed files/folders to create or modify

- `App.BLL.Contracts/Onboarding/**`
- `App.BLL/Onboarding/**`
- `App.BLL/Mappers/Onboarding/**`
- Onboarding-related `App.Contracts/DAL/**` only if existing repositories need new methods
- Onboarding-related WebApp mappers
- `WebApp/Controllers/OnboardingController.cs`
- `WebApp/ApiControllers/Onboarding/**`
- Onboarding ViewModels, if needed
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Avoid creating new repositories if existing repositories from previous slices already cover the required data.

## Current behavior inventory

Before editing, identify:

- Onboarding MVC routes.
- Onboarding API routes, if present.
- Existing redirect flow.
- Existing view model fields.
- Existing session/temp-data/context behavior.
- Existing user/company/workspace selection behavior.
- Existing join request behavior.
- Existing status codes/messages/errors.
- Existing use of `ManagementCompanyJoinRequestStatus`.

Preserve all behavior.

## BLL contracts

Create:

```text
App.BLL.Contracts/Onboarding/
  Services/
    IAccountOnboardingService.cs
    IContextSelectionService.cs
    IWorkspaceCatalogService.cs
  Queries/
    GetOnboardingStateQuery.cs
    GetWorkspaceCatalogQuery.cs
  Commands/
    SelectWorkspaceCommand.cs
    CreateCompanyJoinRequestCommand.cs
    CompleteAccountOnboardingCommand.cs
  Models/
    OnboardingStateModel.cs
    WorkspaceCatalogModel.cs
    WorkspaceOptionModel.cs
    OnboardingJoinRequestModel.cs
```

Only include contracts for currently implemented flows.

Suggested services:

```csharp
public interface IAccountOnboardingService
{
    Task<Result<OnboardingStateModel>> GetStateAsync(
        GetOnboardingStateQuery query,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteAsync(
        CompleteAccountOnboardingCommand command,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IContextSelectionService
{
    Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
        GetWorkspaceCatalogQuery query,
        CancellationToken cancellationToken = default);

    Task<Result> SelectWorkspaceAsync(
        SelectWorkspaceCommand command,
        CancellationToken cancellationToken = default);
}
```

If join request creation is already handled by `ICompanyJoinRequestService`, reuse that service instead of duplicating behavior in onboarding.

## BLL implementation

Create or refactor:

```text
App.BLL/Onboarding/AccountOnboardingService.cs
App.BLL/Onboarding/ContextSelectionService.cs
App.BLL/Onboarding/WorkspaceCatalogService.cs
App.BLL/Mappers/Onboarding/OnboardingBllMapper.cs
```

Rules:

- Use `IAppUOW` and/or BLL services from previous slices.
- Do not use `AppDbContext`.
- Do not use `App.DTO`.
- Use `ManagementCompanyJoinRequestStatusCodes.Pending` when creating join requests.
- Do not hardcode seeded GUIDs.
- Return FluentResults errors for expected failures.
- Preserve existing redirect/context decisions.

## DAL usage

Prefer using repositories already created:

- `IManagementCompanyRepository`
- `IManagementCompanyJoinRequestRepository`
- `ICustomerRepository`
- `IResidentRepository`
- `ILookupRepository`

Add repository methods only if onboarding currently needs data not yet exposed.

## WebApp MVC mapper

Create:

```text
WebApp/Mappers/Mvc/Onboarding/OnboardingViewModelMapper.cs
```

Responsibilities:

- BLL model -> current onboarding ViewModel.
- Onboarding form ViewModel -> BLL command.

## WebApp API mapper, if API exists

Create:

```text
WebApp/Mappers/Api/Onboarding/OnboardingApiMapper.cs
```

Only if onboarding API controllers currently exist.

## MVC controller refactor

Refactor:

```text
WebApp/Controllers/OnboardingController.cs
```

Rules:

- Controller calls BLL onboarding services.
- Controller maps BLL models to ViewModels.
- Controller maps form posts to BLL commands.
- Controller converts FluentResults errors to current ModelState/redirect behavior.
- Controller does not use repositories or `AppDbContext`.

## API controller refactor, if present

Refactor:

```text
WebApp/ApiControllers/Onboarding/*
```

Rules:

- Preserve routes/auth.
- Use BLL service + API mapper + `ToActionResult`.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register onboarding BLL services.
- Register onboarding WebApp mappers.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when onboarding functionality is refactored and builds.

Do not perform final cleanup beyond files made obsolete by onboarding itself.

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

This slice should replace the current onboarding implementation contracts living inside `App.BLL/Onboarding/**` with contracts in `App.BLL.Contracts/Onboarding/**`, while keeping the implementation in `App.BLL/Onboarding/**`.

## Precondition

Plan 09 Management Company / Membership / Join Requests is implemented.

Before starting this slice, the management-company refactor should already provide the reusable management repositories/services needed by onboarding, especially:

```text
IManagementCompanyRepository
IManagementCompanyJoinRequestRepository
IManagementCompanyAccessService / membership authorization services
ICompanyMembershipAdminService or the split management membership services that replaced it
ManagementCompanyJoinRequestStatusCodes
ILookupRepository / lookup access for roles and join-request statuses
```

Do not rework the management-company, customer, resident, unit, property, or lease slices in this plan. Only make small compatibility changes if onboarding cannot compile against the already-refactored contracts.

If the solution still has active references to old pre-refactor namespaces from earlier slices, fix those as cleanup before starting this onboarding slice.

## Current codebase context

At the time this plan is written, onboarding still has old contracts and models inside `App.BLL/Onboarding/**` rather than `App.BLL.Contracts/Onboarding/**`.

Current onboarding implementation areas include:

```text
App.BLL/Onboarding/Account
App.BLL/Onboarding/Api
App.BLL/Onboarding/CompanyJoinRequests
App.BLL/Onboarding/ContextSelection
App.BLL/Onboarding/WorkspaceCatalog
```

Several current onboarding services still reference `App.DAL.EF`, `AppDbContext`, EF queries, or DAL seeding directly. This slice must remove those dependencies from onboarding BLL services.

`WebApp/Controllers/OnboardingController.cs` still consumes old onboarding service namespaces directly. `WebApp/ApiControllers/Onboarding/OnboardingController.cs`, if implemented, must be checked as well. `WebApp/Helpers/DependencyInjectionHelpers.cs` still registers old onboarding service types and must be updated to the new contract/implementation registrations.

## Scope

Refactor existing onboarding flows only:

- Registration/login/logout flows.
- Account onboarding state.
- Create new management company flow.
- Company selection.
- Workspace/context selection and redirect resolution.
- Workspace catalog data, if currently used.
- Join request creation as part of onboarding, if currently implemented.
- Initial redirect/context cookie flow.
- Regular MVC onboarding controller.
- Onboarding API controller(s), if present.

Do not add new onboarding steps, screens, endpoints, or behavior.

## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `WebApp/Controllers/OnboardingController.cs`
- `WebApp/ApiControllers/Onboarding/**`
- Onboarding views/ViewModels in `WebApp`
- Current BLL services/contracts/models under:
  - `App.BLL/Onboarding/Account/**`
  - `App.BLL/Onboarding/Api/**`
  - `App.BLL/Onboarding/CompanyJoinRequests/**`
  - `App.BLL/Onboarding/ContextSelection/**`
  - `App.BLL/Onboarding/WorkspaceCatalog/**`
- New or existing management contracts/services from Plan 09:
  - `App.BLL.Contracts/ManagementCompanies/**`
  - `App.BLL/ManagementCompanies/**`
- Current onboarding DTOs in `App.DTO`, if API exists
- `WebApp/Helpers/DependencyInjectionHelpers.cs`
- `App.Domain/**/ManagementCompany.cs`
- `App.Domain/**/ManagementCompanyUser.cs`
- `App.Domain/**/ManagementCompanyRole.cs`
- `App.Domain/**/ManagementCompanyJoinRequest.cs`
- `App.Domain/**/ManagementCompanyJoinRequestStatus.cs`
- `App.Domain/**/Customer.cs`
- `App.Domain/**/Resident.cs`
- `App.Domain/**/Identity/AppUser.cs`
- `App.DAL.EF/AppDbContext.cs`, only to understand old behavior that must be moved out of BLL
- `IAppUOW` / `AppUOW`

## Allowed files/folders to create or modify

- `App.BLL.Contracts/Onboarding/**`
- `App.BLL/Onboarding/**`
- `App.BLL/Mappers/Onboarding/**`
- Onboarding-related `App.Contracts/DAL/**` only if existing repositories need new methods
- Onboarding-related DAL repository methods/mappers only when no existing method can support current onboarding behavior
- Onboarding-related WebApp mappers
- `WebApp/Controllers/OnboardingController.cs`
- `WebApp/ApiControllers/Onboarding/**`
- Onboarding ViewModels, if needed to preserve existing behavior
- `WebApp/Helpers/DependencyInjectionHelpers.cs`

Avoid creating new repositories if existing repositories from previous slices already cover the required data. Prefer extending existing management/customer/resident repositories with onboarding-specific read/write methods only when necessary.

Do not modify management membership/profile/join-request behavior except for small interface/namespace compatibility changes needed by onboarding.

## Current behavior inventory

Before editing, identify and preserve:

- Onboarding MVC routes.
- Onboarding API routes, if present.
- Register/login/logout behavior and validation messages.
- New management company creation behavior, including owner membership creation.
- Existing redirect flow after login/register/company creation/context selection.
- Existing view model fields.
- Existing cookie/session/temp-data/context behavior.
- Existing user/company/workspace selection behavior.
- Existing join request behavior from onboarding.
- Existing management role option behavior used by onboarding join-request forms.
- Existing status codes/messages/errors.
- Existing use of `ManagementCompanyJoinRequestStatus`.
- Existing resident/customer/management context redirect decisions.

Preserve all behavior. The refactor should only change internal architecture.

## Old onboarding replacement rule

Do not keep old onboarding service contracts/models inside `App.BLL/Onboarding/**` as the final public service boundary.

During this slice, move or recreate the onboarding service contracts, commands, queries, models, and result models under:

```text
App.BLL.Contracts/Onboarding/**
```

The implementation may remain organized under `App.BLL/Onboarding/**`, but it must implement interfaces from `App.BLL.Contracts.Onboarding`.

After the new services and WebApp controllers are wired:

- old `App.BLL.Onboarding.*` service interfaces should be deleted, moved, or excluded from compilation;
- WebApp should not import old onboarding implementation namespaces for service contracts;
- `DependencyInjectionHelpers.cs` should register contracts from `App.BLL.Contracts.Onboarding` to implementations in `App.BLL.Onboarding`;
- onboarding BLL services must not reference `App.DAL.EF`, `AppDbContext`, `App.DAL.EF.Seeding`, or EF Core query APIs.

Do not recreate direct `AppDbContext` access in onboarding services just to preserve behavior. Use `IAppUOW`, existing repositories, lookup repository methods, and Identity services where appropriate.

## BLL contracts

Create:

```text
App.BLL.Contracts/Onboarding/
  Services/
    IAccountOnboardingService.cs
    IWorkspaceRedirectService.cs
    IContextSelectionService.cs
    IWorkspaceCatalogService.cs
    IOnboardingCompanyJoinRequestService.cs
  Queries/
    GetOnboardingStateQuery.cs
    GetWorkspaceCatalogQuery.cs
    ResolveWorkspaceRedirectQuery.cs
    AuthorizeContextSelectionQuery.cs
  Commands/
    RegisterAccountCommand.cs
    LoginAccountCommand.cs
    LogoutCommand.cs
    CreateManagementCompanyCommand.cs
    SelectWorkspaceCommand.cs
    CreateCompanyJoinRequestCommand.cs
    CompleteAccountOnboardingCommand.cs
  Models/
    OnboardingStateModel.cs
    WorkspaceCatalogModel.cs
    WorkspaceOptionModel.cs
    WorkspaceRedirectModel.cs
    WorkspaceSelectionAuthorizationModel.cs
    OnboardingJoinRequestModel.cs
    AccountRegisterModel.cs
    AccountLoginModel.cs
    CreateManagementCompanyModel.cs
```

Only include contracts for currently implemented flows.

Suggested account service shape:

```csharp
public interface IAccountOnboardingService
{
    Task<Result<AccountRegisterModel>> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<AccountLoginModel>> LoginAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        CreateManagementCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<OnboardingStateModel>> GetStateAsync(
        GetOnboardingStateQuery query,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteAsync(
        CompleteAccountOnboardingCommand command,
        CancellationToken cancellationToken = default);
}
```

```csharp
public interface IWorkspaceRedirectService
{
    Task<Result<WorkspaceRedirectModel?>> ResolveContextRedirectAsync(
        ResolveWorkspaceRedirectQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        AuthorizeContextSelectionQuery query,
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

```csharp
public interface IOnboardingCompanyJoinRequestService
{
    Task<Result<OnboardingJoinRequestModel>> CreateJoinRequestAsync(
        CreateCompanyJoinRequestCommand command,
        CancellationToken cancellationToken = default);
}
```

If management slice services already contain reusable join-request behavior, call those services from onboarding instead of duplicating rules. Do not confuse onboarding join-request creation with management-side join-request approval/rejection.

## BLL implementation

Create or refactor implementations under the existing onboarding module structure, for example:

```text
App.BLL/Onboarding/Account/AccountOnboardingService.cs
App.BLL/Onboarding/ContextSelection/WorkspaceRedirectService.cs
App.BLL/Onboarding/ContextSelection/ContextSelectionService.cs
App.BLL/Onboarding/WorkspaceCatalog/WorkspaceCatalogService.cs
App.BLL/Onboarding/CompanyJoinRequests/OnboardingCompanyJoinRequestService.cs
App.BLL/Mappers/Onboarding/OnboardingBllMapper.cs
```

Rules:

- Use `IAppUOW` and/or BLL services from previous slices.
- Do not use `AppDbContext`.
- Do not reference `App.DAL.EF`.
- Do not reference `App.DAL.EF.Seeding` / `InitialData`.
- Do not use EF Core query APIs in onboarding services.
- Do not use `App.DTO`.
- `UserManager<AppUser>` and `SignInManager<AppUser>` may remain in account onboarding for registration, login, and logout because those are identity operations.
- Data operations such as management-company creation, owner membership creation, context lookup, and join-request creation should use `IAppUOW` repositories and lookup methods.
- Use `ManagementCompanyJoinRequestStatusCodes.Pending` when creating join requests.
- Use role code lookup, for example `OWNER`, instead of hardcoded seeded GUIDs or DAL seeding constants.
- Return FluentResults errors for expected failures.
- Preserve existing redirect/context decisions and cookie semantics.

## DAL usage

Prefer using repositories already created:

- `IManagementCompanyRepository`
- `IManagementCompanyJoinRequestRepository`
- `ICustomerRepository`
- `IResidentRepository`
- `ILookupRepository`

Add repository methods only if onboarding currently needs data not yet exposed.

Likely repository support needed by onboarding includes:

```text
- Find active management company by registry code.
- Check duplicate management company registry code.
- Create management company and initial owner membership.
- Resolve default management context by user.
- Check management/customer/resident context access by user.
- Create management company join request with PENDING status.
- Check duplicate pending join request and existing membership.
- List role options for onboarding join-request form.
```

Implement these as methods on existing repositories where appropriate. Do not introduce new repositories unless the current repository boundaries cannot support the behavior cleanly.

## WebApp MVC mapper

Create:

```text
WebApp/Mappers/Mvc/Onboarding/OnboardingViewModelMapper.cs
```

Responsibilities:

- BLL model -> current onboarding ViewModel.
- Onboarding form ViewModel -> BLL command.
- FluentResults errors -> existing ModelState/TempData behavior, where this logic should be centralized instead of duplicated in the controller.

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

- Controller calls BLL onboarding services from `App.BLL.Contracts.Onboarding`.
- Controller maps BLL models to ViewModels.
- Controller maps form posts to BLL commands.
- Controller converts FluentResults errors to current ModelState/redirect behavior.
- Controller does not use repositories or `AppDbContext`.
- Preserve current routes, TempData messages, cookie names, and redirects.

## API controller refactor, if present

Refactor:

```text
WebApp/ApiControllers/Onboarding/OnboardingController.cs
WebApp/ApiControllers/Onboarding/*
```

Rules:

- Preserve routes/auth.
- Use BLL service + API mapper + `ToActionResult`.
- Controller depends on `App.BLL.Contracts.Onboarding` interfaces, not implementation namespaces.

## DI registration

Update `WebApp/Helpers/DependencyInjectionHelpers.cs`:

- Register onboarding BLL contracts from `App.BLL.Contracts.Onboarding.Services` to implementations in `App.BLL.Onboarding`.
- Register onboarding WebApp mappers.
- Remove old service registrations that point at service interfaces living under `App.BLL.Onboarding.*`.
- Keep management-company service registrations from Plan 09 intact.

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop when onboarding functionality is refactored and builds.

This slice is not complete until:

```text
No onboarding BLL service references App.DAL.EF.
No onboarding BLL service references AppDbContext.
No onboarding BLL service references App.DAL.EF.Seeding or InitialData.
No onboarding BLL service uses EF Core query APIs directly.
WebApp onboarding controllers depend on App.BLL.Contracts.Onboarding service interfaces, not old App.BLL.Onboarding service interfaces.
DependencyInjectionHelpers registers onboarding contracts from App.BLL.Contracts.Onboarding to App.BLL.Onboarding implementations.
Join request creation uses ManagementCompanyJoinRequestStatusCodes.Pending / lookup code resolution, not hardcoded seeded GUIDs.
Management-company creation resolves the owner role by code, not by seeded GUID or DAL seed constants.
Existing onboarding routes, redirects, cookies, TempData messages, ModelState behavior, and response shapes are preserved.
```

Do not perform final cleanup beyond files made obsolete by onboarding itself.

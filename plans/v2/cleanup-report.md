# Final Cleanup Report

Date: 2026-04-29

## Build Result

- Solution rebuild: passed, confirmed by project owner after cleanup edits.
- No tests were added, per repository instruction to not write tests until further notice.

## Cleanup Changes Made

- Removed `App.BLL` project references to `App.DAL.EF`, `App.Domain`, and the `Microsoft.AspNetCore.App` framework reference.
- Added `IAccountIdentityService` in `App.BLL.Contracts` and a WebApp adapter implementation, `IdentityAccountService`, so account onboarding BLL no longer depends directly on ASP.NET Identity or `App.Domain.Identity`.
- Registered the identity adapter in `WebApp/Helpers/DependencyInjectionHelpers.cs`.

## Check Results

### BLL to DAL/EF

- `App.BLL` no longer references `App.DAL.EF`.
- `App.BLL` no longer references `AppDbContext`.
- `App.BLL` no longer references `App.Domain.Identity`, `UserManager<TUser>`, or `SignInManager<TUser>`.

### BLL to App.DTO

- `App.BLL` and `App.BLL.Contracts` do not reference `App.DTO`.

### Old Pre-Refactor BLL Namespaces

- No active production references found for:
  - `App.BLL.CustomerWorkspace`
  - `App.BLL.PropertyWorkspace`
  - `App.BLL.UnitWorkspace`
  - `App.BLL.ResidentWorkspace`
  - `App.BLL.LeaseAssignments`
  - `App.BLL.ManagementCompany`

### Controller DAL Usage

Remaining direct `AppDbContext` usage:

- `WebApp/Areas/Admin/Controllers/*Controller.cs`
  - Reason not changed: these are scaffolded Admin CRUD controllers. The repository agent guide says Admin CRUD scaffolding is handled manually by the project owner and AI contributors should delegate explicit admin scaffolding tasks back to the user.
  - Safe to refactor later: yes, but this should be a dedicated Admin CRUD/service-layer migration.
  - Suggested follow-up: create a separate admin-area refactor plan if Admin controllers should also move behind BLL services.

- `WebApp/ApiControllers/Identity/AccountController.cs`
  - Reason not changed: this is the legacy JWT/refresh-token identity API. It directly manages refresh tokens and token response shape. Moving it behind BLL would risk route and response behavior changes during final cleanup.
  - Safe to refactor later: yes, with dedicated identity API contract preservation checks.
  - Suggested follow-up: create an identity API slice that preserves `/api/v1/identity/account/*` routes and `SpaJwtResponseDto`/`JWTResponse` response shapes while moving refresh-token persistence behind a service.

No direct `AppDbContext`, repository, or `IAppUOW` usage was found in regular MVC controllers outside Admin, except for the legacy Identity API exception above.

### Repository Contracts

- `App.Contracts/DAL/**` repository contracts do not use FluentResults.
- No domain entity return types were found in `App.Contracts/DAL/**`.
- Repository contracts return DAL DTOs, nullable DAL DTOs, booleans, lists, IDs, or `Task`.

### FluentResults Compliance

Refactored customer, property, unit, resident, lease, and onboarding service boundaries generally use `Result` or `Result<T>`.

Legacy/service-helper exceptions that do not currently return `Result` or `Result<T>`:

- `App.BLL.Contracts/Onboarding/Services/IAccountOnboardingService.cs`
  - Methods: `HasAnyContextAsync`, `GetDefaultManagementCompanySlugAsync`, `UserHasManagementCompanyAccessAsync`.
  - Reason not changed: these are query/helper methods used by onboarding flow and middleware-style context checks. Changing them would require coordinated caller changes and is not needed for this cleanup.
  - Safe to refactor later: yes.
  - Suggested follow-up: wrap these in `Result<T>` if they become public use-case operations rather than internal helper queries.

- `App.BLL.Contracts/Onboarding/Services/IAccountIdentityService.cs`
  - Methods: `FindUserIdByEmailAsync`, `UserExistsAsync`, `SignOutAsync`.
  - Reason not changed: this is an infrastructure adapter contract, not a business use-case boundary. `CreateUserAsync` and `PasswordSignInAsync` return FluentResults because they produce user-visible success/failure.
  - Safe to refactor later: yes, but not necessary unless adapter failures need richer error mapping.
  - Suggested follow-up: keep this adapter internal to composition where practical.

- `App.BLL.Contracts/ManagementCompanies/Services/IManagementCompanyProfileService.cs`
  - Methods: `GetProfileAsync`, `UpdateProfileAsync`, `DeleteProfileAsync`.
  - Reason not changed: management profile currently uses pre-existing custom result models and nullable profile lookup behavior.
  - Safe to refactor later: yes.
  - Suggested follow-up: migrate to `Result<CompanyProfileModel>` and `Result` in a dedicated management profile cleanup.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyMembershipAdminService.cs`
  - Methods: all methods currently return custom result models or lists instead of FluentResults.
  - Reason not changed: this service aggregates authorization, membership commands, ownership transfer, role options, and access-request review behavior. Converting it during cleanup would be high risk.
  - Safe to refactor later: yes.
  - Suggested follow-up: split into focused FluentResults services by responsibility.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyMembershipAuthorizationService.cs`
  - Methods: `AuthorizeManagementAreaAccessAsync`, `AuthorizeAsync`.
  - Reason not changed: this is part of the same management membership custom-result model set.
  - Safe to refactor later: yes.
  - Suggested follow-up: convert authorization outcomes to `Result<CompanyAdminAuthorizedContext>` or equivalent.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyMembershipCommandService.cs`
  - Methods: `AddUserByEmailAsync`, `UpdateMembershipAsync`, `DeleteMembershipAsync`.
  - Reason not changed: currently represented with custom command result models consumed by the MVC management users flow.
  - Safe to refactor later: yes.
  - Suggested follow-up: migrate with MVC ModelState mapping updates.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyMembershipQueryService.cs`
  - Methods: `ListCompanyMembersAsync`, `GetMembershipForEditAsync`.
  - Reason not changed: custom query result models are still consumed by existing management UI.
  - Safe to refactor later: yes.
  - Suggested follow-up: convert to `Result<T>` with unchanged view models.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyRoleOptionsService.cs`
  - Methods: `GetAddRoleOptionsAsync`, `GetEditRoleOptionsAsync`, `GetAvailableRolesAsync`.
  - Reason not changed: option-list helper service uses list/custom option result models.
  - Safe to refactor later: yes.
  - Suggested follow-up: keep simple list methods only if treated as lookup helpers; otherwise wrap use-case calls in FluentResults.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyOwnershipTransferService.cs`
  - Methods: `GetOwnershipTransferCandidatesAsync`, `TransferOwnershipAsync`.
  - Reason not changed: part of the management membership custom-result flow.
  - Safe to refactor later: yes.
  - Suggested follow-up: convert ownership transfer command to `Result`/`Result<T>` with explicit conflict and forbidden errors.

- `App.BLL.Contracts/ManagementCompanies/Services/ICompanyAccessRequestReviewService.cs`
  - Methods: `GetPendingAccessRequestsAsync`, `ApprovePendingAccessRequestAsync`, `RejectPendingAccessRequestAsync`.
  - Reason not changed: custom results are tied to the current management access-request UI.
  - Safe to refactor later: yes.
  - Suggested follow-up: align with join-request FluentResults behavior in a management/onboarding cleanup slice.

- `App.BLL.Contracts/ManagementCompanies/Services/IManagementCompanyAccessService.cs`
  - Methods: `AuthorizeManagementAreaAccessAsync`.
  - Reason not changed: currently shares management membership authorization result models.
  - Safe to refactor later: yes.
  - Suggested follow-up: consolidate with the authorization service migration.

### DI Registration

- `WebApp/Helpers/DependencyInjectionHelpers.cs` contains `AddAppDalEf`, `AddAppBll`, and `AddWebAppMappers`.
- `WebApp/Program.cs` uses those helpers.
- No `App.BLL/DependencyInjection.cs` or `App.DAL.EF/DependencyInjection.cs` file exists.

### Project References

- `App.BLL` now references only `App.BLL.Contracts`, `App.Contracts`, and `App.Resources`.
- `App.DAL.EF` references `App.Contracts`, `App.Domain`, and `Base.DAL.EF`, as expected for persistence.
- `WebApp` references concrete DAL/BLL projects as the composition root.
- Existing note: `App.Contracts` still references `Base.Domain` for `ILookUpEntity.LangStr`. This is consistent with the current localization model but differs from the idealized target dependency list in the master plan.

### Excluded Features

- No new ticket, vendor, scheduled-work, or work-log functionality was added.
- Existing Admin scaffold controllers for ticket/vendor/scheduled-work/work-log entities were left untouched.

## Remaining Legacy Exceptions Summary

- Admin scaffold controllers still use `AppDbContext` directly.
- Legacy Identity API controller still uses `AppDbContext` directly for refresh-token workflows.
- Management-company membership/profile service contracts still use custom result models instead of FluentResults.
- `App.Contracts` still depends on `Base.Domain` for `LangStr` in `ILookUpEntity`.

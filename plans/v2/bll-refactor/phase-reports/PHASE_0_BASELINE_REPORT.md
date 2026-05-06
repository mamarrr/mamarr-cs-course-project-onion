# Phase 0 Baseline Report

Source briefs:

- `plans/v2/bllrefactor/by-phases/00_MASTER_BLL_AGENT_HANDOFF.md`
- `plans/v2/bllrefactor/by-phases/01_PHASE_0_BASELINE_AND_GUARDRAILS.md`

## Build Status

Not run by this agent. The master handoff explicitly instructs agents not to build the projects or solution and to ask the user for build verification instead.

Required user verification:

```powershell
dotnet build mamarrproject.sln -nologo
```

Report `OK` or provide the build errors before Phase 1 starts.

## Current IAppBLL Surface

`App.BLL.Contracts/IAppBLL.cs` currently exposes 23 granular service properties:

- `AccountOnboarding`
- `OnboardingCompanyJoinRequests`
- `WorkspaceContexts`
- `WorkspaceCatalog`
- `WorkspaceRedirect`
- `ContextSelection`
- `ManagementCompanyProfiles`
- `CompanyMembershipAdmin`
- `CompanyCustomers`
- `CustomerAccess`
- `CustomerProfiles`
- `CustomerWorkspaces`
- `PropertyProfiles`
- `PropertyWorkspaces`
- `ResidentAccess`
- `ResidentProfiles`
- `ResidentWorkspaces`
- `UnitAccess`
- `UnitProfiles`
- `UnitWorkspaces`
- `LeaseAssignments`
- `LeaseLookups`
- `ManagementTickets`

This matches the broad pre-refactor surface described in the master handoff.

## Current BLL Service List

Current implementations under `App.BLL/Services`:

- `AccountOnboardingService`
- `OnboardingCompanyJoinRequestService`
- `WorkspaceContextService`
- `UserWorkspaceCatalogService`
- `WorkspaceRedirectService`
- `ManagementCompanyProfileService`
- `CompanyMembershipAdminService`
- `CompanyCustomerService`
- `CustomerAccessService`
- `CustomerProfileService`
- `CustomerWorkspaceService`
- `PropertyProfileService`
- `PropertyWorkspaceService`
- `ResidentAccessService`
- `ResidentProfileService`
- `ResidentWorkspaceService`
- `UnitAccessService`
- `UnitProfileService`
- `UnitWorkspaceService`
- `LeaseAssignmentService`
- `LeaseLookupService`
- `ManagementTicketService`
- `AppDeleteGuard`

`WorkspaceRedirectService` implements both `IWorkspaceRedirectService` and `IContextSelectionService`; `AppBLL.ContextSelection` casts `WorkspaceRedirect` to that second interface.

## Likely Aggregate-Backed Targets

These have canonical BLL DTOs and DAL repositories available and should be reviewed first for `BaseService` adoption in later phases:

- Customer: `CustomerBllDto`, `ICustomerRepository`
- Property: `PropertyBllDto`, `IPropertyRepository`
- Unit: `UnitBllDto`, `IUnitRepository`
- Resident: `ResidentBllDto`, `IResidentRepository`
- Lease: `LeaseBllDto`, `ILeaseRepository`
- Ticket: `TicketBllDto`, `ITicketRepository`
- Management company: `ManagementCompanyBllDto`, `IManagementCompanyRepository`
- Management company join request: `ManagementCompanyJoinRequestBllDto`, `IManagementCompanyJoinRequestRepository`
- Contact: `ContactBllDto`, `IContactRepository`
- Vendor: `VendorBllDto`, `IVendorRepository`

Likely current granular services that belong under aggregate-backed domain services:

- `CompanyCustomerService`
- `CustomerProfileService`
- `CustomerWorkspaceService`
- `PropertyProfileService`
- `PropertyWorkspaceService`
- `UnitProfileService`
- `UnitWorkspaceService`
- `ResidentProfileService`
- `ResidentWorkspaceService`
- `LeaseAssignmentService`
- `ManagementTicketService`
- `ManagementCompanyProfileService`

## Likely Orchestration Exceptions

These appear workflow/context oriented and may remain orchestration services, or be grouped behind domain-first services without direct `BaseService` inheritance:

- `AccountOnboardingService`
- `OnboardingCompanyJoinRequestService`
- `WorkspaceContextService`
- `UserWorkspaceCatalogService`
- `WorkspaceRedirectService`
- `ContextSelectionService` surface, currently implemented by `WorkspaceRedirectService`
- `CustomerAccessService`
- `ResidentAccessService`
- `UnitAccessService`
- `LeaseLookupService`
- `CompanyMembershipAdminService`, unless later phases define a natural membership aggregate service
- `AppDeleteGuard`, which is an internal guard/helper

## Current BLL DTO Shape

Canonical DTOs exist for:

- `ContactBllDto`
- `CustomerBllDto`
- `LeaseBllDto`
- `ManagementCompanyBllDto`
- `ManagementCompanyJoinRequestBllDto`
- `PropertyBllDto`
- `ResidentBllDto`
- `TicketBllDto`
- `UnitBllDto`
- `VendorBllDto`

DTO folders and high-level categories:

- `Common`: shared validation model and typed app errors.
- `Contacts`: canonical contact DTO.
- `Customers`: canonical DTO plus commands, queries, models, and customer-specific errors.
- `Leases`: canonical DTO plus command/query/model files.
- `ManagementCompanies`: canonical DTOs, join request status codes, and membership/profile models.
- `Onboarding`: commands, queries, and context/catalog/selection models.
- `Properties`: canonical DTO plus commands, queries, and profile/workspace/dashboard models.
- `Residents`: canonical DTO plus commands, queries, models, and resident-specific errors.
- `Tickets`: canonical DTO, workflow constants, and command/query/model files.
- `Units`: canonical DTO plus commands, queries, and profile/workspace/dashboard models.
- `Vendors`: canonical vendor DTO.

Obvious later audit candidates:

- `Customers/Commands`
- `Properties/Commands`
- `Residents/Commands`
- `Units/Commands`
- `Leases/Commands`
- `Tickets/Commands`

Some commands may only wrap canonical entity state and should be checked against the canonical-DTO-first rule. Queries/models generally appear more likely to be justified because they carry scope, filtering, dashboard, option, or workflow context.

## BaseService Current Behavior

`Base.BLL/BaseService.cs` implements `IBaseService` from `Base.BLL.Contracts/IBaseService.cs`.

Current method signatures use FluentResults:

- `Task<Result<IEnumerable<TBLLEntity>>> AllAsync(...)`
- `Task<Result<TBLLEntity?>> FindAsync(...)`
- `Result<TKey> Add(...)`
- `Task<Result<TBLLEntity>> UpdateAsync(...)`
- `Result Remove(...)`
- `Task<Result> RemoveAsync(...)`

Static inspection found no current `App.BLL/Services` classes inheriting `BaseService`. Later phases should introduce inheritance only for the selected aggregate-backed public domain services and keep workflow rules in concrete services.

## Dependency Audit

Static project reference audit:

- `App.BLL` references `App.BLL.Contracts`, `App.DAL.Contracts`, `App.Resources`, `Base.BLL`, `Base.BLL.Contracts`, and `Base.Contracts`.
- `App.BLL` does not project-reference `App.DAL.EF`, `WebApp`, or `App.DTO`.
- `App.BLL.Contracts` references `App.BLL.DTO` and `Base.BLL.Contracts`.
- `App.BLL.DTO` references `Base.Domain` and `FluentResults`.

Static code search findings:

- `App.BLL.Contracts` did not expose `App.DAL.DTO`, `App.DAL.EF`, `WebApp`, `App.DTO`, MVC, or `HttpContext` symbols.
- `App.BLL` uses `App.DAL.DTO` in mappers and several service implementations. This is currently allowed by dependency direction through `App.DAL.Contracts`, but later phases should reduce service-level DAL DTO mapping where `BaseService` and canonical mappers can take over.
- No `App.BLL` reference to `App.DAL.EF` was found.

## Known Risks

- No build was run due to the master handoff restriction, so compile status must be supplied externally before Phase 1.
- `IAppBLL` is broad and UI/workflow sliced, increasing coupling for later facade changes.
- No exposed service currently inherits `BaseService`, so later phases will need careful incremental adoption.
- Authorization and tenant filters are distributed across access, workspace, profile, and workflow services. Refactors must preserve the order of resolving actor context, tenant context, permission checks, and tenant-scoped materialization.
- `ContextSelection` depends on a cast from `WorkspaceRedirect`; changing the facade or onboarding grouping can break this if not preserved intentionally.
- `ManagementCompany.DeleteCascadeAsync` behavior must not be touched without explicit approval.
- Several services pass tenant IDs through command/query models. Later canonical DTO simplification must not make client-provided tenant IDs authoritative.
- Lookup/option DTOs and dashboard/read projections should not be collapsed into canonical DTOs if they encode scoped filtering or view-specific projections.

## Recommended Next-Phase Notes

- Start Phase 1 from the current `IAppBLL` surface and group services by target domain facade before changing any code.
- Treat access/context services as policy inputs to aggregate services, not as simple CRUD service targets.
- For each aggregate-backed service, verify the DAL repository has the required `IBaseRepository` path, a canonical DAL DTO, a canonical BLL DTO, and an existing mapper before adopting `BaseService`.
- Keep workflow commands and read projections until proven redundant.
- Preserve existing `Result` typed errors during regrouping.
- Avoid touching WebApp, API controllers, DAL EF, migrations, tests, and admin scaffolding.

## Assumptions

- The existing `plans/v2/bllrefactor/by-phases/phase-reports` folder is the intended location for phase deliverables.
- Static dependency inspection is sufficient for Phase 0 until the user supplies the build result.
- The duplicated `App.DAL.Contracts/IAppUOW.cs` and `App.DAL.Contracts/IAppUow.cs` paths shown by search are outside Phase 0 scope and were not changed.

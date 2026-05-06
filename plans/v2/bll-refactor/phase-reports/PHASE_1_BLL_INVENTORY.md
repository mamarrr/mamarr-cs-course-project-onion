# Phase 1 BLL Inventory Report

Source briefs:

- `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md`
- `plans/v2/bll-refactor/02_PHASE_1_BLL_INVENTORY.md`
- `plans/v2/bll-refactor/phase-reports/PHASE_0_BASELINE_REPORT.md`

## Build Status

Not run by this agent. The master handoff explicitly instructs agents not to build projects or the solution.

Required user verification:

```powershell
dotnet build mamarrproject.sln -nologo
```

Report `OK` or provide build errors before Phase 2 starts.

## Scope Executed

This phase was documentation-only. No contracts, services, DTOs, mappers, controllers, tests, DAL, migrations, or behavior were changed.

Inventoried areas:

- `App.BLL.Contracts`
- `App.BLL.DTO`
- `App.BLL`
- BLL mappers
- DAL repository/BaseService readiness where needed for classification

## IAppBLL Facade Inventory

Current `IAppBLL` exposes 23 granular services.

| Facade property | Current contract | Current implementation | Target owner | Notes |
| --- | --- | --- | --- | --- |
| `AccountOnboarding` | `IAccountOnboardingService` | `AccountOnboardingService` | `OnboardingService` | Pure onboarding orchestration; not BaseService-backed. |
| `OnboardingCompanyJoinRequests` | `IOnboardingCompanyJoinRequestService` | `OnboardingCompanyJoinRequestService` | `OnboardingService` or `CompanyMembershipService` | Uses join request aggregate but exposed as onboarding workflow. |
| `WorkspaceContexts` | `IWorkspaceContextService` | `WorkspaceContextService` | `WorkspaceService` | Context catalog orchestration. |
| `WorkspaceCatalog` | `IWorkspaceCatalogService` | `UserWorkspaceCatalogService` | `WorkspaceService` | User workspace catalog/read projection. |
| `WorkspaceRedirect` | `IWorkspaceRedirectService` | `WorkspaceRedirectService` | `WorkspaceService` | Redirect/context-selection orchestration. |
| `ContextSelection` | `IContextSelectionService` | `WorkspaceRedirectService` cast | `WorkspaceService` | Fragile facade cast must be removed carefully in later phases. |
| `ManagementCompanyProfiles` | `IManagementCompanyProfileService` | `ManagementCompanyProfileService` | `ManagementCompanyService` | Aggregate-backed profile operations plus tenant/member checks. |
| `CompanyMembershipAdmin` | `ICompanyMembershipAdminService` | `CompanyMembershipAdminService` | `CompanyMembershipService` | Large orchestration/policy service; possible documented BaseService exception. |
| `CompanyCustomers` | `ICompanyCustomerService` | `CompanyCustomerService` | `CustomerService` | Customer aggregate create/list. |
| `CustomerAccess` | `ICustomerAccessService` | `CustomerAccessService` | Internal helper/policy under customer/workspace | Access resolver; should not remain public domain CRUD service. |
| `CustomerProfiles` | `ICustomerProfileService` | `CustomerProfileService` | `CustomerService` | Customer aggregate profile/update/delete. |
| `CustomerWorkspaces` | `ICustomerWorkspaceService` | `CustomerWorkspaceService` | `CustomerService` or `WorkspaceService` | Customer workspace projection. |
| `PropertyProfiles` | `IPropertyProfileService` | `PropertyProfileService` | `PropertyService` | Property aggregate profile/update/delete. |
| `PropertyWorkspaces` | `IPropertyWorkspaceService` | `PropertyWorkspaceService` | `PropertyService` | Property aggregate create/list/profile/dashboard. |
| `ResidentAccess` | `IResidentAccessService` | `ResidentAccessService` | Internal helper/policy under resident/workspace | Access resolver. |
| `ResidentProfiles` | `IResidentProfileService` | `ResidentProfileService` | `ResidentService` | Resident aggregate profile/update/delete. |
| `ResidentWorkspaces` | `IResidentWorkspaceService` | `ResidentWorkspaceService` | `ResidentService` | Resident aggregate create/list/dashboard. |
| `UnitAccess` | `IUnitAccessService` | `UnitAccessService` | Internal helper/policy under unit/workspace | Access resolver. |
| `UnitProfiles` | `IUnitProfileService` | `UnitProfileService` | `UnitService` | Unit aggregate profile/update/delete. |
| `UnitWorkspaces` | `IUnitWorkspaceService` | `UnitWorkspaceService` | `UnitService` | Unit aggregate create/list/dashboard. |
| `LeaseAssignments` | `ILeaseAssignmentService` | `LeaseAssignmentService` | `LeaseService` | Lease aggregate assignment workflow from resident/unit contexts. |
| `LeaseLookups` | `ILeaseLookupService` | `LeaseLookupService` | `LeaseService` or internal lookup helper | Search/options read service. |
| `ManagementTickets` | `IManagementTicketService` | `ManagementTicketService` | `TicketService` | Ticket aggregate workflow and management projections. |

## Contract Method Inventory

Classification key:

- Result usage: `Result`, `Result<T>`, or `Non-Result`.
- Kind: `CRUD`, `workflow`, `query`, `projection`, `access`, `delete`, `options`.
- DTO style: `canonical`, `command/query`, `projection`, `context`, `primitive`, or `none`.
- BaseService fit: `Yes` means the future aggregate service can inherit `BaseService` and expose equivalent CRUD/simple behavior; `Partial` means BaseService can support the aggregate but this method keeps explicit workflow/query logic; `No` means orchestration/helper.
- API-ready: `Yes` means BLL contract shape is DTO-first and independent of WebApp/API; `Partial` means usable but too UI/workspace-sliced or non-Result for the target direction.

| Service | Method | Input | Output | Result usage | Kind | DTO style | BaseService fit | API-ready |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ICompanyCustomerService` | `GetCompanyCustomersAsync` | `GetCompanyCustomersQuery` | `IReadOnlyList<CustomerListItemModel>` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `ICompanyCustomerService` | `CreateCustomerAsync` | `CreateCustomerCommand` | `CompanyCustomerModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `ICustomerAccessService` | `ResolveCompanyWorkspaceAsync` | `GetCompanyCustomersQuery` | `CompanyWorkspaceModel` | `Result<T>` | access | query + context | No | Yes |
| `ICustomerAccessService` | `ResolveCustomerWorkspaceAsync` | `GetCustomerWorkspaceQuery` | `CustomerWorkspaceModel` | `Result<T>` | access | query + context | No | Yes |
| `ICustomerProfileService` | `GetAsync` | `GetCustomerProfileQuery` | `CustomerProfileModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `ICustomerProfileService` | `UpdateAsync` | `UpdateCustomerProfileCommand` | `CustomerProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `ICustomerProfileService` | `DeleteAsync` | `DeleteCustomerCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `ICustomerWorkspaceService` | `GetWorkspaceAsync` | `GetCustomerWorkspaceQuery` | `CustomerWorkspaceModel` | `Result<T>` | access/projection | query + context | No | Yes |
| `IPropertyProfileService` | `GetAsync` | `GetPropertyProfileQuery` | `PropertyProfileModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IPropertyProfileService` | `UpdateAsync` | `UpdatePropertyProfileCommand` | `PropertyProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `IPropertyProfileService` | `DeleteAsync` | `DeletePropertyCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `IPropertyWorkspaceService` | `GetWorkspaceAsync` | `GetPropertyWorkspaceQuery` | `PropertyWorkspaceModel` | `Result<T>` | access/projection | query + context | No | Yes |
| `IPropertyWorkspaceService` | `GetDashboardAsync` | `GetPropertyWorkspaceQuery` | `PropertyDashboardModel` | `Result<T>` | projection | query + projection | No | Yes |
| `IPropertyWorkspaceService` | `GetCustomerPropertiesAsync` | `GetPropertyWorkspaceQuery` | `IReadOnlyList<PropertyListItemModel>` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IPropertyWorkspaceService` | `GetPropertyTypeOptionsAsync` | cancellation only | `IReadOnlyList<PropertyTypeOptionModel>` | `Result<T>` | options | options | No | Yes |
| `IPropertyWorkspaceService` | `CreateAsync` | `CreatePropertyCommand` | `PropertyProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `IUnitAccessService` | `ResolveUnitWorkspaceAsync` | `GetUnitDashboardQuery` | `UnitWorkspaceModel` | `Result<T>` | access | query + context | No | Yes |
| `IUnitProfileService` | `GetAsync` | `GetUnitProfileQuery` | `UnitProfileModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IUnitProfileService` | `UpdateAsync` | `UpdateUnitCommand` | `UnitProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `IUnitProfileService` | `DeleteAsync` | `DeleteUnitCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `IUnitWorkspaceService` | `GetDashboardAsync` | `GetUnitDashboardQuery` | `UnitDashboardModel` | `Result<T>` | projection | query + projection | No | Yes |
| `IUnitWorkspaceService` | `GetPropertyUnitsAsync` | `GetPropertyUnitsQuery` | `PropertyUnitsModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IUnitWorkspaceService` | `CreateAsync` | `CreateUnitCommand` | `UnitProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `IResidentAccessService` | `ResolveCompanyResidentsAsync` | `GetResidentsQuery` | `CompanyResidentsModel` | `Result<T>` | access/projection | query + context | No | Yes |
| `IResidentAccessService` | `ResolveResidentWorkspaceAsync` | `GetResidentProfileQuery` | `ResidentWorkspaceModel` | `Result<T>` | access | query + context | No | Yes |
| `IResidentProfileService` | `GetAsync` | `GetResidentProfileQuery` | `ResidentProfileModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IResidentProfileService` | `UpdateAsync` | `UpdateResidentProfileCommand` | `ResidentProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `IResidentProfileService` | `DeleteAsync` | `DeleteResidentCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `IResidentWorkspaceService` | `GetDashboardAsync` | `GetResidentProfileQuery` | `ResidentDashboardModel` | `Result<T>` | projection | query + projection | No | Yes |
| `IResidentWorkspaceService` | `GetResidentsAsync` | `GetResidentsQuery` | `CompanyResidentsModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IResidentWorkspaceService` | `CreateAsync` | `CreateResidentCommand` | `ResidentProfileModel` | `Result<T>` | CRUD/workflow | command + projection | Partial | Yes |
| `ILeaseAssignmentService` | `ListForResidentAsync` | `GetResidentLeasesQuery` | `ResidentLeaseListModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `ILeaseAssignmentService` | `ListForUnitAsync` | `GetUnitLeasesQuery` | `UnitLeaseListModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `ILeaseAssignmentService` | `GetForResidentAsync` | `GetResidentLeaseQuery` | `LeaseModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `ILeaseAssignmentService` | `GetForUnitAsync` | `GetUnitLeaseQuery` | `LeaseModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `ILeaseAssignmentService` | `CreateFromResidentAsync` | `CreateLeaseFromResidentCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | command + helper model | Partial | Yes |
| `ILeaseAssignmentService` | `CreateFromUnitAsync` | `CreateLeaseFromUnitCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | command + helper model | Partial | Yes |
| `ILeaseAssignmentService` | `UpdateFromResidentAsync` | `UpdateLeaseFromResidentCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | command + helper model | Partial | Yes |
| `ILeaseAssignmentService` | `UpdateFromUnitAsync` | `UpdateLeaseFromUnitCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | command + helper model | Partial | Yes |
| `ILeaseAssignmentService` | `DeleteFromResidentAsync` | `DeleteLeaseFromResidentCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `ILeaseAssignmentService` | `DeleteFromUnitAsync` | `DeleteLeaseFromUnitCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `ILeaseLookupService` | `SearchPropertiesAsync` | `SearchLeasePropertiesQuery` | `LeasePropertySearchResultModel` | `Result<T>` | query/options | query + projection | No | Yes |
| `ILeaseLookupService` | `ListUnitsForPropertyAsync` | `GetLeaseUnitsForPropertyQuery` | `LeaseUnitOptionsModel` | `Result<T>` | options | query + options | No | Yes |
| `ILeaseLookupService` | `SearchResidentsAsync` | `SearchLeaseResidentsQuery` | `LeaseResidentSearchResultModel` | `Result<T>` | query/options | query + projection | No | Yes |
| `ILeaseLookupService` | `ListLeaseRolesAsync` | cancellation only | `LeaseRoleOptionsModel` | `Result<T>` | options | options | No | Yes |
| `IManagementTicketService` | `GetTicketsAsync` | `GetManagementTicketsQuery` | `ManagementTicketsModel` | `Result<T>` | query/projection | query/filter + projection | Partial | Yes |
| `IManagementTicketService` | `GetDetailsAsync` | `GetManagementTicketQuery` | `ManagementTicketDetailsModel` | `Result<T>` | query/projection | query + projection | Partial | Yes |
| `IManagementTicketService` | `GetCreateFormAsync` | `GetManagementTicketsQuery` | `ManagementTicketFormModel` | `Result<T>` | projection/options | query + form/options | No | Partial |
| `IManagementTicketService` | `GetEditFormAsync` | `GetManagementTicketQuery` | `ManagementTicketFormModel` | `Result<T>` | projection/options | query + form/options | No | Partial |
| `IManagementTicketService` | `GetSelectorOptionsAsync` | `GetManagementTicketSelectorOptionsQuery` | `TicketSelectorOptionsModel` | `Result<T>` | options | query + options | No | Yes |
| `IManagementTicketService` | `CreateAsync` | `CreateManagementTicketCommand` | `Guid` | `Result<T>` | CRUD/workflow | command | Partial | Yes |
| `IManagementTicketService` | `UpdateAsync` | `UpdateManagementTicketCommand` | none | `Result` | CRUD/workflow | command | Partial | Yes |
| `IManagementTicketService` | `DeleteAsync` | `DeleteManagementTicketCommand` | none | `Result` | delete/workflow | command | Partial | Yes |
| `IManagementTicketService` | `AdvanceStatusAsync` | `AdvanceManagementTicketStatusCommand` | none | `Result` | workflow | command | No | Yes |
| `IManagementCompanyProfileService` | `GetProfileAsync` | `appUserId`, `companySlug` | `CompanyProfileModel` | `Result<T>` | query/projection | primitive + projection | Partial | Yes |
| `IManagementCompanyProfileService` | `UpdateProfileAsync` | `appUserId`, `companySlug`, `CompanyProfileUpdateRequest` | none | `Result` | CRUD/workflow | primitive + request | Partial | Yes |
| `IManagementCompanyProfileService` | `DeleteProfileAsync` | `appUserId`, `companySlug` | none | `Result` | delete/workflow | primitive | Partial | Yes |
| `IManagementCompanyAccessService` | `AuthorizeManagementAreaAccessAsync` | `appUserId`, `companySlug` | `CompanyMembershipContext` | `Result<T>` | access | primitive + context | No | Yes |
| `ICompanyMembershipAuthorizationService` | `AuthorizeManagementAreaAccessAsync` | `appUserId`, `companySlug` | `CompanyMembershipContext` | `Result<T>` | access | primitive + context | No | Yes |
| `ICompanyMembershipAuthorizationService` | `AuthorizeAsync` | `appUserId`, `companySlug` | `CompanyAdminAuthorizedContext` | `Result<T>` | access | primitive + context | No | Yes |
| `ICompanyMembershipQueryService` | `ListCompanyMembersAsync` | `CompanyAdminAuthorizedContext` | `CompanyMembershipListResult` | `Result<T>` | query/projection | context + projection | No | Yes |
| `ICompanyMembershipQueryService` | `GetMembershipForEditAsync` | `CompanyAdminAuthorizedContext`, `membershipId` | `CompanyMembershipEditModel` | `Result<T>` | query/projection | context + projection | No | Yes |
| `ICompanyMembershipCommandService` | `AddUserByEmailAsync` | `CompanyAdminAuthorizedContext`, `CompanyMembershipAddRequest` | `Guid` | `Result<T>` | workflow | context + request | No | Yes |
| `ICompanyMembershipCommandService` | `UpdateMembershipAsync` | `CompanyAdminAuthorizedContext`, `membershipId`, `CompanyMembershipUpdateRequest` | none | `Result` | workflow | context + request | No | Yes |
| `ICompanyMembershipCommandService` | `DeleteMembershipAsync` | `CompanyAdminAuthorizedContext`, `membershipId` | none | `Result` | delete/workflow | context + primitive | No | Yes |
| `ICompanyRoleOptionsService` | `GetAddRoleOptionsAsync` | `CompanyAdminAuthorizedContext` | `IReadOnlyList<CompanyMembershipRoleOption>` | `Non-Result` | options | context + options | No | Partial |
| `ICompanyRoleOptionsService` | `GetEditRoleOptionsAsync` | `CompanyAdminAuthorizedContext`, `membershipId` | `IReadOnlyList<CompanyMembershipRoleOption>` | `Result<T>` | options | context + options | No | Yes |
| `ICompanyRoleOptionsService` | `GetAvailableRolesAsync` | cancellation only | `IReadOnlyList<CompanyMembershipRoleOption>` | `Non-Result` | options | options | No | Partial |
| `ICompanyOwnershipTransferService` | `GetOwnershipTransferCandidatesAsync` | `CompanyAdminAuthorizedContext` | `IReadOnlyList<OwnershipTransferCandidate>` | `Result<T>` | query/workflow | context + projection | No | Yes |
| `ICompanyOwnershipTransferService` | `TransferOwnershipAsync` | `CompanyAdminAuthorizedContext`, `TransferOwnershipRequest` | `OwnershipTransferModel` | `Result<T>` | workflow | context + request | No | Yes |
| `ICompanyAccessRequestReviewService` | `GetPendingAccessRequestsAsync` | `CompanyAdminAuthorizedContext` | `PendingAccessRequestListResult` | `Result<T>` | query/workflow | context + projection | No | Yes |
| `ICompanyAccessRequestReviewService` | `ApprovePendingAccessRequestAsync` | `CompanyAdminAuthorizedContext`, `requestId` | none | `Result` | workflow | context + primitive | No | Yes |
| `ICompanyAccessRequestReviewService` | `RejectPendingAccessRequestAsync` | `CompanyAdminAuthorizedContext`, `requestId` | none | `Result` | workflow | context + primitive | No | Yes |
| `ICompanyMembershipAdminService` | all authorization/query/command/options/transfer/review methods above | mixed | mixed | mixed | mixed | mixed | No | Partial |
| `IAccountOnboardingService` | `CreateManagementCompanyAsync` | `CreateManagementCompanyCommand` | `CreateManagementCompanyModel` | `Result<T>` | workflow | command + result model | No | Yes |
| `IAccountOnboardingService` | `GetStateAsync` | `GetOnboardingStateQuery` | `OnboardingStateModel` | `Result<T>` | query | query + model | No | Yes |
| `IAccountOnboardingService` | `CompleteAsync` | `CompleteAccountOnboardingCommand` | none | `Result` | workflow | command | No | Yes |
| `IAccountOnboardingService` | `HasAnyContextAsync` | `appUserId` | `bool` | `Non-Result` | access/query | primitive | No | Partial |
| `IAccountOnboardingService` | `GetDefaultManagementCompanySlugAsync` | `appUserId` | `string?` | `Non-Result` | query | primitive | No | Partial |
| `IAccountOnboardingService` | `UserHasManagementCompanyAccessAsync` | `appUserId`, `companySlug` | `bool` | `Non-Result` | access | primitive | No | Partial |
| `IOnboardingCompanyJoinRequestService` | `CreateJoinRequestAsync` | `CreateCompanyJoinRequestCommand` | `OnboardingJoinRequestModel` | `Result<T>` | workflow | command + result model | Partial | Yes |
| `IWorkspaceContextService` | `GetContextsAsync` | `appUserId` | `WorkspaceContextCatalogModel` | `Result<T>` | query/projection | primitive + projection | No | Yes |
| `IWorkspaceCatalogService` | `GetWorkspaceCatalogAsync` | `GetWorkspaceCatalogQuery` | `WorkspaceCatalogModel` | `Result<T>` | query/projection | query + projection | No | Yes |
| `IWorkspaceRedirectService` | `ResolveContextRedirectAsync` | `ResolveWorkspaceRedirectQuery` | `WorkspaceRedirectModel?` | `Result<T>` | workflow/access | query + result model | No | Yes |
| `IWorkspaceRedirectService` | `AuthorizeContextSelectionAsync` | `AuthorizeContextSelectionQuery` | `WorkspaceSelectionAuthorizationModel` | `Result<T>` | access | query + result model | No | Yes |
| `IContextSelectionService` | `GetWorkspaceCatalogAsync` | `GetWorkspaceCatalogQuery` | `WorkspaceCatalogModel` | `Result<T>` | query/projection | query + projection | No | Yes |
| `IContextSelectionService` | `SelectWorkspaceAsync` | `SelectWorkspaceCommand` | none | `Result` | workflow/access | command | No | Yes |
| `IAppDeleteGuard` | `CanDeleteUnitAsync` | entity and scope IDs | `bool` | `Non-Result` | delete/access | primitive | No | Partial |
| `IAppDeleteGuard` | `CanDeleteTicketAsync` | entity and scope IDs | `bool` | `Non-Result` | delete/access | primitive | No | Partial |
| `IAppDeleteGuard` | `CanDeletePropertyAsync` | entity and scope IDs | `bool` | `Non-Result` | delete/access | primitive | No | Partial |
| `IAppDeleteGuard` | `CanDeleteCustomerAsync` | entity and scope IDs | `bool` | `Non-Result` | delete/access | primitive | No | Partial |
| `IAppDeleteGuard` | `CanDeleteResidentAsync` | entity and scope IDs | `bool` | `Non-Result` | delete/access | primitive | No | Partial |

## DTO Inventory

### Canonical DTOs

These are the primary BaseService candidates because canonical BLL DTO, canonical DAL DTO, mapper, and repository exist.

| DTO | DAL DTO | Mapper | Repository | Classification | BaseService target |
| --- | --- | --- | --- | --- | --- |
| `ContactBllDto` | `ContactDalDto` | `ContactBllDtoMapper` | `IContactRepository : IBaseRepository<ContactDalDto>` | canonical DTO | `ContactService` if exposed later |
| `CustomerBllDto` | `CustomerDalDto` | `CustomerBllDtoMapper` | `ICustomerRepository : IBaseRepository<CustomerDalDto>` | canonical DTO | `CustomerService` |
| `LeaseBllDto` | `LeaseDalDto` | `LeaseBllDtoMapper` | `ILeaseRepository : IBaseRepository<LeaseDalDto>` | canonical DTO | `LeaseService` |
| `ManagementCompanyBllDto` | `ManagementCompanyDalDto` | `ManagementCompanyBllDtoMapper` | `IManagementCompanyRepository : IBaseRepository<ManagementCompanyDalDto>` | canonical DTO | `ManagementCompanyService` |
| `ManagementCompanyJoinRequestBllDto` | `ManagementCompanyJoinRequestDalDto` | `ManagementCompanyJoinRequestBllDtoMapper` | `IManagementCompanyJoinRequestRepository : IBaseRepository<ManagementCompanyJoinRequestDalDto>` | canonical DTO | `OnboardingService` workflow or separate join-request aggregate service |
| `PropertyBllDto` | `PropertyDalDto` | `PropertyBllDtoMapper` | `IPropertyRepository : IBaseRepository<PropertyDalDto>` | canonical DTO | `PropertyService` |
| `ResidentBllDto` | `ResidentDalDto` | `ResidentBllDtoMapper` | `IResidentRepository : IBaseRepository<ResidentDalDto>` | canonical DTO | `ResidentService` |
| `TicketBllDto` | `TicketDalDto` | `TicketBllDtoMapper` | `ITicketRepository : IBaseRepository<TicketDalDto>` | canonical DTO | `TicketService` |
| `UnitBllDto` | `UnitDalDto` | `UnitBllDtoMapper` | `IUnitRepository : IBaseRepository<UnitDalDto>` | canonical DTO | `UnitService` |
| `VendorBllDto` | `VendorDalDto` | `VendorBllDtoMapper` | `IVendorRepository : IBaseRepository<VendorDalDto>` | canonical DTO | `VendorService` if exposed later |

### Commands and Requests

| DTO | Classification | Redundancy candidate | Notes |
| --- | --- | --- | --- |
| `CreateCustomerCommand` | workflow command/request | Partial | Entity fields duplicate `CustomerBllDto`, but actor/company slug scope and slug generation make a command useful. Could become `CustomerBllDto + scope` later. |
| `UpdateCustomerProfileCommand` | workflow command/request | Partial | Duplicates canonical customer fields plus route/user scope. |
| `DeleteCustomerCommand` | workflow delete command | No | Carries confirmation name and route/user scope. |
| `CreatePropertyCommand` | workflow command/request | Partial | Duplicates canonical property fields plus route/user/customer scope. |
| `UpdatePropertyProfileCommand` | workflow command/request | Partial | Duplicates canonical property fields plus route/user/customer/property scope. |
| `DeletePropertyCommand` | workflow delete command | No | Carries confirmation and scoped route data. |
| `CreateUnitCommand` | workflow command/request | Partial | Duplicates canonical unit fields plus route/user/property scope. |
| `UpdateUnitCommand` | workflow command/request | Partial | Duplicates canonical unit fields plus route/user/property/unit scope. |
| `DeleteUnitCommand` | workflow delete command | No | Carries confirmation unit number and scope. |
| `CreateResidentCommand` | workflow command/request | Partial | Duplicates canonical resident fields plus user/company scope. |
| `UpdateResidentProfileCommand` | workflow command/request | Partial | Duplicates canonical resident fields plus route/user scope. |
| `DeleteResidentCommand` | workflow delete command | No | Carries confirmation ID code and scope. |
| `CreateLeaseFromResidentCommand` | workflow command/request | No | Inherits resident workspace scope and adds unit/role/dates. |
| `CreateLeaseFromUnitCommand` | workflow command/request | No | Inherits unit workspace scope and adds resident/role/dates. |
| `UpdateLeaseFromResidentCommand` | workflow command/request | Partial | Lease state duplicates canonical DTO, but parent context and IDOR scope are essential. |
| `UpdateLeaseFromUnitCommand` | workflow command/request | Partial | Same as above from unit context. |
| `DeleteLeaseFromResidentCommand` | workflow delete command | No | Context-specific delete guard. |
| `DeleteLeaseFromUnitCommand` | workflow delete command | No | Context-specific delete guard. |
| `CreateManagementTicketCommand` | workflow command/request | Partial | Duplicates many `TicketBllDto` fields plus actor/company scope and create-status policy. |
| `UpdateManagementTicketCommand` | workflow command/request | Partial | Duplicates `TicketBllDto` fields plus scope and status guard semantics. |
| `DeleteManagementTicketCommand` | workflow delete command | No | Scope-aware delete. |
| `AdvanceManagementTicketStatusCommand` | workflow command/request | No | Explicit lifecycle transition command. |
| `CompanyProfileUpdateRequest` | workflow command/request | Partial | Duplicates `ManagementCompanyBllDto` mutable fields. |
| `CompanyMembershipAddRequest` | workflow command/request | No | Membership workflow; no canonical BLL membership DTO exists. |
| `CompanyMembershipUpdateRequest` | workflow command/request | No | Membership workflow; no canonical BLL membership DTO exists. |
| `TransferOwnershipRequest` | workflow command/request | No | Explicit ownership workflow. |
| `CompleteAccountOnboardingCommand` | workflow command/request | No | Onboarding action. |
| `CreateCompanyJoinRequestCommand` | workflow command/request | No | Maps to join-request aggregate plus lookup/status policy. |
| `CreateManagementCompanyCommand` | workflow command/request | Partial | Duplicates management company fields plus app user and initial owner policy. |
| `LoginAccountCommand` | workflow command/request | No | Account auth request. |
| `LogoutCommand` | workflow command/request | Possible remove | Empty command; not currently exposed through `IAppBLL` methods. Keep until usage checked. |
| `RegisterAccountCommand` | workflow command/request | No | Account auth request. |
| `SelectWorkspaceCommand` | workflow command/request | No | Context selection command. |

### Queries and Filters

| DTO | Classification | Redundancy candidate | Notes |
| --- | --- | --- | --- |
| `GetCompanyCustomersQuery` | query/scope | No | Actor + company scope. |
| `GetCustomerProfileQuery` | query/scope | No | Actor + company/customer route scope. |
| `GetCustomerWorkspaceQuery` | query/scope | No | Actor + company/customer route scope. |
| `GetPropertyProfileQuery` | query/scope | No | Actor + nested route scope. |
| `GetPropertyWorkspaceQuery` | query/scope | No | Actor + nested route scope. |
| `GetPropertyUnitsQuery` | query/scope | No | Actor + nested route scope. |
| `GetUnitDashboardQuery` | query/scope | No | Actor + nested route scope. |
| `GetUnitProfileQuery` | query/scope | No | Actor + nested route scope. |
| `GetResidentsQuery` | query/scope | No | Actor + company scope. |
| `GetResidentProfileQuery` | query/scope | No | Actor + company/resident natural key. |
| `GetResidentLeasesQuery` | query/context | No | Resident workspace context used by lease flows. |
| `GetUnitLeasesQuery` | query/context | No | Unit workspace context used by lease flows. |
| `GetResidentLeaseQuery` | query/context | No | Adds lease ID to resident lease context. |
| `GetUnitLeaseQuery` | query/context | No | Adds lease ID to unit lease context. |
| `SearchLeasePropertiesQuery` | query/filter | No | Search term plus resident lease context. |
| `GetLeaseUnitsForPropertyQuery` | query/filter | No | Property selection plus resident lease context. |
| `SearchLeaseResidentsQuery` | query/filter | No | Search term plus unit lease context. |
| `GetManagementTicketsQuery` | query/filter | No | Actor/company scope plus ticket filters. |
| `GetManagementTicketQuery` | query/scope | No | Actor/company/ticket scope. |
| `GetManagementTicketSelectorOptionsQuery` | query/filter | No | Actor/company scope plus cascading option context. |
| `AuthorizeContextSelectionQuery` | access query | No | Context authorization. |
| `GetOnboardingStateQuery` | query | No | Actor scope. |
| `GetWorkspaceCatalogQuery` | query | No | Actor + optional selected company. |
| `ResolveWorkspaceRedirectQuery` | workflow/access query | No | Actor + remembered context. |
| `RememberedWorkspaceContext` | helper/context model | No | Nested query helper. |

### Projections, Read Models, Options, Constants, and Errors

| DTO/type | Classification | Redundancy candidate | Notes |
| --- | --- | --- | --- |
| `CompanyCustomerModel` | projection/read model | Partial | Created-customer response duplicates customer fields plus company context. |
| `CompanyWorkspaceModel` | access/context model | No | Tenant/workspace context. |
| `CustomerListItemModel` | projection/read model | No | List projection with property links. |
| `CustomerProfileModel` | projection/read model | Partial | Close to canonical customer plus company context. |
| `CustomerPropertyLinkModel` | projection/read model | No | Nested customer list data. |
| `CustomerWorkspaceModel` | access/context model | No | Tenant/customer context. |
| `PropertyDashboardModel` | dashboard/read model | No | Dashboard projection. |
| `PropertyListItemModel` | projection/read model | No | List projection with lookup label. |
| `PropertyProfileModel` | projection/read model | Partial | Close to canonical property plus company/customer/lookup context. |
| `PropertyTypeOptionModel` | options/dropdown model | No | Lookup option. |
| `PropertyWorkspaceModel` | access/context model | No | Tenant/customer/property context. |
| `PropertyUnitsModel` | projection/read model | No | Context plus unit list. |
| `UnitDashboardModel` | dashboard/read model | No | Dashboard projection. |
| `UnitListItemModel` | projection/read model | No | Unit list item. |
| `UnitProfileModel` | projection/read model | Partial | Close to canonical unit plus company/customer/property context. |
| `UnitWorkspaceModel` | access/context model | No | Tenant/customer/property/unit context. |
| `CompanyResidentsModel` | projection/read model | No | Company context plus resident list. |
| `ResidentContactModel` | projection/read model | No | Contact projection with lookup labels. |
| `ResidentDashboardModel` | dashboard/read model | No | Dashboard projection. |
| `ResidentLeaseSummaryModel` | projection/read model | No | Lease summary projection. |
| `ResidentListItemModel` | projection/read model | No | Resident list item with full name. |
| `ResidentProfileModel` | projection/read model | Partial | Close to canonical resident plus company/full-name context. |
| `ResidentWorkspaceModel` | access/context model | No | Tenant/resident context. |
| `ResidentLeaseListModel` | projection/read model | No | Resident lease list wrapper. |
| `ResidentLeaseModel` | projection/read model | No | Resident lease projection. |
| `UnitLeaseListModel` | projection/read model | No | Unit lease list wrapper. |
| `UnitLeaseModel` | projection/read model | No | Unit lease projection. |
| `LeaseModel` | projection/read model | Partial | Close to canonical lease but uses `LeaseId` property and omits route context. |
| `LeaseCommandModel` | helper/result model | Possible remove | Only returns `LeaseId`; could be replaced by `Result<Guid>` in later phase. |
| `LeasePropertySearchResultModel` | options/search model | No | Search result wrapper. |
| `LeasePropertySearchItemModel` | options/search model | No | Search projection. |
| `LeaseUnitOptionsModel` | options/dropdown model | No | Options wrapper. |
| `LeaseUnitOptionModel` | options/dropdown model | No | Option item. |
| `LeaseResidentSearchResultModel` | options/search model | No | Search result wrapper. |
| `LeaseResidentSearchItemModel` | options/search model | No | Search projection. |
| `LeaseRoleOptionsModel` | options/dropdown model | No | Lookup options wrapper. |
| `LeaseRoleOptionModel` | options/dropdown model | No | Lookup option. |
| `ManagementTicketsModel` | projection/read model | No | List page/API-ready aggregate projection with filters/options. |
| `ManagementTicketListItemModel` | projection/read model | No | Ticket list projection. |
| `ManagementTicketDetailsModel` | projection/read model | No | Ticket details projection. |
| `ManagementTicketFormModel` | form/read model | Partial | Web-form-shaped, but still DTO-first; later API contract may prefer separate create/edit defaults models. |
| `TicketFilterModel` | query/filter model | No | Preserves filter state. |
| `TicketSelectorOptionsModel` | options/dropdown model | No | Cascading ticket options. |
| `TicketOptionModel` | options/dropdown model | No | Option item. |
| `TicketWorkflowConstants` | constant/helper | No | Lifecycle constants. |
| `CompanyMembershipAuthorizationFailureReason` | constant/helper enum | No | Typed authorization reason. |
| `CompanyMembershipUserActionBlockReason` | constant/helper enum | No | Typed block reason. |
| `CompanyMembershipContext` | access/context model | No | Effective member context. |
| `CompanyAdminAuthorizedContext` | access/context model | No | Admin-authorized context. |
| `CompanyMembershipListResult` | projection/read model | No | Membership list wrapper. |
| `CompanyMembershipUserListItem` | projection/read model | No | Membership list item. |
| `CompanyMembershipEditModel` | projection/read model | No | Membership edit projection and capabilities. |
| `CompanyMembershipRoleOption` | options/dropdown model | No | Role option. |
| `OwnershipTransferModel` | workflow result model | No | Ownership transfer result. |
| `OwnershipTransferCandidate` | projection/read model | No | Ownership transfer candidate. |
| `PendingAccessRequestListResult` | projection/read model | No | Access request list wrapper. |
| `PendingAccessRequestItem` | projection/read model | No | Access request list item. |
| `CompanyProfileModel` | projection/read model | Partial | Close to canonical management company plus naming differences. |
| `ManagementCompanyJoinRequestStatusCodes` | constant/helper | No | Stable status code constants. |
| `AccountLoginModel` | workflow result model | Possible remove | Not currently surfaced by listed contracts; verify usage. |
| `AccountRegisterModel` | workflow result model | Possible remove | Not currently surfaced by listed contracts; verify usage. |
| `CreateManagementCompanyModel` | workflow result model | No | Onboarding create result. |
| `OnboardingJoinRequestModel` | workflow result model | Possible replace | Only returns `RequestId`; could become `Result<Guid>` in later phase. |
| `OnboardingStateModel` | projection/read model | No | Onboarding state. |
| `WorkspaceCatalogModel` | projection/read model | No | Workspace catalog. |
| `WorkspaceContextCatalogModel` | projection/read model | No | Context catalog wrapper. |
| `WorkspaceContextModel` | projection/read model | No | Context item. |
| `WorkspaceOptionModel` | options/dropdown model | No | Workspace option. |
| `WorkspaceRedirectDestination` | constant/helper enum | No | Redirect destination enum. |
| `WorkspaceRedirectModel` | workflow result model | No | Redirect result. |
| `WorkspaceSelectionAuthorizationModel` | access result model | No | Authorization result. |
| `ValidationFailureModel` | error/helper | No | Structured validation failure. |
| `BusinessRuleError` | error | No | App error. |
| `ConflictError` | error | No | App error. |
| `ForbiddenError` | error | No | App error. |
| `NotFoundError` | error | No | App error. |
| `UnauthorizedError` | error | No | App error. |
| `UnexpectedAppError` | error | No | App error. |
| `ValidationAppError` | error | No | App error. |
| `DuplicateRegistryCodeError` | error | Possible remove | Verify usage; generic `ConflictError` may be enough if unused. |
| `DuplicateResidentIdCodeError` | error | Possible remove | Verify usage; generic `ConflictError` or `ValidationAppError` may be enough if unused. |
| `ResidentValidationError` | error | Possible remove | Overlaps `ValidationAppError`; verify usage. |

## Mapper Inventory

| Mapper | Classification | BaseService readiness | Notes |
| --- | --- | --- | --- |
| `ContactBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<ContactBllDto, ContactDalDto>`. |
| `CustomerBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<CustomerBllDto, CustomerDalDto>`. |
| `PropertyBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<PropertyBllDto, PropertyDalDto>`. |
| `UnitBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<UnitBllDto, UnitDalDto>`. |
| `ResidentBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<ResidentBllDto, ResidentDalDto>`. |
| `LeaseBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<LeaseBllDto, LeaseDalDto>`. |
| `TicketBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<TicketBllDto, TicketDalDto>`. |
| `ManagementCompanyBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<ManagementCompanyBllDto, ManagementCompanyDalDto>`. |
| `ManagementCompanyJoinRequestBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<ManagementCompanyJoinRequestBllDto, ManagementCompanyJoinRequestDalDto>`. |
| `VendorBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | Ready | Direct `IBaseMapper<VendorBllDto, VendorDalDto>`. |
| `CustomerProfileBllMapper` | workflow/read-model projection mapper | Not BaseService | Maps `CustomerProfileDalDto` to `CustomerProfileModel`. |
| `CustomerWorkspaceBllMapper` | workflow/read-model projection mapper | Not BaseService | Maps management/customer workspace/list/create projections. |
| `PropertyBllMapper` | workflow/read-model projection mapper | Not BaseService | Maps workspace/list/profile/type-option projections. |
| `UnitBllMapper` | workflow/read-model projection mapper | Not BaseService | Maps dashboard/profile/list projections. |
| `ResidentBllMapper` | workflow/read-model projection mapper | Not BaseService | Maps resident workspace/profile/list/contact/lease summary projections. |
| `LeaseBllMapper` | workflow/read-model projection mapper | Not BaseService | Maps lease context, list, details, search, and options projections. |

Missing mapper candidates:

- `CompanyMembershipAdminService` maps membership, role, ownership, and pending request models inline. This is acceptable as internal workflow mapping today, but a future `CompanyMembershipService` may benefit from private mapper helpers if the service is split.
- `ManagementTicketService` maps ticket list/details/options inline. Later `TicketService` can either keep private workflow mapping or introduce a `TicketBllMapper` for projections. Do not confuse this with the canonical `TicketBllDtoMapper`.
- `ManagementCompanyProfileService` likely maps company profile inline; a projection mapper is optional.

Duplicate/unnecessary mapper candidates:

- None among canonical mappers; all have corresponding DAL DTOs and repositories.
- Projection mappers are justified because their target models carry workspace, lookup label, dashboard, or option data that canonical DTOs do not carry.

## Current Service-to-Target Mapping

| Current service | Implements | Target owner | Role after refactor |
| --- | --- | --- | --- |
| `AccountOnboardingService` | `IAccountOnboardingService` | `OnboardingService` | Orchestration exception. |
| `OnboardingCompanyJoinRequestService` | `IOnboardingCompanyJoinRequestService` | `OnboardingService` or `CompanyMembershipService` | Workflow around join request aggregate. |
| `WorkspaceContextService` | `IWorkspaceContextService` | `WorkspaceService` | Orchestration exception. |
| `UserWorkspaceCatalogService` | `IWorkspaceCatalogService` | `WorkspaceService` | Orchestration exception. |
| `WorkspaceRedirectService` | `IWorkspaceRedirectService`, `IContextSelectionService` | `WorkspaceService` | Orchestration exception; remove facade cast carefully. |
| `ManagementCompanyProfileService` | `IManagementCompanyProfileService` | `ManagementCompanyService` | Aggregate-backed service with member authorization. |
| `CompanyMembershipAdminService` | `ICompanyMembershipAdminService` plus split membership interfaces | `CompanyMembershipService` | Orchestration/policy exception unless a natural membership aggregate is introduced. |
| `CompanyCustomerService` | `ICompanyCustomerService` | `CustomerService` | Aggregate-backed customer service method group. |
| `CustomerAccessService` | `ICustomerAccessService` | Internal helper/policy | Tenant/access resolver. |
| `CustomerProfileService` | `ICustomerProfileService` | `CustomerService` | Aggregate-backed customer service method group. |
| `CustomerWorkspaceService` | `ICustomerWorkspaceService` | `CustomerService` or `WorkspaceService` | Workspace projection; likely helper under customer domain. |
| `PropertyProfileService` | `IPropertyProfileService` | `PropertyService` | Aggregate-backed property service method group. |
| `PropertyWorkspaceService` | `IPropertyWorkspaceService` | `PropertyService` | Aggregate-backed property service plus projections/options. |
| `ResidentAccessService` | `IResidentAccessService` | Internal helper/policy | Tenant/access resolver. |
| `ResidentProfileService` | `IResidentProfileService` | `ResidentService` | Aggregate-backed resident service method group. |
| `ResidentWorkspaceService` | `IResidentWorkspaceService` | `ResidentService` | Aggregate-backed resident service plus projections. |
| `UnitAccessService` | `IUnitAccessService` | Internal helper/policy | Tenant/access resolver. |
| `UnitProfileService` | `IUnitProfileService` | `UnitService` | Aggregate-backed unit service method group. |
| `UnitWorkspaceService` | `IUnitWorkspaceService` | `UnitService` | Aggregate-backed unit service plus projections. |
| `LeaseAssignmentService` | `ILeaseAssignmentService` | `LeaseService` | Aggregate-backed lease workflow. |
| `LeaseLookupService` | `ILeaseLookupService` | `LeaseService` or internal helper | Search/options helper under lease domain. |
| `ManagementTicketService` | `IManagementTicketService` | `TicketService` | Aggregate-backed ticket service plus workflow/projections. |
| `AppDeleteGuard` | `IAppDeleteGuard` | Internal helper/policy | Delete dependency guard; not public domain service. |
| `SlugGenerator` | none | Internal helper | Utility for slugs; no service contract. |

## BaseService Inheritance Target List

Strong candidates for future public domain services that should inherit `BaseService`:

| Future service | Primary aggregate | Canonical BLL DTO | DAL repository | Mapper | Current methods to absorb |
| --- | --- | --- | --- | --- | --- |
| `CustomerService` | Customer | `CustomerBllDto` | `ICustomerRepository` | `CustomerBllDtoMapper` | Company customer list/create, profile get/update/delete, workspace helpers. |
| `PropertyService` | Property | `PropertyBllDto` | `IPropertyRepository` | `PropertyBllDtoMapper` | Property profile/workspace/list/create/update/delete/options. |
| `UnitService` | Unit | `UnitBllDto` | `IUnitRepository` | `UnitBllDtoMapper` | Unit dashboard/list/create/profile/update/delete. |
| `ResidentService` | Resident | `ResidentBllDto` | `IResidentRepository` | `ResidentBllDtoMapper` | Resident list/create/dashboard/profile/update/delete/access helpers. |
| `LeaseService` | Lease | `LeaseBllDto` | `ILeaseRepository` | `LeaseBllDtoMapper` | Lease assignment list/get/create/update/delete and lookup helpers. |
| `TicketService` | Ticket | `TicketBllDto` | `ITicketRepository` | `TicketBllDtoMapper` | Management ticket list/details/forms/options/create/update/delete/status advance. |
| `ManagementCompanyService` | Management company | `ManagementCompanyBllDto` | `IManagementCompanyRepository` | `ManagementCompanyBllDtoMapper` | Profile get/update/delete and maybe management access checks. |

Conditional candidates:

| Future service | Primary aggregate | Reason conditional |
| --- | --- | --- |
| `OnboardingService` | Management company join request | `ManagementCompanyJoinRequestBllDto`, repository, and mapper exist, but the service is primarily onboarding workflow. It may compose a BaseService-backed internal join-request service or remain an orchestration exception. |
| `ContactService` | Contact | Ready but not currently exposed in `IAppBLL`; expose only when needed. |
| `VendorService` | Vendor | Ready but not currently exposed in `IAppBLL`; expose only when needed. |
| `CompanyMembershipService` | Management company membership | No canonical BLL membership DTO or membership repository currently exists; likely documented orchestration exception unless a membership aggregate/repository is introduced. |

Known BaseService issues to handle before adoption:

- No current `App.BLL/Services` class inherits `BaseService`.
- `BaseService.FindAsync` returns `Result.Fail("Entity not found")`, not a typed `NotFoundError`; concrete services may need to wrap or override for app-level errors.
- Tenant-scoped parent IDs must be passed deliberately. BaseService alone does not enforce management-company tenant rules or role checks.
- Command DTOs currently include actor and route scope; future canonical DTO adoption must not make client-provided tenant IDs authoritative.
- Projection-heavy methods should remain explicit concrete methods on top of BaseService.

## Orchestration Exception Candidates

These should be documented as exceptions if exposed from the final `IAppBLL` facade and not inherited from `BaseService`:

| Candidate | Reason |
| --- | --- |
| `OnboardingService` | Multi-step account, company creation, join request, and context setup workflow. |
| `WorkspaceService` | Cross-domain workspace catalog, remembered-context redirect, and context-selection authorization. |
| `CompanyMembershipService` | Membership authorization, role assignment, owner transfer, pending access review, transaction handling, and capability policy; no natural BaseService-backed membership repository exists today. |

Not public domain service candidates:

- `CustomerAccessService`
- `ResidentAccessService`
- `UnitAccessService`
- `AppDeleteGuard`
- `SlugGenerator`
- Internal validators/builders that may be extracted later

## DTO Removal and Replacement Candidates

Do not remove in Phase 1. These are next-phase review candidates only.

| Candidate | Possible replacement | Risk |
| --- | --- | --- |
| `CreateCustomerCommand`, `UpdateCustomerProfileCommand` | `CustomerBllDto + explicit actor/company/customer scope` | Must preserve slug generation, uniqueness checks, tenant authorization, and route natural keys. |
| `CreatePropertyCommand`, `UpdatePropertyProfileCommand` | `PropertyBllDto + explicit scope` | Must preserve customer/property route scope and lookup validation. |
| `CreateUnitCommand`, `UpdateUnitCommand` | `UnitBllDto + explicit scope` | Must preserve property route scope and slug uniqueness. |
| `CreateResidentCommand`, `UpdateResidentProfileCommand` | `ResidentBllDto + explicit scope` | Must preserve ID code uniqueness and tenant checks. |
| `CreateManagementTicketCommand`, `UpdateManagementTicketCommand` | `TicketBllDto + explicit actor/company scope` | Must preserve lifecycle guard, default Created status, reference tenant validation, and due/vendor guard. |
| `CompanyProfileUpdateRequest` | `ManagementCompanyBllDto + explicit actor/company scope` | Must preserve slug/profile semantics and `DeleteCascadeAsync` guardrails. |
| `LeaseCommandModel` | `Result<Guid>` | Low risk if callers only need lease ID. Verify WebApp/API usage first. |
| `OnboardingJoinRequestModel` | `Result<Guid>` | Low risk if callers only need request ID. Verify WebApp/API usage first. |
| `AccountLoginModel`, `AccountRegisterModel`, `LogoutCommand` | Remove if unused | Must verify WebApp/API usage before deleting. |
| `DuplicateRegistryCodeError`, `DuplicateResidentIdCodeError`, `ResidentValidationError` | Generic `ConflictError` / `ValidationAppError` | Verify usage and external error mapping expectations. |
| `CompanyMembershipAdmin` split interfaces | Keep split internally or expose grouped domain facade | Current implementation already implements smaller interfaces, but `IAppBLL` exposes the broad admin interface. |

DTOs that should not be collapsed into canonical DTOs without strong reason:

- Workspace/context models: `CompanyWorkspaceModel`, `CustomerWorkspaceModel`, `PropertyWorkspaceModel`, `UnitWorkspaceModel`, `ResidentWorkspaceModel`, `WorkspaceCatalogModel`, `WorkspaceContextCatalogModel`.
- Options/dropdown/search models: property type options, lease search/options, ticket selector options, membership role options.
- Dashboard/read projections: property/unit/resident dashboards, ticket list/details, membership capability projections.
- Delete confirmation commands.
- Ticket status transition command.

## Service Merge and Wrapper Candidates

Likely facade regrouping in later phases:

| Target facade | Absorb/wrap current contracts |
| --- | --- |
| `ICustomerService Customers` | `ICompanyCustomerService`, `ICustomerProfileService`, `ICustomerWorkspaceService`; use `ICustomerAccessService` internally. |
| `IPropertyService Properties` | `IPropertyProfileService`, `IPropertyWorkspaceService`. |
| `IUnitService Units` | `IUnitProfileService`, `IUnitWorkspaceService`; use `IUnitAccessService` internally. |
| `IResidentService Residents` | `IResidentProfileService`, `IResidentWorkspaceService`; use `IResidentAccessService` internally. |
| `ILeaseService Leases` | `ILeaseAssignmentService`, `ILeaseLookupService`. |
| `ITicketService Tickets` | `IManagementTicketService`. |
| `IManagementCompanyService ManagementCompanies` | `IManagementCompanyProfileService`, maybe `IManagementCompanyAccessService`. |
| `ICompanyMembershipService Memberships` | `ICompanyMembershipAdminService` and its split authorization/query/command/options/ownership/request-review interfaces. |
| `IOnboardingService Onboarding` | `IAccountOnboardingService`, `IOnboardingCompanyJoinRequestService`. |
| `IWorkspaceService Workspaces` | `IWorkspaceContextService`, `IWorkspaceCatalogService`, `IWorkspaceRedirectService`, `IContextSelectionService`. |

## Risks and Unresolved Questions

- Build status is unknown because agents are instructed not to build.
- Existing uncommitted edits were present in `plans/v2/bll-refactor/02_PHASE_1_BLL_INVENTORY.md` and `plans/v2/bll-refactor/03_PHASE_2_BASESERVICE_RULES.md`; this report did not modify them.
- `ContextSelection` currently casts `WorkspaceRedirect` to `IContextSelectionService`. Later facade changes should replace that with an explicit `WorkspaceService` contract or a single implementation property.
- `CompanyMembershipAdminService` is large and implements many smaller interfaces. It is the highest-risk split/merge area because it contains authorization, business rules, ownership transfer, access request review, role options, and transaction logic.
- `BaseService` can provide CRUD foundation, but tenant isolation and role checks must remain in concrete methods before materialization. Do not expose inherited `FindAsync(id)` as public tenant-scoped behavior without scope/authorization wrapping.
- Ticket create/update/status advance have explicit lifecycle and reference-validation logic; BaseService adoption should support common CRUD mechanics only, not replace workflow guards.
- `ManagementCompany.DeleteCascadeAsync` is used by management company delete behavior and remains protected by master handoff constraints.
- Some DTOs look redundant only because WebApp routes currently encode scope in command/query DTOs. Reducing DTOs must not move trust to client-provided tenant IDs.
- `GetAddRoleOptionsAsync`, `GetAvailableRolesAsync`, onboarding bool/string helpers, and delete guard methods are non-Result; master handoff prefers `Result` for app-level methods, but these may remain internal helpers if no longer exposed publicly.

## Assumptions

- The intended deliverable location is `plans/v2/bll-refactor/phase-reports/PHASE_1_BLL_INVENTORY.md`, because the master handoff says phase reports go in `phase-reports` and the user explicitly pointed to that folder.
- `plans/bll-refactor/PHASE_1_BLL_INVENTORY.md` in the phase brief is an outdated path relative to the current `plans/v2/bll-refactor` structure.
- Static code inspection is sufficient for Phase 1 because the phase is inventory-only and build/test execution is prohibited by the master handoff.
- Contracts under `App.BLL.Contracts` that are not exposed from `IAppBLL` still count as BLL contract methods and were included in the method inventory.
- Projection/read models are considered API-ready if they are in `App.BLL.DTO`, contain no WebApp/API types, and return through `Result` where appropriate, even if later public REST DTOs should still live in `App.DTO/v1`.

## Handoff to Phase 2

Recommended Phase 2 starting points:

1. Define future domain-first contracts without changing WebApp callers yet, or introduce adapter/wrapper properties if Phase 2 allows compatibility layering.
2. Start BaseService adoption with one low-risk aggregate such as `CustomerService`, `PropertyService`, `UnitService`, or `ResidentService`.
3. Keep access services and delete guard internal; do not expose inherited BaseService CRUD directly without tenant-scoped wrappers.
4. Document orchestration exceptions for `OnboardingService`, `WorkspaceService`, and likely `CompanyMembershipService`.
5. Review DTO candidates, but only remove command/result DTOs after caller usage and tenant-scope semantics are verified.

Recommended commit message when build verification is OK:

```text
docs: add phase 1 BLL inventory
```

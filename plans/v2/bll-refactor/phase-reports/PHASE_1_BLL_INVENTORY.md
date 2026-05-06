# Phase 1 BLL Inventory Report

Source brief: `plans/v2/bll-refactor/02_PHASE_1_BLL_INVENTORY.md`  
Master handoff: `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md`  
Phase 0 report: `plans/v2/bll-refactor/phase-reports/PHASE_0_BASELINE_REPORT.md`  
Date: 2026-05-06

## Scope and Output Location

This phase was inventory-only. No BLL contracts, services, DTOs, mappers, WebApp code, tests, DAL code, schema, or behavior were changed.

The phase brief names `plans/bll-refactor/PHASE_1_BLL_INVENTORY.md`, but the master handoff states that phase reports go to `plans/v2/bll-refactor/phase-reports`. This report follows the master handoff location.

## Contract Method Inventory

Legend:

- Kind: `CRUD`, `workflow`, `query`, `projection`, `access`, `delete`, `options`, `guard`, `facade`.
- Canonical DTO: whether the method directly uses a canonical `*BllDto`.
- Command/query DTO: whether the input is a command/query DTO rather than primitive route/scope parameters.
- BaseService candidate: whether the method could eventually sit on, or be composed by, an aggregate-backed BaseService-derived domain service.
- API-ready: whether the method shape is usable behind API/MVC after mapping without WebApp dependencies.

| Service | Method | Input | Output | Result usage | Kind | Canonical DTO | Command/query DTO | BaseService candidate | API-ready | Notes |
|---|---|---|---|---|---|---|---|---|---|---|
| `IAppBLL` | `AccountOnboarding` | none | `IAccountOnboardingService` | no | facade | no | no | no | yes | Wide service exposure; target owner `OnboardingService`. |
| `IAppBLL` | `OnboardingCompanyJoinRequests` | none | `IOnboardingCompanyJoinRequestService` | no | facade | no | no | no | yes | Target owner `OnboardingService`. |
| `IAppBLL` | `WorkspaceContexts` | none | `IWorkspaceContextService` | no | facade | no | no | no | yes | Target owner `WorkspaceService`. |
| `IAppBLL` | `WorkspaceCatalog` | none | `IWorkspaceCatalogService` | no | facade | no | no | no | yes | Target owner `WorkspaceService`/`OnboardingService`. |
| `IAppBLL` | `WorkspaceRedirect` | none | `IWorkspaceRedirectService` | no | facade | no | no | no | yes | Target owner `WorkspaceService`. |
| `IAppBLL` | `ContextSelection` | none | `IContextSelectionService` | no | facade | no | no | no | yes | Implemented by redirect service; target owner `WorkspaceService`. |
| `IAppBLL` | `ManagementCompanyProfiles` | none | `IManagementCompanyProfileService` | no | facade | no | no | yes | yes | Target owner `ManagementCompanyService`. |
| `IAppBLL` | `CompanyMembershipAdmin` | none | `ICompanyMembershipAdminService` | no | facade | no | no | maybe | yes | Large orchestration/aggregate decision pending. |
| `IAppBLL` | `CompanyCustomers` | none | `ICompanyCustomerService` | no | facade | no | no | yes | yes | Target owner `CustomerService`. |
| `IAppBLL` | `CustomerAccess` | none | `ICustomerAccessService` | no | facade | no | no | no | yes | Target internal access policy/helper. |
| `IAppBLL` | `CustomerProfiles` | none | `ICustomerProfileService` | no | facade | no | no | yes | yes | Target owner `CustomerService`. |
| `IAppBLL` | `CustomerWorkspaces` | none | `ICustomerWorkspaceService` | no | facade | no | no | no | yes | Target owner `WorkspaceService` or customer projection methods. |
| `IAppBLL` | `PropertyProfiles` | none | `IPropertyProfileService` | no | facade | no | no | yes | yes | Target owner `PropertyService`. |
| `IAppBLL` | `PropertyWorkspaces` | none | `IPropertyWorkspaceService` | no | facade | no | no | partly | yes | Create belongs in `PropertyService`; workspace/list reads are projections. |
| `IAppBLL` | `ResidentAccess` | none | `IResidentAccessService` | no | facade | no | no | no | yes | Target internal access policy/helper. |
| `IAppBLL` | `ResidentProfiles` | none | `IResidentProfileService` | no | facade | no | no | yes | yes | Target owner `ResidentService`. |
| `IAppBLL` | `ResidentWorkspaces` | none | `IResidentWorkspaceService` | no | facade | no | no | partly | yes | Create belongs in `ResidentService`; dashboard/list reads are projections. |
| `IAppBLL` | `UnitAccess` | none | `IUnitAccessService` | no | facade | no | no | no | yes | Target internal access policy/helper. |
| `IAppBLL` | `UnitProfiles` | none | `IUnitProfileService` | no | facade | no | no | yes | yes | Target owner `UnitService`. |
| `IAppBLL` | `UnitWorkspaces` | none | `IUnitWorkspaceService` | no | facade | no | no | partly | yes | Create belongs in `UnitService`; dashboard/list reads are projections. |
| `IAppBLL` | `LeaseAssignments` | none | `ILeaseAssignmentService` | no | facade | no | no | yes | yes | Target owner `LeaseService`. |
| `IAppBLL` | `LeaseLookups` | none | `ILeaseLookupService` | no | facade | no | no | no | yes | Target lookup/query methods under `LeaseService` or helper. |
| `IAppBLL` | `ManagementTickets` | none | `IManagementTicketService` | no | facade | no | no | yes | yes | Target owner `TicketService`. |
| `ICompanyCustomerService` | `GetCompanyCustomersAsync` | `GetCompanyCustomersQuery` | `IReadOnlyList<CustomerListItemModel>` | `Result<T>` | query/projection | no | yes | no | yes | Tenant-scoped customer list projection. |
| `ICompanyCustomerService` | `CreateCustomerAsync` | `CreateCustomerCommand` | `CompanyCustomerModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Future canonical mutation should return `CustomerBllDto`; projection method can compose it. |
| `ICustomerAccessService` | `ResolveCompanyWorkspaceAsync` | `GetCompanyCustomersQuery` | `CompanyWorkspaceModel` | `Result<T>` | access/projection | no | yes | no | yes | Should become internal scope/access resolution. |
| `ICustomerAccessService` | `ResolveCustomerWorkspaceAsync` | `GetCustomerWorkspaceQuery` | `CustomerWorkspaceModel` | `Result<T>` | access/projection | no | yes | no | yes | Should become internal scope/access resolution. |
| `ICustomerProfileService` | `GetAsync` | `GetCustomerProfileQuery` | `CustomerProfileModel` | `Result<T>` | query/projection | no | yes | partly | yes | Aggregate read can compose BaseService after trusted scope resolution. |
| `ICustomerProfileService` | `UpdateAsync` | `UpdateCustomerProfileCommand` | `CustomerProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for `CustomerRoute + CustomerBllDto`; projection wrapper composes canonical update. |
| `ICustomerProfileService` | `DeleteAsync` | `DeleteCustomerCommand` | none | `Result` | delete | no | yes | yes | yes | Needs tenant scope, confirmation, delete guard; can compose BaseService remove after checks. |
| `ICustomerWorkspaceService` | `GetWorkspaceAsync` | `GetCustomerWorkspaceQuery` | `CustomerWorkspaceModel` | `Result<T>` | access/projection | no | yes | no | yes | Workspace projection, not CRUD. |
| `IPropertyProfileService` | `GetAsync` | `GetPropertyProfileQuery` | `PropertyProfileModel` | `Result<T>` | query/projection | no | yes | partly | yes | Aggregate read can compose BaseService after scope resolution. |
| `IPropertyProfileService` | `UpdateAsync` | `UpdatePropertyProfileCommand` | `PropertyProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for `PropertyRoute + PropertyBllDto`. |
| `IPropertyProfileService` | `DeleteAsync` | `DeletePropertyCommand` | none | `Result` | delete | no | yes | yes | yes | Needs tenant/customer scope, confirmation, delete guard. |
| `IPropertyWorkspaceService` | `GetWorkspaceAsync` | `GetPropertyWorkspaceQuery` | `PropertyWorkspaceModel` | `Result<T>` | access/projection | no | yes | no | yes | Workspace projection. |
| `IPropertyWorkspaceService` | `GetDashboardAsync` | `GetPropertyWorkspaceQuery` | `PropertyDashboardModel` | `Result<T>` | projection | no | yes | no | yes | Dashboard projection. |
| `IPropertyWorkspaceService` | `GetCustomerPropertiesAsync` | `GetPropertyWorkspaceQuery` | `IReadOnlyList<PropertyListItemModel>` | `Result<T>` | query/projection | no | yes | no | yes | Customer-scoped list projection. |
| `IPropertyWorkspaceService` | `GetPropertyTypeOptionsAsync` | none | `IReadOnlyList<PropertyTypeOptionModel>` | `Result<T>` | options | no | no | no | yes | Lookup/options method. |
| `IPropertyWorkspaceService` | `CreateAsync` | `CreatePropertyCommand` | `PropertyProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for canonical create returning `PropertyBllDto`, then profile projection. |
| `IResidentAccessService` | `ResolveCompanyResidentsAsync` | `GetResidentsQuery` | `CompanyResidentsModel` | `Result<T>` | access/projection | no | yes | no | yes | Should become internal scope/access resolution. |
| `IResidentAccessService` | `ResolveResidentWorkspaceAsync` | `GetResidentProfileQuery` | `ResidentWorkspaceModel` | `Result<T>` | access/projection | no | yes | no | yes | Should become internal scope/access resolution. |
| `IResidentProfileService` | `GetAsync` | `GetResidentProfileQuery` | `ResidentProfileModel` | `Result<T>` | query/projection | no | yes | partly | yes | Aggregate read can compose BaseService after scope resolution. |
| `IResidentProfileService` | `UpdateAsync` | `UpdateResidentProfileCommand` | `ResidentProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for `ResidentRoute + ResidentBllDto`. |
| `IResidentProfileService` | `DeleteAsync` | `DeleteResidentCommand` | none | `Result` | delete | no | yes | yes | yes | Needs tenant scope, confirmation, delete guard. |
| `IResidentWorkspaceService` | `GetDashboardAsync` | `GetResidentProfileQuery` | `ResidentDashboardModel` | `Result<T>` | projection | no | yes | no | yes | Dashboard projection. |
| `IResidentWorkspaceService` | `GetResidentsAsync` | `GetResidentsQuery` | `CompanyResidentsModel` | `Result<T>` | query/projection | no | yes | no | yes | Company-scoped list projection. |
| `IResidentWorkspaceService` | `CreateAsync` | `CreateResidentCommand` | `ResidentProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for canonical create returning `ResidentBllDto`, then profile projection. |
| `IUnitAccessService` | `ResolveUnitWorkspaceAsync` | `GetUnitDashboardQuery` | `UnitWorkspaceModel` | `Result<T>` | access/projection | no | yes | no | yes | Should become internal scope/access resolution. |
| `IUnitProfileService` | `GetAsync` | `GetUnitProfileQuery` | `UnitProfileModel` | `Result<T>` | query/projection | no | yes | partly | yes | Aggregate read can compose BaseService after scope resolution. |
| `IUnitProfileService` | `UpdateAsync` | `UpdateUnitCommand` | `UnitProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for `UnitRoute + UnitBllDto`. |
| `IUnitProfileService` | `DeleteAsync` | `DeleteUnitCommand` | none | `Result` | delete | no | yes | yes | yes | Needs tenant/property scope, confirmation, delete guard. |
| `IUnitWorkspaceService` | `GetDashboardAsync` | `GetUnitDashboardQuery` | `UnitDashboardModel` | `Result<T>` | projection | no | yes | no | yes | Dashboard projection. |
| `IUnitWorkspaceService` | `GetPropertyUnitsAsync` | `GetPropertyUnitsQuery` | `PropertyUnitsModel` | `Result<T>` | query/projection | no | yes | no | yes | Property-scoped list projection. |
| `IUnitWorkspaceService` | `CreateAsync` | `CreateUnitCommand` | `UnitProfileModel` | `Result<T>` | CRUD/projection | no | yes | yes | yes | Candidate for canonical create returning `UnitBllDto`, then profile projection. |
| `ILeaseAssignmentService` | `ListForResidentAsync` | `GetResidentLeasesQuery` | `ResidentLeaseListModel` | `Result<T>` | query/projection | no | yes | no | yes | Resident-scoped lease list. |
| `ILeaseAssignmentService` | `ListForUnitAsync` | `GetUnitLeasesQuery` | `UnitLeaseListModel` | `Result<T>` | query/projection | no | yes | no | yes | Unit-scoped lease list. |
| `ILeaseAssignmentService` | `GetForResidentAsync` | `GetResidentLeaseQuery` | `LeaseModel` | `Result<T>` | query/projection | no | yes | partly | yes | Could use BaseService find after resident scope checks. |
| `ILeaseAssignmentService` | `GetForUnitAsync` | `GetUnitLeaseQuery` | `LeaseModel` | `Result<T>` | query/projection | no | yes | partly | yes | Could use BaseService find after unit scope checks. |
| `ILeaseAssignmentService` | `CreateFromResidentAsync` | `CreateLeaseFromResidentCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | no | yes | yes | yes | Multi-parent route variant; canonical create should use `LeaseBllDto` after scope resolution. |
| `ILeaseAssignmentService` | `CreateFromUnitAsync` | `CreateLeaseFromUnitCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | no | yes | yes | yes | Same canonical mutation should back both route variants. |
| `ILeaseAssignmentService` | `UpdateFromResidentAsync` | `UpdateLeaseFromResidentCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | no | yes | yes | yes | Candidate for canonical update plus projection/command response wrapper. |
| `ILeaseAssignmentService` | `UpdateFromUnitAsync` | `UpdateLeaseFromUnitCommand` | `LeaseCommandModel` | `Result<T>` | CRUD/workflow | no | yes | yes | yes | Same canonical mutation should back both route variants. |
| `ILeaseAssignmentService` | `DeleteFromResidentAsync` | `DeleteLeaseFromResidentCommand` | none | `Result` | delete/workflow | no | yes | yes | yes | Needs resident/unit tenant scope; compose BaseService remove after checks. |
| `ILeaseAssignmentService` | `DeleteFromUnitAsync` | `DeleteLeaseFromUnitCommand` | none | `Result` | delete/workflow | no | yes | yes | yes | Same delete mutation should back both route variants. |
| `ILeaseLookupService` | `SearchPropertiesAsync` | `SearchLeasePropertiesQuery` | `LeasePropertySearchResultModel` | `Result<T>` | query/options | no | yes | no | yes | Lookup/search projection. |
| `ILeaseLookupService` | `ListUnitsForPropertyAsync` | `GetLeaseUnitsForPropertyQuery` | `LeaseUnitOptionsModel` | `Result<T>` | options | no | yes | no | yes | Lookup/options projection. |
| `ILeaseLookupService` | `SearchResidentsAsync` | `SearchLeaseResidentsQuery` | `LeaseResidentSearchResultModel` | `Result<T>` | query/options | no | yes | no | yes | Lookup/search projection. |
| `ILeaseLookupService` | `ListLeaseRolesAsync` | none | `LeaseRoleOptionsModel` | `Result<T>` | options | no | no | no | yes | Global lookup/options. |
| `IManagementTicketService` | `GetTicketsAsync` | `GetManagementTicketsQuery` | `ManagementTicketsModel` | `Result<T>` | query/projection | no | yes | no | yes | Filtered ticket list projection. |
| `IManagementTicketService` | `GetDetailsAsync` | `GetManagementTicketQuery` | `ManagementTicketDetailsModel` | `Result<T>` | query/projection | no | yes | partly | yes | Could compose BaseService find after scope checks. |
| `IManagementTicketService` | `GetCreateFormAsync` | `GetManagementTicketsQuery` | `ManagementTicketFormModel` | `Result<T>` | projection/options | no | yes | no | yes | Form/options projection. |
| `IManagementTicketService` | `GetEditFormAsync` | `GetManagementTicketQuery` | `ManagementTicketFormModel` | `Result<T>` | projection/options | no | yes | no | yes | Form/options projection with entity state. |
| `IManagementTicketService` | `GetSelectorOptionsAsync` | `GetManagementTicketSelectorOptionsQuery` | `TicketSelectorOptionsModel` | `Result<T>` | options | no | yes | no | yes | Scoped selector options. |
| `IManagementTicketService` | `CreateAsync` | `CreateManagementTicketCommand` | `Guid` | `Result<T>` | CRUD/workflow | no | yes | yes | yes | Candidate for canonical create returning `TicketBllDto`; workflow/selector validation remains in service. |
| `IManagementTicketService` | `UpdateAsync` | `UpdateManagementTicketCommand` | none | `Result` | CRUD/workflow | no | yes | yes | yes | Candidate for canonical update using `TicketBllDto`; lifecycle guards remain explicit. |
| `IManagementTicketService` | `DeleteAsync` | `DeleteManagementTicketCommand` | none | `Result` | delete | no | yes | yes | yes | Needs tenant scope and delete guard. |
| `IManagementTicketService` | `AdvanceStatusAsync` | `AdvanceManagementTicketStatusCommand` | none | `Result` | workflow | no | yes | no | yes | Real lifecycle workflow, not generic CRUD. |
| `IManagementCompanyProfileService` | `GetProfileAsync` | `Guid appUserId`, `string companySlug` | `CompanyProfileModel` | `Result<T>` | query/projection | no | no | partly | yes | Could compose BaseService find after membership authorization. |
| `IManagementCompanyProfileService` | `UpdateProfileAsync` | `Guid appUserId`, `string companySlug`, `CompanyProfileUpdateRequest` | none | `Result` | CRUD | no | no | yes | yes | Candidate for `ManagementCompanyScope + ManagementCompanyBllDto`. |
| `IManagementCompanyProfileService` | `DeleteProfileAsync` | `Guid appUserId`, `string companySlug` | none | `Result` | delete | no | no | yes | yes | Preserve existing `ManagementCompany.DeleteCascadeAsync` behavior per handoff. |
| `ICompanyMembershipAdminService` | `AuthorizeManagementAreaAccessAsync` | `Guid appUserId`, `string companySlug` | `CompanyMembershipContext` | `Result<T>` | access | no | no | no | yes | Scope/authorization operation. |
| `ICompanyMembershipAdminService` | `AuthorizeAsync` | `Guid appUserId`, `string companySlug` | `CompanyAdminAuthorizedContext` | `Result<T>` | access | no | no | no | yes | Scope/authorization operation. |
| `ICompanyMembershipAdminService` | `ListCompanyMembersAsync` | `CompanyAdminAuthorizedContext` | `CompanyMembershipListResult` | `Result<T>` | query/projection | no | no | maybe | yes | Membership aggregate decision pending. |
| `ICompanyMembershipAdminService` | `GetMembershipForEditAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId` | `CompanyMembershipEditModel` | `Result<T>` | query/projection | no | no | maybe | yes | Membership aggregate decision pending. |
| `ICompanyMembershipAdminService` | `GetAddRoleOptionsAsync` | `CompanyAdminAuthorizedContext` | `IReadOnlyList<CompanyMembershipRoleOption>` | no `Result` | options | no | no | no | mostly | Should likely return `Result<T>` for consistent BLL outcomes. |
| `ICompanyMembershipAdminService` | `GetEditRoleOptionsAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId` | `IReadOnlyList<CompanyMembershipRoleOption>` | `Result<T>` | options | no | no | no | yes | Scoped role options. |
| `ICompanyMembershipAdminService` | `AddUserByEmailAsync` | `CompanyAdminAuthorizedContext`, `CompanyMembershipAddRequest` | `Guid` | `Result<T>` | workflow/CRUD | no | no | maybe | yes | User lookup plus membership mutation; orchestration likely. |
| `ICompanyMembershipAdminService` | `UpdateMembershipAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId`, `CompanyMembershipUpdateRequest` | none | `Result` | CRUD/workflow | no | no | maybe | yes | Role/capability business rules. |
| `ICompanyMembershipAdminService` | `DeleteMembershipAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId` | none | `Result` | delete/workflow | no | no | maybe | yes | Self/owner protection rules. |
| `ICompanyMembershipAdminService` | `GetOwnershipTransferCandidatesAsync` | `CompanyAdminAuthorizedContext` | `IReadOnlyList<OwnershipTransferCandidate>` | `Result<T>` | query/projection | no | no | no | yes | Ownership workflow projection. |
| `ICompanyMembershipAdminService` | `TransferOwnershipAsync` | `CompanyAdminAuthorizedContext`, `TransferOwnershipRequest` | `OwnershipTransferModel` | `Result<T>` | workflow | no | no | no | yes | Real workflow. |
| `ICompanyMembershipAdminService` | `GetAvailableRolesAsync` | none | `IReadOnlyList<CompanyMembershipRoleOption>` | no `Result` | options | no | no | no | partial | Existing compatibility method; not actor-aware. |
| `ICompanyMembershipAdminService` | `GetPendingAccessRequestsAsync` | `CompanyAdminAuthorizedContext` | `PendingAccessRequestListResult` | `Result<T>` | query/projection | no | no | no | yes | Onboarding/membership workflow. |
| `ICompanyMembershipAdminService` | `ApprovePendingAccessRequestAsync` | `CompanyAdminAuthorizedContext`, `Guid requestId` | none | `Result` | workflow | no | no | no | yes | Onboarding/membership workflow. |
| `ICompanyMembershipAdminService` | `RejectPendingAccessRequestAsync` | `CompanyAdminAuthorizedContext`, `Guid requestId` | none | `Result` | workflow | no | no | no | yes | Onboarding/membership workflow. |
| `ICompanyMembershipAuthorizationService` | `AuthorizeManagementAreaAccessAsync` | `Guid appUserId`, `string companySlug` | `CompanyMembershipContext` | `Result<T>` | access | no | no | no | yes | Split interface implemented by same admin service. |
| `ICompanyMembershipAuthorizationService` | `AuthorizeAsync` | `Guid appUserId`, `string companySlug` | `CompanyAdminAuthorizedContext` | `Result<T>` | access | no | no | no | yes | Split interface implemented by same admin service. |
| `IManagementCompanyAccessService` | `AuthorizeManagementAreaAccessAsync` | `Guid appUserId`, `string companySlug` | `CompanyMembershipContext` | `Result<T>` | access | no | no | no | yes | Duplicate access contract shape; not exposed in `IAppBLL`. |
| `ICompanyMembershipQueryService` | `ListCompanyMembersAsync` | `CompanyAdminAuthorizedContext` | `CompanyMembershipListResult` | `Result<T>` | query/projection | no | no | maybe | yes | Split interface duplicate of admin service method. |
| `ICompanyMembershipQueryService` | `GetMembershipForEditAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId` | `CompanyMembershipEditModel` | `Result<T>` | query/projection | no | no | maybe | yes | Split interface duplicate of admin service method. |
| `ICompanyMembershipCommandService` | `AddUserByEmailAsync` | `CompanyAdminAuthorizedContext`, `CompanyMembershipAddRequest` | `Guid` | `Result<T>` | workflow/CRUD | no | no | maybe | yes | Split interface duplicate of admin service method. |
| `ICompanyMembershipCommandService` | `UpdateMembershipAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId`, `CompanyMembershipUpdateRequest` | none | `Result` | CRUD/workflow | no | no | maybe | yes | Split interface duplicate of admin service method. |
| `ICompanyMembershipCommandService` | `DeleteMembershipAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId` | none | `Result` | delete/workflow | no | no | maybe | yes | Split interface duplicate of admin service method. |
| `ICompanyRoleOptionsService` | `GetAddRoleOptionsAsync` | `CompanyAdminAuthorizedContext` | `IReadOnlyList<CompanyMembershipRoleOption>` | no `Result` | options | no | no | no | mostly | Consider `Result<T>` consistency. |
| `ICompanyRoleOptionsService` | `GetEditRoleOptionsAsync` | `CompanyAdminAuthorizedContext`, `Guid membershipId` | `IReadOnlyList<CompanyMembershipRoleOption>` | `Result<T>` | options | no | no | no | yes | Split interface duplicate of admin service method. |
| `ICompanyRoleOptionsService` | `GetAvailableRolesAsync` | none | `IReadOnlyList<CompanyMembershipRoleOption>` | no `Result` | options | no | no | no | partial | Compatibility method; not actor-aware. |
| `ICompanyOwnershipTransferService` | `GetOwnershipTransferCandidatesAsync` | `CompanyAdminAuthorizedContext` | `IReadOnlyList<OwnershipTransferCandidate>` | `Result<T>` | query/projection | no | no | no | yes | Split interface duplicate of admin service method. |
| `ICompanyOwnershipTransferService` | `TransferOwnershipAsync` | `CompanyAdminAuthorizedContext`, `TransferOwnershipRequest` | `OwnershipTransferModel` | `Result<T>` | workflow | no | no | no | yes | Split interface duplicate of admin service method. |
| `ICompanyAccessRequestReviewService` | `GetPendingAccessRequestsAsync` | `CompanyAdminAuthorizedContext` | `PendingAccessRequestListResult` | `Result<T>` | query/projection | no | no | no | yes | Split interface duplicate of admin service method. |
| `ICompanyAccessRequestReviewService` | `ApprovePendingAccessRequestAsync` | `CompanyAdminAuthorizedContext`, `Guid requestId` | none | `Result` | workflow | no | no | no | yes | Split interface duplicate of admin service method. |
| `ICompanyAccessRequestReviewService` | `RejectPendingAccessRequestAsync` | `CompanyAdminAuthorizedContext`, `Guid requestId` | none | `Result` | workflow | no | no | no | yes | Split interface duplicate of admin service method. |
| `IAccountOnboardingService` | `CreateManagementCompanyAsync` | `CreateManagementCompanyCommand` | `CreateManagementCompanyModel` | `Result<T>` | workflow/CRUD | no | yes | maybe | yes | Onboarding orchestration creates company and initial membership. |
| `IAccountOnboardingService` | `GetStateAsync` | `GetOnboardingStateQuery` | `OnboardingStateModel` | `Result<T>` | query/projection | no | yes | no | yes | Onboarding state projection. |
| `IAccountOnboardingService` | `CompleteAsync` | `CompleteAccountOnboardingCommand` | none | `Result` | workflow | no | yes | no | yes | User onboarding workflow. |
| `IAccountOnboardingService` | `HasAnyContextAsync` | `Guid appUserId` | `bool` | no | query/access | no | no | no | partial | Consider `Result<bool>` for consistency if contract changes. |
| `IAccountOnboardingService` | `GetDefaultManagementCompanySlugAsync` | `Guid appUserId` | `string?` | no | query/access | no | no | no | partial | Nullable non-Result may be acceptable internal query but inconsistent with rule. |
| `IAccountOnboardingService` | `UserHasManagementCompanyAccessAsync` | `Guid appUserId`, `string companySlug` | `bool` | no | access | no | no | no | partial | Consider `Result<bool>` or scoped auth method. |
| `IOnboardingCompanyJoinRequestService` | `CreateJoinRequestAsync` | `CreateCompanyJoinRequestCommand` | `OnboardingJoinRequestModel` | `Result<T>` | workflow/CRUD | no | yes | maybe | yes | Join request aggregate exists; can use canonical DTO internally. |
| `IWorkspaceContextService` | `GetContextsAsync` | `Guid appUserId` | `WorkspaceContextCatalogModel` | `Result<T>` | query/projection | no | no | no | yes | Workspace projection. |
| `IWorkspaceCatalogService` | `GetWorkspaceCatalogAsync` | `GetWorkspaceCatalogQuery` | `WorkspaceCatalogModel` | `Result<T>` | query/projection | no | yes | no | yes | Workspace projection. |
| `IWorkspaceRedirectService` | `ResolveContextRedirectAsync` | `ResolveWorkspaceRedirectQuery` | `WorkspaceRedirectModel?` | `Result<T?>` | access/workflow | no | yes | no | yes | Explicit nullable success result; redirect orchestration. |
| `IWorkspaceRedirectService` | `AuthorizeContextSelectionAsync` | `AuthorizeContextSelectionQuery` | `WorkspaceSelectionAuthorizationModel` | `Result<T>` | access | no | yes | no | yes | Context authorization. |
| `IContextSelectionService` | `GetWorkspaceCatalogAsync` | `GetWorkspaceCatalogQuery` | `WorkspaceCatalogModel` | `Result<T>` | query/projection | no | yes | no | yes | Duplicate/cross-facade with workspace catalog. |
| `IContextSelectionService` | `SelectWorkspaceAsync` | `SelectWorkspaceCommand` | none | `Result` | workflow | no | yes | no | yes | Context selection workflow. |
| `IAppDeleteGuard` | `CanDeleteUnitAsync` | `Guid unitId`, `Guid propertyId`, `Guid managementCompanyId` | `bool` | no | guard/delete | no | no | no | internal | Internal guard; not exposed by `IAppBLL`. |
| `IAppDeleteGuard` | `CanDeleteTicketAsync` | `Guid ticketId`, `Guid managementCompanyId` | `bool` | no | guard/delete | no | no | no | internal | Internal guard. |
| `IAppDeleteGuard` | `CanDeletePropertyAsync` | `Guid propertyId`, `Guid customerId`, `Guid managementCompanyId` | `bool` | no | guard/delete | no | no | no | internal | Internal guard. |
| `IAppDeleteGuard` | `CanDeleteCustomerAsync` | `Guid customerId`, `Guid managementCompanyId` | `bool` | no | guard/delete | no | no | no | internal | Internal guard. |
| `IAppDeleteGuard` | `CanDeleteResidentAsync` | `Guid residentId`, `Guid managementCompanyId` | `bool` | no | guard/delete | no | no | no | internal | Internal guard. |

## DTO Inventory

### Canonical DTOs

| DTO | Category | BaseService target | Notes |
|---|---|---|---|
| `ContactBllDto` | canonical DTO | possible `ContactService` if exposed | Canonical contact state; mapper exists. |
| `CustomerBllDto` | canonical DTO | `CustomerService` | Should replace simple create/update command field duplication after route/scope separation. |
| `LeaseBllDto` | canonical DTO | `LeaseService` | Should back create/update variants from resident and unit routes. |
| `ManagementCompanyBllDto` | canonical DTO | `ManagementCompanyService` | Should back profile update/create where appropriate. |
| `ManagementCompanyJoinRequestBllDto` | canonical DTO | `OnboardingService` or join-request aggregate helper | Mapper exists; workflow can compose canonical add/update internally. |
| `PropertyBllDto` | canonical DTO | `PropertyService` | Should replace simple create/update command field duplication. |
| `ResidentBllDto` | canonical DTO | `ResidentService` | Should replace simple create/update command field duplication. |
| `TicketBllDto` | canonical DTO | `TicketService` | Should back management ticket CRUD while lifecycle methods remain explicit. |
| `UnitBllDto` | canonical DTO | `UnitService` | Should replace simple create/update command field duplication. |
| `VendorBllDto` | canonical DTO | possible `VendorService` if exposed | Mapper exists; currently no public BLL service. |

### Commands and Workflow Requests

| DTO | Category | Redundant candidate | Notes |
|---|---|---|---|
| `CreateCustomerCommand` | CRUD command/request | yes | Combines actor/company route with fields duplicated by `CustomerBllDto`. Replace with route/scope plus canonical DTO. |
| `UpdateCustomerProfileCommand` | CRUD command/request | yes | Duplicates `CustomerBllDto` mutable fields plus route/actor context. |
| `DeleteCustomerCommand` | delete command | partial | Confirmation name is workflow/delete context; route/actor can become parameters/scope. |
| `CreatePropertyCommand` | CRUD command/request | yes | Duplicates `PropertyBllDto` fields plus route/actor context. |
| `UpdatePropertyProfileCommand` | CRUD command/request | yes | Duplicates `PropertyBllDto` fields plus route/actor context. |
| `DeletePropertyCommand` | delete command | partial | Confirmation name should remain delete workflow data; route/actor can become parameters/scope. |
| `CreateUnitCommand` | CRUD command/request | yes | Duplicates `UnitBllDto` fields plus route/actor context. |
| `UpdateUnitCommand` | CRUD command/request | yes | Duplicates `UnitBllDto` fields plus route/actor context. |
| `DeleteUnitCommand` | delete command | partial | Confirmation unit number should remain delete workflow data; route/actor can become parameters/scope. |
| `CreateResidentCommand` | CRUD command/request | yes | Duplicates `ResidentBllDto` fields plus route/actor context. |
| `UpdateResidentProfileCommand` | CRUD command/request | yes | Duplicates `ResidentBllDto` fields plus route/actor context. |
| `DeleteResidentCommand` | delete command | partial | Confirmation ID code should remain delete workflow data; route/actor can become parameters/scope. |
| `CreateLeaseFromResidentCommand` | workflow CRUD command | partial | Adds route context and alternate parent flow; mutable fields duplicate `LeaseBllDto`. |
| `CreateLeaseFromUnitCommand` | workflow CRUD command | partial | Same canonical lease mutation can back both variants. |
| `UpdateLeaseFromResidentCommand` | workflow CRUD command | partial | Mutable fields duplicate `LeaseBllDto`; route variant may remain as route model. |
| `UpdateLeaseFromUnitCommand` | workflow CRUD command | partial | Same canonical lease mutation can back both variants. |
| `DeleteLeaseFromResidentCommand` | delete command | partial | Inherits route/scope query; can become route/scope plus lease id. |
| `DeleteLeaseFromUnitCommand` | delete command | partial | Same as resident variant. |
| `CreateManagementTicketCommand` | CRUD/workflow command | partial | Entity fields duplicate `TicketBllDto`; reference validation and lifecycle defaults justify explicit operation. |
| `UpdateManagementTicketCommand` | CRUD/workflow command | partial | Entity fields duplicate `TicketBllDto`; status/lifecycle validation remains workflow. |
| `DeleteManagementTicketCommand` | delete command | partial | Route/actor plus id; can become scope plus id. |
| `AdvanceManagementTicketStatusCommand` | workflow command | no | Real ticket lifecycle transition command. |
| `CompanyProfileUpdateRequest` | CRUD request | yes | Duplicates `ManagementCompanyBllDto` profile fields. |
| `CompanyMembershipAddRequest` | workflow command/request | no | User-by-email membership workflow; not canonical aggregate state only. |
| `CompanyMembershipUpdateRequest` | workflow command/request | no | Role/status update with membership policy context. |
| `TransferOwnershipRequest` | workflow command/request | no | Real ownership transfer workflow. |
| `CreateManagementCompanyCommand` | onboarding workflow command | partial | Duplicates company fields but also creates initial membership in onboarding flow. |
| `CreateCompanyJoinRequestCommand` | onboarding workflow command | no | Join request workflow with requested role and message. |
| `CompleteAccountOnboardingCommand` | onboarding workflow command | no | Onboarding state transition. |
| `LoginAccountCommand` | account workflow command | no | Authentication/account surface. |
| `LogoutCommand` | account workflow command | no | Account/session workflow marker. |
| `RegisterAccountCommand` | account workflow command | no | Registration workflow. |
| `SelectWorkspaceCommand` | workspace workflow command | no | Workspace selection workflow. |

### Queries and Filters

| DTO | Category | Redundant candidate | Notes |
|---|---|---|---|
| `GetCompanyCustomersQuery` | query/access route model | partial | Actor + company slug; future route/scope model. |
| `GetCustomerProfileQuery` | query/access route model | partial | Actor + company/customer slugs; future route/scope model. |
| `GetCustomerWorkspaceQuery` | query/access route model | partial | Actor + company/customer slugs; future route/scope model. |
| `GetPropertyWorkspaceQuery` | query/access route model | partial | Actor + company/customer/property slugs; future route/scope model. |
| `GetPropertyProfileQuery` | query/access route model | partial | Same route shape as workspace query; could be consolidated. |
| `GetResidentsQuery` | query/access route model | partial | Actor + company slug; future route/scope model. |
| `GetResidentProfileQuery` | query/access route model | partial | Actor + company slug + resident ID code. |
| `GetPropertyUnitsQuery` | query/access route model | partial | Actor + company/customer/property slugs. |
| `GetUnitDashboardQuery` | query/access route model | partial | Actor + company/customer/property/unit slugs. |
| `GetUnitProfileQuery` | query/access route model | partial | Same route shape as dashboard query; could be consolidated. |
| `GetResidentLeasesQuery` | trusted-ish scope/query model | no | Already contains resolved IDs/names and route context; could evolve into trusted scope. |
| `GetUnitLeasesQuery` | trusted-ish scope/query model | no | Already contains resolved IDs/names and route context; could evolve into trusted scope. |
| `GetResidentLeaseQuery` | query/access route model | partial | Adds lease id to resident lease scope. |
| `GetUnitLeaseQuery` | query/access route model | partial | Adds lease id to unit lease scope. |
| `SearchLeasePropertiesQuery` | query/filter | no | Search/filter over resolved resident lease context. |
| `GetLeaseUnitsForPropertyQuery` | query/options | no | Options lookup with selected property id. |
| `SearchLeaseResidentsQuery` | query/filter | no | Search/filter over resolved unit lease context. |
| `GetManagementTicketsQuery` | query/filter | no | Actor/company plus ticket filters; justified. |
| `GetManagementTicketQuery` | query/access route model | partial | Actor/company plus ticket id; future scope plus id. |
| `GetManagementTicketSelectorOptionsQuery` | query/options/filter | no | Scoped cascading selector options. |
| `GetOnboardingStateQuery` | query | no | Onboarding state. |
| `GetWorkspaceCatalogQuery` | query | no | Workspace catalog for actor. |
| `ResolveWorkspaceRedirectQuery` | workflow/access query | no | Redirect decision including remembered context. |
| `RememberedWorkspaceContext` | access/context helper | no | Input value object inside redirect query. |
| `AuthorizeContextSelectionQuery` | access query | no | Context authorization. |

### Projections, Read Models, and Option Models

| DTO | Category | Notes |
|---|---|---|
| `CompanyCustomerModel` | projection/read model | Create result projection; should be composed from canonical create in later phase. |
| `CompanyWorkspaceModel` | access/projection model | Company workspace context projection. |
| `CustomerListItemModel` | projection/read model | List item with property links. |
| `CustomerProfileModel` | projection/read model | Profile read model. |
| `CustomerPropertyLinkModel` | projection/read model | Nested list projection. |
| `CustomerWorkspaceModel` | access/projection model | Customer workspace context projection. |
| `PropertyWorkspaceModel` | access/projection model | Property workspace context projection. |
| `PropertyDashboardModel` | projection/read model | Dashboard model. |
| `PropertyListItemModel` | projection/read model | Customer property list item. |
| `PropertyProfileModel` | projection/read model | Profile read model. |
| `PropertyTypeOptionModel` | options/dropdown model | Global property type lookup display. |
| `CompanyResidentsModel` | projection/read model | Company residents list wrapper. |
| `ResidentContactModel` | projection/read model | Resident contact projection. |
| `ResidentDashboardModel` | projection/read model | Dashboard model. |
| `ResidentLeaseSummaryModel` | projection/read model | Resident lease summary. |
| `ResidentListItemModel` | projection/read model | Resident list item. |
| `ResidentProfileModel` | projection/read model | Profile read model. |
| `ResidentWorkspaceModel` | access/projection model | Resident workspace context projection. |
| `PropertyUnitsModel` | projection/read model | Property unit list wrapper. |
| `UnitDashboardModel` | projection/read model | Dashboard model. |
| `UnitListItemModel` | projection/read model | Unit list item. |
| `UnitProfileModel` | projection/read model | Profile read model. |
| `UnitWorkspaceModel` | access/projection model | Unit workspace context projection. |
| `ResidentLeaseListModel` | projection/read model | Resident route lease list. |
| `ResidentLeaseModel` | projection/read model | Resident route lease item. |
| `UnitLeaseListModel` | projection/read model | Unit route lease list. |
| `UnitLeaseModel` | projection/read model | Unit route lease item. |
| `LeaseModel` | projection/read model | Lease edit/details model; overlaps canonical `LeaseBllDto` but includes route-specific naming. |
| `LeaseCommandModel` | command result model | Only returns `LeaseId`; likely replace with canonical DTO or `Guid` if no projection value. |
| `LeasePropertySearchResultModel` | options/search model | Search result wrapper. |
| `LeasePropertySearchItemModel` | options/search model | Property search item. |
| `LeaseUnitOptionsModel` | options/dropdown model | Unit options wrapper. |
| `LeaseUnitOptionModel` | options/dropdown model | Unit option. |
| `LeaseResidentSearchResultModel` | options/search model | Resident search result wrapper. |
| `LeaseResidentSearchItemModel` | options/search model | Resident search item. |
| `LeaseRoleOptionsModel` | options/dropdown model | Lease roles wrapper. |
| `LeaseRoleOptionModel` | options/dropdown model | Lease role option. |
| `ManagementTicketsModel` | projection/read model | Ticket list page model with filter/options. |
| `ManagementTicketListItemModel` | projection/read model | Ticket list item. |
| `ManagementTicketDetailsModel` | projection/read model | Ticket details. |
| `ManagementTicketFormModel` | projection/read model | Create/edit form state plus selector options. |
| `TicketFilterModel` | query/filter model | Filter values within ticket list projection. |
| `TicketSelectorOptionsModel` | options/dropdown model | Ticket selector groups. |
| `TicketOptionModel` | options/dropdown model | Generic ticket option. |
| `CompanyProfileModel` | projection/read model | Management company profile projection. |
| `CompanyMembershipContext` | trusted scope/access model | Authorization result and scope carrier. |
| `CompanyAdminAuthorizedContext` | trusted scope/access model | Privileged scope carrier. |
| `CompanyMembershipListResult` | projection/read model | Membership list wrapper. |
| `CompanyMembershipUserListItem` | projection/read model | Membership list item. |
| `CompanyMembershipEditModel` | projection/read model | Membership edit projection. |
| `CompanyMembershipRoleOption` | options/dropdown model | Role option with policy state. |
| `OwnershipTransferModel` | workflow result model | Ownership transfer result. |
| `OwnershipTransferCandidate` | projection/read model | Ownership candidate projection. |
| `PendingAccessRequestListResult` | projection/read model | Pending access request wrapper. |
| `PendingAccessRequestItem` | projection/read model | Pending access request item. |
| `AccountLoginModel` | workflow/view model | Account model; BLL-facing auth workflow. |
| `AccountRegisterModel` | workflow/view model | Account model; BLL-facing registration workflow. |
| `CreateManagementCompanyModel` | workflow/read model | Onboarding create-company result. |
| `OnboardingJoinRequestModel` | workflow/read model | Join request result. |
| `OnboardingStateModel` | projection/read model | Onboarding state. |
| `WorkspaceCatalogModel` | projection/read model | Workspace selection catalog. |
| `WorkspaceOptionModel` | options/dropdown model | Workspace option. |
| `WorkspaceContextCatalogModel` | projection/read model | Workspace context catalog. |
| `WorkspaceContextModel` | projection/read model | Workspace context entry. |
| `WorkspaceSelectionAuthorizationModel` | access/read model | Context selection authorization result. |
| `WorkspaceRedirectModel` | workflow/read model | Redirect decision. |

### Errors, Constants, and Helpers

| DTO/helper | Category | Notes |
|---|---|---|
| `ValidationFailureModel` | error/helper | Field validation detail. |
| `BusinessRuleError` | error | Typed app error. |
| `ConflictError` | error | Typed app error. |
| `ForbiddenError` | error | Typed app error. |
| `NotFoundError` | error | Typed app error. |
| `UnauthorizedError` | error | Typed app error. |
| `UnexpectedAppError` | error | Typed app error. |
| `ValidationAppError` | error | Typed app error with failures. |
| `DuplicateRegistryCodeError` | error | Customer-specific duplicate error. |
| `DuplicateResidentIdCodeError` | error | Resident-specific duplicate error. |
| `ResidentValidationError` | error | Resident-specific validation error. |
| `ManagementCompanyJoinRequestStatusCodes` | constant/helper | Status code constants for join requests. |
| `TicketWorkflowConstants` | constant/helper | Ticket lifecycle constants and order. |
| `CompanyMembershipAuthorizationFailureReason` | constant/helper enum | Authorization reason codes. |
| `CompanyMembershipUserActionBlockReason` | constant/helper enum | Membership action block reason codes. |
| `WorkspaceRedirectDestination` | constant/helper enum | Redirect destination codes. |

## Mapper Inventory

| Mapper | Classification | Source/target | Notes |
|---|---|---|---|
| `ContactBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `ContactBllDto` <-> `ContactDalDto` | BaseService-ready if contact service is exposed. |
| `CustomerBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `CustomerBllDto` <-> `CustomerDalDto` | BaseService-ready for `CustomerService`. |
| `LeaseBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `LeaseBllDto` <-> `LeaseDalDto` | BaseService-ready for `LeaseService`. |
| `ManagementCompanyBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `ManagementCompanyBllDto` <-> `ManagementCompanyDalDto` | BaseService-ready for `ManagementCompanyService`. |
| `ManagementCompanyJoinRequestBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `ManagementCompanyJoinRequestBllDto` <-> `ManagementCompanyJoinRequestDalDto` | Useful for onboarding/join-request workflow. |
| `PropertyBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `PropertyBllDto` <-> `PropertyDalDto` | BaseService-ready for `PropertyService`. |
| `ResidentBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `ResidentBllDto` <-> `ResidentDalDto` | BaseService-ready for `ResidentService`. |
| `TicketBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `TicketBllDto` <-> `TicketDalDto` | BaseService-ready for `TicketService`. |
| `UnitBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `UnitBllDto` <-> `UnitDalDto` | BaseService-ready for `UnitService`. |
| `VendorBllDtoMapper` | canonical BLL DTO <-> DAL DTO mapper | `VendorBllDto` <-> `VendorDalDto` | BaseService-ready if vendor service is exposed. |
| `CustomerWorkspaceBllMapper` | workflow/read-model projection mapper | DAL/customer/company data -> workspace model | Projection mapper; not BaseService mapper. |
| `CustomerProfileBllMapper` | workflow/read-model projection mapper | DAL/customer/company data -> profile model | Projection mapper; can compose canonical read. |
| `LeaseBllMapper` | workflow/read-model projection mapper | lease DAL graphs -> lease models/options | Projection mapper; not duplicate of canonical mapper because it enriches labels/context. |
| `PropertyBllMapper` | workflow/read-model projection mapper | property DAL graphs -> property models/options | Projection mapper; not BaseService mapper. |
| `ResidentBllMapper` | workflow/read-model projection mapper | resident DAL graphs -> resident models | Projection mapper; not BaseService mapper. |
| `UnitBllMapper` | workflow/read-model projection mapper | unit DAL graphs -> unit models | Projection mapper; not BaseService mapper. |

Missing mapper observations:

- No canonical BLL mapper exists for management company membership because there is no obvious canonical `CompanyMembershipBllDto` in `App.BLL.DTO`.
- No canonical BLL mapper exists for lookup tables used only as options, which is acceptable unless those become public aggregate services.

## Service-to-Target-Domain Mapping

| Current service | Current contracts | Target owner | Role | Notes |
|---|---|---|---|---|
| `AccountOnboardingService` | `IAccountOnboardingService` | `OnboardingService` | orchestration exception | Account, company creation, membership/onboarding state. |
| `OnboardingCompanyJoinRequestService` | `IOnboardingCompanyJoinRequestService` | `OnboardingService` | workflow service | Could use join-request canonical DTO internally. |
| `WorkspaceContextService` | `IWorkspaceContextService` | `WorkspaceService` | orchestration exception | Workspace context catalog. |
| `UserWorkspaceCatalogService` | `IWorkspaceCatalogService` | `WorkspaceService` | orchestration exception | Workspace catalog projection. |
| `WorkspaceRedirectService` | `IWorkspaceRedirectService`, `IContextSelectionService` | `WorkspaceService` | orchestration exception | Redirect and context selection. |
| `ManagementCompanyProfileService` | `IManagementCompanyProfileService` | `ManagementCompanyService` | aggregate-backed service | Should inherit BaseService after Phase 2 readiness. |
| `CompanyMembershipAdminService` | admin and split membership contracts | `CompanyMembershipService` or `Memberships` facade | orchestration/aggregate decision pending | Large policy-heavy service; may be documented exception if no natural aggregate DTO/repository chosen. |
| `CompanyCustomerService` | `ICompanyCustomerService` | `CustomerService` | aggregate-backed service | Create/list customer operations. |
| `CustomerAccessService` | `ICustomerAccessService` | internal customer access policy/helper | internal helper/policy | Should not remain public facade service long-term. |
| `CustomerProfileService` | `ICustomerProfileService` | `CustomerService` | aggregate-backed service | Get/update/delete customer profile. |
| `CustomerWorkspaceService` | `ICustomerWorkspaceService` | `WorkspaceService` or `CustomerService` projection methods | projection/orchestration | Workspace projection only. |
| `PropertyProfileService` | `IPropertyProfileService` | `PropertyService` | aggregate-backed service | Get/update/delete property profile. |
| `PropertyWorkspaceService` | `IPropertyWorkspaceService` | `PropertyService` plus workspace/projection methods | mixed | Create is aggregate mutation; dashboard/list/options are projections. |
| `ResidentAccessService` | `IResidentAccessService` | internal resident access policy/helper | internal helper/policy | Should not remain public facade service long-term. |
| `ResidentProfileService` | `IResidentProfileService` | `ResidentService` | aggregate-backed service | Get/update/delete resident profile. |
| `ResidentWorkspaceService` | `IResidentWorkspaceService` | `ResidentService` plus workspace/projection methods | mixed | Create is aggregate mutation; dashboard/list are projections. |
| `UnitAccessService` | `IUnitAccessService` | internal unit access policy/helper | internal helper/policy | Should not remain public facade service long-term. |
| `UnitProfileService` | `IUnitProfileService` | `UnitService` | aggregate-backed service | Get/update/delete unit profile. |
| `UnitWorkspaceService` | `IUnitWorkspaceService` | `UnitService` plus workspace/projection methods | mixed | Create is aggregate mutation; dashboard/list are projections. |
| `LeaseAssignmentService` | `ILeaseAssignmentService` | `LeaseService` | aggregate-backed service with workflow route variants | Should centralize canonical lease mutations. |
| `LeaseLookupService` | `ILeaseLookupService` | `LeaseService` or internal lookup helper | query/options helper | Not BaseService. |
| `ManagementTicketService` | `IManagementTicketService` | `TicketService` | aggregate-backed service plus workflow | CRUD should compose BaseService; lifecycle remains explicit. |
| `AppDeleteGuard` | `IAppDeleteGuard` | internal helper/policy | delete guard | Should remain internal helper, not public `IAppBLL` service. |
| `SlugGenerator` | none | internal helper | utility | Static helper; no BaseService relevance. |

## BaseService Inheritance Target List

Strong targets once Phase 2 fixes BaseService readiness:

- `CustomerService` using `CustomerBllDto`, `CustomerDalDto`, `CustomerBllDtoMapper`, customer repository.
- `PropertyService` using `PropertyBllDto`, `PropertyDalDto`, `PropertyBllDtoMapper`, property repository.
- `UnitService` using `UnitBllDto`, `UnitDalDto`, `UnitBllDtoMapper`, unit repository.
- `ResidentService` using `ResidentBllDto`, `ResidentDalDto`, `ResidentBllDtoMapper`, resident repository.
- `LeaseService` using `LeaseBllDto`, `LeaseDalDto`, `LeaseBllDtoMapper`, lease repository.
- `TicketService` using `TicketBllDto`, `TicketDalDto`, `TicketBllDtoMapper`, ticket repository.
- `ManagementCompanyService` using `ManagementCompanyBllDto`, `ManagementCompanyDalDto`, `ManagementCompanyBllDtoMapper`, management company repository.

Possible future targets if exposed:

- `ContactService` using `ContactBllDto`, `ContactDalDto`, `ContactBllDtoMapper`.
- `VendorService` using `VendorBllDto`, `VendorDalDto`, `VendorBllDtoMapper`.
- Join-request service using `ManagementCompanyJoinRequestBllDto`, `ManagementCompanyJoinRequestDalDto`, `ManagementCompanyJoinRequestBllDtoMapper`, if modeled as aggregate-backed instead of pure onboarding workflow.
- `CompanyMembershipService` only if a canonical membership DTO and natural repository-backed aggregate boundary are introduced or identified.

Known BaseService issues from Phase 0 that block adoption:

- `IBaseService` still exposes public `Add(TEntity entity)`.
- `BaseService` still exposes public `Add(TEntity entity)`.
- `IBaseService` still exposes public `Remove(TEntity entity)`.
- `BaseService.Remove(entity)` maps and removes without repository existence check.
- Protected `AddCore(entity)` does not exist.
- Protected `AddAndFindCoreAsync(entity, parentId, ct)` does not exist.

## Orchestration Exception Candidates

- `OnboardingService`: account onboarding, management company creation from onboarding, join requests, completion state.
- `WorkspaceService`: context catalog, workspace catalog, remembered context redirect, context selection authorization.
- `CompanyMembershipService`: exception candidate if retained as policy-heavy user/membership orchestration rather than a pure membership aggregate service.
- Access policies/helpers: customer, resident, unit, and management-company membership scope resolution.
- `LeaseLookupService` behavior: search/options support under `LeaseService` or internal lookup helper.
- `AppDeleteGuard`: delete dependency checks.
- `SlugGenerator`: utility/helper.

## DTO Removal and Replacement Candidates

High-confidence later-phase simplification candidates:

- `CreateCustomerCommand` -> route/scope model plus `CustomerBllDto`.
- `UpdateCustomerProfileCommand` -> route/scope model plus `CustomerBllDto`.
- `CreatePropertyCommand` -> route/scope model plus `PropertyBllDto`.
- `UpdatePropertyProfileCommand` -> route/scope model plus `PropertyBllDto`.
- `CreateUnitCommand` -> route/scope model plus `UnitBllDto`.
- `UpdateUnitCommand` -> route/scope model plus `UnitBllDto`.
- `CreateResidentCommand` -> route/scope model plus `ResidentBllDto`.
- `UpdateResidentProfileCommand` -> route/scope model plus `ResidentBllDto`.
- `CompanyProfileUpdateRequest` -> trusted company scope plus `ManagementCompanyBllDto`.
- `LeaseCommandModel` -> canonical `LeaseBllDto` or `Guid`, depending on caller needs.

Partial candidates requiring workflow preservation:

- `DeleteCustomerCommand`, `DeletePropertyCommand`, `DeleteUnitCommand`, `DeleteResidentCommand`: keep confirmation data as workflow input, but separate route/actor scope from delete-specific confirmation.
- Lease route commands: preserve resident-route and unit-route workflows or route models, but have both compose one canonical create/update/delete implementation based on `LeaseBllDto`.
- Management ticket create/update/delete commands: separate route/actor scope from canonical `TicketBllDto`, while preserving reference validation and lifecycle behavior.
- `CreateManagementCompanyCommand`: may keep onboarding command, but use `ManagementCompanyBllDto` internally for canonical company state.

Likely keep:

- Filter/search query DTOs such as `GetManagementTicketsQuery`, `SearchLeasePropertiesQuery`, and `SearchLeaseResidentsQuery`.
- Option/read projections and dashboard/profile models.
- Typed app errors and domain-specific errors.
- Workflow DTOs for ownership transfer, context selection, onboarding completion, and ticket status advancement.

## Candidate Service Merges and Wrappers

- Merge `CompanyCustomerService`, `CustomerProfileService`, and selected customer workspace/profile projection methods under `CustomerService`.
- Move `CustomerAccessService` behavior behind `CustomerService` as internal scope/access policy or helper.
- Merge `PropertyProfileService` and the create/list/profile parts of `PropertyWorkspaceService` under `PropertyService`.
- Merge `UnitProfileService` and the create/list/profile parts of `UnitWorkspaceService` under `UnitService`.
- Move `UnitAccessService` behavior behind `UnitService` as internal scope/access policy or helper.
- Merge `ResidentProfileService` and the create/list/profile parts of `ResidentWorkspaceService` under `ResidentService`.
- Move `ResidentAccessService` behavior behind `ResidentService` as internal scope/access policy or helper.
- Merge `LeaseAssignmentService` operations under `LeaseService`; keep lookup/search methods either on `LeaseService` or internal helper.
- Merge `ManagementTicketService` under `TicketService`; keep lifecycle transition as explicit workflow method.
- Wrap onboarding/account/join-request/context-selection/catalog/redirect operations behind `OnboardingService` and `WorkspaceService`.
- Replace wide `IAppBLL` properties with domain-first properties in a later phase after domain contracts are introduced.

## Risks and Unresolved Questions

- Build baseline remains pending by instruction. The master handoff says agents should ask the user to build manually, so this phase did not run `dotnet build`.
- The split membership contracts (`ICompanyMembershipAuthorizationService`, `ICompanyMembershipQueryService`, `ICompanyMembershipCommandService`, `ICompanyRoleOptionsService`, `ICompanyOwnershipTransferService`, `ICompanyAccessRequestReviewService`) duplicate methods from `ICompanyMembershipAdminService` and are not currently exposed by `IAppBLL`; later phases need to decide whether these become facets of `CompanyMembershipService` or internal-only boundaries.
- `CompanyMembershipAdminService` has no canonical `CompanyMembershipBllDto` visible in `App.BLL.DTO`; this makes BaseService inheritance unclear.
- Several current BLL methods return `Task<bool>`, `Task<string?>`, or `Task<IReadOnlyList<T>>` without `Result`. Later phases should decide whether to normalize these to `Result<T>` under the handoff rule.
- Current command/query DTOs often mix untrusted route slugs, actor IDs, resolved IDs, display names, and mutable entity fields. Later trusted-scope work must separate these carefully to preserve tenant isolation and avoid IDOR regressions.
- Projection-returning mutation methods currently perform repository-changing work and return page/profile models. Later phases should introduce canonical mutation methods first, then compose projection wrappers on top.
- Ticket workflow operations must preserve lifecycle guards and should not be collapsed into generic BaseService update behavior.
- Management company delete behavior is explicitly protected by the master handoff and must not be changed without approval.

## Handoff Notes for Next Phase

Phase 2 should focus on BaseService/IBaseService readiness before any app domain service refactor:

- Remove public generic create from `IBaseService`.
- Remove or restrict public `Remove(entity)` because it bypasses the required existence-check pattern.
- Add protected `AddCore(entity)`.
- Add protected `AddAndFindCoreAsync(entity, parentId, ct)` if needed by concrete services.
- Keep BaseService errors generic and non-app-specific.
- Do not add tenant, authorization, workflow, route-slug, or delete-guard logic to BaseService.

After BaseService is ready, introduce aggregate-backed domain contracts/services around:

- `CustomerService`
- `PropertyService`
- `UnitService`
- `ResidentService`
- `LeaseService`
- `TicketService`
- `ManagementCompanyService`

Use canonical BLL DTOs by default for normal CRUD mutations, and add projection-returning methods only as wrappers that compose canonical mutation methods.

## Assumptions

- Generated `obj` files under `App.BLL.Contracts` are not part of the source inventory.
- The current modified `plans/v2/bll-refactor/00_MASTER_BLL_AGENT_HANDOFF.md` predates this work and was not edited in this phase.
- The existing untracked `plans/v2/bll-refactor/phase-reports` folder predates this work except for this added Phase 1 report.
- "Every App.BLL.Contracts method" includes helper contracts not currently exposed by `IAppBLL`, such as `ICompanyMembershipCommandService` and `IAppDeleteGuard`.
- "Future API-ready" means free of WebApp/MVC/API DTO dependencies and suitable to be called by API/MVC after transport mapping, not necessarily that the current DTO names are final.

## Verification

- Static inventory was built from `App.BLL.Contracts`, `App.BLL.DTO`, `App.BLL/Mappers`, `App.BLL/Services`, `Base.BLL.Contracts/IBaseService.cs`, and `Base.BLL/BaseService.cs`.
- Build was not run, per master handoff instruction to ask the user to build manually.

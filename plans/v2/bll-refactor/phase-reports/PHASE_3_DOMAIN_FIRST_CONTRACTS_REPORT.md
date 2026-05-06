# Phase 3 Domain-First Contracts Report

## Summary

Created new domain-first BLL service contracts while leaving the existing granular contracts in place for transition.

Added initial BLL route request models to replace the current command/query shape where operations combine actor identity, public route slugs, and duplicated entity fields.

## Files Changed

- `App.BLL.DTO/Common/Routes/RouteRequestModels.cs`
- `App.BLL.Contracts/Customers/ICustomerService.cs`
- `App.BLL.Contracts/Properties/IPropertyService.cs`
- `App.BLL.Contracts/Units/IUnitService.cs`
- `App.BLL.Contracts/Residents/IResidentService.cs`
- `App.BLL.Contracts/Leases/ILeaseService.cs`
- `App.BLL.Contracts/Tickets/ITicketService.cs`
- `App.BLL.Contracts/ManagementCompanies/IManagementCompanyService.cs`
- `App.BLL.Contracts/ManagementCompanies/ICompanyMembershipService.cs`
- `App.BLL.Contracts/Onboarding/IOnboardingService.cs`
- `App.BLL.Contracts/Onboarding/IWorkspaceService.cs`

## Contract Shape

Aggregate-backed target contracts:

- `ICustomerService : IBaseService<CustomerBllDto>`
- `IPropertyService : IBaseService<PropertyBllDto>`
- `IUnitService : IBaseService<UnitBllDto>`
- `IResidentService : IBaseService<ResidentBllDto>`
- `ILeaseService : IBaseService<LeaseBllDto>`
- `ITicketService : IBaseService<TicketBllDto>`
- `IManagementCompanyService : IBaseService<ManagementCompanyBllDto>`

Documented orchestration exceptions:

- `ICompanyMembershipService` because there is no canonical membership BLL DTO in the current BLL DTO project.
- `IOnboardingService` because it coordinates identity/onboarding workflows.
- `IWorkspaceService` because it resolves workspace catalog, remembered context, and context selection workflows.

## Route Request Models

Added:

- `ManagementCompanyRoute`
- `CustomerRoute`
- `PropertyRoute`
- `UnitRoute`
- `ResidentRoute`
- `TicketRoute`
- `ResidentLeaseRoute`
- `UnitLeaseRoute`
- `ManagementTicketSearchRoute`
- `TicketSelectorOptionsRoute`

These are intentionally small Phase 3 placeholders for the Phase 3.5 route/scope standardization step.

## Old Granular Contract Mapping

- `ICompanyCustomerService`, `ICustomerAccessService`, `ICustomerProfileService`, `ICustomerWorkspaceService` map to `ICustomerService`.
- `IPropertyProfileService`, `IPropertyWorkspaceService` map to `IPropertyService`.
- `IUnitAccessService`, `IUnitProfileService`, `IUnitWorkspaceService` map to `IUnitService`.
- `IResidentAccessService`, `IResidentProfileService`, `IResidentWorkspaceService` map to `IResidentService`.
- `ILeaseAssignmentService`, `ILeaseLookupService` map to `ILeaseService`.
- `IManagementTicketService` maps to `ITicketService`.
- `IManagementCompanyAccessService`, `IManagementCompanyProfileService` map to `IManagementCompanyService`.
- `ICompanyMembershipAdminService`, `ICompanyMembershipAuthorizationService`, `ICompanyMembershipQueryService`, `ICompanyMembershipCommandService`, `ICompanyRoleOptionsService`, `ICompanyOwnershipTransferService`, and `ICompanyAccessRequestReviewService` map to `ICompanyMembershipService`.
- `IAccountOnboardingService` and `IOnboardingCompanyJoinRequestService` map to `IOnboardingService`.
- `IWorkspaceContextService`, `IWorkspaceCatalogService`, `IWorkspaceRedirectService`, and `IContextSelectionService` map to `IWorkspaceService`.

## Method Signatures Needing Route/Scope Conversion

- Customer create/update/delete operations now use `ManagementCompanyRoute` or `CustomerRoute` plus `CustomerBllDto`.
- Property create/update/delete operations now use `CustomerRoute` or `PropertyRoute` plus `PropertyBllDto`.
- Unit create/update/delete operations now use `PropertyRoute` or `UnitRoute` plus `UnitBllDto`.
- Resident create/update/delete operations now use `ManagementCompanyRoute` or `ResidentRoute` plus `ResidentBllDto`.
- Lease create/update/delete operations now use `ResidentRoute`, `UnitRoute`, `ResidentLeaseRoute`, or `UnitLeaseRoute` plus `LeaseBllDto`.
- Ticket create/update/delete/status operations now use `ManagementCompanyRoute` or `TicketRoute` plus `TicketBllDto`.
- Management company create operations now use `appUserId` plus `ManagementCompanyBllDto`; update/delete operations use `ManagementCompanyRoute` plus `ManagementCompanyBllDto`.

## Existing Command DTOs That Duplicate Canonical DTO Fields

- `CreateCustomerCommand`, `UpdateCustomerProfileCommand`
- `CreatePropertyCommand`, `UpdatePropertyProfileCommand`
- `CreateUnitCommand`, `UpdateUnitCommand`
- `CreateResidentCommand`, `UpdateResidentProfileCommand`
- `CreateLeaseFromResidentCommand`, `CreateLeaseFromUnitCommand`, `UpdateLeaseFromResidentCommand`, `UpdateLeaseFromUnitCommand`
- `CreateManagementTicketCommand`, `UpdateManagementTicketCommand`
- `CreateManagementCompanyCommand`

Delete commands still carry confirmation strings and route context. The new contracts move confirmation strings to explicit method parameters and route context to route request models.

## Orchestration Exceptions

- Membership, onboarding, and workspace contracts remain orchestration-style contracts and do not inherit `IBaseService`.
- Ticket form/options/search methods remain projection/use-case methods because they are not simple CRUD.
- Lease lookup methods remain on `ILeaseService` because the lookup options are domain-owned by lease assignment workflows.

## Build Status

Build was not run. The master handoff directs agents to ask the user to run builds manually.

Expected compile impact should be limited because the new contracts are additive and `IAppBLL` was not changed in this phase.

## Assumptions

- Phase 3 should not change `IAppBLL` because adding new properties would require implementation changes scheduled for later phases.
- `CompanyMembershipService` has no natural aggregate-backed contract until a canonical membership BLL DTO is introduced or selected.
- Route models are untrusted BLL request models, not trusted scope models. Phase 3.5 should replace or enrich them with standardized trusted scope models before service implementation.

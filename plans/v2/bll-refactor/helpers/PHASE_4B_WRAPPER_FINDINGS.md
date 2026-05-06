Wrapper Findings

1. App.BLL/Services/Tickets/TicketService.cs:43

These public methods are thin route-to-query wrappers and can be inlined:

- SearchAsync(ManagementTicketSearchRoute) -> GetTicketsAsync(GetManagementTicketsQuery)
- GetDetailsAsync(TicketRoute) -> GetDetailsAsync(GetManagementTicketQuery)
- GetCreateFormAsync(TicketSelectorOptionsRoute) -> GetCreateFormAsync(GetManagementTicketsQuery)
- GetEditFormAsync(TicketRoute) -> GetEditFormAsync(GetManagementTicketQuery)
- GetSelectorOptionsAsync(TicketSelectorOptionsRoute) -> GetSelectorOptionsAsync(GetManagementTicketSelectorOptionsQuery)

Redundant private methods/classes:

- private query methods at lines 321, 365, 420, 458, 505
- route/query mappers at lines 911, 920, 939, 952, 962
- delete App.BLL.DTO/Tickets/Queries/TicketQueries.cs
- change ToFilterDto(GetManagementTicketsQuery) to accept ManagementTicketSearchRoute or explicit filter fields

2. App.BLL/Services/Leases/LeaseService.cs:40

These public methods resolve route context, then rewrap that context into lease query DTOs:

- ListForResidentAsync(ResidentRoute) -> GetResidentLeasesQuery
- ListForUnitAsync(UnitRoute) -> GetUnitLeasesQuery
- GetForResidentAsync(ResidentLeaseRoute) -> GetResidentLeaseQuery
- GetForUnitAsync(UnitLeaseRoute) -> GetUnitLeaseQuery
- SearchPropertiesAsync(ResidentRoute, string?) -> SearchLeasePropertiesQuery
- ListUnitsForPropertyAsync(ResidentRoute, Guid) -> GetLeaseUnitsForPropertyQuery
- SearchResidentsAsync(UnitRoute, string?) -> SearchLeaseResidentsQuery

Redundant private methods/classes:

- private query methods at lines 434, 449, 465, 480, 496, 511, 536
- query mapper methods at lines 637, 642, 647, 662, 707, 722, 737
- LeaseBllMapper.ToResidentLeasesQuery(...)
- LeaseBllMapper.ToUnitLeasesQuery(...)
- delete App.BLL.DTO/Leases/Queries/LeaseQueries.cs

3. App.BLL/Services/Onboarding/OnboardingService.cs:121

Wrapper:

- GetStateAsync(Guid appUserId) creates GetOnboardingStateQuery and calls private GetStateAsync(GetOnboardingStateQuery)

Redundant:

- private GetStateAsync(GetOnboardingStateQuery)
- delete App.BLL.DTO/Onboarding/Queries/GetOnboardingStateQuery.cs

4. App.BLL/Services/Onboarding/WorkspaceService.cs:98

Wrappers:

- GetCatalogAsync(ManagementCompanyRoute) creates GetWorkspaceCatalogQuery and calls private GetWorkspaceCatalogAsync(GetWorkspaceCatalogQuery)
- SelectAsync(SelectWorkspaceCommand) creates AuthorizeContextSelectionQuery with identical fields and calls AuthorizeContextSelectionAsync(...)

Redundant:

- private GetWorkspaceCatalogAsync(GetWorkspaceCatalogQuery)
- delete App.BLL.DTO/Onboarding/Queries/GetWorkspaceCatalogQuery.cs
- SelectWorkspaceCommand and AuthorizeContextSelectionQuery duplicate AppUserId, ContextType, ContextId; one should go, or both should become primitives/shared model.

Unused DTOs

These currently have no references and can be deleted:

- CompanyProfileUpdateRequest
- LeaseCommandModel
- CompleteAccountOnboardingCommand
- CreateManagementCompanyCommand
- LogoutCommand

WebApp-Only Leftovers

These are no longer used by App.BLL.Services, but WebApp mappers/controllers still create them. Since the service contracts now take route + canonical DTO/primitive confirmation, these are redundant cleanup
candidates:

- Customer command/query DTOs
- Property command/query DTOs
- Unit command/query DTOs
- Resident command/query DTOs

Those need WebApp mapper/controller cleanup, not BLL service inlining.

No Matching Wrapper Pattern Found

No route-to-query wrapper pattern like your ticket example found in:

- CustomerService
- PropertyService
- UnitService
- ResidentService
- ManagementCompanyService
- CompanyMembershipService
- AppDeleteGuard
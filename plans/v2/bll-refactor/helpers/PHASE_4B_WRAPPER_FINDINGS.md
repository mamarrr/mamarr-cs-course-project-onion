# Phase 4B Wrapper Findings

## Removed Service Wrappers

- Inlined `TicketService` route methods so `SearchAsync`, `GetDetailsAsync`, `GetCreateFormAsync`, `GetEditFormAsync`, and `GetSelectorOptionsAsync` operate directly on route DTOs.
- Removed ticket private query-wrapper methods and route-to-query mapper helpers.
- Inlined `LeaseService` route methods so resident/unit lease list, details, searches, and option loading use resolved workspace context directly.
- Removed lease private query-wrapper methods and workspace-to-query mapper helpers.
- Inlined `OnboardingService.GetStateAsync(Guid appUserId)` and removed its private `GetOnboardingStateQuery` wrapper.
- Inlined `WorkspaceService.GetCatalogAsync(ManagementCompanyRoute)` and removed its private `GetWorkspaceCatalogQuery` wrapper.
- Changed `WorkspaceService.SelectAsync` to accept primitive context selection values instead of `SelectWorkspaceCommand`.

## Removed DTOs

- Deleted ticket query DTOs: `GetManagementTicketsQuery`, `GetManagementTicketQuery`, `GetManagementTicketSelectorOptionsQuery`.
- Deleted lease query DTOs: resident/unit lease queries, lease detail queries, lease property/unit/resident lookup queries.
- Deleted customer, property, unit, and resident CRUD command/query DTOs that were only WebApp mapper leftovers.
- Deleted unused onboarding DTOs: `CompleteAccountOnboardingCommand`, `CreateManagementCompanyCommand`, `LogoutCommand`, `SelectWorkspaceCommand`, `GetOnboardingStateQuery`, `GetWorkspaceCatalogQuery`.
- Removed unused model classes: `CompanyProfileUpdateRequest`, `LeaseCommandModel`.

## WebApp Cleanup

- Removed WebApp mapper methods that created redundant CRUD command/query DTOs.
- Removed `CustomerWorkspaceMvcMapper`, which only existed to create `GetCustomerWorkspaceQuery`.
- Updated controller validation references to use canonical BLL DTO property names or explicit confirmation-property names.
- Removed empty DTO `Commands` and `Queries` folders created by the cleanup.

## Kept Intentionally

- Kept onboarding identity command DTOs used by `WebApp.Services.Identity`.
- Kept `CreateCompanyJoinRequestCommand` because it is a join-request workflow payload, not a normal CRUD wrapper.
- Kept `ResolveWorkspaceRedirectQuery` and `AuthorizeContextSelectionQuery` because they are workspace/context workflow inputs still used by WebApp infrastructure.

## Verification

Static scans found no remaining references to the removed wrapper DTOs or service wrapper mappers.

Build was not run by the agent; per handoff instructions, the user should run the build manually and provide compiler output.

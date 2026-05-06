# Phase 4B Workflow Domain Services Report

## Summary

Implemented the Phase 4B grouped workflow-domain service shape:

- `IAppBLL.Leases`
- `IAppBLL.Tickets`
- `IAppBLL.ManagementCompanies`
- `IAppBLL.CompanyMemberships`
- `IAppBLL.Onboarding`
- `IAppBLL.Workspaces`

The old granular Phase 4B facade entries, contracts, implementation classes, and DI registrations were removed after MVC callers were migrated.

## New Service Classes

- `App.BLL/Services/Leases/LeaseService.cs`
- `App.BLL/Services/Tickets/TicketService.cs`
- `App.BLL/Services/ManagementCompanies/ManagementCompanyService.cs`
- `App.BLL/Services/ManagementCompanies/CompanyMembershipService.cs`
- `App.BLL/Services/Onboarding/OnboardingService.cs`
- `App.BLL/Services/Onboarding/WorkspaceService.cs`

## Inheritance and Exceptions

- `LeaseService` inherits `BaseService<LeaseBllDto, LeaseDalDto, ILeaseRepository, IAppUOW>`.
- `TicketService` inherits `BaseService<TicketBllDto, TicketDalDto, ITicketRepository, IAppUOW>`.
- `ManagementCompanyService` inherits `BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>`.
- `CompanyMembershipService` is an orchestration exception because there is no current membership aggregate repository/BLL DTO shape suitable for `BaseService`.
- `OnboardingService` is an orchestration exception.
- `WorkspaceService` is an orchestration exception.

## CRUD DTO Cleanup

Removed trivial CRUD command DTOs and public command-wrapper methods for:

- management ticket create/update/delete/status wrapper payloads where route + `TicketBllDto` is sufficient
- lease create/update/delete wrapper payloads where route + `LeaseBllDto` is sufficient
- management company profile update wrapper flow
- onboarding company creation wrapper flow

Remaining command/query DTOs are workflow or context models:

- onboarding join request
- workspace selection
- workspace redirect/context authorization
- private filtered/query implementation helpers

## MVC Migration

Migrated Phase 4B MVC callers to grouped facade services:

- management tickets use `_bll.Tickets`
- management profile/dashboard/user flows use `_bll.ManagementCompanies` / `_bll.CompanyMemberships`
- resident/unit lease flows use `_bll.Leases`
- public onboarding uses `_bll.Onboarding`
- workspace middleware/resolver use `_bll.Workspaces`

## Deleted Old Services

Deleted absorbed contracts/classes for:

- lease assignment and lookup
- management ticket service
- management company profile/access service
- company membership admin/authorization/query/command/role/ownership/access-review services
- account onboarding and onboarding join request services
- workspace context/catalog/redirect/context-selection services

Empty service/DTO folders created by those deletions were removed.

## Verification

Static checks performed:

- old granular service names no longer referenced from `App.BLL`, `App.BLL.Contracts`, or `WebApp`
- old trivial ticket/lease/company/onboarding CRUD command types no longer referenced
- no empty folders remain under `App.BLL/Services` or removed command DTO folders
- BLL contracts expose grouped Phase 4B services

Build status:

- Not run by agent. Per `00_MASTER_BLL_AGENT_HANDOFF.md`, the user should run the build manually and provide any compile output.

## Risks and Notes

- `CompanyMembershipService`, `OnboardingService`, and `WorkspaceService` remain documented orchestration exceptions.
- `IWorkspaceService` still accepts workflow/context query DTOs where the operation is not normal CRUD and must stay API-ready.
- Public API controllers remain intentionally deleted and were not rebuilt.

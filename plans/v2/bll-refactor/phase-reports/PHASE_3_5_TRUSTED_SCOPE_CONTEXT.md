# Phase 3.5 Trusted Scope Context Report

Date: 2026-05-06

## Summary

Standardized the BLL route/scope pattern by adding trusted scope models under `App.BLL.DTO/Common/Scopes`.

The Phase 3 route request models already existed under `App.BLL.DTO/Common/Routes` and the new Phase 3 domain-first contracts already use route + canonical DTO signatures for customer, property, unit, resident, lease, ticket, and management company operations. This phase keeps those route models as untrusted request input and adds separate resolved scope models for future service implementations.

No WebApp, API, DAL, schema, or controller changes were made.

## Files Changed

- `App.BLL.DTO/Common/Scopes/TrustedScopeModels.cs`
- `plans/v2/bll-refactor/phase-reports/PHASE_3_5_TRUSTED_SCOPE_CONTEXT.md`

## Route Request Models

Existing route request models retained:

- `ManagementCompanyRoute` in `App.BLL.DTO.Common.Routes`
- `CustomerRoute` in `App.BLL.DTO.Common.Routes`
- `PropertyRoute` in `App.BLL.DTO.Common.Routes`
- `UnitRoute` in `App.BLL.DTO.Common.Routes`
- `ResidentRoute` in `App.BLL.DTO.Common.Routes`
- `TicketRoute` in `App.BLL.DTO.Common.Routes`
- `ResidentLeaseRoute` in `App.BLL.DTO.Common.Routes`
- `UnitLeaseRoute` in `App.BLL.DTO.Common.Routes`
- `ManagementTicketSearchRoute` in `App.BLL.DTO.Common.Routes`
- `TicketSelectorOptionsRoute` in `App.BLL.DTO.Common.Routes`

These models carry actor id and external route/natural keys only. They are lookup input, not authorization proof.

## Trusted Scope Models Added

Added namespace: `App.BLL.DTO.Common.Scopes`.

- `ManagementCompanyScope`
- `CustomerScope`
- `PropertyScope`
- `UnitScope`
- `ResidentScope`
- `TicketScope`
- `ResidentLeaseScope`
- `UnitLeaseScope`

The scopes carry BLL-resolved ids, route keys needed for continuity, and the existing `CompanyMembershipContext` where management membership authorization is required.

`ResidentScope` allows nullable `CompanyMembershipContext` because resident-facing paths may eventually resolve through resident identity rather than management company membership. Later implementations must choose the correct authorization path explicitly before using it.

## Scope Resolver Recommendations

Keep resolvers internal to `App.BLL` unless a later phase finds a strong contract reason to expose them.

Recommended internal interfaces:

- `IManagementCompanyScopeResolver.ResolveAsync(ManagementCompanyRoute route, ...) -> Result<ManagementCompanyScope>`
- `ICustomerScopeResolver.ResolveAsync(CustomerRoute route, ...) -> Result<CustomerScope>`
- `IPropertyScopeResolver.ResolveAsync(PropertyRoute route, ...) -> Result<PropertyScope>`
- `IUnitScopeResolver.ResolveAsync(UnitRoute route, ...) -> Result<UnitScope>`
- `IResidentScopeResolver.ResolveAsync(ResidentRoute route, ...) -> Result<ResidentScope>`
- `ITicketScopeResolver.ResolveAsync(TicketRoute route, ...) -> Result<TicketScope>`
- `ILeaseScopeResolver.ResolveAsync(ResidentLeaseRoute route, ...) -> Result<ResidentLeaseScope>`
- `ILeaseScopeResolver.ResolveAsync(UnitLeaseRoute route, ...) -> Result<UnitLeaseScope>`

Resolvers should validate route shape, resolve slugs or natural keys to ids under the tenant boundary, authorize the actor through membership or resident access policy, and return typed app errors for not found, forbidden, unauthorized, validation, or conflict outcomes.

Current access services that can be internalized or wrapped by these resolvers in later phases:

- `ManagementCompanyAccessService`
- `CustomerAccessService`
- `UnitAccessService`
- `ResidentAccessService`
- customer/property/unit/resident workspace/profile services where they currently duplicate route resolution

## Commands To Replace With Route/Scope + Canonical DTO

These command DTOs duplicate canonical DTO fields plus actor/route context and should be retired by later implementation phases when callers move to the domain-first contracts:

- `CreateCustomerCommand` -> `ManagementCompanyRoute + CustomerBllDto`
- `UpdateCustomerProfileCommand` -> `CustomerRoute + CustomerBllDto`
- `CreatePropertyCommand` -> `CustomerRoute + PropertyBllDto`
- `UpdatePropertyProfileCommand` -> `PropertyRoute + PropertyBllDto`
- `CreateUnitCommand` -> `PropertyRoute + UnitBllDto`
- `UpdateUnitCommand` -> `UnitRoute + UnitBllDto`
- `CreateResidentCommand` -> `ManagementCompanyRoute + ResidentBllDto`
- `UpdateResidentProfileCommand` -> `ResidentRoute + ResidentBllDto`
- `CreateManagementTicketCommand` -> `ManagementCompanyRoute + TicketBllDto`
- `UpdateManagementTicketCommand` -> `TicketRoute + TicketBllDto`
- `CreateLeaseFromResidentCommand` -> `ResidentRoute + LeaseBllDto`
- `CreateLeaseFromUnitCommand` -> `UnitRoute + LeaseBllDto`
- `UpdateLeaseFromResidentCommand` -> `ResidentLeaseRoute + LeaseBllDto`
- `UpdateLeaseFromUnitCommand` -> `UnitLeaseRoute + LeaseBllDto`
- `CreateManagementCompanyCommand` -> `Guid appUserId + ManagementCompanyBllDto` or a future account route if Phase 4/5 introduces one

## Commands To Keep As Workflow Commands

Keep command/request models when they carry real workflow intent rather than simple entity state:

- `CompleteAccountOnboardingCommand`
- `CreateCompanyJoinRequestCommand`
- `LoginAccountCommand`
- `LogoutCommand`
- `RegisterAccountCommand`
- `SelectWorkspaceCommand`
- `CompanyMembershipAddRequest`
- `CompanyMembershipUpdateRequest`
- `TransferOwnershipRequest`
- pending access request approve/reject inputs, currently explicit `requestId` parameters
- ticket status transition methods, including `AdvanceStatusAsync`, because status transitions are workflow actions even when the current signature has no extra transition payload

Delete commands currently carry route context plus confirmation text. New domain-first contracts already prefer route models plus explicit confirmation parameters:

- `CustomerRoute + confirmationName`
- `PropertyRoute + confirmationName`
- `UnitRoute + confirmationUnitNr`
- `ResidentRoute + confirmationIdCode`
- `TicketRoute` for current ticket delete

If later delete flows need dependency review snapshots or richer confirmation state, introduce focused delete confirmation DTOs instead of reusing broad command DTOs.

## Contracts Needing Signature Adjustments

New Phase 3 aggregate contracts already follow the target route + canonical DTO pattern:

- `ICustomerService`
- `IPropertyService`
- `IUnitService`
- `IResidentService`
- `ILeaseService`
- `ITicketService`
- `IManagementCompanyService`

Remaining granular transition contracts still expose old command/query DTOs and should be retired or adapted when Phase 4/5 moves callers:

- `ICompanyCustomerService`
- `ICustomerProfileService`
- `IPropertyProfileService`
- `IPropertyWorkspaceService`
- `IUnitProfileService`
- `IUnitWorkspaceService`
- `IResidentProfileService`
- `IResidentWorkspaceService`
- `ILeaseAssignmentService`
- `IManagementTicketService`

`ICompanyMembershipService`, `IOnboardingService`, and `IWorkspaceService` remain orchestration exceptions. Their command/request DTOs are justified unless later phases identify exact duplicates of canonical aggregate DTOs.

## Implementation Guidance For Phase 4A/4B

Domain services should use this order before any BaseService CRUD call:

1. Validate route request shape.
2. Resolve route/natural keys into one of the trusted scope models.
3. Authorize the actor using membership, role, or resident access policy.
4. Validate canonical DTO state and business rules.
5. Validate cross-tenant references and duplicate business keys.
6. Set server-owned fields from scope, such as `ManagementCompanyId`, `CustomerId`, `PropertyId`, `UnitId`, slug, status, timestamps, and ticket number.
7. Call protected BaseService add helpers or inherited read/update/delete primitives only after scope is trusted.
8. Save and reload through the canonical mutation method.
9. Compose projection-returning methods by calling the canonical mutation method first.

Do not pass client-provided tenant or parent ids from canonical DTOs through as authority. Treat those ids as ignored, overwritten, or validated against scope depending on the operation.

## Build Status

Build was not run. The master handoff directs agents to ask the user to run builds manually.

Recommended manual build:

```powershell
dotnet build App.BLL.DTO\App.BLL.DTO.csproj -nologo
```

## Risks And Unresolved Questions

- `ManagementCompanyRoute` is the current company route model name, while the plan examples use `CompanyRoute`. Keeping the existing name avoids churn in Phase 3 contracts.
- `ResidentScope` authorization can be management-side or resident-side depending on the use case. Later services must not assume nullable membership means authorized.
- `TicketScope` includes optional related aggregate ids because tickets can reference multiple tenant resources. Ticket service implementations must still validate all provided references belong to the resolved management company.
- Lease flows have two route/scope entry points, resident-based and unit-based. Later phases should keep both until the UI/navigation model is unified.
- Some canonical DTOs still contain server-owned ids such as `ManagementCompanyId` because they model persisted entity state. Service implementations must set or validate these from trusted scope instead of trusting client input.

## Assumptions

- Phase 3.5 should not rewrite current services or controllers.
- Existing route request models from Phase 3 are acceptable and do not need conversion to records for this phase.
- `CompanyMembershipContext` remains the authoritative current management membership context model.
- Trusted scope models belong in `App.BLL.DTO` so public contracts can use them later without introducing WebApp or DAL dependencies.

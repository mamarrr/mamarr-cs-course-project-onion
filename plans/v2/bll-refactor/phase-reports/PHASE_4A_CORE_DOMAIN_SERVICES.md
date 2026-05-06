# Phase 4A Core Domain Services Report

Date: 2026-05-06

## Summary

Flattened the Phase 4A customer, property, unit, and resident BLL services so the aggregate services are the BLL entrypoints for their domains.

The old profile/workspace/access/company-customer service contracts and implementations were removed. Their access checks, workspace resolution, list, profile, create, update, and delete behavior now live directly in:

- `CustomerService`
- `PropertyService`
- `UnitService`
- `ResidentService`

## Domain Entrypoints

`IAppBLL` now exposes only the domain-first aggregate services for these areas:

- `Customers`
- `Properties`
- `Units`
- `Residents`

The removed granular facade entries are no longer exposed:

- customer access/profile/workspace/company-customer services
- property profile/workspace services
- unit access/profile/workspace services
- resident access/profile/workspace services

## Mutation Behavior

Canonical mutation methods use route models for trusted scope and canonical BLL DTOs for payload state:

- `CreateAsync(route, dto, ct)` resolves scope, validates payload and references, sets server-owned IDs/slugs, then calls `AddAndFindCoreAsync`.
- `UpdateAsync(route, dto, ct)` resolves scope, preserves server-owned IDs/slugs, validates, then calls `base.UpdateAsync` and `SaveChangesAsync`.
- `DeleteAsync(route, confirmation, ct)` resolves scope, validates confirmation and role, runs `AppDeleteGuard`, then calls `base.RemoveAsync` and `SaveChangesAsync`.

Projection helpers remain wrappers over canonical mutations:

- `CreateAndGetProfileAsync(...)`
- `UpdateAndGetProfileAsync(...)`

## WebApp Migration

MVC callers were migrated away from deleted granular services and now call:

- `_bll.Customers`, `_bll.Properties`, `_bll.Units`, `_bll.Residents`
- directly injected `ICustomerService`, `IPropertyService`, `IUnitService`, `IResidentService`

The existing REST API surface for these areas was intentionally deleted instead of migrated in place:

- API controllers under `WebApp/ApiControllers`
- API DTO files under `App.DTO/v1`
- API mapper files under `WebApp/Mappers/Api`

Those contracts and mappings need a complete rewrite after the BLL flattening, so preserving or partially migrating the old API layer would create misleading compatibility and stale DTO boundaries.

`ManagementTicketService` now depends on `ICustomerService` and uses `ResolveCompanyWorkspaceAsync(...)` for management company scope resolution.

## DI

`WebApp/Helpers/DependencyInjectionHelpers.cs` registers only the aggregate services for these domains:

- `ICustomerService`
- `IPropertyService`
- `IUnitService`
- `IResidentService`

No registrations remain for the removed granular services.

## Verification

Static checks performed:

- No WebApp/AppBLL references remain to the removed granular service contracts or implementations.
- No DI registrations remain for the removed granular services.
- No domain service depends on the removed granular services.
- Aggregate create paths call `AddAndFindCoreAsync`.
- Aggregate update paths call `base.UpdateAsync`.
- Aggregate delete paths call `base.RemoveAsync`.

Build was not rerun after the final nullability fixes because the project owner asked the agent to ask before building.

## Notes

No tests were written, per the repository testing override.

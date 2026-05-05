# BLL + WebApp Akaver-Style Refactor Plan

Repository: `mamarrr/mamarr-cs-course-project`  
Branch context: `dev`  
Reference architecture: `mamarrr/akaver-hw-demo`  
Plan file: `BLL_WEBAPP_AKAVER_REFACTOR_PLAN.md`

---

## 1. Purpose

This plan defines the next refactor after the DAL refactor.

The goal is to align the BLL and MVC WebApp layers with the high-level architecture style used in `akaver-hw-demo`, while keeping the project practical and not over-engineered.

The BLL must be ready for future API controllers. API controllers are out of scope for this refactor, but the BLL must be designed so that future API controllers can be added without changing BLL contracts, DTOs, service boundaries, or result/error behavior.

---

## 2. Confirmed source-of-truth decisions

These decisions are confirmed and should be treated as constraints for the implementation agent.

### 2.1 DAL refactor status

The DAL refactor is complete.

The BLL must use `IAppUOW`. It reportedly already does.

Canonical DAL DTOs are now the standard for simple CRUD. Workflow-specific DAL DTOs may remain if they are genuinely needed.

The BLL delete guard stays.

`ManagementCompany.DeleteCascadeAsync` is an intentional exception. It may later be moved to Admin-only usage and removed from user-facing UI, but this refactor must not remove it.

### 2.2 Base and DAL project constraints

`Base.BLL` and `Base.BLL.Contracts` already exist.

Base solution/projects and DAL projects should not be changed as part of this refactor.

Minimal DAL changes are allowed only when they are safe and clearly justified, for example removing an unnecessary non-canonical DAL DTO while preserving functionality.

If something completely breaks the architecture and appears to require a Base/DAL change, stop and ask for permission. The implementation agent must explain:

- what breaks,
- why the existing architecture cannot support the use case,
- what change is required,
- why the change is minimal,
- what alternatives were considered.

### 2.3 DTO simplification goal

The goal is not to delete DTOs blindly.

The goal is to keep the project as simple as possible while preserving good architecture.

A custom DTO should stay if it represents a real workflow, projection, access context, dashboard, filtered query, status transition, ownership transfer, or other application-specific use case.

A custom DTO should be removed if it only duplicates a canonical DTO for simple CRUD and can be replaced without harming clarity or architecture.

### 2.4 BLL service boundary rule

BLL services should be split by domain capability first, then by UI workspace second.

The domain/service boundary should not mirror MVC controller folders one-to-one unless that split also makes sense as a business/application capability.

### 2.5 WebApp Area model

Target WebApp Areas:

```text
Areas/
  Public/
  Portal/
  Admin/
```

`Public` is for public-facing and authentication/onboarding flows.

`Portal` is the protected user-facing application area.

`Admin` is the administrative area. It may temporarily contain scaffolded controllers and direct `App.DAL.EF` usage, but this is technical debt and should be isolated.

### 2.6 WebApp dependency rule

Portal and Public MVC controllers should inject `IAppBLL` where possible.

Admin controllers may temporarily use `App.DAL.EF` because the Admin area is scaffolded. This must be marked as temporary technical debt.

No non-Admin controller may use `App.DAL.EF`.

### 2.7 BLL result style

BLL should use `FluentResults` and typed BLL errors as the standard result style.

BLL should not throw exceptions for normal business outcomes such as not found, forbidden, validation errors, conflicts, or business rule failures.

Exceptions remain acceptable for unexpected infrastructure/programmer failures.

### 2.8 MVC ViewModel rule

MVC pages should use ViewModels.

Controllers may map ViewModel -> BLL DTO directly in controller code. Dedicated MVC ViewModel-to-BLL mappers are not required.

Example target style:

```csharp
var dto = new CustomerBllDto
{
    Id = vm.Id,
    Name = vm.Name,
    RegistryCode = vm.RegistryCode
};

var result = await _bll.Customers.UpdateAsync(dto, userId, cancellationToken);
```

### 2.9 BLL/DAL mapper rule

Canonical BLL DTO <-> canonical DAL DTO mapping must use mappers.

These mappers must implement `Base.Contracts.IBaseMapper`.

Canonical BLL DTOs should inherit from `BaseEntity`, same as canonical DAL DTOs.

MVC ViewModel -> BLL DTO does not need mapper classes.

Future API DTO -> BLL DTO mapping can be implemented later in API controllers or API mapper classes.

### 2.10 API-readiness hard requirement

API controllers are out of scope now.

No API DTOs need to be created now.

However, BLL API-readiness is non-negotiable.

BLL must not depend on:

- MVC controllers,
- ViewModels,
- API DTOs,
- `HttpContext`,
- cookies,
- route data,
- Razor,
- TempData,
- ViewData,
- session,
- request/response objects,
- Area names.

BLL should accept explicit BLL DTOs and explicit BLL context models.

MVC maps ViewModel -> BLL DTO/model.

Future API controllers will map API DTO -> BLL DTO/model.

---

## 3. Target architecture overview

### 3.1 Dependency direction

Target dependency direction:

```text
Base.Domain
Base.Contracts
Base.BLL.Contracts
Base.BLL

App.Domain
App.DAL.DTO
App.DAL.Contracts
App.DAL.EF

App.BLL.DTO
App.BLL.Contracts
App.BLL

App.DTO/v1          future API DTOs, not part of this refactor
WebApp ViewModels   MVC only
WebApp              MVC composition/presentation
```

Forbidden dependency directions:

```text
App.BLL -> WebApp
App.BLL -> App.DTO/v1
App.BLL -> App.DAL.EF
App.BLL.Contracts -> App.DAL.DTO
WebApp Public/Portal -> App.DAL.EF
Repositories -> App.BLL.DTO
Repositories -> FluentResults
```

### 3.2 DTO chain

Target DTO chain:

```text
Domain entity
  -> DAL DTO
  -> BLL DTO
    -> MVC ViewModel
    -> future API DTO
```

Rules:

- DAL repositories map Domain <-> DAL DTO.
- BLL services map DAL DTO <-> BLL DTO.
- MVC controllers map ViewModel <-> BLL DTO.
- Future API controllers map API DTO <-> BLL DTO.
- Controllers must not use DAL DTOs.
- Controllers must not use domain entities.
- BLL contracts must not expose DAL DTOs.
- API controllers, when added later, must not expose BLL DTOs directly if App.DTO/v1 DTOs exist.

### 3.3 IAppBLL facade

WebApp should normally depend on `IAppBLL`.

Target controller dependency style:

```csharp
private readonly IAppBLL _bll;

public CustomersController(IAppBLL bll)
{
    _bll = bll;
}
```

Target usage style:

```csharp
var result = await _bll.Customers.UpdateAsync(dto, userId, cancellationToken);
await _bll.SaveChangesAsync(cancellationToken);
```

Individual BLL service injection should be rare in Public/Portal controllers and must be justified.

### 3.4 BLL service shape

BLL services should follow domain capability boundaries.

Suggested top-level service families:

```text
Customers
Properties
Units
Residents
Leases
Tickets
ManagementCompanies
Memberships / CompanyUsers
Onboarding
Workspace / Access
Lookups / Options
DeleteGuard
```

A UI workspace can still influence method names and context models, but it should not be the primary reason for creating separate services.

Good split:

```text
PropertyService
  - canonical property CRUD where applicable
  - property profile/update/delete use cases
  - property workspace/detail use cases

UnitService
  - canonical unit CRUD where applicable
  - unit dashboard/profile use cases
  - unit tenant/lease-related use cases only if still unit-owned

CustomerService
  - customer CRUD
  - company/customer relationship use cases
  - customer profile/workspace use cases
```

Avoid service explosion like:

```text
CustomerProfileService
CustomerWorkspaceService
CompanyCustomerService
ManagementCustomerService
ManagementCustomerAccessService
```

unless each service has a strong, stable, domain-level reason to exist.

---

## 4. Target URL and Area architecture

### 4.1 Area names

Use:

```text
Public
Portal
Admin
```

### 4.2 Route style

Use Option A with company-scoped property/unit shortcuts.

This means:

- nested routes for natural list/create flows,
- shorter company-scoped routes for detail/profile/dashboard flows,
- BLL always validates ownership/access.

### 4.3 Public routes

Target public routes:

```text
/
 /login
 /register
 /onboarding
 /logout
 /set-language
```

Public area should contain:

```text
Areas/Public/Controllers/
  HomeController
  AccountController
  OnboardingController
```

Exact controller names can be adjusted, but authentication/onboarding/public pages belong in Public, not Portal.

### 4.4 Portal routes

Target protected application routes:

```text
/m/{companySlug}
  management/company dashboard

/m/{companySlug}/profile
  management company profile

/m/{companySlug}/users
  company memberships / company users

/m/{companySlug}/customers
  customer list/create for company

/m/{companySlug}/customers/{customerSlug}
  customer dashboard/profile

/m/{companySlug}/customers/{customerSlug}/properties
  property list/create for customer

/m/{companySlug}/properties/{propertySlug}
  property dashboard/profile

/m/{companySlug}/properties/{propertySlug}/units
  unit list/create for property

/m/{companySlug}/units/{unitSlug}
  unit dashboard/profile

/m/{companySlug}/leases
  management/company lease list/search if needed

/m/{companySlug}/tickets
  management/company ticket list/search if needed

/resident
  resident dashboard/landing

/resident/units
  resident unit list

/resident/tickets
  resident tickets
```

The key rule:

```text
Use nested parent routes where parent context is part of the action.
Use company-scoped shortcut routes where the resource itself can identify the parent relation.
```

Examples:

Property list/create belongs under customer:

```text
/m/{companySlug}/customers/{customerSlug}/properties
```

Property profile/dashboard can be company-scoped:

```text
/m/{companySlug}/properties/{propertySlug}
```

Unit list/create belongs under property:

```text
/m/{companySlug}/properties/{propertySlug}/units
```

Unit profile/dashboard can be company-scoped:

```text
/m/{companySlug}/units/{unitSlug}
```

### 4.5 Future API compatibility

The route strategy should support future API routes like:

```text
/api/v1/companies/{companyIdOrSlug}/customers
/api/v1/companies/{companyIdOrSlug}/customers/{customerIdOrSlug}
/api/v1/companies/{companyIdOrSlug}/customers/{customerIdOrSlug}/properties
/api/v1/companies/{companyIdOrSlug}/properties/{propertyIdOrSlug}
/api/v1/companies/{companyIdOrSlug}/properties/{propertyIdOrSlug}/units
/api/v1/companies/{companyIdOrSlug}/units/{unitIdOrSlug}
```

The MVC route design should not force BLL methods to require deeply nested route arguments unless the use case truly needs them.

BLL methods should accept explicit context models and identifiers:

```csharp
Task<Result<PropertyWorkspaceModel>> GetPropertyWorkspaceAsync(
    PortalWorkspaceContext context,
    string propertySlug,
    CancellationToken cancellationToken = default);
```

not MVC-specific route objects.

---

## 5. Target context management

### 5.1 Route-first, cookie-second

Context management should be route-first.

Route values are the source of truth for the current requested resource:

```text
companySlug
customerSlug
propertySlug
unitSlug
```

Cookies are secondary and should only remember the user's last selected context for redirect convenience after login/onboarding.

Cookies must not be used as the main business authorization source.

### 5.2 WebApp context resolver

Introduce a WebApp-level resolver/accessor.

Suggested names:

```text
ICurrentPortalContextResolver
ICurrentPortalContextAccessor
PortalContextResolver
```

Suggested model:

```csharp
public sealed class PortalRouteContext
{
    public Guid AppUserId { get; init; }
    public string? CompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitSlug { get; init; }
    public PortalContextKind Kind { get; init; }
}
```

This resolver may use:

- `ClaimsPrincipal`,
- route values,
- remembered context cookie,
- current request path.

It must not perform deep business decisions itself. It gathers request context and calls BLL access/workspace methods when needed.

### 5.3 BLL access/context validation

BLL owns authorization/access decisions.

BLL should decide:

- unauthorized,
- forbidden,
- not found,
- validation failure,
- conflict,
- business rule violation.

Repositories only answer factual questions.

The WebApp resolver/controller translates BLL result errors to MVC action results.

Suggested MVC translation helper:

```csharp
public static IActionResult ToActionResult(this Result result, Controller controller)
```

or a controller base/helper method.

Avoid leaking MVC into BLL.

---

## 6. BLL DTO simplification rules

### 6.1 Canonical BLL DTOs

Every entity that supports simple CRUD should have a canonical BLL DTO.

Canonical BLL DTOs should:

- live in `App.BLL.DTO`,
- inherit from `BaseEntity`,
- represent the simple entity persistence/application shape,
- not include UI-specific fields,
- not include API-specific fields,
- not require `CreatedAt` unless the BLL use case truly exposes it,
- map to/from canonical DAL DTOs through mapper classes implementing `Base.Contracts.IBaseMapper`.

Candidate canonical BLL DTOs:

```text
CustomerBllDto
PropertyBllDto
UnitBllDto
ResidentBllDto
LeaseBllDto
TicketBllDto
VendorBllDto
ContactBllDto
ManagementCompanyBllDto
ManagementCompanyJoinRequestBllDto
```

Exact names should follow current project naming conventions, but the concept should be consistent.

### 6.2 Workflow DTOs

Workflow DTOs may remain when they represent real use cases.

Keep specialized DTOs for:

- lease assignment,
- ticket status/workflow transitions,
- ownership transfer,
- membership administration,
- onboarding/account flow,
- workspace/context resolution,
- filtered searches,
- dashboard/read projections,
- confirmation flows,
- operations requiring extra context not part of the entity.

Examples of acceptable specialized DTOs:

```text
TransferOwnershipRequest
ApproveJoinRequestModel
TicketDetailsModel
TicketStatusTransitionModel
LeaseAssignmentModel
WorkspaceCatalogModel
PropertyWorkspaceModel
UnitDashboardModel
```

### 6.3 DTOs to remove or merge

Remove/merge specialized DTOs when:

- they are only a wrapper around a canonical CRUD DTO,
- they duplicate fields exactly,
- they do not express workflow intent,
- they are used by only one simple CRUD method,
- replacing them with a canonical BLL DTO does not make the method less clear.

Bad pattern:

```text
CreateCustomerCommand
UpdateCustomerProfileCommand
DeleteCustomerCommand
```

when these are just simple CRUD shapes and can use `CustomerBllDto`.

Acceptable pattern:

```text
DeleteCustomerRequest
```

if the delete requires confirmation, dependency checks, actor context, or special business reason.

### 6.4 No DTO deletion without behavior preservation

DTO cleanup must preserve all existing behavior.

If a DTO appears redundant but removing it would make access checks, validation, or future API mapping less clear, keep it and document why.

---

## 7. Mapper rules

### 7.1 Required mapper boundary

Canonical BLL DTO <-> DAL DTO requires mapper classes.

Mappers must implement `Base.Contracts.IBaseMapper`.

Example shape:

```csharp
public class CustomerBllMapper
    : IBaseMapper<CustomerBllDto, CustomerDalDto>
{
    public CustomerBllDto? Map(CustomerDalDto? entity) { ... }

    public CustomerDalDto? Map(CustomerBllDto? entity) { ... }
}
```

Use the exact interface method names from the existing Base contracts.

### 7.2 No mappers required for MVC ViewModels

MVC controllers may map ViewModel -> BLL DTO inline.

ViewModel mapping is presentation composition and does not need to be abstracted unless repetition becomes painful.

### 7.3 Future API mapping

Future API controllers may map API DTO -> BLL DTO inline or through API mappers.

That is out of scope now.

BLL must not depend on those future API mappings.

---

## 8. FluentResults and typed error model

### 8.1 Standard BLL result shape

BLL service methods should return `Result<T>` or `Result`.

Examples:

```csharp
Task<Result<CustomerBllDto>> FindAsync(...);
Task<Result<IReadOnlyList<CustomerListItemModel>>> AllAsync(...);
Task<Result<Guid>> AddAsync(...);
Task<Result> UpdateAsync(...);
Task<Result> RemoveAsync(...);
```

For simple CRUD, use a consistent result shape across services.

### 8.2 Typed errors

Typed BLL errors should represent normal application outcomes.

Examples:

```text
ValidationAppError
NotFoundError
ForbiddenError
UnauthorizedError
ConflictError
BusinessRuleError
UnexpectedAppError
```

Specific errors may exist for domain-specific cases, but avoid excessive one-off error types if a generic typed error is enough.

### 8.3 MVC translation

Controllers should translate BLL results to MVC responses.

Example:

```csharp
if (result.HasError<NotFoundError>())
{
    return NotFound();
}

if (result.HasError<ForbiddenError>())
{
    return Forbid();
}

if (result.HasError<ValidationAppError>())
{
    AddValidationErrorsToModelState(result);
    return View(vm);
}
```

Do not make BLL return MVC concepts.

### 8.4 Future API translation

Future API controllers should translate the same BLL errors to HTTP responses.

Example future mapping:

```text
UnauthorizedError -> 401
ForbiddenError -> 403
NotFoundError -> 404
ValidationAppError -> 400
ConflictError -> 409
BusinessRuleError -> 422 or 400
UnexpectedAppError -> 500
```

This is why BLL must standardize around typed errors now.

---

## 9. BaseService usage

### 9.1 Existing Base projects

`Base.BLL` and `Base.BLL.Contracts` already exist.

Do not redesign them in this refactor.

### 9.2 Candidate usage

Use BaseService only where it fits naturally.

Good candidates:

- simple CRUD,
- canonical BLL DTO <-> canonical DAL DTO,
- no complex access workflow hidden inside base methods,
- no status transition,
- no multi-repository orchestration,
- no deep validation beyond simple existence/scope checks.

Bad candidates:

- onboarding,
- membership administration,
- ownership transfer,
- ticket workflow,
- lease assignment workflow,
- workspace/context selection,
- delete guard,
- management company cascade delete,
- profile/dashboard methods with complex access checks,
- methods combining many repositories.

### 9.3 Do not force workflows into BaseService

Akaver-style architecture does not mean every service must inherit a base service.

Use BaseService to remove boring duplication. Do not use it to hide business rules.

---

## 10. Target BLL service boundaries

The implementation agent should inventory existing services and migrate toward this structure gradually.

### 10.1 Customers

Owns:

- customer canonical CRUD,
- customer list under company,
- customer profile,
- customer-property relationship reads where customer is the natural parent,
- customer access validation when customer is the requested resource.

Avoid:

- property internals,
- unit internals,
- ticket workflow details unless customer-specific view projection requires it.

### 10.2 Properties

Owns:

- property canonical CRUD,
- property profile/dashboard,
- property list/create under customer,
- property access validation,
- property delete through delete guard,
- property units list/create when property is parent.

Avoid:

- customer membership workflows,
- unit profile internals beyond list/projection needs.

### 10.3 Units

Owns:

- unit canonical CRUD,
- unit profile/dashboard,
- unit access validation,
- unit lease/tenant projections when unit is the requested resource,
- unit delete through delete guard.

Avoid:

- lease assignment orchestration if it becomes large; that belongs in Leases.

### 10.4 Residents

Owns:

- resident canonical CRUD,
- resident profile/dashboard,
- resident access validation,
- resident unit/ticket list projections where resident is the requested actor/context.

### 10.5 Leases

Owns:

- lease canonical CRUD if applicable,
- lease assignment,
- lease overlap/business validation,
- resident/unit lease views,
- lease-scoped deletes that only delete lease rows after scoped lookup.

Avoid:

- property/unit/customer existence ownership facts; call owning repositories via BLL/UOW.

### 10.6 Tickets

Owns:

- ticket canonical CRUD if applicable,
- ticket workflow,
- ticket status transitions,
- ticket list/details,
- ticket delete through delete guard.

### 10.7 ManagementCompanies

Owns:

- management company profile,
- management company canonical CRUD if applicable,
- management company access facts at BLL level,
- `DeleteCascadeAsync` exception exposure if needed.

User-facing destructive management-company cascade delete should be removed later from Portal and moved to Admin-only use.

### 10.8 Memberships / CompanyUsers

Owns:

- company membership administration,
- role assignment rules,
- ownership transfer,
- join request review,
- membership access policy.

This service may remain workflow-heavy and should not be forced into BaseService.

### 10.9 Onboarding

Owns:

- registration/login onboarding flows,
- context chooser flow,
- management company creation during onboarding,
- join requests from onboarding,
- default redirect target resolution.

Onboarding must remain API-ready by not depending on MVC objects.

### 10.10 Workspace / Access

Owns shared BLL context/access use cases that do not naturally belong to one domain service.

Examples:

- resolve current portal workspace,
- authorize selected workspace,
- list available workspaces for user,
- produce workspace catalog.

Keep this service focused. Do not let it become a dump for all dashboard logic.

### 10.11 DeleteGuard

Owns blocked-delete policy.

It asks repositories for dependency facts and returns business decisions.

It does not delete anything.

It does not produce UI dependency reports.

---

## 11. WebApp target structure

### 11.1 Public Area

Suggested structure:

```text
WebApp/
  Areas/
    Public/
      Controllers/
        HomeController.cs
        AccountController.cs
        OnboardingController.cs
      Views/
        Home/
        Account/
        Onboarding/
```

Public may use layouts suitable for unauthenticated pages.

### 11.2 Portal Area

Suggested structure:

```text
WebApp/
  Areas/
    Portal/
      Controllers/
        Management/
          DashboardController.cs
          ProfileController.cs
          UsersController.cs
        Customers/
          CustomersController.cs
          CustomerProfileController.cs
        Properties/
          PropertiesController.cs
          PropertyProfileController.cs
          PropertyUnitsController.cs
        Units/
          UnitsController.cs
          UnitProfileController.cs
          UnitTenantsController.cs
        Residents/
          ResidentDashboardController.cs
          ResidentUnitsController.cs
          ResidentTicketsController.cs
        Leases/
          LeasesController.cs
        Tickets/
          TicketsController.cs
      Views/
        Management/
        Customers/
        Properties/
        Units/
        Residents/
        Leases/
        Tickets/
      ViewModels/
        Management/
        Customers/
        Properties/
        Units/
        Residents/
        Leases/
        Tickets/
```

Controllers may be grouped by directories while sharing the same `Portal` Area.

### 11.3 Admin Area

Suggested structure:

```text
WebApp/
  Areas/
    Admin/
      Controllers/
      Views/
      ViewModels/
```

Admin is allowed to remain scaffolded for now.

Mark all direct `App.DAL.EF` usage in Admin as temporary technical debt.

No Portal/Public controller may follow that pattern.

### 11.4 Layout services

Layout services should live in WebApp because they compose UI-specific navigation and display models.

They may call `IAppBLL`.

They must not use `AppDbContext`.

They must not expose domain/DAL DTOs to Razor views.

---

## 12. Phased implementation plan

## Phase 0 — Preparation and guardrails

### Scope

Prepare the branch for safe refactoring.

### Tasks

1. Confirm solution builds before refactor.
2. Create this plan file in the repository, for example under `plans/`.
3. Add a short architecture note to the implementation prompt:
   - no API controller refactor,
   - no test refactor,
   - no Base/DAL changes unless explicitly approved,
   - BLL must be API-ready.
4. Create a checklist of forbidden dependencies:
   - `App.BLL` must not reference `App.DAL.EF`,
   - `App.BLL.Contracts` must not expose DAL DTOs,
   - Portal/Public controllers must not use `App.DAL.EF`,
   - BLL must not use MVC/API/WebApp concepts.

### Acceptance criteria

- Project builds before code changes.
- The implementation agent has a written checklist.
- No behavior changes yet.

---

## Phase 1 — Inventory current BLL and WebApp

### Scope

Classify before changing.

### Tasks

For every BLL service method, record:

- service name,
- method name,
- input types,
- output/result type,
- repositories/UOW members used,
- whether it is simple CRUD,
- whether it is workflow,
- whether it is access/context,
- whether it is projection/read model,
- whether it mutates state,
- whether it calls `SaveChangesAsync` internally,
- whether it should use canonical BLL DTO,
- whether it can use BaseService,
- whether it exposes DAL DTOs,
- whether it is called by MVC controller,
- whether future API could call it without MVC context.

For every MVC controller, record:

- Area,
- route,
- injected dependencies,
- whether it injects `IAppBLL`,
- whether it injects BLL services directly,
- whether it injects `AppDbContext`/repositories,
- ViewModels used,
- whether route/context resolution is duplicated.

### Deliverable

Create an internal inventory document or checklist, such as:

```text
plans/BLL_WEBAPP_REFACTOR_INVENTORY.md
```

This can later be deleted or kept as refactor documentation.

### Acceptance criteria

- No code changes yet except optional documentation.
- All BLL services and MVC controllers are classified.
- Known exceptions are listed.

---

## Phase 2 — BLL contracts and DTO cleanup design

### Scope

Decide canonical DTOs and DTOs to keep/remove.

### Tasks

1. List all `App.BLL.DTO` types.
2. Mark each as one of:
   - canonical BLL DTO,
   - workflow DTO,
   - query/filter DTO,
   - projection/read model,
   - options/dropdown model,
   - error type,
   - redundant candidate.
3. Ensure canonical BLL DTOs inherit from `BaseEntity`.
4. Ensure canonical BLL DTOs map to canonical DAL DTOs.
5. Identify simple CRUD methods currently using command/query DTOs.
6. Replace only if the canonical DTO is equally clear or clearer.
7. Keep workflow DTOs where they express real application behavior.

### Acceptance criteria

- Clear list of canonical BLL DTOs.
- Clear list of DTOs to keep.
- Clear list of DTOs to remove/merge.
- No API DTOs introduced.
- No MVC ViewModels moved into BLL.

---

## Phase 3 — BLL mapper standardization

### Scope

Standardize BLL DTO <-> DAL DTO mappers.

### Tasks

1. Locate all BLL mappers.
2. Ensure canonical BLL/DAL DTO mappers implement `Base.Contracts.IBaseMapper`.
3. Rename/organize mappers consistently.
4. Remove ad-hoc mapping duplication inside services when mapper exists.
5. Keep inline mapping for highly specialized workflow projections only when mapper abstraction would add noise.

### Acceptance criteria

- Canonical BLL DTOs have clear mapper coverage.
- BLL services no longer duplicate simple canonical mapping.
- No MVC ViewModel mapping classes are introduced.
- No DAL DTOs leak to BLL contracts.

---

## Phase 4 — IAppBLL facade alignment

### Scope

Make `IAppBLL` the main boundary for WebApp.

### Tasks

1. Audit `IAppBLL`.
2. Ensure it exposes domain-capability service properties.
3. Avoid exposing every tiny workflow service as a top-level property unless needed.
4. Align naming with Akaver style.
5. Ensure `AppBLL` composes services from `IAppUOW` and mappers.
6. Ensure `SaveChangesAsync` is available from `IAppBLL` if this is the established project style.
7. Avoid controller-level service explosion.

### Example target

```csharp
public interface IAppBLL
{
    ICustomerService Customers { get; }
    IPropertyService Properties { get; }
    IUnitService Units { get; }
    IResidentService Residents { get; }
    ILeaseService Leases { get; }
    ITicketService Tickets { get; }
    IManagementCompanyService ManagementCompanies { get; }
    IMembershipService Memberships { get; }
    IOnboardingService Onboarding { get; }
    IWorkspaceService Workspaces { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Exact names can follow existing code, but the shape should be domain-first.

### Acceptance criteria

- Portal/Public controllers can inject `IAppBLL`.
- `IAppBLL` does not expose DAL concepts.
- Service properties are domain-capability oriented.

---

## Phase 5 — Service boundary cleanup

### Scope

Move methods to correct BLL services.

### Tasks

1. Start with low-risk domain slices.
2. Move simple CRUD methods into domain services.
3. Move access/context methods into domain access methods or shared workspace/access service.
4. Move ownership/membership workflows into membership service.
5. Move ticket workflow methods into ticket service.
6. Move lease assignment methods into lease service.
7. Remove or merge services that only exist because of old Area/controller structure.
8. Do not force every UI page into its own service.

### Suggested order

1. Lookups/options/simple supporting services.
2. Customers.
3. Properties.
4. Units.
5. Residents.
6. Leases.
7. Tickets.
8. Memberships.
9. Onboarding.
10. Workspace/access.

### Acceptance criteria

- Services are domain-first.
- UI workspace-specific methods exist only where justified.
- No functionality is lost.
- BLL remains API-ready.

---

## Phase 6 — Simple CRUD and BaseService application

### Scope

Apply BaseService only where appropriate.

### Tasks

1. Identify simple CRUD methods.
2. For each candidate, decide:
   - can it use canonical BLL DTO?
   - can it use canonical DAL DTO?
   - does it need custom workflow validation?
   - does it require multiple repositories?
   - does it need delete guard?
3. Use existing `Base.BLL`/`Base.BLL.Contracts` patterns where suitable.
4. Do not modify Base projects unless explicitly approved.
5. Keep workflow-heavy methods custom.

### Good candidates

- simple lookup-like service,
- simple customer/property/unit/resident CRUD methods if access rules are cleanly separable,
- simple vendor/contact CRUD if present.

### Bad candidates

- onboarding,
- ticket workflow,
- lease assignment,
- ownership transfer,
- membership administration,
- delete guard,
- management company cascade delete.

### Acceptance criteria

- BaseService reduces duplication without hiding business rules.
- Workflow methods remain explicit.
- No Base project changes without permission.

---

## Phase 7 — FluentResults/typed error standardization

### Scope

Make BLL results consistent.

### Tasks

1. Audit current BLL result models.
2. Convert normal business outcomes to `Result`/`Result<T>`.
3. Use typed errors consistently.
4. Remove ad-hoc boolean result classes where they only encode standard errors.
5. Keep specialized result models only when they carry meaningful domain data.
6. Ensure MVC can translate BLL errors cleanly.
7. Ensure future API can translate the same errors cleanly.

### Acceptance criteria

- BLL does not throw for normal not-found/forbidden/validation outcomes.
- Typed errors are consistent.
- MVC action-result translation is straightforward.
- Future API translation is straightforward.

---

## Phase 8 — Portal/Public/Admin Area reorganization

### Scope

Restructure MVC Areas.

### Tasks

1. Create/standardize `Public`, `Portal`, and `Admin` Areas.
2. Move Home/Login/Register/Onboarding into Public.
3. Move protected application controllers into Portal.
4. Move current management/customer/property/unit/resident Areas into Portal directories.
5. Keep Admin scaffolded for now.
6. Update `_ViewImports.cshtml`, layouts, and routes.
7. Ensure `asp-area` usages are updated.
8. Avoid changing business logic during this move.

### Acceptance criteria

- Areas are reduced to Public/Portal/Admin.
- Protected app controllers live under Portal.
- Public controllers live under Public.
- Admin remains isolated.
- Routes still work according to target route plan.

---

## Phase 9 — Portal route refactor

### Scope

Implement Option A with company-scoped property/unit shortcuts.

### Target routes

```text
/m/{companySlug}
/m/{companySlug}/profile
/m/{companySlug}/users
/m/{companySlug}/customers
/m/{companySlug}/customers/{customerSlug}
/m/{companySlug}/customers/{customerSlug}/properties
/m/{companySlug}/properties/{propertySlug}
/m/{companySlug}/properties/{propertySlug}/units
/m/{companySlug}/units/{unitSlug}
/resident
/resident/units
/resident/tickets
```

### Tasks

1. Define route attributes or route conventions.
2. Prefer stable route names for frequently linked pages.
3. Update links in views/layouts.
4. Remove old route assumptions.
5. Add redirects from old routes only if needed for usability.
6. Ensure BLL methods receive explicit company/resource identifiers.

### Acceptance criteria

- Company-scoped property/unit detail routes work.
- Nested list/create routes work.
- Route values drive context resolution.
- Cookie context is not required for resource authorization.

---

## Phase 10 — Context resolver implementation

### Scope

Introduce route-first context management.

### Tasks

1. Create WebApp-level context resolver/accessor.
2. Read user id from claims.
3. Read route values.
4. Read remembered context cookie only for fallback redirect/chooser behavior.
5. Call BLL workspace/access services for authorization.
6. Return a WebApp context model or translated MVC result.
7. Remove repeated context-resolution code from controllers gradually.

### Acceptance criteria

- Context logic is centralized.
- BLL receives explicit context models.
- Cookies are secondary.
- Controllers become smaller.
- BLL remains independent of MVC and cookies.

---

## Phase 11 — MVC controller refactor to IAppBLL

### Scope

Refactor controllers to use ViewModels and `IAppBLL`.

### Tasks

1. For each Portal/Public controller:
   - inject `IAppBLL`,
   - remove individual service injections where possible,
   - remove `AppDbContext`,
   - remove repository usage,
   - map ViewModel -> BLL DTO inline,
   - call BLL services through `_bll`,
   - call `_bll.SaveChangesAsync` where needed.
2. Use ViewModels for all MVC views.
3. Keep BLL DTOs out of Razor views unless a page is genuinely trivial and already aligned, but target ViewModels for MVC pages.
4. Translate BLL results to MVC responses.
5. Keep Admin exceptions isolated.

### Acceptance criteria

- Portal/Public controllers inject `IAppBLL` where possible.
- Portal/Public controllers do not use `App.DAL.EF`.
- MVC views use ViewModels.
- Controllers map ViewModel -> BLL DTO.
- BLL is not polluted by MVC needs.

---

## Phase 12 — Layout and navigation cleanup

### Scope

Make layout/navigation use the new context and BLL boundaries.

### Tasks

1. Ensure Portal layout uses WebApp ViewModels.
2. Layout providers may call `IAppBLL`.
3. Layout providers must not use `AppDbContext`.
4. Context switcher uses BLL workspace catalog.
5. Links use new route names/route shapes.
6. Remove ViewData/ViewBag where strongly typed models are practical.

### Acceptance criteria

- Layouts are strongly typed where practical.
- No layout uses `AppDbContext`.
- Navigation respects new Area/route structure.
- Workspace switching uses route-first/cookie-second rules.

---

## Phase 13 — Final architecture audit

### Scope

Verify the refactor.

### Audit checklist

BLL:

- `App.BLL` does not reference `App.DAL.EF`.
- BLL services use `IAppUOW`.
- BLL contracts expose BLL DTOs/models/results only.
- Canonical BLL DTOs inherit from `BaseEntity`.
- Canonical BLL DTO <-> DAL DTO mappers implement `IBaseMapper`.
- Simple CRUD uses canonical DTOs where practical.
- Workflow DTOs remain only where justified.
- FluentResults/typed errors are consistent.
- Delete guard still handles converted blocked deletes.
- `ManagementCompany.DeleteCascadeAsync` still exists as intentional exception.
- BLL has no MVC/API/WebApp dependencies.

WebApp:

- Areas are Public/Portal/Admin.
- Portal/Public controllers inject `IAppBLL` where possible.
- Portal/Public controllers do not use `App.DAL.EF`.
- Admin direct EF usage is isolated and marked technical debt.
- MVC pages use ViewModels.
- Controllers map ViewModel -> BLL DTO directly.
- Context management is route-first/cookie-second.
- Routes follow Option A with company-scoped property/unit shortcuts.

Future API-readiness:

- Future API controllers can map API DTO -> BLL DTO.
- Future API controllers can call existing BLL methods.
- Future API controllers can translate existing BLL typed errors.
- No BLL changes are required merely to add API controllers.

### Acceptance criteria

- Build succeeds.
- Manual smoke test succeeds for main flows.
- No forbidden dependencies remain outside documented exceptions.
- API-readiness hard requirement is satisfied.

---

## 13. Out of scope

These are not part of this refactor:

- API controller creation/refactor.
- API DTO creation/refactor.
- Test rewrite.
- Admin area full cleanup.
- Removing `ManagementCompany.DeleteCascadeAsync`.
- Redesigning Base projects.
- Redesigning DAL projects.
- Large DAL refactor.
- Changing database schema unless a separate explicit need appears.

---

## 14. Permission gates

The implementation agent must stop and ask before doing any of the following:

1. Changing Base projects.
2. Making non-trivial DAL changes.
3. Removing `ManagementCompany.DeleteCascadeAsync`.
4. Changing database schema.
5. Refactoring API controllers.
6. Refactoring tests.
7. Introducing API DTOs.
8. Making BLL depend on MVC/API/WebApp concepts.
9. Keeping a direct `App.DAL.EF` dependency in Portal/Public.
10. Deleting a DTO that appears redundant but whose removal could alter workflow behavior.

The request for permission must include:

- the exact problem,
- the proposed change,
- why it is necessary,
- alternatives,
- risk,
- expected files affected.

---

## 15. Recommended vertical slice order

Use vertical slices to keep the refactor safe.

### Slice 1 — Supporting architecture

- `IAppBLL` alignment.
- Mapper conventions.
- Result/error conventions.
- Context resolver skeleton.
- No major domain behavior changes.

### Slice 2 — Customers

- Canonical DTO cleanup.
- Customer service boundary.
- Portal customer routes.
- Customer controllers use `IAppBLL`.

### Slice 3 — Properties

- Property canonical DTO cleanup.
- Property service boundary.
- `/m/{companySlug}/customers/{customerSlug}/properties`
- `/m/{companySlug}/properties/{propertySlug}`

### Slice 4 — Units

- Unit canonical DTO cleanup.
- Unit service boundary.
- `/m/{companySlug}/properties/{propertySlug}/units`
- `/m/{companySlug}/units/{unitSlug}`

### Slice 5 — Residents

- Resident service boundary.
- Resident dashboard/unit/ticket flows.
- `/resident` routes.

### Slice 6 — Leases

- Lease assignment/workflow cleanup.
- Preserve workflow DTOs where needed.

### Slice 7 — Tickets

- Ticket workflow cleanup.
- Preserve status/workflow DTOs where needed.

### Slice 8 — Memberships and management company users

- Membership admin.
- Role options.
- Ownership transfer.
- Join request review.

### Slice 9 — Onboarding and workspace catalog

- Public onboarding.
- Workspace chooser.
- Last-selected context cookies.
- Route-first redirect behavior.

### Slice 10 — Admin isolation

- Ensure Admin is isolated.
- Mark scaffolded EF usage.
- Do not fully refactor Admin yet.

---

## 16. Definition of done

The refactor plan is complete when:

- BLL is domain-first and Akaver-style.
- BLL uses `IAppUOW`.
- BLL contracts are MVC/API-neutral.
- BLL is ready for future API controllers.
- BLL uses FluentResults/typed errors consistently.
- Canonical BLL DTOs exist for simple CRUD and inherit from `BaseEntity`.
- Canonical BLL DTO <-> DAL DTO mappers implement `IBaseMapper`.
- DTO explosion is reduced without deleting useful workflow DTOs.
- MVC uses ViewModels.
- MVC controllers map ViewModel -> BLL DTO directly.
- Portal/Public controllers use `IAppBLL` where possible.
- Public/Portal/Admin Areas exist.
- Portal routes use Option A with company-scoped property/unit shortcuts.
- Context management is route-first/cookie-second.
- Admin EF usage is isolated as temporary debt.
- No API controller refactor was required.
- Future API controllers can be added without changing BLL.

---

## 17. Short implementation prompt for a coding agent

Use this condensed prompt when handing the plan to an implementation agent:

```text
Refactor BLL and MVC WebApp toward akaver-hw-demo style.

Do not refactor API controllers or tests.
Do not change Base projects.
Do not change DAL except tiny safe cleanup with permission.
BLL must use IAppUOW and must be API-ready.
BLL must not depend on MVC, API DTOs, HttpContext, cookies, routes, ViewModels, or WebApp.
Portal/Public MVC controllers should inject IAppBLL where possible.
MVC views should use ViewModels.
Controllers may map ViewModel -> BLL DTO inline.
Canonical BLL DTOs inherit from BaseEntity.
Canonical BLL DTO <-> DAL DTO mapping uses IBaseMapper mappers.
Use FluentResults/typed BLL errors.
Reduce DTO explosion only where custom DTOs duplicate canonical CRUD DTOs.
Keep workflow DTOs when justified.
Create Public, Portal, Admin Areas.
Use Option A routes with company-scoped property/unit shortcuts.
Context management is route-first and cookie-second.
Admin may temporarily use App.DAL.EF because it is scaffolded; mark this as technical debt.
Stop and ask permission before changing Base/DAL materially, touching API controllers/tests, changing schema, or removing ManagementCompany.DeleteCascadeAsync.
```

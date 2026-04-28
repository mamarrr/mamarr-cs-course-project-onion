# API controllers plan

## Goal
Add JWT-first API controllers so the solution can serve a separate frontend application while preserving the same business workflows and navigation structure that already exist in the MVC app.

The API should mirror the existing MVC controller responsibilities, but not the MVC rendering concerns such as page shell composition, Razor view models, TempData, anti-forgery posts, or cookie-based context switching.

## Planning assumptions
- Authentication for the frontend app is JWT-based, building on the existing identity API pattern in [`WebApp/ApiControllers/Identity/AccountController.cs`](../../WebApp/ApiControllers/Identity/AccountController.cs).
- Frontend context switching must be explicit API state, not cookie state. The API should return available contexts and let the frontend choose which management company or workspace resource to open.
- Requested scope excludes management-company access request workflows. Only these onboarding flows are needed now:
  - register
  - login
  - create management company
- Requested functional scope includes:
  - management company onboarding
  - management customer creation and traversal
  - property creation under customer
  - unit creation under property
  - resident creation under management
  - lease management from unit tenants page
  - lease management from resident units page
  - empty dashboards for customer, property, unit, resident
  - profile pages for customer, property, unit, resident

## Design principles
- Keep controllers thin and place business rules in BLL services, consistent with [`AGENTS.md`](../../AGENTS.md).
- Reuse existing authorization and dashboard context services already used by MVC controllers, such as [`ResolveDashboardAccessAsync()`](../../WebApp/Areas/Customer/Controllers/CustomerDashboardController.cs:73), [`ResolvePropertyDashboardContextAsync()`](../../WebApp/Areas/Property/Controllers/PropertyDashboardController.cs:90), and [`ResolveUnitDashboardContextAsync()`](../../WebApp/Areas/Unit/Controllers/UnitProfileController.cs:282).
- Use DTO-first API contracts in [`App.DTO/v1`](../../App.DTO/v1) instead of exposing domain entities or MVC view models.
- Preserve tenant isolation and IDOR protections for every read and write path.
- Return API-friendly validation and business errors using [`App.DTO/v1/RestApiErrorResponse.cs`](../../App.DTO/v1/RestApiErrorResponse.cs).
- Treat layout metadata as frontend responsibility. API responses should return resource data and navigation identifiers, not Razor shell objects.

## High-level delivery strategy
1. Establish common API infrastructure for authenticated workspace access.
2. Add missing DTO contracts for requested workflows.
3. Implement onboarding and context-selection endpoints.
4. Implement management-to-customer-to-property-to-unit resource APIs.
5. Implement resident resource APIs.
6. Implement lease APIs from both unit and resident workflows.
7. Add tests covering auth, tenant isolation, validation, and workflow parity.

## Proposed API controller groups

### 1. Identity and onboarding APIs
Existing baseline:
- [`WebApp/ApiControllers/Identity/AccountController.cs`](../../WebApp/ApiControllers/Identity/AccountController.cs)
- [`WebApp/Controllers/OnboardingController.cs`](../../WebApp/Controllers/OnboardingController.cs)

Planned controllers:
- `WebApp/ApiControllers/Identity/AccountController.cs`
  - keep register, login, refresh, logout aligned with frontend JWT flow
  - expand only if current endpoints do not already satisfy SPA needs
- `WebApp/ApiControllers/Onboarding/OnboardingController.cs`
  - `GET /api/v1/onboarding/contexts`
    - returns available contexts for authenticated user
    - replaces MVC cookie/context redirect logic from [`ResolveContextRedirectAsync()`](../../WebApp/Controllers/OnboardingController.cs:381)
  - `POST /api/v1/onboarding/management-companies`
    - create management company
    - mirrors [`NewManagementCompany()`](../../WebApp/Controllers/OnboardingController.cs:275)
  - optional `POST /api/v1/onboarding/context-selection`
    - only if frontend needs a canonical API payload for storing currently selected context on its side

Notes:
- No join-management-company or access-request API in this phase.
- Response after login should include enough information for frontend routing, such as available contexts, preferred default context, and principal roles.

### 2. Management customers API
MVC source:
- [`WebApp/Areas/Management/Controllers/CustomersController.cs`](../../WebApp/Areas/Management/Controllers/CustomersController.cs)

Planned controller:
- `WebApp/ApiControllers/Management/CustomersController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers`
    - list customers for management company
    - include summary links needed by frontend cards or tables
  - `POST /api/v1/management/companies/{companySlug}/customers`
    - create customer
    - mirrors [`Add()`](../../WebApp/Areas/Management/Controllers/CustomersController.cs:50)
  - list response should optionally include lightweight property summary if frontend needs parity with MVC list assembly in [`BuildPageViewModelAsync()`](../../WebApp/Areas/Management/Controllers/CustomersController.cs:160)

### 3. Customer workspace API
MVC sources:
- [`WebApp/Areas/Customer/Controllers/CustomerDashboardController.cs`](../../WebApp/Areas/Customer/Controllers/CustomerDashboardController.cs)
- [`WebApp/Areas/Customer/Controllers/CustomerProfileController.cs`](../../WebApp/Areas/Customer/Controllers/CustomerProfileController.cs)

Planned controllers:
- `WebApp/ApiControllers/Customer/DashboardController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/dashboard`
    - empty dashboard payload for current phase
    - should still validate access and return canonical customer context header/body data
- `WebApp/ApiControllers/Customer/PropertiesController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties`
    - list properties under customer
  - `POST /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties`
    - create property
    - mirrors the MVC customer-properties workflow even though that controller was not included in the first read pass
- `WebApp/ApiControllers/Customer/ProfileController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/profile`
  - `PUT /api/v1/management/companies/{companySlug}/customers/{customerSlug}/profile`
    - mirrors [`Edit()`](../../WebApp/Areas/Customer/Controllers/CustomerProfileController.cs:50)
  - `DELETE /api/v1/management/companies/{companySlug}/customers/{customerSlug}`
    - mirrors [`Delete()`](../../WebApp/Areas/Customer/Controllers/CustomerProfileController.cs:110)
    - delete confirmation string should be request-body based, not form based

### 4. Property workspace API
MVC sources:
- [`WebApp/Areas/Property/Controllers/PropertyDashboardController.cs`](../../WebApp/Areas/Property/Controllers/PropertyDashboardController.cs)
- [`WebApp/Areas/Property/Controllers/PropertyUnitsController.cs`](../../WebApp/Areas/Property/Controllers/PropertyUnitsController.cs)
- property profile MVC controller already exists and should be mirrored in API implementation

Planned controllers:
- `WebApp/ApiControllers/Property/DashboardController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/dashboard`
    - empty dashboard payload for current phase
- `WebApp/ApiControllers/Property/UnitsController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units`
    - list units
  - `POST /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units`
    - create unit
    - mirrors [`AddUnit()`](../../WebApp/Areas/Property/Controllers/PropertyUnitsController.cs:52)
- `WebApp/ApiControllers/Property/ProfileController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile`
  - `PUT /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile`
  - `DELETE /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}`

### 5. Unit workspace API
MVC sources:
- unit dashboard MVC controller already exists and should be mirrored in API implementation
- [`WebApp/Areas/Unit/Controllers/UnitProfileController.cs`](../../WebApp/Areas/Unit/Controllers/UnitProfileController.cs)
- [`WebApp/Areas/Unit/Controllers/UnitTenantsController.cs`](../../WebApp/Areas/Unit/Controllers/UnitTenantsController.cs)

Planned controllers:
- `WebApp/ApiControllers/Unit/DashboardController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/dashboard`
    - empty dashboard payload for current phase
- `WebApp/ApiControllers/Unit/ProfileController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile`
  - `PUT /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile`
    - mirrors [`Edit()`](../../WebApp/Areas/Unit/Controllers/UnitProfileController.cs:60)
  - `DELETE /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}`
    - mirrors [`Delete()`](../../WebApp/Areas/Unit/Controllers/UnitProfileController.cs:121)
- `WebApp/ApiControllers/Unit/TenantsController.cs`
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/tenants`
    - list leases for unit tenants page
    - include lease roles and optionally active resident search term echo if frontend wants a single screen bootstrap response
  - `GET /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/resident-search?searchTerm=`
    - mirrors [`SearchResidents()`](../../WebApp/Areas/Unit/Controllers/UnitTenantsController.cs:63)
  - `POST /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases`
    - mirrors [`Add()`](../../WebApp/Areas/Unit/Controllers/UnitTenantsController.cs:89)
  - `PUT /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId}`
    - mirrors [`Edit()`](../../WebApp/Areas/Unit/Controllers/UnitTenantsController.cs:143)
  - `DELETE /api/v1/management/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId}`
    - mirrors [`Delete()`](../../WebApp/Areas/Unit/Controllers/UnitTenantsController.cs:202)

### 6. Management residents API
MVC source:
- [`WebApp/Areas/Management/Controllers/ResidentsController.cs`](../../WebApp/Areas/Management/Controllers/ResidentsController.cs)

Planned controller:
- `WebApp/ApiControllers/Management/ResidentsController.cs`
  - `GET /api/v1/management/companies/{companySlug}/residents`
    - list residents
  - `POST /api/v1/management/companies/{companySlug}/residents`
    - create resident
    - mirrors [`Add()`](../../WebApp/Areas/Management/Controllers/ResidentsController.cs:45)

### 7. Resident workspace API
MVC sources:
- resident dashboard MVC controller already exists and should be mirrored in API implementation
- resident profile MVC controller already exists and should be mirrored in API implementation
- [`WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs)

Planned controllers:
- `WebApp/ApiControllers/Resident/DashboardController.cs`
  - `GET /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/dashboard`
    - empty dashboard payload for current phase
- `WebApp/ApiControllers/Resident/ProfileController.cs`
  - `GET /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/profile`
  - `PUT /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/profile`
  - `DELETE /api/v1/management/companies/{companySlug}/residents/{residentIdCode}`
- `WebApp/ApiControllers/Resident/UnitsController.cs`
  - `GET /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/units`
    - list resident leases and lookup data needed by page
  - `GET /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/property-search?searchTerm=`
    - mirrors [`SearchProperties()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:59)
  - `GET /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/properties/{propertyId}/units`
    - mirrors [`ListUnitsForProperty()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:85)
  - `POST /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/leases`
    - mirrors [`Add()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:113)
  - `PUT /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId}`
    - mirrors [`Edit()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:165)
  - `DELETE /api/v1/management/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId}`
    - mirrors [`Delete()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:222)

## DTO plan
Current DTO coverage in [`App.DTO/v1`](../../App.DTO/v1) is mostly identity-focused. This phase needs a substantial DTO expansion.

### Add DTO namespaces
- `App.DTO/v1/Onboarding/*`
- `App.DTO/v1/Management/Customers/*`
- `App.DTO/v1/Management/Residents/*`
- `App.DTO/v1/Customer/*`
- `App.DTO/v1/Property/*`
- `App.DTO/v1/Unit/*`
- `App.DTO/v1/Resident/*`
- `App.DTO/v1/Shared/*`

### Common DTO patterns
- `SummaryDto`
  - lightweight objects for cards, tables, lists, and breadcrumbs
- `DetailDto`
  - profile and page bootstrap payloads
- `CreateRequestDto`
- `UpdateRequestDto`
- `DeleteRequestDto`
  - only where delete confirmation text is required
- `LookupOptionDto`
  - for lease roles, selectable units, selectable contexts
- `PagedListDto<T>` only if pagination is introduced now

### DTOs needed immediately
- onboarding context DTOs
  - available management companies
  - available customer or resident contexts if login can land into them
- management customer list and create DTOs
- customer property list and create DTOs
- customer profile detail and update DTOs
- property dashboard DTO
- property unit list and create DTOs
- property profile detail and update DTOs
- unit dashboard DTO
- unit profile detail and update DTOs
- unit tenants page bootstrap DTO
- unit resident-search result DTO
- unit lease create and update DTOs
- management resident list and create DTOs
- resident dashboard DTO
- resident profile detail and update DTOs
- resident units page bootstrap DTO
- resident property-search result DTO
- resident property-unit option DTO
- resident lease create and update DTOs

## API response shape strategy
To keep the frontend easy to build, each page-oriented frontend route should have one primary bootstrap endpoint that returns all data needed for first render.

Examples:
- unit tenants page endpoint should return:
  - unit context summary
  - current leases
  - lease role options
  - maybe empty resident search results initially
- resident units page endpoint should return:
  - resident context summary
  - current leases
  - lease role options
  - maybe empty property search results and unit options initially
- empty dashboard endpoints should still return:
  - route context
  - title or section identifier
  - empty widgets array or reserved dashboard object for future growth

This avoids forcing the frontend to reconstruct a page from many small calls unless search and dependent dropdown workflows actually require them.

## Authorization and tenant isolation rules
Every API endpoint in this plan must preserve the same authorization structure already present in MVC.

### Mandatory rules
- Never query tenant-scoped resources by ID or slug alone without prior company and parent-scope authorization.
- Reuse existing access chains from MVC controllers:
  - customer context via [`ResolveDashboardAccessAsync()`](../../WebApp/Areas/Customer/Controllers/CustomerDashboardController.cs:73)
  - property context via [`ResolvePropertyDashboardContextAsync()`](../../WebApp/Areas/Property/Controllers/PropertyDashboardController.cs:90)
  - unit context via [`ResolveUnitDashboardContextAsync()`](../../WebApp/Areas/Unit/Controllers/UnitProfileController.cs:282)
  - resident context via [`ResolveDashboardAccessAsync()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:265)
- For writes, validate parent-child relationships explicitly:
  - property belongs to customer
  - unit belongs to property
  - lease unit belongs to authorized property path
  - resident belongs to authorized management company
- Return `404` when parent resource does not exist inside the scoped chain.
- Return `403` when resource exists but actor is not allowed.
- Do not leak cross-tenant existence details.

### Likely shared helper need
Plan a shared API base or helper layer for route-to-context resolution so all new API controllers do not reimplement the same multi-step authorization code from MVC.

Possible examples:
- `ApiControllerBase.GetAppUserId()`
- `IManagementApiRouteContextResolver`
- helper methods returning typed result objects for customer, property, unit, resident route chains

## Validation and error-handling plan
MVC currently mixes `ModelState`, TempData, and returning views. The API layer should normalize this.

### API behavior
- Input validation failures return `400` with structured field errors.
- Authentication failures return `401`.
- Authorization failures return `403`.
- Missing scoped resources return `404`.
- Business-rule conflicts or duplicates should usually return `400`, unless a specific case clearly fits `409`.

### Error mapping examples from MVC
- duplicate customer registry code from [`CustomersController.Add()`](../../WebApp/Areas/Management/Controllers/CustomersController.cs:102)
- duplicate resident ID code from [`ResidentsController.Add()`](../../WebApp/Areas/Management/Controllers/ResidentsController.cs:86)
- invalid unit number, floor, size from [`PropertyUnitsController.AddUnit()`](../../WebApp/Areas/Property/Controllers/PropertyUnitsController.cs:84)
- lease validation from [`ApplyCreateErrors()`](../../WebApp/Areas/Unit/Controllers/UnitTenantsController.cs:435) and [`ApplyCreateErrors()`](../../WebApp/Areas/Resident/Controllers/ResidentUnitsController.cs:437)
- delete confirmation mismatch from [`CustomerProfileController.Delete()`](../../WebApp/Areas/Customer/Controllers/CustomerProfileController.cs:130) and [`UnitProfileController.Delete()`](../../WebApp/Areas/Unit/Controllers/UnitProfileController.cs:143)

### Required output format
Use or extend [`App.DTO/v1/RestApiErrorResponse.cs`](../../App.DTO/v1/RestApiErrorResponse.cs) so it can represent:
- general error message
- error code
- field-level validation dictionary
- optional trace or correlation identifier if already standardized in project

## Frontend-routing and context model
Because the frontend app should look and behave like the MVC app, the API should preserve the same hierarchical route semantics in returned data.

Recommended route context object shape in most detail responses:
- companySlug
- companyName
- customerSlug and customerName when applicable
- propertySlug and propertyName when applicable
- unitSlug and unitName when applicable
- residentIdCode and residentDisplayName when applicable
- currentSection

This gives the frontend enough information to rebuild breadcrumbs, side navigation, page titles, and links without depending on MVC page-shell objects.

## Suggested implementation order

### Phase 1: infrastructure and contracts
- review existing [`WebApp/ApiControllers/Identity/AccountController.cs`](../../WebApp/ApiControllers/Identity/AccountController.cs) for reuse and gaps
- define new DTO folders in [`App.DTO/v1`](../../App.DTO/v1)
- add shared API error format extensions if needed
- add shared route-context resolution helpers in BLL or WebApp API layer

### Phase 2: onboarding and context APIs
- add onboarding contexts endpoint
- add create-management-company endpoint
- ensure login response plus contexts gives frontend enough routing information

### Phase 3: management resource creation chain
- add management customers list and create API
- add customer properties list and create API
- add management residents list and create API
- add property units list and create API

### Phase 4: profile APIs
- add customer profile get, update, delete
- add property profile get, update, delete
- add unit profile get, update, delete
- add resident profile get, update, delete

### Phase 5: dashboard APIs
- add empty dashboard APIs for customer, property, unit, resident
- keep payloads intentionally minimal but stable

### Phase 6: lease workflow APIs
- add unit tenants bootstrap, resident search, add, edit, delete
- add resident units bootstrap, property search, list units for property, add, edit, delete

### Phase 7: tests and documentation
- add controller or integration tests for JWT auth and route-scope authorization
- add tests for duplicate and validation rules
- add tests for delete confirmation mismatch
- add tests for resident and unit lease workflows
- document endpoint map in Swagger and keep versioning consistent

## Testing plan
Minimum coverage for this API work should include:
- register and login success and validation failure
- create management company under authenticated user
- list and create customer only within authorized management company
- list and create property only within authorized customer
- list and create unit only within authorized property
- create resident only within authorized management company
- profile update and delete authorization checks for customer, property, unit, resident
- lease create, edit, delete from unit-scoped route
- lease create, edit, delete from resident-scoped route
- search endpoints only returning authorized tenant data
- `404` versus `403` behavior for cross-tenant or nonexistent route chains

## Known follow-up items outside immediate scope
- access-request or join-management-company APIs
- customer tickets, property tickets, resident self-service ticket flows
- pagination, filtering, and sorting standardization
- frontend-specific persisted current-context endpoint if local storage alone proves insufficient
- richer dashboard widgets once backend analytics are implemented

## Deliverables expected from implementation mode
- new or expanded API controllers under [`WebApp/ApiControllers`](../../WebApp/ApiControllers)
- new DTO contracts under [`App.DTO/v1`](../../App.DTO/v1)
- any required API support services for route-context resolution
- tests proving tenant isolation, RBAC behavior, and workflow parity
- Swagger-visible versioned endpoints for the requested frontend workflows

## Recommended next execution checklist
- inspect the existing API auth controller and current Swagger setup in detail
- inspect MVC controllers not yet read for customer-properties, property-profile, resident-profile, and unit-dashboard parity
- enumerate exact DTO types before coding
- implement shared API route-context resolver before adding endpoint bodies
- add endpoints in the phase order above
- add tests before widening scope beyond the requested workflows

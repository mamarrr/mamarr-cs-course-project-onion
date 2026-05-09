# API Controllers Implementation Plan for Vue Frontend

## Goal

Implement a Vue-ready REST API over the currently implemented onboarding and Portal workflows by adding new API controllers under:

```text
WebApp/ApiControllers
```

and public API DTOs under:

```text
App.DTO/v1
```

API mapper implementations should live under `App.DTO/Mappers` and map between public `App.DTO.v1.*` API DTOs and BLL DTOs/models. Use `IBaseMapper<ApiDto, BllDto>` wherever the mapping is naturally two-way, especially for command/save DTOs such as `TicketBllDto`, `LeaseBllDto`, `CustomerBllDto`, `PropertyBllDto`, `UnitBllDto`, `ResidentBllDto`, `VendorBllDto`, `ScheduledWorkBllDto`, and `WorkLogBllDto`.

Do not implement API endpoints for placeholder/shell-only MVC workflows such as Resident Access onboarding, customer-scoped residents, property-scoped residents, resident representations, and disabled management-level properties. Customer, property, unit, and resident ticket list workflows are implemented and must be exposed as scoped ticket APIs.

API controllers must call only public services exposed by `IAppBLL`. They must never expose DAL DTOs, BLL DTOs/models, domain entities, MVC ViewModels, `ViewBag`, or `ViewData`.

---

## Existing Project Constraints

### Existing API infrastructure

`Program.cs` already contains the required foundation for a SPA API:

- JWT bearer authentication.
- Cookie authentication for MVC.
- CORS.
- Swagger.
- API versioning with URL segment substitution.
- `App.DTO` referenced from `WebApp`.

Use this existing infrastructure instead of creating a parallel startup configuration.

### Existing mapper contract

`Base.Contracts.IBaseMapper<TEntityOut, TEntityIn>` is:

```csharp
public interface IBaseMapper<TEntityOut, TEntityIn>
    where TEntityOut : class
    where TEntityIn : class
{
    TEntityOut? Map(TEntityIn? entity);
    TEntityIn? Map(TEntityOut? entity);
}
```

For API work, use it like:

```csharp
public sealed class TicketApiMapper : IBaseMapper<TicketDto, TicketBllDto>
{
    public TicketDto? Map(TicketBllDto? entity) { ... }
    public TicketBllDto? Map(TicketDto? entity) { ... }
}
```

### App.DTO mapper reference

`App.DTO` currently has no project references. Because API mappers live in `App.DTO/Mappers` and must map to BLL DTOs/models, add a reference from `App.DTO` to `App.BLL.Contracts`.

```xml
<ProjectReference Include="..\App.BLL.Contracts\App.BLL.Contracts.csproj" />
```

Required placement:

- public DTO contracts in `App.DTO/v1` using `App.DTO.v1.*` namespaces
- API mapper implementations in `App.DTO/Mappers`
- no API mapper implementations in `WebApp/Mappers/Api`

Suggested folders:

```text
App.DTO/v1/
  Identity/
  Onboarding/
  Workspace/
  Shared/
  Portal/
    Companies/
    Dashboards/
    Customers/
    Properties/
    Units/
    Residents/
    Leases/
    Tickets/
    ScheduledWork/
    WorkLogs/
    Vendors/
    Contacts/
    Users/
    Lookups/

App.DTO/Mappers/
  Auth/
  Onboarding/
  Workspace/
  Portal/
    Companies/
    Dashboards/
    Customers/
    Properties/
    Units/
    Residents/
    Leases/
    Tickets/
    ScheduledWork/
    WorkLogs/
    Vendors/
    Contacts/
    Users/
    Lookups/
```

The rest of this plan assumes versioned public DTOs in `App.DTO/v1` and mappers in `App.DTO/Mappers`.

---

## API Route Convention

Use versioned REST routes:

```text
/api/v1/auth
/api/v1/onboarding
/api/v1/workspaces
/api/v1/portal/companies/{companySlug}/...
```

Use resource-oriented controller names. Do not mirror Razor MVC page controllers 1:1 unless the MVC controller already maps cleanly to a resource.

### Base controller conventions

Create a common API base controller:

```text
WebApp/ApiControllers/ApiControllerBase.cs
```

Responsibilities:

- Apply `[ApiController]`.
- Apply `[Route("api/v{version:apiVersion}/[controller]")]` only where suitable, or let each concrete controller define explicit routes.
- Provide helper methods:
  - `Guid? GetAppUserId()`
  - `ActionResult ToApiError(IReadOnlyList<IError> errors)`
  - `RestApiErrorResponse ToValidationError(...)`
  - `ActionResult<T> FromResult<T>(Result<T> result, Func<T, object> mapper)`
- Map BLL errors consistently:
  - `UnauthorizedError` -> `401 Unauthorized`
  - `ForbiddenError` -> `403 Forbidden`
  - `NotFoundError` -> `404 Not Found`
  - `ConflictError` -> `409 Conflict`
  - `ValidationAppError` -> `400 ValidationProblem`
  - fallback -> `400 BadRequest`

### Authorization convention

For API controllers except login/register/refresh:

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
```

For auth endpoints:

```csharp
[AllowAnonymous]
```

---

## Cross-Cutting DTOs

Create these first.

### `App.DTO/v1/Shared` and root v1 errors

```text
App.DTO/v1/RestApiErrorResponse.cs
App.DTO/v1/Shared/ApiErrorCodes.cs
App.DTO/v1/Shared/LookupOptionDto.cs
App.DTO/v1/Common/PagedResultDto.cs
App.DTO/v1/Common/CommandResultDto.cs
```

Reuse existing DTOs where they already define the public shape:

```csharp
// App.DTO.v1.RestApiErrorResponse
public class RestApiErrorResponse
{
    public System.Net.HttpStatusCode Status { get; set; }
    public string Error { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public Dictionary<string, string[]> Errors { get; set; } = new();
    public string? TraceId { get; set; }
}

// App.DTO.v1.Shared.LookupOptionDto
public class LookupOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed record CommandResultDto(
    bool Success,
    string? Message = null);
```

Use `RestApiErrorResponse` as the canonical structured error response and `ApiErrorCodes` for stable error codes. Keep `LookupOptionDto` unless a different public shape is required. Prefer class DTOs for consistency with current `App.DTO/v1`.

---

## Phase 1 — Authentication API

### Controller

```text
WebApp/ApiControllers/Auth/AuthController.cs
```

Routes:

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
POST /api/v1/auth/refresh
POST /api/v1/auth/logout
GET  /api/v1/auth/me
```

### DTOs

```text
App.DTO/v1/Identity/RegisterInfo.cs
App.DTO/v1/Identity/LoginInfo.cs
App.DTO/v1/Identity/TokenRefreshInfo.cs
App.DTO/v1/Identity/LogoutInfo.cs
App.DTO/v1/Identity/JWTResponse.cs
App.DTO/v1/Identity/UserDto.cs
```

Reuse existing identity DTOs before adding new request/response shapes. Add only the missing `UserDto` or related response details needed by the SPA contract.

### Implementation tasks

1. Reuse `IIdentityAccountService.CreateUserAsync` for registration.
2. Reuse `IIdentityAccountService.PasswordSignInAsync` for login.
3. Add or implement token issuing service:

```text
WebApp/Services/Identity/IJwtTokenService.cs
WebApp/Services/Identity/JwtTokenService.cs
```

4. Add refresh token service:

```text
WebApp/Services/Identity/IRefreshTokenService.cs
WebApp/Services/Identity/RefreshTokenService.cs
```

5. Use existing `AppRefreshToken` domain entity for refresh-token persistence and rotation.
6. Login response should return:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresAt": "2026-05-08T12:00:00Z",
  "user": {
    "id": "...",
    "email": "...",
    "firstName": "...",
    "lastName": "...",
    "roles": ["..."]
  }
}
```

### Notes

- Access token lifetime should be short, for example 15 minutes.
- Refresh token lifetime should match the domain default or config, currently conceptually 7 days.
- Store refresh tokens hashed if possible; if keeping current plain-token model for course scope, isolate the token-generation logic so it can be upgraded later.
- `logout` should revoke/delete the current refresh token.
- `refresh` should rotate the refresh token and preserve the previous-token window only if the existing domain design requires it.

---

## Phase 2 — Onboarding API

Only implement actual onboarding flows.

Do not implement resident-access API yet because the MVC version is a placeholder page only.

### Controller

```text
WebApp/ApiControllers/Onboarding/OnboardingController.cs
```

Routes:

```text
GET  /api/v1/onboarding/status
POST /api/v1/onboarding/management-companies
GET  /api/v1/onboarding/management-company-roles
POST /api/v1/onboarding/management-company-join-requests
```

### DTOs

```text
App.DTO/v1/Onboarding/OnboardingStatusDto.cs
App.DTO/v1/Onboarding/CreateManagementCompanyDto.cs
App.DTO/v1/Onboarding/CreatedManagementCompanyDto.cs
App.DTO/v1/Onboarding/JoinManagementCompanyRequestDto.cs
App.DTO/v1/Onboarding/JoinRequestResultDto.cs
```

### Mappers

```text
App.DTO/Mappers/Onboarding/ManagementCompanyApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateManagementCompanyDto, ManagementCompanyBllDto>
```

### Implementation tasks

1. `GET /status`
   - Use workspace redirect service to determine whether the user can go directly to a workspace.
   - Return available onboarding actions:
     - `createManagementCompany`
     - `joinManagementCompany`
2. `POST /management-companies`
   - Map `CreateManagementCompanyDto` -> `ManagementCompanyBllDto`.
   - Call `_bll.ManagementCompanies.CreateAsync(...)`.
   - Return created id, slug, name, and frontend path such as `/companies/{slug}`.
3. `GET /management-company-roles`
   - Use `_bll.CompanyMemberships.GetAvailableRolesAsync(...)`.
   - Return `LookupOptionDto[]` or a more specific role option DTO if extra metadata is needed.
4. `POST /management-company-join-requests`
   - Use `CreateCompanyJoinRequestCommand`.
   - Return success message and request id if available.

---

## Phase 3 — Workspace API

### Controller

```text
WebApp/ApiControllers/Workspace/WorkspacesController.cs
```

Routes:

```text
GET  /api/v1/workspaces
GET  /api/v1/workspaces/default-redirect
POST /api/v1/workspaces/select
```

### DTOs

```text
App.DTO/v1/Workspace/WorkspaceCatalogDto.cs
App.DTO/v1/Workspace/WorkspaceOptionDto.cs
App.DTO/v1/Workspace/WorkspaceRedirectDto.cs
App.DTO/v1/Workspace/SelectWorkspaceDto.cs
```

### Mappers

```text
App.DTO/Mappers/Workspace/WorkspaceCatalogApiMapper.cs
App.DTO/Mappers/Workspace/WorkspaceOptionApiMapper.cs
```

### Implementation tasks

1. `GET /workspaces`
   - Use `_bll.Workspaces.GetCatalogAsync(...)`.
   - Return management companies, customer contexts, resident context, default context, and permission flags.
2. `GET /default-redirect`
   - Use `_bll.Workspaces.ResolveContextRedirectAsync(...)`.
   - Return a frontend route path, not an MVC redirect.
3. `POST /select`
   - Use `_bll.Workspaces.AuthorizeContextSelectionAsync(...)`.
   - Return selected context metadata and frontend route target.

### Important

Do not depend on MVC `ctx.*` cookies for Vue. The SPA should store selected workspace state client-side or derive it from the route and `/workspaces`.

---

## Phase 4 — Portal Dashboards API

Expose the implemented Portal dashboard workflows for every supported workspace level. These endpoints are read-only and must call `_bll.PortalDashboards`; do not reuse MVC dashboard ViewModels or `WebApp.Mappers.Dashboards`.

### Controller

```text
WebApp/ApiControllers/Portal/DashboardsController.cs
```

Routes:

```text
GET /api/v1/portal/companies/{companySlug}/dashboard
GET /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/dashboard
GET /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/dashboard
GET /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/dashboard
GET /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/dashboard
```

### DTOs

```text
App.DTO/v1/Portal/Dashboards/ManagementDashboardDto.cs
App.DTO/v1/Portal/Dashboards/CustomerDashboardDto.cs
App.DTO/v1/Portal/Dashboards/PropertyDashboardDto.cs
App.DTO/v1/Portal/Dashboards/UnitDashboardDto.cs
App.DTO/v1/Portal/Dashboards/ResidentDashboardDto.cs
App.DTO/v1/Portal/Dashboards/DashboardContextDto.cs
App.DTO/v1/Portal/Dashboards/DashboardMetricDto.cs
App.DTO/v1/Portal/Dashboards/DashboardBreakdownItemDto.cs
App.DTO/v1/Portal/Dashboards/DashboardTicketPreviewDto.cs
App.DTO/v1/Portal/Dashboards/DashboardWorkPreviewDto.cs
App.DTO/v1/Portal/Dashboards/DashboardRecentActivityDto.cs
App.DTO/v1/Portal/Dashboards/DashboardLeasePreviewDto.cs
App.DTO/v1/Portal/Dashboards/DashboardContactSummaryDto.cs
App.DTO/v1/Portal/Dashboards/DashboardRepresentativePreviewDto.cs
App.DTO/v1/Portal/Dashboards/DashboardUnitPreviewDto.cs
App.DTO/v1/Portal/Dashboards/DashboardTimelineItemDto.cs
```

Use context-specific dashboard DTOs instead of one over-wide response object. Shared nested DTOs are fine for metrics, breakdowns, previews, activity, leases, contacts, and timeline rows.

### Mappers

```text
App.DTO/Mappers/Portal/Dashboards/PortalDashboardApiMapper.cs
```

Map from:

```text
App.BLL.DTO.Dashboards.Models.ManagementDashboardModel
App.BLL.DTO.Dashboards.Models.CustomerDashboardModel
App.BLL.DTO.Dashboards.Models.PropertyDashboardModel
App.BLL.DTO.Dashboards.Models.UnitDashboardModel
App.BLL.DTO.Dashboards.Models.ResidentDashboardModel
```

### Implementation tasks

1. Management dashboard:
   - Build `ManagementCompanyRoute`.
   - Call `_bll.PortalDashboards.GetManagementDashboardAsync(...)`.
2. Customer dashboard:
   - Build `CustomerRoute`.
   - Call `_bll.PortalDashboards.GetCustomerDashboardAsync(...)`.
3. Property dashboard:
   - Build `PropertyRoute`.
   - Call `_bll.PortalDashboards.GetPropertyDashboardAsync(...)`.
4. Unit dashboard:
   - Build `UnitRoute`.
   - Call `_bll.PortalDashboards.GetUnitDashboardAsync(...)`.
5. Resident dashboard:
   - Build `ResidentRoute`.
   - Call `_bll.PortalDashboards.GetResidentDashboardAsync(...)`.
6. Map BLL dashboard models into public DTOs only. Do not include MVC chrome, navigation state, current section labels, or `AppChromeViewModel`.

---

## Phase 5 — Management Company API

### Controller

```text
WebApp/ApiControllers/Portal/ManagementCompaniesController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/summary
GET    /api/v1/portal/companies/{companySlug}/profile
PUT    /api/v1/portal/companies/{companySlug}/profile
DELETE /api/v1/portal/companies/{companySlug}/profile
```

### DTOs

```text
App.DTO/v1/Portal/Companies/ManagementCompanySummaryDto.cs
App.DTO/v1/Portal/Companies/ManagementCompanyProfileDto.cs
App.DTO/v1/Portal/Companies/UpdateManagementCompanyProfileDto.cs
App.DTO/v1/Portal/Companies/DeleteManagementCompanyDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Companies/ManagementCompanyProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<UpdateManagementCompanyProfileDto, ManagementCompanyBllDto>
```

### Implementation tasks

1. `GET /summary`
   - Return lightweight workspace data for layout/header.
   - Do not add this unless there is real summary data to return.
2. `GET /profile`
   - Use `_bll.ManagementCompanies.GetProfileAsync(...)`.
3. `PUT /profile`
   - Map API DTO -> `ManagementCompanyBllDto`.
   - Use `_bll.ManagementCompanies.UpdateAsync(...)`.
4. `DELETE /profile`
   - Require confirmation value.
   - Use `_bll.ManagementCompanies.DeleteAsync(...)`.

---

## Phase 6 — Customers API

### Controller

```text
WebApp/ApiControllers/Portal/CustomersController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/customers
POST   /api/v1/portal/companies/{companySlug}/customers

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/profile
PUT    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/profile
DELETE /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/profile

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties
POST   /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties
```

### DTOs

```text
App.DTO/v1/Portal/Customers/CustomerListItemDto.cs
App.DTO/v1/Portal/Customers/CreateCustomerDto.cs
App.DTO/v1/Portal/Customers/CustomerProfileDto.cs
App.DTO/v1/Portal/Customers/UpdateCustomerProfileDto.cs
App.DTO/v1/Portal/Customers/DeleteCustomerDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Customers/CustomerApiMapper.cs
App.DTO/Mappers/Portal/Customers/CustomerListItemApiMapper.cs
App.DTO/Mappers/Portal/Customers/CustomerProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateCustomerDto, CustomerBllDto>
IBaseMapper<UpdateCustomerProfileDto, CustomerBllDto>
```

### Implementation tasks

1. List customers:
   - Use `_bll.Customers.ListForCompanyAsync(...)`.
2. Create customer:
   - Use `_bll.Customers.CreateAndGetProfileAsync(...)`.
   - Return `201 Created` with profile.
3. Customer profile:
   - Use `_bll.Customers.GetProfileAsync(...)`.
4. Update profile:
   - Use `_bll.Customers.UpdateAndGetProfileAsync(...)`.
5. Delete:
   - Use `_bll.Customers.DeleteAsync(...)`.

---

## Phase 7 — Properties API

### Controller

```text
WebApp/ApiControllers/Portal/PropertiesController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties
POST   /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile
PUT    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile
DELETE /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/profile

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units
POST   /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units
```

### DTOs

```text
App.DTO/v1/Portal/Properties/PropertyListItemDto.cs
App.DTO/v1/Portal/Properties/CreatePropertyDto.cs
App.DTO/v1/Portal/Properties/PropertyProfileDto.cs
App.DTO/v1/Portal/Properties/UpdatePropertyProfileDto.cs
App.DTO/v1/Portal/Properties/DeletePropertyDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Properties/PropertyApiMapper.cs
App.DTO/Mappers/Portal/Properties/PropertyListItemApiMapper.cs
App.DTO/Mappers/Portal/Properties/PropertyProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreatePropertyDto, PropertyBllDto>
IBaseMapper<UpdatePropertyProfileDto, PropertyBllDto>
```

### Implementation tasks

1. List properties for customer.
2. Create property.
3. Get property profile.
4. Update property profile.
5. Delete property.

---

## Phase 8 — Units API

### Controller

```text
WebApp/ApiControllers/Portal/UnitsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units
POST   /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile
PUT    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile
DELETE /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/profile
```

### DTOs

```text
App.DTO/v1/Portal/Units/UnitListItemDto.cs
App.DTO/v1/Portal/Units/CreateUnitDto.cs
App.DTO/v1/Portal/Units/UnitProfileDto.cs
App.DTO/v1/Portal/Units/UpdateUnitProfileDto.cs
App.DTO/v1/Portal/Units/DeleteUnitDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Units/UnitApiMapper.cs
App.DTO/Mappers/Portal/Units/UnitListItemApiMapper.cs
App.DTO/Mappers/Portal/Units/UnitProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateUnitDto, UnitBllDto>
IBaseMapper<UpdateUnitProfileDto, UnitBllDto>
```

---

## Phase 9 — Residents API

### Controller

```text
WebApp/ApiControllers/Portal/ResidentsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/residents
POST   /api/v1/portal/companies/{companySlug}/residents

GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/profile
PUT    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/profile
DELETE /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/profile
```

### DTOs

```text
App.DTO/v1/Portal/Residents/ResidentListItemDto.cs
App.DTO/v1/Portal/Residents/CreateResidentDto.cs
App.DTO/v1/Portal/Residents/ResidentProfileDto.cs
App.DTO/v1/Portal/Residents/UpdateResidentProfileDto.cs
App.DTO/v1/Portal/Residents/DeleteResidentDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Residents/ResidentApiMapper.cs
App.DTO/Mappers/Portal/Residents/ResidentListItemApiMapper.cs
App.DTO/Mappers/Portal/Residents/ResidentProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateResidentDto, ResidentBllDto>
IBaseMapper<UpdateResidentProfileDto, ResidentBllDto>
```

---

## Phase 10 — Leases API

Leases should be one API area even though MVC exposes them through unit tenants and resident units.

### Controller

```text
WebApp/ApiControllers/Portal/LeasesController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases
POST   /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases
PUT    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId}
DELETE /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId}

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/resident-search?searchTerm=

GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases
PUT    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId}
DELETE /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId}

GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/property-search?searchTerm=
GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/properties/{propertyId}/units
```

### DTOs

```text
App.DTO/v1/Portal/Leases/UnitLeaseListItemDto.cs
App.DTO/v1/Portal/Leases/ResidentLeaseListItemDto.cs
App.DTO/v1/Portal/Leases/CreateUnitLeaseDto.cs
App.DTO/v1/Portal/Leases/UpdateUnitLeaseDto.cs
App.DTO/v1/Portal/Leases/CreateResidentLeaseDto.cs
App.DTO/v1/Portal/Leases/UpdateResidentLeaseDto.cs
App.DTO/v1/Portal/Leases/LeaseResidentSearchResultDto.cs
App.DTO/v1/Portal/Leases/LeasePropertySearchResultDto.cs
App.DTO/v1/Portal/Leases/LeaseUnitOptionDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Leases/LeaseApiMapper.cs
App.DTO/Mappers/Portal/Leases/UnitLeaseListItemApiMapper.cs
App.DTO/Mappers/Portal/Leases/ResidentLeaseListItemApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>
IBaseMapper<UpdateUnitLeaseDto, LeaseBllDto>
IBaseMapper<CreateResidentLeaseDto, LeaseBllDto>
IBaseMapper<UpdateResidentLeaseDto, LeaseBllDto>
```

### Implementation tasks

1. Reuse `LeaseBllDto` for create/update commands.
2. Unit-side actions:
   - `ListForUnitAsync`
   - `SearchResidentsAsync`
   - `CreateForUnitAsync`
   - `UpdateFromUnitAsync`
   - `DeleteFromUnitAsync`
3. Resident-side actions:
   - `ListForResidentAsync`
   - `SearchPropertiesAsync`
   - `ListUnitsForPropertyAsync`
   - `CreateForResidentAsync`
   - `UpdateFromResidentAsync`
   - `DeleteFromResidentAsync`

---

## Phase 11 — Contacts API

Implement resident and vendor contact assignment workflows. Prefer one controller with two resource groups.

### Controller

```text
WebApp/ApiControllers/Portal/ContactsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/attach
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts
PUT    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId}
DELETE /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId}
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId}/set-primary
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId}/confirm
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId}/unconfirm

GET    /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts/attach
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts
PUT    /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts/{vendorContactId}
DELETE /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts/{vendorContactId}
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts/{vendorContactId}/set-primary
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts/{vendorContactId}/confirm
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/contacts/{vendorContactId}/unconfirm
```

### DTOs

```text
App.DTO/v1/Portal/Contacts/ContactDto.cs
App.DTO/v1/Portal/Contacts/CreateContactDto.cs
App.DTO/v1/Portal/Contacts/ResidentContactAssignmentDto.cs
App.DTO/v1/Portal/Contacts/AttachResidentContactDto.cs
App.DTO/v1/Portal/Contacts/CreateResidentContactDto.cs
App.DTO/v1/Portal/Contacts/UpdateResidentContactDto.cs
App.DTO/v1/Portal/Contacts/VendorContactAssignmentDto.cs
App.DTO/v1/Portal/Contacts/AttachVendorContactDto.cs
App.DTO/v1/Portal/Contacts/CreateVendorContactDto.cs
App.DTO/v1/Portal/Contacts/UpdateVendorContactDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Contacts/ContactApiMapper.cs
App.DTO/Mappers/Portal/Contacts/ResidentContactApiMapper.cs
App.DTO/Mappers/Portal/Contacts/VendorContactApiMapper.cs
```

Use where possible:

```csharp
IBaseMapper<CreateContactDto, ContactBllDto>
IBaseMapper<AttachResidentContactDto, ResidentContactBllDto>
IBaseMapper<UpdateResidentContactDto, ResidentContactBllDto>
IBaseMapper<AttachVendorContactDto, VendorContactBllDto>
IBaseMapper<UpdateVendorContactDto, VendorContactBllDto>
```

### Implementation tasks

Resident contacts must call `_bll.Residents` directly:

1. List: `_bll.Residents.ListContactsAsync(...)`.
2. Attach or create: `_bll.Residents.AddContactAsync(...)`.
3. Update assignment: `_bll.Residents.UpdateContactAsync(...)`.
4. Primary, confirmation, and removal actions:
   - `_bll.Residents.SetPrimaryContactAsync(...)`
   - `_bll.Residents.ConfirmContactAsync(...)`
   - `_bll.Residents.UnconfirmContactAsync(...)`
   - `_bll.Residents.RemoveContactAsync(...)`

Vendor contacts must call `_bll.Vendors` directly:

1. List: `_bll.Vendors.ListContactsAsync(...)`.
2. Attach or create: `_bll.Vendors.AddContactAsync(...)`.
3. Update assignment: `_bll.Vendors.UpdateContactAsync(...)`.
4. Primary, confirmation, and removal actions:
   - `_bll.Vendors.SetPrimaryContactAsync(...)`
   - `_bll.Vendors.ConfirmContactAsync(...)`
   - `_bll.Vendors.UnconfirmContactAsync(...)`
   - `_bll.Vendors.RemoveContactAsync(...)`

Do not plan `_bll.Contacts` usage unless `IAppBLL` is intentionally expanded later.

---

## Phase 12 — Tickets API

Keep `TicketsController` limited to ticket workflows. Do not put scheduled-work or work-log mutations in this controller.

### Controller

```text
WebApp/ApiControllers/Portal/TicketsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/tickets
POST   /api/v1/portal/companies/{companySlug}/tickets
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId}
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/advance-status

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/tickets
GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/tickets
GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/tickets
GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/tickets

GET    /api/v1/portal/companies/{companySlug}/tickets/options
GET    /api/v1/portal/companies/{companySlug}/tickets/options/properties?customerId=
GET    /api/v1/portal/companies/{companySlug}/tickets/options/units?propertyId=
GET    /api/v1/portal/companies/{companySlug}/tickets/options/residents?unitId=
GET    /api/v1/portal/companies/{companySlug}/tickets/options/vendors?categoryId=
```

### DTOs

```text
App.DTO/v1/Portal/Tickets/TicketFilterDto.cs
App.DTO/v1/Portal/Tickets/TicketListDto.cs
App.DTO/v1/Portal/Tickets/ContextTicketListDto.cs
App.DTO/v1/Portal/Tickets/TicketListItemDto.cs
App.DTO/v1/Portal/Tickets/TicketDetailsDto.cs
App.DTO/v1/Portal/Tickets/TicketScheduledWorkSummaryDto.cs
App.DTO/v1/Portal/Tickets/TicketDto.cs
App.DTO/v1/Portal/Tickets/CreateTicketDto.cs
App.DTO/v1/Portal/Tickets/UpdateTicketDto.cs
App.DTO/v1/Portal/Tickets/TicketOptionsDto.cs
App.DTO/v1/Shared/LookupOptionDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Tickets/TicketApiMapper.cs
App.DTO/Mappers/Portal/Tickets/TicketListApiMapper.cs
App.DTO/Mappers/Portal/Tickets/ContextTicketListApiMapper.cs
App.DTO/Mappers/Portal/Tickets/TicketListItemApiMapper.cs
App.DTO/Mappers/Portal/Tickets/TicketDetailsApiMapper.cs
App.DTO/Mappers/Portal/Tickets/TicketOptionsApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateTicketDto, TicketBllDto>
IBaseMapper<UpdateTicketDto, TicketBllDto>
```

### Implementation tasks

1. Search/list:
   - Map query parameters into `ManagementTicketSearchRoute`.
   - Call `_bll.Tickets.SearchAsync(...)`.
2. Context search/list:
   - Map query parameters into `ContextTicketSearchRoute`.
   - For customer route, set `CustomerSlug` and call `_bll.Tickets.SearchCustomerTicketsAsync(...)`.
   - For property route, set `CustomerSlug` and `PropertySlug` and call `_bll.Tickets.SearchPropertyTicketsAsync(...)`.
   - For unit route, set `CustomerSlug`, `PropertySlug`, and `UnitSlug` and call `_bll.Tickets.SearchUnitTicketsAsync(...)`.
   - For resident route, set `ResidentIdCode` and call `_bll.Tickets.SearchResidentTicketsAsync(...)`.
   - Return a context list response with context metadata, filter state, selector options, and ticket rows.
   - Preserve the MVC filter behavior: customer lists hide the customer filter, property lists hide customer and property filters, unit and resident lists hide customer/property/unit filters.
3. Create:
   - Map `CreateTicketDto` -> `TicketBllDto`.
   - Call `_bll.Tickets.CreateAsync(ManagementCompanyRoute, dto, ...)`.
   - Return `201 Created`.
4. Details:
   - Call `_bll.Tickets.GetDetailsAsync(TicketRoute, ...)`.
   - Map `ManagementTicketDetailsModel` -> `TicketDetailsDto`.
   - Include scheduled work summary items from `ManagementTicketDetailsModel.ScheduledWork`.
5. Update:
   - Map `UpdateTicketDto` -> `TicketBllDto`.
   - Call `_bll.Tickets.UpdateAsync(TicketRoute, dto, ...)`.
6. Delete:
   - Call `_bll.Tickets.DeleteAsync(TicketRoute, ...)`.
7. Advance status:
   - Call `_bll.Tickets.AdvanceStatusAsync(TicketRoute, ...)`.
8. Selector options:
   - Call `_bll.Tickets.GetSelectorOptionsAsync(...)`.

---

## Phase 13 — Scheduled Work API

`ScheduledWorkController` must call `_bll.ScheduledWorks` directly.

### Controller

```text
WebApp/ApiControllers/Portal/ScheduledWorkController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/form
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/form
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/start
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/complete
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/cancel
```

### DTOs

```text
App.DTO/v1/Portal/ScheduledWork/ScheduledWorkListItemDto.cs
App.DTO/v1/Portal/ScheduledWork/ScheduledWorkDetailsDto.cs
App.DTO/v1/Portal/ScheduledWork/ScheduledWorkFormDto.cs
App.DTO/v1/Portal/ScheduledWork/ScheduledWorkDto.cs
App.DTO/v1/Portal/ScheduledWork/CreateScheduledWorkDto.cs
App.DTO/v1/Portal/ScheduledWork/UpdateScheduledWorkDto.cs
App.DTO/v1/Portal/ScheduledWork/ScheduledWorkActionDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/ScheduledWork/ScheduledWorkApiMapper.cs
App.DTO/Mappers/Portal/ScheduledWork/ScheduledWorkListItemApiMapper.cs
App.DTO/Mappers/Portal/ScheduledWork/ScheduledWorkDetailsApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateScheduledWorkDto, ScheduledWorkBllDto>
IBaseMapper<UpdateScheduledWorkDto, ScheduledWorkBllDto>
```

### Implementation tasks

1. List:
   - Call `_bll.ScheduledWorks.ListForTicketAsync(TicketRoute, ...)`.
2. Create form:
   - Call `_bll.ScheduledWorks.GetCreateFormAsync(TicketRoute, ...)`.
   - Return create defaults plus vendor and work-status options.
3. Schedule:
   - Map `CreateScheduledWorkDto` -> `ScheduledWorkBllDto`.
   - Call `_bll.ScheduledWorks.ScheduleAsync(TicketRoute, dto, ...)`.
4. Details:
   - Call `_bll.ScheduledWorks.GetDetailsAsync(ScheduledWorkRoute, ...)`.
5. Edit form:
   - Call `_bll.ScheduledWorks.GetEditFormAsync(ScheduledWorkRoute, ...)`.
6. Update:
   - Map `UpdateScheduledWorkDto` -> `ScheduledWorkBllDto`.
   - Call `_bll.ScheduledWorks.UpdateScheduleAsync(ScheduledWorkRoute, dto, ...)`.
7. Delete:
   - Call `_bll.ScheduledWorks.DeleteAsync(ScheduledWorkRoute, ...)`.
8. Start:
   - Use `ScheduledWorkActionDto.ActionAt`.
   - Call `_bll.ScheduledWorks.StartWorkAsync(ScheduledWorkRoute, realStart, ...)`.
9. Complete:
   - Use `ScheduledWorkActionDto.ActionAt`.
   - Call `_bll.ScheduledWorks.CompleteWorkAsync(ScheduledWorkRoute, realEnd, ...)`.
10. Cancel:
   - No body required.
   - Call `_bll.ScheduledWorks.CancelWorkAsync(ScheduledWorkRoute, ...)`.

---

## Phase 14 — Work Logs API

`WorkLogsController` must call `_bll.WorkLogs` directly.

### Controller

```text
WebApp/ApiControllers/Portal/WorkLogsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/form
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/{workLogId}/form
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/{workLogId}/delete-model
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/{workLogId}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/{workLogId}
```

### DTOs

```text
App.DTO/v1/Portal/WorkLogs/WorkLogListItemDto.cs
App.DTO/v1/Portal/WorkLogs/WorkLogTotalsDto.cs
App.DTO/v1/Portal/WorkLogs/WorkLogListDto.cs
App.DTO/v1/Portal/WorkLogs/WorkLogFormDto.cs
App.DTO/v1/Portal/WorkLogs/WorkLogDeleteDto.cs
App.DTO/v1/Portal/WorkLogs/WorkLogDto.cs
App.DTO/v1/Portal/WorkLogs/CreateWorkLogDto.cs
App.DTO/v1/Portal/WorkLogs/UpdateWorkLogDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/WorkLogs/WorkLogApiMapper.cs
App.DTO/Mappers/Portal/WorkLogs/WorkLogListItemApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateWorkLogDto, WorkLogBllDto>
IBaseMapper<UpdateWorkLogDto, WorkLogBllDto>
```

### Implementation tasks

1. List:
   - Call `_bll.WorkLogs.ListForScheduledWorkAsync(ScheduledWorkRoute, ...)`.
   - Response includes `CanViewCosts`, totals, and list items.
2. Create form:
   - Call `_bll.WorkLogs.GetCreateFormAsync(ScheduledWorkRoute, ...)`.
3. Add:
   - Map `CreateWorkLogDto` -> `WorkLogBllDto`.
   - Call `_bll.WorkLogs.AddAsync(ScheduledWorkRoute, dto, ...)`.
4. Edit form:
   - Call `_bll.WorkLogs.GetEditFormAsync(WorkLogRoute, ...)`.
5. Delete model:
   - Call `_bll.WorkLogs.GetDeleteModelAsync(WorkLogRoute, ...)`.
6. Update:
   - Map `UpdateWorkLogDto` -> `WorkLogBllDto`.
   - Call `_bll.WorkLogs.UpdateAsync(WorkLogRoute, dto, ...)`.
7. Delete:
   - Call `_bll.WorkLogs.DeleteAsync(WorkLogRoute, ...)`.

---

## Phase 15 — Vendors API

### Controller

```text
WebApp/ApiControllers/Portal/VendorsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/vendors
POST   /api/v1/portal/companies/{companySlug}/vendors
GET    /api/v1/portal/companies/{companySlug}/vendors/{vendorId}
PUT    /api/v1/portal/companies/{companySlug}/vendors/{vendorId}
DELETE /api/v1/portal/companies/{companySlug}/vendors/{vendorId}

GET    /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/categories
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/categories
PUT    /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/categories/{ticketCategoryId}
DELETE /api/v1/portal/companies/{companySlug}/vendors/{vendorId}/categories/{ticketCategoryId}
```

### DTOs

```text
App.DTO/v1/Portal/Vendors/VendorListItemDto.cs
App.DTO/v1/Portal/Vendors/VendorProfileDto.cs
App.DTO/v1/Portal/Vendors/VendorDto.cs
App.DTO/v1/Portal/Vendors/CreateVendorDto.cs
App.DTO/v1/Portal/Vendors/UpdateVendorDto.cs
App.DTO/v1/Portal/Vendors/DeleteVendorDto.cs
App.DTO/v1/Portal/Vendors/VendorCategoryAssignmentDto.cs
App.DTO/v1/Portal/Vendors/AssignVendorCategoryDto.cs
App.DTO/v1/Portal/Vendors/UpdateVendorCategoryDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Vendors/VendorApiMapper.cs
App.DTO/Mappers/Portal/Vendors/VendorListItemApiMapper.cs
App.DTO/Mappers/Portal/Vendors/VendorProfileApiMapper.cs
App.DTO/Mappers/Portal/Vendors/VendorCategoryApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateVendorDto, VendorBllDto>
IBaseMapper<UpdateVendorDto, VendorBllDto>
IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>
IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>
```

---

## Phase 16 — Company Users API

### Controller

```text
WebApp/ApiControllers/Portal/CompanyUsersController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/users
POST   /api/v1/portal/companies/{companySlug}/users
GET    /api/v1/portal/companies/{companySlug}/users/{membershipId}
PUT    /api/v1/portal/companies/{companySlug}/users/{membershipId}
DELETE /api/v1/portal/companies/{companySlug}/users/{membershipId}

GET    /api/v1/portal/companies/{companySlug}/users/roles
GET    /api/v1/portal/companies/{companySlug}/users/ownership-transfer-candidates
POST   /api/v1/portal/companies/{companySlug}/users/transfer-ownership

POST   /api/v1/portal/companies/{companySlug}/users/access-requests/{requestId}/approve
POST   /api/v1/portal/companies/{companySlug}/users/access-requests/{requestId}/reject
```

### DTOs

```text
App.DTO/v1/Portal/Users/CompanyUserListItemDto.cs
App.DTO/v1/Portal/Users/AddCompanyUserDto.cs
App.DTO/v1/Portal/Users/UpdateCompanyUserDto.cs
App.DTO/v1/Portal/Users/CompanyUserEditDto.cs
App.DTO/v1/Portal/Users/PendingAccessRequestDto.cs
App.DTO/v1/Portal/Users/TransferOwnershipDto.cs
App.DTO/v1/Portal/Users/OwnershipTransferCandidateDto.cs
App.DTO/v1/Portal/Users/CompanyUsersPageDto.cs
```

### Mappers

```text
App.DTO/Mappers/Portal/Users/CompanyUserApiMapper.cs
App.DTO/Mappers/Portal/Users/PendingAccessRequestApiMapper.cs
App.DTO/Mappers/Portal/Users/OwnershipTransferApiMapper.cs
```

### Implementation tasks

1. Reuse existing company membership BLL calls from MVC:
   - `AuthorizeAsync`
   - `ListCompanyMembersAsync`
   - `GetPendingAccessRequestsAsync`
   - `GetAddRoleOptionsAsync`
   - `AddUserByEmailAsync`
   - `GetMembershipForEditAsync`
   - `UpdateMembershipAsync`
   - `DeleteMembershipAsync`
   - `GetOwnershipTransferCandidatesAsync`
   - `TransferOwnershipAsync`
   - `ApprovePendingAccessRequestAsync`
   - `RejectPendingAccessRequestAsync`
2. Return permission flags such as `CanEdit`, `CanDelete`, `CanTransferOwnership`, and `ProtectedReason` because Vue needs them to enable/disable buttons.

---

## Phase 17 — Lookups API

### Controller

```text
WebApp/ApiControllers/Portal/LookupsController.cs
```

Routes:

```text
GET /api/v1/portal/lookups/property-types
GET /api/v1/portal/lookups/lease-roles
GET /api/v1/portal/companies/{companySlug}/lookups/ticket-options
GET /api/v1/portal/companies/{companySlug}/lookups/vendor-ticket-categories
```

### DTOs

Use shared:

```text
App.DTO/v1/Shared/LookupOptionDto.cs
```

### Implementation tasks

1. Expose property type options used by create property:
   - Call `_bll.Properties.GetPropertyTypeOptionsAsync(...)`.
2. Expose lease roles used by unit/resident lease workflows:
   - Call `_bll.Leases.ListLeaseRolesAsync(...)`.
3. Expose ticket statuses, priorities, categories, customers, properties, units, residents, and vendors:
   - Call `_bll.Tickets.GetSelectorOptionsAsync(...)`.
4. Expose scheduled-work form options through the scheduled-work API form endpoints:
   - Call `_bll.ScheduledWorks.GetCreateFormAsync(...)`.
   - Call `_bll.ScheduledWorks.GetEditFormAsync(...)`.
5. Expose vendor ticket category options only through the existing vendor/category assignment BLL flow.
   - Do not add direct DAL access from API controllers.

---

## Dependency Injection Plan

### Register mappers manually first

In `Program.cs`:

```csharp
builder.Services.AddScoped<IBaseMapper<CreateTicketDto, TicketBllDto>, TicketCreateApiMapper>();
builder.Services.AddScoped<IBaseMapper<UpdateTicketDto, TicketBllDto>, TicketUpdateApiMapper>();
builder.Services.AddScoped<IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>, CreateUnitLeaseApiMapper>();
```

If mapper count becomes large, add an extension:

```text
WebApp/Extensions/ApiMapperServiceCollectionExtensions.cs
```

```csharp
public static IServiceCollection AddApiMappers(this IServiceCollection services)
{
    services.AddScoped<...>();
    return services;
}
```

Then in `Program.cs`:

```csharp
builder.Services.AddApiMappers();
```

### Mapper naming convention

Use explicit names when request DTOs differ:

```text
CreateTicketApiMapper
UpdateTicketApiMapper
CreateUnitLeaseApiMapper
UpdateUnitLeaseApiMapper
CreateResidentLeaseApiMapper
UpdateResidentLeaseApiMapper
```

Use shared mapper only when the DTO shape is genuinely identical.

---

## Controller Implementation Pattern

Each controller action should follow this pattern:

```csharp
[HttpPost]
public async Task<ActionResult<CustomerProfileDto>> Create(
    string companySlug,
    CreateCustomerDto dto,
    CancellationToken cancellationToken)
{
    var appUserId = GetAppUserId();
    if (appUserId is null)
    {
        return Unauthorized();
    }

    var route = new ManagementCompanyRoute
    {
        AppUserId = appUserId.Value,
        CompanySlug = companySlug
    };

    var bllDto = _customerCreateMapper.Map(dto)!;

    var result = await _bll.Customers.CreateAndGetProfileAsync(
        route,
        bllDto,
        cancellationToken);

    if (result.IsFailed)
    {
        return ToApiError(result.Errors);
    }

    return CreatedAtAction(
        nameof(GetProfile),
        new { companySlug, customerSlug = result.Value.CustomerSlug },
        _customerProfileMapper.Map(result.Value));
}
```

---

## Result and Error Handling Plan

Create helper methods to avoid repeated MVC-style error switches.

### Add API error extension

```text
WebApp/ApiControllers/ApiErrorMappingExtensions.cs
```

Responsibilities:

- Convert `FluentResults.IError` to status codes.
- Convert `ValidationAppError` failures to `RestApiErrorResponse.Errors`.
- Set `RestApiErrorResponse.ErrorCode` from `ApiErrorCodes`.
- Preserve business error message.
- Avoid leaking stack traces.

### Recommended response formats

Validation error:

```json
{
  "status": 400,
  "error": "Validation failed.",
  "errorCode": "validation_failed",
  "errors": {
    "name": ["Name is required"]
  },
  "traceId": "00-..."
}
```

Conflict:

```json
{
  "status": 409,
  "error": "Ticket number already exists.",
  "errorCode": "conflict",
  "errors": {},
  "traceId": "00-..."
}
```

Unauthorized:

```json
{
  "status": 401,
  "error": "Authentication is required.",
  "errorCode": "unauthorized",
  "errors": {},
  "traceId": "00-..."
}
```

---

## Frontend Route Mapping

The API should return resource identifiers and slugs, not MVC redirect results.

Example after creating management company:

```json
{
  "id": "guid",
  "slug": "acme-management",
  "name": "ACME Management",
  "frontendPath": "/companies/acme-management"
}
```

Example after creating customer:

```json
{
  "customerId": "guid",
  "customerSlug": "big-customer",
  "name": "Big Customer",
  "frontendPath": "/companies/acme-management/customers/big-customer"
}
```

---

## OpenAPI / Swagger Plan

For each controller:

- Add `[ApiVersion("1.0")]`.
- Add `[Produces("application/json")]`.
- Add `[ProducesResponseType]` for:
  - `200 OK`
  - `201 Created`
  - `204 NoContent`
  - `400 BadRequest`
  - `401 Unauthorized`
  - `403 Forbidden`
  - `404 NotFound`
  - `409 Conflict`
- Use DTO summaries if XML docs are enabled later.

---

## Verification Plan

Project override: do not create tests until further notice.

Run build and manual verification only:

1. Build the solution and fix compile errors.
2. Manually inspect Swagger/OpenAPI output for route, DTO, and response metadata.
3. Manually exercise representative happy paths with an authenticated JWT:
   - auth/login/refresh/logout
   - onboarding and workspace selection
   - dashboard data for management, customer, property, unit, and resident workspaces
   - customer/property/unit/resident create/list
   - management, customer, property, unit, and resident ticket list/search
   - ticket create/update/status advance
   - scheduled work lifecycle
   - work log lifecycle
   - vendor category/contact flows
   - company user access-request flow
4. Manually verify scoped route behavior:
   - no token -> `401`
   - token without company access -> `403` or `404`, depending on current BLL behavior
   - valid token with access -> success
   - cross-company slug access fails without leaking existence

---

## Suggested Implementation Order

### Milestone 1 — API foundation

1. Add `ApiControllerBase`.
2. Add `ApiErrorMappingExtensions`.
3. Add `AuthController`.
4. Add JWT token service.
5. Add refresh token service.
6. Add common DTOs.

### Milestone 2 — Onboarding and workspace

1. Add onboarding DTOs and mapper.
2. Add `OnboardingController`.
3. Add workspace DTOs and mappers.
4. Add `WorkspacesController`.

### Milestone 3 — Core property-management resources

1. Portal dashboards.
2. Companies/profile.
3. Customers.
4. Properties.
5. Units.
6. Residents.

### Milestone 4 — Leases and contacts

1. Leases API.
2. Resident contacts.
3. Vendor contacts.

### Milestone 5 — Tickets and maintenance

1. Tickets API, including management, customer, property, unit, and resident ticket lists.
2. Scheduled work API.
3. Work logs API.

### Milestone 6 — Vendors and users

1. Vendors API.
2. Vendor categories.
3. Company users and access requests.

### Milestone 7 — polish

1. Lookups controller.
2. Swagger response metadata.
3. Manual API verification.
4. Frontend path response consistency.
5. CORS production tightening.

---

## Do Not Implement Yet

Do not build API controllers for these until the underlying BLL/MVC workflow is implemented beyond a placeholder or page shell:

```text
Resident access onboarding
Management-level properties
Customer-scoped residents
Property-scoped residents
Resident representations
```

---

## Final Target Folder Layout

```text
App.DTO/v1/
  Common/
  Identity/
  Onboarding/
  Workspace/
  Shared/
  Portal/
    Companies/
    Dashboards/
    Customers/
    Properties/
    Units/
    Residents/
    Leases/
    Contacts/
    Tickets/
    ScheduledWork/
    WorkLogs/
    Vendors/
    Users/
    Lookups/
App.DTO/Mappers/
  Auth/
  Onboarding/
  Workspace/
  Portal/
    Companies/
    Dashboards/
    Customers/
    Properties/
    Units/
    Residents/
    Leases/
    Contacts/
    Tickets/
    ScheduledWork/
    WorkLogs/
    Vendors/
    Users/
    Lookups/

WebApp/
  ApiControllers/
    ApiControllerBase.cs
    ApiErrorMappingExtensions.cs
    Auth/
      AuthController.cs
    Onboarding/
      OnboardingController.cs
    Workspace/
      WorkspacesController.cs
    Portal/
      ManagementCompaniesController.cs
      DashboardsController.cs
      CustomersController.cs
      PropertiesController.cs
      UnitsController.cs
      ResidentsController.cs
      LeasesController.cs
      ContactsController.cs
      TicketsController.cs
      ScheduledWorkController.cs
      WorkLogsController.cs
      VendorsController.cs
      CompanyUsersController.cs
      LookupsController.cs
  Services/
    Identity/
      IJwtTokenService.cs
      JwtTokenService.cs
      IRefreshTokenService.cs
      RefreshTokenService.cs
  Extensions/
    ApiMapperServiceCollectionExtensions.cs
```

---

## Acceptance Criteria

The implementation is complete when:

- Vue can register/login/refresh/logout using JWT and refresh token.
- Vue can create a management company and enter its Portal workspace.
- Vue can submit a join request for a management company.
- Vue can list/select available workspaces.
- Vue can load dashboard data for management, customer, property, unit, and resident workspaces.
- Vue can perform all implemented Portal workflows through JSON APIs:
  - company profile
  - customers
  - properties
  - units
  - residents
  - leases
  - contacts
  - tickets, including management, customer, property, unit, and resident ticket lists
  - scheduled work
  - work logs
  - vendors
  - vendor categories
  - company users/access requests
- All public API contracts live in `App.DTO/v1` and use `App.DTO.v1.*` namespaces.
- API mapper implementations live in `App.DTO/Mappers`.
- Error responses are compatible with `RestApiErrorResponse`.
- Mapping logic is separated from controllers.
- `IBaseMapper<ApiDto, BllDto>` is used where mapping is two-way and canonical BLL DTOs are available.
- Controllers call only public `IAppBLL` services; they do not invent DAL access.
- `DashboardsController` only calls `_bll.PortalDashboards`.
- `TicketsController` only calls `_bll.Tickets`.
- `ScheduledWorkController` calls `_bll.ScheduledWorks` directly.
- `WorkLogsController` calls `_bll.WorkLogs` directly.
- SPA create/edit flows have form/options endpoints where existing BLL form models provide required options.
- Controllers return public API DTOs, never DAL DTOs, BLL DTOs/models, domain entities, MVC ViewModels, `ViewBag`, or `ViewData`.
- Controllers return status codes and validation errors suitable for a SPA.
- Placeholder/shell workflows are not exposed as real APIs.
- No tests are added while the testing override remains active.

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

API mapper implementations should live under `App.DTO/v1/Mappers` and map between public `App.DTO.v1.*` API DTOs and BLL DTOs/models. Keep mapper namespaces versioned with the DTOs, for example `App.DTO.v1.Mappers.Portal.Tickets`. Use `IBaseMapper<ApiDto, BllDto>` wherever the mapping is naturally two-way, especially for command/save DTOs such as `TicketBllDto`, `LeaseBllDto`, `CustomerBllDto`, `PropertyBllDto`, `UnitBllDto`, `ResidentBllDto`, `VendorBllDto`, `ScheduledWorkBllDto`, and `WorkLogBllDto`.

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

### Existing partial API foundation

The repository already contains a partial API foundation:

- `WebApp/ApiControllers/ApiControllerBase.cs`
- `WebApp/ApiControllers/Auth/AuthController.cs`
- `WebApp/Services/Identity/IIdentityAccountService.cs`
- `WebApp/Services/Identity/IJwtTokenService.cs`
- identity DTOs under `App.DTO/v1/Identity`
- shared error/lookup DTOs under `App.DTO/v1`

Audit and align these existing files with the public contract instead of recreating them.

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

### API mapper placement

API mappers belong beside the versioned public DTOs so each API version owns its transport mapping. `App.DTO` must add the project references needed for those mappers, specifically `Base.Contracts` for `IBaseMapper` and `App.BLL.DTO` for BLL DTO/model inputs. `App.DTO` must not depend on `WebApp`, MVC ViewModels, DAL DTOs, EF, or domain entities.

Required placement:

- public DTO contracts in `App.DTO/v1` using `App.DTO.v1.*` namespaces
- API mapper implementations in `App.DTO/v1/Mappers`
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
  Mappers/
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

The rest of this plan assumes versioned public DTOs in `App.DTO/v1` and mappers in `App.DTO/v1/Mappers`.

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

Use route constraints for all GUID route values. This is required anywhere fixed route segments share a prefix with dynamic routes, and is preferred everywhere for consistency:

```text
{ticketId:guid}
{scheduledWorkId:guid}
{workLogId:guid}
{leaseId:guid}
{vendorId:guid}
{residentContactId:guid}
{vendorContactId:guid}
{ticketCategoryId:guid}
{membershipId:guid}
{requestId:guid}
{propertyId:guid}
```

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

For API controllers except login/register/refresh/logout:

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

Reuse existing identity DTOs before adding new request/response shapes. Keep `JWTResponse` token-only with `Jwt` and `RefreshToken`; remove any `ExpiresAt`, `User`, or profile fields from that DTO. User profile data belongs to `GET /api/v1/auth/me`.

### Implementation tasks

1. Audit the existing `AuthController`, `IIdentityAccountService`, and `IJwtTokenService` implementation instead of recreating them.
2. Reuse `IIdentityAccountService.CreateUserAsync` for registration.
3. Reuse `IIdentityAccountService.ValidateCredentialsAsync` for JWT login.
4. Keep `register`, `login`, `refresh`, and `logout` as `[AllowAnonymous]`; keep `me` protected with JWT bearer authentication.
5. Use existing `_bll.AuthSessions` for refresh-token persistence, rotation, reuse handling, and revocation. Do not add a parallel WebApp refresh-token service.
6. Use existing `AppRefreshToken` domain entity through the BLL auth-session flow.
7. Login and refresh responses should return only the access token and refresh token:

```json
{
  "jwt": "...",
  "refreshToken": "..."
}
```

8. Registration does not log the user in and must not issue JWT or refresh tokens. Return the created public user DTO from `POST /auth/register`; the client must call `POST /auth/login` to start an authenticated session.
9. `GET /auth/me` should remain the dedicated endpoint for current user details.

### Notes

- Access token lifetime should be short, for example 15 minutes.
- Do not include access-token expiry in `JWTResponse`; the JWT already carries its expiry in the `exp` claim.
- Refresh token lifetime should match the domain default or config, currently conceptually 7 days.
- Store refresh tokens hashed through the existing auth-session implementation.
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
App.DTO/v1/Mappers/Onboarding/ManagementCompanyApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateManagementCompanyDto, ManagementCompanyBllDto>
```

### Implementation tasks

1. `GET /status`
   - Use the workspace entry-point service to determine whether the user can go directly to a workspace.
   - Return available onboarding actions:
     - `createManagementCompany`
     - `joinManagementCompany`
2. `POST /management-companies`
   - Map `CreateManagementCompanyDto` -> `ManagementCompanyBllDto`.
   - Call `_bll.ManagementCompanies.CreateAsync(...)`.
   - Return created id, slug, name, and `path` such as `/companies/{slug}`.
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
App.DTO/v1/Workspace/WorkspaceOptionPermissionsDto.cs
App.DTO/v1/Workspace/WorkspaceRedirectDto.cs
App.DTO/v1/Workspace/SelectWorkspaceDto.cs
```

`WorkspaceCatalogDto` must make management-company workspaces first-class and treat customer/resident workspaces as optional data-driven contexts. Customer and resident contexts currently depend on deferred `CustomerRepresentatives` and `ResidentUsers` workflows, so these collections may be empty in v1 and must not block management-company Portal APIs.

```csharp
public class WorkspaceCatalogDto
{
    public IReadOnlyList<WorkspaceOptionDto> ManagementCompanies { get; set; } = [];
    public IReadOnlyList<WorkspaceOptionDto> Customers { get; set; } = [];
    public IReadOnlyList<WorkspaceOptionDto> Residents { get; set; } = [];
    public WorkspaceOptionDto? DefaultContext { get; set; }
}
```

`WorkspaceOptionDto` must include enough data for direct SPA navigation and per-workspace permissions:

```csharp
public class WorkspaceOptionDto
{
    public Guid Id { get; set; }
    public string ContextType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? ManagementCompanySlug { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public WorkspaceOptionPermissionsDto Permissions { get; set; } = new();
}

public class WorkspaceOptionPermissionsDto
{
    public bool CanManageCompanyUsers { get; set; }
}
```

`SelectWorkspaceDto` must require an explicit context id. Do not auto-select a resident workspace when the id is omitted.

```csharp
public class SelectWorkspaceDto
{
    public string ContextType { get; set; } = string.Empty;
    public Guid ContextId { get; set; }
}
```

`WorkspaceRedirectDto` must return a path-only SPA route:

```csharp
public class WorkspaceRedirectDto
{
    public string Destination { get; set; } = string.Empty;
    public string? CompanySlug { get; set; }
    public string? CustomerSlug { get; set; }
    public string? ResidentIdCode { get; set; }
    public string Path { get; set; } = string.Empty;
}
```

`Path` is always a route path starting with `/`, never an absolute URL.

Supported `ContextType` values are `management`, `customer`, and `resident`. Management-company users can still manage customers, properties, units, residents, leases, tickets, scheduled work, work logs, vendors, contacts, and company users through company-scoped Portal APIs even when no customer/resident self-service workspace contexts exist.

SystemAdmin workspaces are MVC-only for this plan. Do not add an API default destination or workspace option for SystemAdmin.

### BLL workspace entry-point cleanup

The BLL should resolve authorized workspace targets, not Web/MVC/Vue redirects. Rename the current BLL redirect concepts before implementing the API:

```text
ResolveContextRedirectAsync        -> ResolveWorkspaceEntryPointAsync
ResolveWorkspaceRedirectQuery      -> ResolveWorkspaceEntryPointQuery
WorkspaceRedirectModel             -> WorkspaceEntryPointModel
WorkspaceRedirectDestination       -> WorkspaceEntryPointKind
```

Keep the existing `GetCatalogAsync(ManagementCompanyRoute route)` behavior and the current MVC-shaped `WorkspaceCatalogModel` for MVC chrome and company-scoped internal flows. MVC already enters the Portal through a company route and uses this method to build current-company shell state such as current company display name, current workspace option, and catalog-level `CanManageCompanyUsers`.

Do not force the global Vue workspace catalog into the MVC-shaped `WorkspaceCatalogModel`. Add a separate user-wide BLL model for the API:

```csharp
public class UserWorkspaceCatalogModel
{
    public IReadOnlyList<WorkspaceOptionModel> ManagementCompanies { get; init; } = [];
    public IReadOnlyList<WorkspaceOptionModel> Customers { get; init; } = [];
    public IReadOnlyList<WorkspaceOptionModel> Residents { get; init; } = [];
    public WorkspaceOptionModel? DefaultContext { get; init; }
}
```

Extend `WorkspaceOptionModel` with `CanManageCompanyUsers` so permissions can be attached per workspace option:

```csharp
public class WorkspaceOptionModel
{
    public Guid Id { get; init; }
    public string ContextType { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Slug { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public bool IsDefault { get; init; }
    public bool CanManageCompanyUsers { get; init; }
}
```

Only management workspace options may set `CanManageCompanyUsers = true`, and only when the management-company role code is `OWNER` or `MANAGER`. Customer and resident options default to `false`.

Add a user-level catalog contract for the global workspace API:

```csharp
Task<Result<UserWorkspaceCatalogModel>> GetUserCatalogAsync(
    Guid appUserId,
    CancellationToken cancellationToken = default);
```

`GetUserCatalogAsync` is for `/api/v1/workspaces`, where Vue may not yet have a selected company route. It returns all authorized management-company workspace options for the authenticated user without requiring a `companySlug` parameter. It may also return customer and resident workspace options if data already exists, but v1 must work correctly when those collections are empty because customer representative and resident-user link workflows are deferred.

Build `GetUserCatalogAsync` from existing DAL capabilities where possible:

- management options from `ActiveUserManagementContextsAsync`
- customer options from `ActiveUserCustomerContextsAsync`
- resident options from `FirstActiveUserResidentContextAsync`, returned as a 0/1 `Residents` list for v1

Default context precedence is: first management company, then resident, then customer.

The workspace service contract after cleanup should expose:

```csharp
Task<Result<WorkspaceCatalogModel>> GetCatalogAsync(
    ManagementCompanyRoute route,
    CancellationToken cancellationToken = default);

Task<Result<UserWorkspaceCatalogModel>> GetUserCatalogAsync(
    Guid appUserId,
    CancellationToken cancellationToken = default);

Task<Result<WorkspaceEntryPointModel?>> ResolveWorkspaceEntryPointAsync(
    ResolveWorkspaceEntryPointQuery query,
    CancellationToken cancellationToken = default);

Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
    AuthorizeContextSelectionQuery query,
    CancellationToken cancellationToken = default);
```

`WorkspaceEntryPointModel` must contain only business/application context facts:

```csharp
public class WorkspaceEntryPointModel
{
    public required WorkspaceEntryPointKind Kind { get; init; }
    public Guid? ContextId { get; init; }
    public string? CompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? ResidentIdCode { get; init; }
}
```

`WorkspaceSelectionAuthorizationModel` must also return route facts for the selected workspace so both MVC and API can use the same BLL method:

```csharp
public class WorkspaceSelectionAuthorizationModel
{
    public bool Authorized { get; init; }
    public string ContextType { get; init; } = default!;
    public Guid? ContextId { get; init; }
    public string? Name { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? ResidentIdCode { get; init; }
}
```

For management selection, BLL must authorize the selected management company by id and return its management company slug. For customer selection, BLL must authorize the selected customer context by id and return its customer slug and management company slug if that context exists. For resident selection, BLL must authorize the selected resident context by id and return its resident id code and management company slug if that context exists. If the selected customer/resident context does not exist because the deferred workflows have not created those links, return `Authorized = false`; do not create links or infer access.

`AuthorizeContextSelectionAsync` must require `ContextId` for all supported context types:

- management: authorize by management company id and return company slug
- customer: match the id from the active customer context list and return management company slug and customer slug
- resident: match the id from the active resident context and return management company slug and resident id code

`ResolveWorkspaceEntryPointAsync` should first honor a remembered context when one is supplied and still authorized, then fall back to the default precedence used by `GetUserCatalogAsync`. API callers should pass no MVC cookie state for default redirect; MVC onboarding may continue passing cookie-derived remembered context.

Do not add `Path` or Vue route strings to BLL DTOs/models. Build `WorkspaceOptionDto.Path` and `WorkspaceRedirectDto.Path` in the API mapper/controller layer from BLL context facts:

```text
management -> /companies/{companySlug}
customer   -> /companies/{companySlug}/customers/{customerSlug}
resident   -> /companies/{companySlug}/residents/{residentIdCode}
```

Update MVC callers affected by the rename, especially public onboarding redirect logic, so the web app compiles against the entry-point terminology.

### Mappers

```text
App.DTO/v1/Mappers/Workspace/WorkspaceCatalogApiMapper.cs
App.DTO/v1/Mappers/Workspace/WorkspaceOptionApiMapper.cs
```

### Implementation tasks

1. `GET /workspaces`
   - Use `_bll.Workspaces.GetUserCatalogAsync(appUserId, ...)`.
   - Return management companies, optional customer contexts, optional resident contexts, and default context from BLL.
   - Return per-option permission flags; do not return catalog-level `CanManageCompanyUsers`.
   - Add path-only SPA routes while mapping to public API DTOs.
2. `GET /default-redirect`
   - Use `_bll.Workspaces.ResolveWorkspaceEntryPointAsync(...)` without MVC cookie state.
   - Return a direct tenant workspace `path`, not an MVC redirect.
   - Return `404 NotFound` with `RestApiErrorResponse` when the authenticated user has no tenant workspace.
   - Do not add a SystemAdmin default destination; SystemAdmin is MVC-only in this plan.
3. `POST /select`
   - Use `_bll.Workspaces.AuthorizeContextSelectionAsync(...)`.
   - Require a non-empty `ContextId` for `management`, `customer`, and `resident`; return `400 BadRequest` for malformed or unsupported context types.
   - Return a structured authorization/not-found error for unauthorized selections through the shared API error mapping.
   - Return selected context metadata from BLL and build `path` in WebApp.

### Workspace verification scenarios

Manual verification must cover:

- unauthenticated workspace calls return `401`
- user with no tenant workspace gets `404` from default redirect
- management member sees management workspaces with correct per-option user-management permissions
- invalid or cross-tenant context selection is rejected without leaking existence
- MVC onboarding redirects still compile and behave through entry-point terminology

### Important

Do not depend on MVC `ctx.*` cookies for Vue. The SPA should store selected workspace state client-side or derive it from the route and `/workspaces`.

### Assumptions

- Customer and resident self-service links remain deferred; customer/resident collections may be empty.
- Current v1 resident catalog supports at most one active resident context because the repository exposes a first active resident context.
- No persistence schema or migration changes are required.

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
App.DTO/v1/Mappers/Portal/Dashboards/PortalDashboardApiMapper.cs
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
GET    /api/v1/portal/companies/{companySlug}/profile
PUT    /api/v1/portal/companies/{companySlug}/profile
DELETE /api/v1/portal/companies/{companySlug}/profile
```

### DTOs

```text
App.DTO/v1/Portal/Companies/ManagementCompanyProfileDto.cs
App.DTO/v1/Portal/Companies/UpdateManagementCompanyProfileDto.cs
App.DTO/v1/Portal/Companies/DeleteManagementCompanyDto.cs
```

### Mappers

```text
App.DTO/v1/Mappers/Portal/Companies/ManagementCompanyProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<UpdateManagementCompanyProfileDto, ManagementCompanyBllDto>
```

### Implementation tasks

1. `GET /profile`
   - Use `_bll.ManagementCompanies.GetProfileAsync(...)`.
2. `PUT /profile`
   - Map API DTO -> `ManagementCompanyBllDto`.
   - Use `_bll.ManagementCompanies.UpdateAsync(...)`.
3. `DELETE /profile`
   - Require confirmation value.
   - Use `_bll.ManagementCompanies.DeleteAsync(...)`.

Do not add a company summary endpoint unless a real BLL summary model exists. Use workspace catalog and dashboard APIs for SPA shell/header context.

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
App.DTO/v1/Mappers/Portal/Customers/CustomerApiMapper.cs
App.DTO/v1/Mappers/Portal/Customers/CustomerListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Customers/CustomerProfileApiMapper.cs
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
App.DTO/v1/Mappers/Portal/Properties/PropertyApiMapper.cs
App.DTO/v1/Mappers/Portal/Properties/PropertyListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Properties/PropertyProfileApiMapper.cs
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
App.DTO/v1/Mappers/Portal/Units/UnitApiMapper.cs
App.DTO/v1/Mappers/Portal/Units/UnitListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Units/UnitProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateUnitDto, UnitBllDto>
IBaseMapper<UpdateUnitProfileDto, UnitBllDto>
```

### Implementation tasks

1. List units:
   - Call `_bll.Units.ListForPropertyAsync(...)`.
2. Create unit:
   - Map `CreateUnitDto` -> `UnitBllDto`.
   - Call `_bll.Units.CreateAndGetProfileAsync(...)`.
   - Return `201 Created` with profile data and `path`.
3. Get unit profile:
   - Call `_bll.Units.GetProfileAsync(...)`.
4. Update unit profile:
   - Map `UpdateUnitProfileDto` -> `UnitBllDto`.
   - Call `_bll.Units.UpdateAndGetProfileAsync(...)`.
   - Return updated profile data and `path` because the unit slug can change.
5. Delete unit:
   - Use `DeleteUnitDto.DeleteConfirmation`.
   - Call `_bll.Units.DeleteAsync(...)`.

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
App.DTO/v1/Mappers/Portal/Residents/ResidentApiMapper.cs
App.DTO/v1/Mappers/Portal/Residents/ResidentListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Residents/ResidentProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateResidentDto, ResidentBllDto>
IBaseMapper<UpdateResidentProfileDto, ResidentBllDto>
```

### Implementation tasks

1. List residents:
   - Call `_bll.Residents.ListForCompanyAsync(...)`.
2. Create resident:
   - Map `CreateResidentDto` -> `ResidentBllDto`.
   - Call `_bll.Residents.CreateAndGetProfileAsync(...)`.
   - Return `201 Created` with profile data and `path`.
3. Get resident profile:
   - Call `_bll.Residents.GetProfileAsync(...)`.
4. Update resident profile:
   - Map `UpdateResidentProfileDto` -> `ResidentBllDto`.
   - Call `_bll.Residents.UpdateAndGetProfileAsync(...)`.
   - Return updated profile data and `path` because the resident id code can change.
5. Delete resident:
   - Use `DeleteResidentDto.DeleteConfirmation`.
   - Call `_bll.Residents.DeleteAsync(...)`.

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
GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId:guid}
PUT    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId:guid}
DELETE /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId:guid}

GET    /api/v1/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/resident-search?searchTerm=

GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases
GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId:guid}
PUT    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId:guid}
DELETE /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/leases/{leaseId:guid}

GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/property-search?searchTerm=
GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/properties/{propertyId:guid}/units
```

### DTOs

```text
App.DTO/v1/Portal/Leases/UnitLeaseListItemDto.cs
App.DTO/v1/Portal/Leases/ResidentLeaseListItemDto.cs
App.DTO/v1/Portal/Leases/LeaseDto.cs
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
App.DTO/v1/Mappers/Portal/Leases/LeaseApiMapper.cs
App.DTO/v1/Mappers/Portal/Leases/UnitLeaseListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Leases/ResidentLeaseListItemApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>
IBaseMapper<UpdateUnitLeaseDto, LeaseBllDto>
IBaseMapper<CreateResidentLeaseDto, LeaseBllDto>
IBaseMapper<UpdateResidentLeaseDto, LeaseBllDto>
```

### Implementation tasks

1. Reuse the same BLL mutation methods as MVC for create/update/delete. Do not introduce API-only BLL mutation methods just to change the response shape.
2. Unit-side actions:
   - `ListForUnitAsync`
   - `GetForUnitAsync`
   - `SearchResidentsAsync`
   - `CreateForUnitAsync`
   - `UpdateFromUnitAsync`
   - `DeleteFromUnitAsync`
3. Resident-side actions:
   - `ListForResidentAsync`
   - `GetForResidentAsync`
   - `SearchPropertiesAsync`
   - `ListUnitsForPropertyAsync`
   - `CreateForResidentAsync`
   - `UpdateFromResidentAsync`
   - `DeleteFromResidentAsync`
4. Response behavior:
   - List endpoints return context-specific enriched rows: `UnitLeaseListItemDto[]` or `ResidentLeaseListItemDto[]`.
   - Detail endpoints return minimal `LeaseDto` mapped from `LeaseModel`.
   - Create returns `201 Created` with minimal `LeaseDto` mapped from the returned `LeaseBllDto`.
   - Update returns `200 OK` with minimal `LeaseDto` mapped from the returned `LeaseBllDto`.
   - Delete returns `204 NoContent`.
   - If Vue needs enriched row labels after create/update, it should refresh the relevant list endpoint instead of forcing read-after-write behavior inside the BLL.

---

## Phase 11 — Contacts API

Implement resident and vendor contact assignment workflows. Prefer one controller with two resource groups. Contacts are owned through resident and vendor workflows in v1; do not add `IAppBLL.Contacts` or a generic contact service dependency for this API pass.

### Controller

```text
WebApp/ApiControllers/Portal/ContactsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/attach
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts
PUT    /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId:guid}
DELETE /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId:guid}
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId:guid}/set-primary
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId:guid}/confirm
POST   /api/v1/portal/companies/{companySlug}/residents/{residentIdCode}/contacts/{residentContactId:guid}/unconfirm

GET    /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts/attach
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts
PUT    /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts/{vendorContactId:guid}
DELETE /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts/{vendorContactId:guid}
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts/{vendorContactId:guid}/set-primary
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts/{vendorContactId:guid}/confirm
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts/{vendorContactId:guid}/unconfirm
```

### DTOs

```text
App.DTO/v1/Portal/Contacts/ContactDto.cs
App.DTO/v1/Portal/Contacts/CreateContactDto.cs
App.DTO/v1/Portal/Contacts/ResidentContactListDto.cs
App.DTO/v1/Portal/Contacts/ResidentContactAssignmentDto.cs
App.DTO/v1/Portal/Contacts/AttachResidentContactDto.cs
App.DTO/v1/Portal/Contacts/CreateResidentContactDto.cs
App.DTO/v1/Portal/Contacts/UpdateResidentContactDto.cs
App.DTO/v1/Portal/Contacts/VendorContactListDto.cs
App.DTO/v1/Portal/Contacts/VendorContactAssignmentDto.cs
App.DTO/v1/Portal/Contacts/AttachVendorContactDto.cs
App.DTO/v1/Portal/Contacts/CreateVendorContactDto.cs
App.DTO/v1/Portal/Contacts/UpdateVendorContactDto.cs
```

`ResidentContactListDto` and `VendorContactListDto` must include:

- context metadata for the resident or vendor
- current assigned contacts
- existing contact options available for attachment
- contact type options needed by create/edit forms

### Mappers

```text
App.DTO/v1/Mappers/Portal/Contacts/ContactApiMapper.cs
App.DTO/v1/Mappers/Portal/Contacts/ResidentContactApiMapper.cs
App.DTO/v1/Mappers/Portal/Contacts/VendorContactApiMapper.cs
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

Do not plan `_bll.Contacts` usage in this implementation. Resident contact endpoints call `_bll.Residents`; vendor contact endpoints call `_bll.Vendors`.

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
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/advance-status

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
App.DTO/v1/Mappers/Portal/Tickets/TicketApiMapper.cs
App.DTO/v1/Mappers/Portal/Tickets/TicketListApiMapper.cs
App.DTO/v1/Mappers/Portal/Tickets/ContextTicketListApiMapper.cs
App.DTO/v1/Mappers/Portal/Tickets/TicketListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Tickets/TicketDetailsApiMapper.cs
App.DTO/v1/Mappers/Portal/Tickets/TicketOptionsApiMapper.cs
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
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/form
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/form
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/start
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/complete
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/cancel
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
App.DTO/v1/Mappers/Portal/ScheduledWork/ScheduledWorkApiMapper.cs
App.DTO/v1/Mappers/Portal/ScheduledWork/ScheduledWorkListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/ScheduledWork/ScheduledWorkDetailsApiMapper.cs
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
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs/form
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs/{workLogId:guid}/form
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs/{workLogId:guid}/delete-model
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs/{workLogId:guid}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs/{workLogId:guid}
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
App.DTO/v1/Mappers/Portal/WorkLogs/WorkLogApiMapper.cs
App.DTO/v1/Mappers/Portal/WorkLogs/WorkLogListItemApiMapper.cs
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
GET    /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}
PUT    /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}
DELETE /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}

GET    /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/categories
POST   /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/categories
PUT    /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/categories/{ticketCategoryId:guid}
DELETE /api/v1/portal/companies/{companySlug}/vendors/{vendorId:guid}/categories/{ticketCategoryId:guid}
```

### DTOs

```text
App.DTO/v1/Portal/Vendors/VendorListItemDto.cs
App.DTO/v1/Portal/Vendors/VendorProfileDto.cs
App.DTO/v1/Portal/Vendors/VendorDto.cs
App.DTO/v1/Portal/Vendors/CreateVendorDto.cs
App.DTO/v1/Portal/Vendors/UpdateVendorDto.cs
App.DTO/v1/Portal/Vendors/DeleteVendorDto.cs
App.DTO/v1/Portal/Vendors/VendorCategoryAssignmentListDto.cs
App.DTO/v1/Portal/Vendors/VendorCategoryAssignmentDto.cs
App.DTO/v1/Portal/Vendors/AssignVendorCategoryDto.cs
App.DTO/v1/Portal/Vendors/UpdateVendorCategoryDto.cs
```

`VendorCategoryAssignmentListDto` must include vendor context metadata, assigned category rows, and available category options for assignment.

### Mappers

```text
App.DTO/v1/Mappers/Portal/Vendors/VendorApiMapper.cs
App.DTO/v1/Mappers/Portal/Vendors/VendorListItemApiMapper.cs
App.DTO/v1/Mappers/Portal/Vendors/VendorProfileApiMapper.cs
App.DTO/v1/Mappers/Portal/Vendors/VendorCategoryApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateVendorDto, VendorBllDto>
IBaseMapper<UpdateVendorDto, VendorBllDto>
IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>
IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>
```

### Implementation tasks

1. List vendors:
   - Call `_bll.Vendors.ListForCompanyAsync(...)`.
2. Create vendor:
   - Map `CreateVendorDto` -> `VendorBllDto`.
   - Call `_bll.Vendors.CreateAndGetProfileAsync(...)`.
   - Return `201 Created` with profile data and `path`.
3. Details/profile:
   - Call `_bll.Vendors.GetProfileAsync(...)`.
4. Update vendor:
   - Map `UpdateVendorDto` -> `VendorBllDto`.
   - Call `_bll.Vendors.UpdateAndGetProfileAsync(...)`.
5. Delete vendor:
   - Use `DeleteVendorDto.ConfirmationRegistryCode`.
   - Call `_bll.Vendors.DeleteAsync(...)`.
6. List category assignments:
   - Call `_bll.Vendors.ListCategoryAssignmentsAsync(...)`.
   - Return assigned categories and available category options in one response.
7. Assign category:
   - Map `AssignVendorCategoryDto` -> `VendorTicketCategoryBllDto`.
   - Call `_bll.Vendors.AssignCategoryAsync(...)`.
   - Return the refreshed category assignment list.
8. Update category assignment:
   - Map `UpdateVendorCategoryDto` -> `VendorTicketCategoryBllDto`.
   - Call `_bll.Vendors.UpdateCategoryAssignmentAsync(...)`.
   - Return the refreshed category assignment list.
9. Remove category:
   - Call `_bll.Vendors.RemoveCategoryAsync(...)`.

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
GET    /api/v1/portal/companies/{companySlug}/users/{membershipId:guid}
PUT    /api/v1/portal/companies/{companySlug}/users/{membershipId:guid}
DELETE /api/v1/portal/companies/{companySlug}/users/{membershipId:guid}

GET    /api/v1/portal/companies/{companySlug}/users/roles
GET    /api/v1/portal/companies/{companySlug}/users/ownership-transfer-candidates
POST   /api/v1/portal/companies/{companySlug}/users/transfer-ownership

POST   /api/v1/portal/companies/{companySlug}/users/access-requests/{requestId:guid}/approve
POST   /api/v1/portal/companies/{companySlug}/users/access-requests/{requestId:guid}/reject
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
App.DTO/v1/Mappers/Portal/Users/CompanyUserApiMapper.cs
App.DTO/v1/Mappers/Portal/Users/PendingAccessRequestApiMapper.cs
App.DTO/v1/Mappers/Portal/Users/OwnershipTransferApiMapper.cs
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
5. Expose vendor ticket category options only through the vendor category assignment endpoints.
   - Use the response from `_bll.Vendors.ListCategoryAssignmentsAsync(...)`, which includes available category options.
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

## SPA Route Mapping

The API should return resource identifiers and slugs, not MVC redirect results. Use `path` as the canonical SPA route field everywhere a response tells Vue where to navigate. `path` is path-only and must never include scheme or host.

Example after creating management company:

```json
{
  "id": "guid",
  "slug": "acme-management",
  "name": "ACME Management",
  "path": "/companies/acme-management"
}
```

Example after creating customer:

```json
{
  "customerId": "guid",
  "customerSlug": "big-customer",
  "name": "Big Customer",
  "path": "/companies/acme-management/customers/big-customer"
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

### Milestone 1 — API foundation alignment

1. Align existing `ApiControllerBase`, `AuthController`, identity services, and identity DTOs with the public API contract.
2. Keep `JWTResponse` token-only with `Jwt` and `RefreshToken`; registration returns `UserDto` only and never starts a session.
3. Add missing common DTOs only.
4. Add `Base.Contracts` and `App.BLL.DTO` references to `App.DTO` for versioned API mapper implementations.
5. Add `ApiErrorMappingExtensions` only if the existing base controller does not cover the shared error mapping cleanly.
6. Register API mappers in WebApp.

### Milestone 2 — Onboarding and workspace

1. Add onboarding DTOs and mapper.
2. Add `OnboardingController`.
3. Rename workspace redirect BLL concepts to entry-point concepts and add `GetUserCatalogAsync`.
4. Add workspace DTOs and mappers.
5. Add `WorkspacesController`.

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
  Mappers/
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
  Extensions/
    ApiMapperServiceCollectionExtensions.cs
```

---

## Acceptance Criteria

The implementation is complete when:

- Vue can register without being logged in automatically, then login/refresh/logout using JWT and refresh token.
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
- API mapper implementations live in `App.DTO/v1/Mappers`.
- Error responses are compatible with `RestApiErrorResponse`.
- Mapping logic is separated from controllers.
- `IBaseMapper<ApiDto, BllDto>` is used where mapping is two-way and canonical BLL DTOs are available.
- MVC and API share the same BLL methods for the same business operations; API-only BLL methods are added only for genuinely different application queries such as the user-wide workspace catalog.
- GUID route parameters use route constraints such as `{ticketId:guid}` and `{membershipId:guid}`.
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

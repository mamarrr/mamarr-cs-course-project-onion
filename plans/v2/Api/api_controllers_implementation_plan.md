# API Controllers Implementation Plan for Vue Frontend

## Goal

Implement a Vue-ready REST API over the currently implemented onboarding and Portal workflows by adding new API controllers under:

```text
WebApp/ApiControllers
```

and public API DTOs under:

```text
App.DTO
```

Mappers should map between public API DTOs and BLL DTOs/models. Use `IBaseMapper<ApiDto, BllDto>` wherever the mapping is naturally two-way, especially for command/save DTOs such as `TicketBllDto`, `LeaseBllDto`, `CustomerBllDto`, `PropertyBllDto`, `UnitBllDto`, `ResidentBllDto`, `VendorBllDto`, `ScheduledWorkBllDto`, and `WorkLogBllDto`.

Do not implement API endpoints for placeholder/shell-only MVC workflows such as Resident Access onboarding, customer tickets/residents shells, property tickets/residents shells, unit tickets shell, resident tickets/representations shell, and disabled management-level properties.

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

### App.DTO reference issue

`App.DTO` currently has no project references. Since mappers must map to BLL DTOs, there are two viable options:

#### Recommended option

Put DTOs in `App.DTO`, but put API mapper implementations in `WebApp/Mappers/Api`.

Reason: `WebApp` already references both `App.DTO` and `App.BLL.Contracts`, so mappers can see both API DTOs and BLL DTOs without coupling the public DTO project back to the BLL layer.

Suggested folders:

```text
App.DTO/
  Auth/
  Onboarding/
  Workspace/
  Portal/
    Companies/
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

WebApp/
  ApiControllers/
  Mappers/
    Api/
```

#### Alternative option

Put both DTOs and mappers in `App.DTO/Mappers`, but then add a project reference:

```xml
<ProjectReference Include="..\App.BLL.Contracts\App.BLL.Contracts.csproj" />
```

This is acceptable if the project convention is that `App.DTO` is an application-bound API contract project, but it creates a stronger dependency from the API DTO layer to BLL contracts.

The rest of this plan assumes the recommended option: DTOs in `App.DTO`, mappers in `WebApp/Mappers/Api`.

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
  - `ValidationProblemDetails ToValidationProblem(...)`
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

### `App.DTO/Common`

```text
App.DTO/Common/ApiErrorDto.cs
App.DTO/Common/ApiValidationErrorDto.cs
App.DTO/Common/OptionDto.cs
App.DTO/Common/PagedResultDto.cs
App.DTO/Common/CommandResultDto.cs
```

Suggested shapes:

```csharp
public sealed record ApiErrorDto(string Code, string Message);

public sealed record ApiValidationErrorDto(
    string? PropertyName,
    string ErrorMessage);

public sealed record OptionDto(
    Guid Id,
    string Label,
    string? Code = null);

public sealed record CommandResultDto(
    bool Success,
    string? Message = null);
```

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
App.DTO/Auth/RegisterRequestDto.cs
App.DTO/Auth/LoginRequestDto.cs
App.DTO/Auth/RefreshTokenRequestDto.cs
App.DTO/Auth/LogoutRequestDto.cs
App.DTO/Auth/TokenResponseDto.cs
App.DTO/Auth/UserDto.cs
```

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
App.DTO/Onboarding/OnboardingStatusDto.cs
App.DTO/Onboarding/CreateManagementCompanyDto.cs
App.DTO/Onboarding/CreatedManagementCompanyDto.cs
App.DTO/Onboarding/JoinManagementCompanyRequestDto.cs
App.DTO/Onboarding/JoinRequestResultDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Onboarding/ManagementCompanyApiMapper.cs
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
   - Return `OptionDto[]`.
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
App.DTO/Workspace/WorkspaceCatalogDto.cs
App.DTO/Workspace/WorkspaceOptionDto.cs
App.DTO/Workspace/WorkspaceRedirectDto.cs
App.DTO/Workspace/SelectWorkspaceDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Workspace/WorkspaceCatalogApiMapper.cs
WebApp/Mappers/Api/Workspace/WorkspaceOptionApiMapper.cs
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

## Phase 4 — Management Company API

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
App.DTO/Portal/Companies/ManagementCompanySummaryDto.cs
App.DTO/Portal/Companies/ManagementCompanyProfileDto.cs
App.DTO/Portal/Companies/UpdateManagementCompanyProfileDto.cs
App.DTO/Portal/Companies/DeleteManagementCompanyDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Companies/ManagementCompanyProfileApiMapper.cs
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

## Phase 5 — Customers API

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
App.DTO/Portal/Customers/CustomerListItemDto.cs
App.DTO/Portal/Customers/CreateCustomerDto.cs
App.DTO/Portal/Customers/CustomerProfileDto.cs
App.DTO/Portal/Customers/UpdateCustomerProfileDto.cs
App.DTO/Portal/Customers/DeleteCustomerDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Customers/CustomerApiMapper.cs
WebApp/Mappers/Api/Portal/Customers/CustomerListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Customers/CustomerProfileApiMapper.cs
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

## Phase 6 — Properties API

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
App.DTO/Portal/Properties/PropertyListItemDto.cs
App.DTO/Portal/Properties/CreatePropertyDto.cs
App.DTO/Portal/Properties/PropertyProfileDto.cs
App.DTO/Portal/Properties/UpdatePropertyProfileDto.cs
App.DTO/Portal/Properties/DeletePropertyDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Properties/PropertyApiMapper.cs
WebApp/Mappers/Api/Portal/Properties/PropertyListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Properties/PropertyProfileApiMapper.cs
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

## Phase 7 — Units API

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
App.DTO/Portal/Units/UnitListItemDto.cs
App.DTO/Portal/Units/CreateUnitDto.cs
App.DTO/Portal/Units/UnitProfileDto.cs
App.DTO/Portal/Units/UpdateUnitProfileDto.cs
App.DTO/Portal/Units/DeleteUnitDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Units/UnitApiMapper.cs
WebApp/Mappers/Api/Portal/Units/UnitListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Units/UnitProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateUnitDto, UnitBllDto>
IBaseMapper<UpdateUnitProfileDto, UnitBllDto>
```

---

## Phase 8 — Residents API

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
App.DTO/Portal/Residents/ResidentListItemDto.cs
App.DTO/Portal/Residents/CreateResidentDto.cs
App.DTO/Portal/Residents/ResidentProfileDto.cs
App.DTO/Portal/Residents/UpdateResidentProfileDto.cs
App.DTO/Portal/Residents/DeleteResidentDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Residents/ResidentApiMapper.cs
WebApp/Mappers/Api/Portal/Residents/ResidentListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Residents/ResidentProfileApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateResidentDto, ResidentBllDto>
IBaseMapper<UpdateResidentProfileDto, ResidentBllDto>
```

---

## Phase 9 — Leases API

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
App.DTO/Portal/Leases/UnitLeaseListItemDto.cs
App.DTO/Portal/Leases/ResidentLeaseListItemDto.cs
App.DTO/Portal/Leases/CreateUnitLeaseDto.cs
App.DTO/Portal/Leases/UpdateUnitLeaseDto.cs
App.DTO/Portal/Leases/CreateResidentLeaseDto.cs
App.DTO/Portal/Leases/UpdateResidentLeaseDto.cs
App.DTO/Portal/Leases/LeaseResidentSearchResultDto.cs
App.DTO/Portal/Leases/LeasePropertySearchResultDto.cs
App.DTO/Portal/Leases/LeaseUnitOptionDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Leases/LeaseApiMapper.cs
WebApp/Mappers/Api/Portal/Leases/UnitLeaseListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Leases/ResidentLeaseListItemApiMapper.cs
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

## Phase 10 — Contacts API

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
App.DTO/Portal/Contacts/ContactDto.cs
App.DTO/Portal/Contacts/CreateContactDto.cs
App.DTO/Portal/Contacts/ResidentContactAssignmentDto.cs
App.DTO/Portal/Contacts/AttachResidentContactDto.cs
App.DTO/Portal/Contacts/CreateResidentContactDto.cs
App.DTO/Portal/Contacts/UpdateResidentContactDto.cs
App.DTO/Portal/Contacts/VendorContactAssignmentDto.cs
App.DTO/Portal/Contacts/AttachVendorContactDto.cs
App.DTO/Portal/Contacts/CreateVendorContactDto.cs
App.DTO/Portal/Contacts/UpdateVendorContactDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Contacts/ContactApiMapper.cs
WebApp/Mappers/Api/Portal/Contacts/ResidentContactApiMapper.cs
WebApp/Mappers/Api/Portal/Contacts/VendorContactApiMapper.cs
```

Use where possible:

```csharp
IBaseMapper<CreateContactDto, ContactBllDto>
IBaseMapper<AttachResidentContactDto, ResidentContactBllDto>
IBaseMapper<UpdateResidentContactDto, ResidentContactBllDto>
IBaseMapper<AttachVendorContactDto, VendorContactBllDto>
IBaseMapper<UpdateVendorContactDto, VendorContactBllDto>
```

---

## Phase 11 — Tickets API

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

GET    /api/v1/portal/companies/{companySlug}/tickets/options
GET    /api/v1/portal/companies/{companySlug}/tickets/options/properties?customerId=
GET    /api/v1/portal/companies/{companySlug}/tickets/options/units?propertyId=
GET    /api/v1/portal/companies/{companySlug}/tickets/options/residents?unitId=
GET    /api/v1/portal/companies/{companySlug}/tickets/options/vendors?categoryId=
```

### DTOs

```text
App.DTO/Portal/Tickets/TicketFilterDto.cs
App.DTO/Portal/Tickets/TicketListItemDto.cs
App.DTO/Portal/Tickets/TicketDetailsDto.cs
App.DTO/Portal/Tickets/TicketDto.cs
App.DTO/Portal/Tickets/CreateTicketDto.cs
App.DTO/Portal/Tickets/UpdateTicketDto.cs
App.DTO/Portal/Tickets/TicketOptionsDto.cs
App.DTO/Portal/Tickets/TicketOptionDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Tickets/TicketApiMapper.cs
WebApp/Mappers/Api/Portal/Tickets/TicketListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Tickets/TicketDetailsApiMapper.cs
WebApp/Mappers/Api/Portal/Tickets/TicketOptionsApiMapper.cs
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
2. Create:
   - Map `CreateTicketDto` -> `TicketBllDto`.
   - Call `_bll.Tickets.CreateAsync(...)`.
   - Return `201 Created`.
3. Details:
   - Call `_bll.Tickets.GetDetailsAsync(...)`.
4. Update:
   - Map `UpdateTicketDto` -> `TicketBllDto`.
   - Call `_bll.Tickets.UpdateAsync(...)`.
5. Delete:
   - Call `_bll.Tickets.DeleteAsync(...)`.
6. Advance status:
   - Call `_bll.Tickets.AdvanceStatusAsync(...)`.
7. Selector options:
   - Reuse `GetSelectorOptionsAsync(...)`.

---

## Phase 12 — Scheduled Work API

### Controller

```text
WebApp/ApiControllers/Portal/ScheduledWorkController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/start
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/complete
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/cancel
```

### DTOs

```text
App.DTO/Portal/ScheduledWork/ScheduledWorkListItemDto.cs
App.DTO/Portal/ScheduledWork/ScheduledWorkDetailsDto.cs
App.DTO/Portal/ScheduledWork/ScheduledWorkDto.cs
App.DTO/Portal/ScheduledWork/CreateScheduledWorkDto.cs
App.DTO/Portal/ScheduledWork/UpdateScheduledWorkDto.cs
App.DTO/Portal/ScheduledWork/ScheduledWorkActionDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/ScheduledWork/ScheduledWorkApiMapper.cs
WebApp/Mappers/Api/Portal/ScheduledWork/ScheduledWorkListItemApiMapper.cs
WebApp/Mappers/Api/Portal/ScheduledWork/ScheduledWorkDetailsApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateScheduledWorkDto, ScheduledWorkBllDto>
IBaseMapper<UpdateScheduledWorkDto, ScheduledWorkBllDto>
```

---

## Phase 13 — Work Logs API

### Controller

```text
WebApp/ApiControllers/Portal/WorkLogsController.cs
```

Routes:

```text
GET    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs
POST   /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs
PUT    /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/{workLogId}
DELETE /api/v1/portal/companies/{companySlug}/tickets/{ticketId}/scheduled-work/{scheduledWorkId}/work-logs/{workLogId}
```

### DTOs

```text
App.DTO/Portal/WorkLogs/WorkLogListItemDto.cs
App.DTO/Portal/WorkLogs/WorkLogTotalsDto.cs
App.DTO/Portal/WorkLogs/WorkLogDto.cs
App.DTO/Portal/WorkLogs/CreateWorkLogDto.cs
App.DTO/Portal/WorkLogs/UpdateWorkLogDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/WorkLogs/WorkLogApiMapper.cs
WebApp/Mappers/Api/Portal/WorkLogs/WorkLogListItemApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateWorkLogDto, WorkLogBllDto>
IBaseMapper<UpdateWorkLogDto, WorkLogBllDto>
```

---

## Phase 14 — Vendors API

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
App.DTO/Portal/Vendors/VendorListItemDto.cs
App.DTO/Portal/Vendors/VendorProfileDto.cs
App.DTO/Portal/Vendors/VendorDto.cs
App.DTO/Portal/Vendors/CreateVendorDto.cs
App.DTO/Portal/Vendors/UpdateVendorDto.cs
App.DTO/Portal/Vendors/DeleteVendorDto.cs
App.DTO/Portal/Vendors/VendorCategoryAssignmentDto.cs
App.DTO/Portal/Vendors/AssignVendorCategoryDto.cs
App.DTO/Portal/Vendors/UpdateVendorCategoryDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Vendors/VendorApiMapper.cs
WebApp/Mappers/Api/Portal/Vendors/VendorListItemApiMapper.cs
WebApp/Mappers/Api/Portal/Vendors/VendorProfileApiMapper.cs
WebApp/Mappers/Api/Portal/Vendors/VendorCategoryApiMapper.cs
```

Use:

```csharp
IBaseMapper<CreateVendorDto, VendorBllDto>
IBaseMapper<UpdateVendorDto, VendorBllDto>
IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>
IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>
```

---

## Phase 15 — Company Users API

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
App.DTO/Portal/Users/CompanyUserListItemDto.cs
App.DTO/Portal/Users/AddCompanyUserDto.cs
App.DTO/Portal/Users/UpdateCompanyUserDto.cs
App.DTO/Portal/Users/CompanyUserEditDto.cs
App.DTO/Portal/Users/PendingAccessRequestDto.cs
App.DTO/Portal/Users/TransferOwnershipDto.cs
App.DTO/Portal/Users/OwnershipTransferCandidateDto.cs
App.DTO/Portal/Users/CompanyUsersPageDto.cs
```

### Mappers

```text
WebApp/Mappers/Api/Portal/Users/CompanyUserApiMapper.cs
WebApp/Mappers/Api/Portal/Users/PendingAccessRequestApiMapper.cs
WebApp/Mappers/Api/Portal/Users/OwnershipTransferApiMapper.cs
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

## Phase 16 — Lookups API

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
App.DTO/Common/OptionDto.cs
```

### Implementation tasks

1. Expose property type options used by create property.
2. Expose lease roles used by unit/resident lease workflows.
3. Expose ticket statuses/priorities/categories/customers/properties/units/residents/vendors.
4. Expose vendor ticket category options where needed.

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
- Convert `ValidationAppError` failures to `ValidationProblemDetails`.
- Preserve business error message.
- Avoid leaking stack traces.

### Recommended response formats

Validation error:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "name": ["Name is required"]
  }
}
```

Conflict:

```json
{
  "code": "conflict",
  "message": "Ticket number already exists."
}
```

Unauthorized:

```json
{
  "code": "unauthorized",
  "message": "Authentication is required."
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

## Testing Plan

### Unit tests

Create mapper tests:

```text
Tests/App.DTO.Tests or Tests/WebApp.Tests/Mappers
```

Test:

- API DTO -> BLL DTO mapping.
- BLL DTO/model -> API DTO mapping.
- Null handling.
- DateOnly/DateTime conversion for lease dates.
- Guid handling.

### Integration tests

Create API integration tests for:

1. Auth:
   - Register.
   - Login.
   - Refresh.
   - Logout.
2. Onboarding:
   - Create management company.
   - Join request submission.
3. Portal:
   - Customer create/list.
   - Property create/list.
   - Unit create/list.
   - Resident create/list.
   - Ticket create/update/status advance.
   - Scheduled work lifecycle.
   - Work log lifecycle.
   - Vendor create/category/contact flows.
   - Company user approval flow.

### Authorization tests

For every scoped route:

- No token -> `401`.
- Token without company access -> `403` or `404`, depending on current BLL behavior.
- Valid token with access -> success.
- Cross-company slug access must fail.

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

1. Companies/profile.
2. Customers.
3. Properties.
4. Units.
5. Residents.

### Milestone 4 — Leases and contacts

1. Leases API.
2. Resident contacts.
3. Vendor contacts.

### Milestone 5 — Tickets and maintenance

1. Tickets API.
2. Scheduled work API.
3. Work logs API.

### Milestone 6 — Vendors and users

1. Vendors API.
2. Vendor categories.
3. Company users and access requests.

### Milestone 7 — polish

1. Lookups controller.
2. Swagger response metadata.
3. Integration tests.
4. Frontend path response consistency.
5. CORS production tightening.

---

## Do Not Implement Yet

Do not build API controllers for these until the underlying BLL/MVC workflow is implemented beyond a placeholder or page shell:

```text
Resident access onboarding
Management-level properties
Customer-scoped tickets
Customer-scoped residents
Property-scoped residents
Property-scoped tickets
Unit-scoped tickets
Resident-scoped tickets
Resident representations
```

---

## Final Target Folder Layout

```text
App.DTO/
  Common/
  Auth/
  Onboarding/
  Workspace/
  Portal/
    Companies/
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
  Mappers/
    Api/
      Auth/
      Onboarding/
      Workspace/
      Portal/
        Companies/
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
- Vue can perform all implemented Portal workflows through JSON APIs:
  - company profile
  - customers
  - properties
  - units
  - residents
  - leases
  - contacts
  - tickets
  - scheduled work
  - work logs
  - vendors
  - vendor categories
  - company users/access requests
- All public API contracts live in `App.DTO`.
- Mapping logic is separated from controllers.
- `IBaseMapper<ApiDto, BllDto>` is used where mapping is two-way and canonical BLL DTOs are available.
- Controllers return DTOs, never MVC view models.
- Controllers return status codes and validation errors suitable for a SPA.
- Placeholder/shell workflows are not exposed as real APIs.

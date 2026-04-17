# BLL Service Map (Current Repository Structure)

## Scope

This document describes the **current** BLL layer structure as it exists in the Git repository.  
It does **not** use the proposed remodel/refactor names. It follows the current folders and current file/service names.

For each service or service family, this document states:

- which **workspace** it works in,
- what **actions / responsibilities** it performs,
- which **MVC controllers** use it directly or through one of its interface slices.

---

## Workspace labels used in this document

- **Root / Shared** = top-level MVC flow or shared app infrastructure
- **Onboarding** = login, register, create/join company, context switching
- **Management** = `Areas/Management`
- **Customer** = `Areas/Customer`
- **Property** = `Areas/Property`
- **Resident** = `Areas/Resident`
- **Unit** = `Areas/Unit`
- **Shared Layout** = layout/context-building services used across workspaces

---

## High-level structure in `App.BLL`

- `App.BLL/Management`
- `App.BLL/Onboarding`
- `App.BLL/Routing`

Inside `Management`, the current folders are:

- `Access`
- `Common`
- `Customers`
- `Leases`
- `Profiles`
- `Properties`
- `Residents`
- `Units`
- `Users`

There are also root-level composite files in `App.BLL/Management`, especially:

- `IManagementCustomersService.cs`
- `IManagementUserAdminService.cs`
- `ManagementCustomersModels.cs`
- `ManagementCustomersService.cs`
- `ManagementUserAdminModels.cs`
- `ManagementUserAdminService.cs`

---

# 1. `App.BLL/Onboarding`

## 1.1 `IOnboardingService` / `OnboardingService`

**Files**
- `App.BLL/Onboarding/IOnboardingService.cs`
- `App.BLL/Onboarding/OnboardingService.cs`
- `App.BLL/Onboarding/OnboardingLoginRequest.cs`
- `App.BLL/Onboarding/OnboardingLoginResult.cs`
- `App.BLL/Onboarding/OnboardingRegisterRequest.cs`
- `App.BLL/Onboarding/OnboardingRegisterResult.cs`
- `App.BLL/Onboarding/OnboardingCreateManagementCompanyRequest.cs`
- `App.BLL/Onboarding/OnboardingCreateManagementCompanyResult.cs`

**Workspace**
- Onboarding
- Root / Shared

**What it does**
- Registers new users
- Logs users in
- Logs users out
- Checks whether a user has any usable workspace context
- Creates a management company during onboarding
- Returns the default management company slug for a user
- Checks whether a user already has access to a management company

**Why it exists**
- It is the main account/bootstrap workflow service for the application.
- It combines Identity work with domain setup work.

**Controllers using it**
- `WebApp/Controllers/OnboardingController`

---

## 1.2 `IOnboardingContextService` / `OnboardingContextService`

**Files**
- `App.BLL/Onboarding/IOnboardingContextService.cs`
- `App.BLL/Onboarding/OnboardingContextService.cs`
- `App.BLL/Onboarding/OnboardingContextModels.cs`

**Workspace**
- Onboarding
- Root / Shared

**What it does**
- Resolves where a signed-in user should be redirected based on current context
- Interprets the context-selection cookie
- Validates selected context targets
- Chooses between management, customer, and resident destinations
- Falls back to the best available context if the selected one is invalid

**Why it exists**
- The redirect/context-selection rules are business logic, not controller glue.

**Controllers using it**
- `WebApp/Controllers/OnboardingController`

---

## 1.3 `IManagementCompanyJoinRequestService` / `ManagementCompanyJoinRequestService`

**Files**
- `App.BLL/Onboarding/IManagementCompanyJoinRequestService.cs`
- `App.BLL/Onboarding/ManagementCompanyJoinRequestService.cs`
- `App.BLL/Onboarding/ManagementCompanyJoinRequestModels.cs`

**Workspace**
- Onboarding
- Management

**What it does**
- Creates “join management company” requests
- Lists pending join requests for a management company
- Approves join requests
- Rejects join requests
- Validates registry-code-based joins and avoids duplicate/pending duplicates

**Why it exists**
- Joining a company is a workflow with validation and moderation, not just a table insert.

**Controllers using it**
- Directly: `WebApp/Controllers/OnboardingController`
- Indirectly through `ManagementUserAdminService`: `WebApp/Areas/Management/Controllers/UsersController`

---

## 1.4 `IUserContextCatalogService` / `UserContextCatalogService`

**Files**
- `App.BLL/Onboarding/IUserContextCatalogService.cs`
- `App.BLL/Onboarding/UserContextCatalogService.cs`
- `App.BLL/Onboarding/UserContextCatalogModels.cs`

**Workspace**
- Shared Layout
- Cross-workspace

**What it does**
- Builds the catalog of contexts available to the current user
- Returns management-company contexts
- Returns customer contexts
- Indicates whether the user has a resident context
- Supports context switching in the shared layouts

**Why it exists**
- Layout/context switching should come from one consistent source of truth.

**Controllers using it**
- No MVC controller calls it directly
- It is used by:
  - `WebApp/Services/SharedLayout/WorkspaceLayoutContextProvider.cs`
  - `WebApp/Services/ManagementLayout/ManagementLayoutViewModelProvider.cs`
- Indirectly affects all workspace controllers that build page shells

---

## 1.5 `ApiOnboardingContextService`

**Files**
- `App.BLL/Onboarding/ApiOnboardingContextService.cs`

**Workspace**
- API / onboarding support
- Not central to the current MVC flow

**What it does**
- API-oriented onboarding/context support

**Controllers using it**
- No direct MVC controller usage in the current MVC request flows reviewed

---

# 2. `App.BLL/Management`

## 2.1 Root composite service family: `IManagementUserAdminService` / `ManagementUserAdminService`

**Files**
- `App.BLL/Management/IManagementUserAdminService.cs`
- `App.BLL/Management/ManagementUserAdminService.cs`
- `App.BLL/Management/ManagementUserAdminModels.cs`

**Related interface slices**
- `App.BLL/Management/Access/IManagementAccessService.cs`
- `App.BLL/Management/Users/IManagementAccessRequestService.cs`
- `App.BLL/Management/Users/IManagementOwnershipTransferService.cs`
- `App.BLL/Management/Users/IManagementUserAuthorizationService.cs`
- `App.BLL/Management/Users/IManagementUserCommandService.cs`
- `App.BLL/Management/Users/IManagementUserQueryService.cs`
- `App.BLL/Management/Users/IManagementUserRoleService.cs`

**Workspace**
- Management
- Management company administration

**What it does**
- Authorizes access to the management area
- Authorizes admin-level management-company user operations
- Lists management-company members
- Loads membership state for edit screens
- Returns available role options
- Adds users to a company by email
- Updates memberships
- Deletes memberships
- Transfers ownership
- Lists pending access requests
- Approves/rejects pending access requests

**Why it exists**
- This is the management-company membership/admin workflow service.
- It contains most of the rules around who may manage company users.

**Controllers using it**
- `WebApp/Areas/Management/Controllers/DashboardController`
- `WebApp/Areas/Management/Controllers/UsersController`

**Indirect service usage**
- `App.BLL/Management/Profiles/ManagementCompanyProfileService.cs` uses it for management-area authorization before company profile update/delete

---

## 2.2 Root composite service family: `IManagementCustomersService` / `ManagementCustomersService`

**Files**
- `App.BLL/Management/IManagementCustomersService.cs`
- `App.BLL/Management/ManagementCustomersService.cs`
- `App.BLL/Management/ManagementCustomersModels.cs`

**Related interface slices**
- `App.BLL/Management/Customers/IManagementCustomerAccessService.cs`
- `App.BLL/Management/Customers/IManagementCustomerService.cs`
- `App.BLL/Management/Properties/IManagementCustomerPropertyService.cs`

**Workspace**
- Management
- Customer
- Property

**What it does**
- Authorizes access to company/customer scopes
- Resolves customer dashboard context
- Lists customers for a management company
- Creates customers
- Lists properties for a customer
- Creates properties
- Resolves property dashboard context

**Why it exists**
- This is currently a composite service covering the whole customer → property branch.

**Controllers using it**
- `WebApp/Areas/Management/Controllers/CustomersController`
- `WebApp/Areas/Customer/Controllers/CustomerDashboardController`
- `WebApp/Areas/Customer/Controllers/CustomerPropertiesController`
- `WebApp/Areas/Property/Controllers/PropertyDashboardController`

**Likely additional interface-slice consumers**
- Customer/property profile and nested property pages rely on the same customer/property context chain even when the final mutation is done by profile/unit services

---

## 2.3 `Management/Customers/IManagementCustomerAccessService`

**File**
- `App.BLL/Management/Customers/IManagementCustomerAccessService.cs`

**Workspace**
- Customer

**What it does**
- Company → customer access authorization
- Customer dashboard context resolution

**Controllers using it**
- `WebApp/Areas/Customer/Controllers/CustomerDashboardController`
- `WebApp/Areas/Customer/Controllers/CustomerPropertiesController`
- customer-scoped profile pages that need customer route context first

**Implementation**
- Implemented by `ManagementCustomersService`

---

## 2.4 `Management/Customers/IManagementCustomerService`

**File**
- `App.BLL/Management/Customers/IManagementCustomerService.cs`

**Workspace**
- Management

**What it does**
- Customer list/create operations for the management-company workspace

**Controllers using it**
- `WebApp/Areas/Management/Controllers/CustomersController`

**Implementation**
- Implemented by `ManagementCustomersService`

---

## 2.5 `Management/Properties/IManagementCustomerPropertyService`

**File**
- `App.BLL/Management/Properties/IManagementCustomerPropertyService.cs`

**Workspace**
- Customer
- Property

**What it does**
- Lists properties under a customer
- Creates properties under a customer
- Resolves property dashboard context

**Controllers using it**
- `WebApp/Areas/Customer/Controllers/CustomerPropertiesController`
- `WebApp/Areas/Property/Controllers/PropertyDashboardController`
- nested property pages that need property route context

**Implementation**
- Implemented by `ManagementCustomersService`

---

## 2.6 `Management/Residents/IManagementResidentAccessService` / `ManagementResidentAccessService`

**Files**
- `App.BLL/Management/Residents/IManagementResidentAccessService.cs`
- `App.BLL/Management/Residents/ManagementResidentAccessService.cs`

**Workspace**
- Resident

**What it does**
- Authorizes resident-area access inside a management company
- Resolves resident dashboard context by resident ID code

**Why it exists**
- Resident routes are a different scope from customer/property/unit routes.

**Controllers using it**
- `WebApp/Areas/Resident/Controllers/ResidentDashboardController`
- `WebApp/Areas/Resident/Controllers/ResidentProfileController`
- `WebApp/Areas/Resident/Controllers/ResidentUnitsController`

---

## 2.7 `Management/Residents/IManagementResidentService` / `ManagementResidentService`

**Files**
- `App.BLL/Management/Residents/IManagementResidentService.cs`
- `App.BLL/Management/Residents/ManagementResidentService.cs`
- `App.BLL/Management/Residents/ManagementResidentModels.cs`

**Workspace**
- Management

**What it does**
- Lists residents for a management company
- Creates residents in the management-company workspace

**Why it exists**
- This is the roster/list-create side of resident management, not resident route authorization.

**Controllers using it**
- `WebApp/Areas/Management/Controllers/ResidentsController`

---

## 2.8 `Management/Units/IManagementPropertyUnitService` / `IManagementUnitDashboardService` / `ManagementPropertyUnitService`

**Files**
- `App.BLL/Management/Units/IManagementPropertyUnitService.cs`
- `App.BLL/Management/Units/IManagementUnitDashboardService.cs`
- `App.BLL/Management/Units/ManagementPropertyUnitService.cs`
- `App.BLL/Management/Units/ManagementPropertyUnitModels.cs`

**Workspace**
- Property
- Unit

**What it does**
- Lists units under a property
- Creates units under a property
- Resolves unit dashboard context

**Why it exists**
- This is the property → unit branch service family.

**Controllers using it**
- `WebApp/Areas/Property/Controllers/PropertyUnitsController`
- `WebApp/Areas/Unit/Controllers/UnitDashboardController`
- `WebApp/Areas/Unit/Controllers/UnitProfileController`
- `WebApp/Areas/Unit/Controllers/UnitTenantsController`

---

## 2.9 `Management/Leases/IManagementLeaseSearchService` / `ManagementLeaseSearchService`

**Files**
- `App.BLL/Management/Leases/IManagementLeaseSearchService.cs`
- `App.BLL/Management/Leases/ManagementLeaseSearchService.cs`

**Related models**
- `App.BLL/Management/Leases/ManagementLeaseModels.cs`

**Workspace**
- Resident
- Unit

**What it does**
- Provides lookup/search data for lease-management UI
- Searches properties for resident-side lease flows
- Lists units for a chosen property
- Searches residents for unit-side lease flows
- Lists lease roles/options

**Why it exists**
- It supports the form/UI side of lease assignment flows.

**Controllers using it**
- `WebApp/Areas/Resident/Controllers/ResidentUnitsController`
- `WebApp/Areas/Unit/Controllers/UnitTenantsController`

---

## 2.10 `Management/Leases/IManagementLeaseService` / `ManagementLeaseService`

**Files**
- `App.BLL/Management/Leases/IManagementLeaseService.cs`
- `App.BLL/Management/Leases/ManagementLeaseService.cs`
- `App.BLL/Management/Leases/ManagementLeaseModels.cs`

**Workspace**
- Resident
- Unit

**What it does**
- Lists leases for a resident
- Lists leases for a unit
- Gets one lease for edit/view
- Creates leases from the resident side
- Creates leases from the unit side
- Updates leases from the resident side
- Updates leases from the unit side
- Deletes leases from the resident side
- Deletes leases from the unit side

**Why it exists**
- This is the actual lease-assignment workflow service.

**Controllers using it**
- `WebApp/Areas/Resident/Controllers/ResidentUnitsController`
- `WebApp/Areas/Unit/Controllers/UnitTenantsController`

---

# 3. `App.BLL/Management/Profiles`

## 3.1 `IManagementCompanyProfileService` / `ManagementCompanyProfileService`

**Files**
- `App.BLL/Management/Profiles/IManagementCompanyProfileService.cs`
- `App.BLL/Management/Profiles/ManagementCompanyProfileService.cs`

**Workspace**
- Management

**What it does**
- Gets management-company profile data
- Updates management-company profile
- Deletes the management company
- Handles the heaviest tenant-level delete/cascade workflow in the BLL

**Controllers using it**
- `WebApp/Areas/Management/Controllers/ManagementProfileController`

**Indirect dependencies**
- Uses `ManagementUserAdminService` authorization logic
- Uses delete helpers/orchestration

---

## 3.2 `IManagementCustomerProfileService` / `ManagementCustomerProfileService`

**Files**
- `App.BLL/Management/Profiles/IManagementCustomerProfileService.cs`
- `App.BLL/Management/Profiles/ManagementCustomerProfileService.cs`

**Workspace**
- Customer

**What it does**
- Gets customer profile data
- Updates customer profile
- Deletes a customer and cascades related data

**Controllers using it**
- `WebApp/Areas/Customer/Controllers/CustomerProfileController`

---

## 3.3 `IManagementPropertyProfileService` / `ManagementPropertyProfileService`

**Files**
- `App.BLL/Management/Profiles/IManagementPropertyProfileService.cs`
- `App.BLL/Management/Profiles/ManagementPropertyProfileService.cs`

**Workspace**
- Property

**What it does**
- Gets property profile data
- Updates property profile
- Deletes a property and cascades related data

**Controllers using it**
- `WebApp/Areas/Property/Controllers/PropertyProfileController`

---

## 3.4 `IManagementResidentProfileService` / `ManagementResidentProfileService`

**Files**
- `App.BLL/Management/Profiles/IManagementResidentProfileService.cs`
- `App.BLL/Management/Profiles/ManagementResidentProfileService.cs`

**Workspace**
- Resident

**What it does**
- Gets resident profile data
- Updates resident profile
- Deletes a resident and cascades related data

**Controllers using it**
- `WebApp/Areas/Resident/Controllers/ResidentProfileController`

---

## 3.5 `IManagementUnitProfileService` / `ManagementUnitProfileService`

**Files**
- `App.BLL/Management/Profiles/IManagementUnitProfileService.cs`
- `App.BLL/Management/Profiles/ManagementUnitProfileService.cs`

**Workspace**
- Unit

**What it does**
- Gets unit profile data
- Updates unit profile
- Deletes a unit and cascades related data

**Controllers using it**
- `WebApp/Areas/Unit/Controllers/UnitProfileController`

---

## 3.6 Shared profile support files

### `ManagementProfileDeleteAuthorization`

**File**
- `App.BLL/Management/Profiles/ManagementProfileDeleteAuthorization.cs`

**Workspace**
- Shared profile delete support

**What it does**
- Centralizes delete permission checks used by profile delete flows

**Controllers using it**
- No direct controller usage
- Used inside profile services

---

### `ManagementProfileDeleteOrchestrator`

**File**
- `App.BLL/Management/Profiles/ManagementProfileDeleteOrchestrator.cs`

**Workspace**
- Shared profile delete support

**What it does**
- Centralizes shared delete orchestration, especially ticket/contact cleanup

**Controllers using it**
- No direct controller usage
- Used inside profile services

---

### `ManagementProfileModels`

**File**
- `App.BLL/Management/Profiles/ManagementProfileModels.cs`

**Workspace**
- Shared profile support

**What it does**
- Shared models used by profile-service flows

**Controllers using it**
- No direct controller usage
- Used by profile services

---

# 4. Other supporting files in `App.BLL/Management`

## 4.1 `Management/Common/ManagementMembershipPolicy`

**File**
- `App.BLL/Management/Common/ManagementMembershipPolicy.cs`

**Workspace**
- Management/shared authorization support

**What it does**
- Shared membership-policy definitions used by management authorization logic

**Controllers using it**
- No direct controller usage
- Used by BLL services that need role/membership rules

---

## 4.2 `Management/Access/IManagementAccessService`

**File**
- `App.BLL/Management/Access/IManagementAccessService.cs`

**Workspace**
- Management

**What it does**
- Interface slice for management-area access/authorization

**Controllers using it**
- Management-area entry points through the `ManagementUserAdminService` family

**Implementation**
- Functionally belongs to the `ManagementUserAdminService` family

---

# 5. `App.BLL/Routing`

## 5.1 `SlugGenerator`

**File**
- `App.BLL/Routing/SlugGenerator.cs`

**Workspace**
- Shared / cross-workspace

**What it does**
- Generates URL-safe slugs
- Ensures slug uniqueness where create flows need it

**Controllers using it**
- No direct controller usage
- Used inside BLL create flows such as customer/property creation

---

# 6. Controller-to-service family summary

This is the quickest “which controller uses which service family?” overview.

## Root / Shared controllers

### `WebApp/Controllers/OnboardingController`
Uses:
- `OnboardingService`
- `OnboardingContextService`
- `ManagementCompanyJoinRequestService`

### Shared layout providers (not controllers, but important)
Use:
- `UserContextCatalogService`

---

## Management workspace controllers

### `DashboardController`
Uses:
- `ManagementUserAdminService` family for management-area authorization

### `UsersController`
Uses:
- `ManagementUserAdminService` family
- indirectly the join-request workflow via `ManagementCompanyJoinRequestService`

### `CustomersController`
Uses:
- `ManagementCustomersService` family through `IManagementCustomerService`

### `ResidentsController`
Uses:
- `ManagementResidentService`

### `ManagementProfileController`
Uses:
- `ManagementCompanyProfileService`

---

## Customer workspace controllers

### `CustomerDashboardController`
Uses:
- `ManagementCustomersService` family through customer access/context resolution

### `CustomerPropertiesController`
Uses:
- `ManagementCustomersService` family through customer/property operations

### `CustomerProfileController`
Uses:
- `ManagementCustomerProfileService`
- plus customer-scope access/context chain

---

## Property workspace controllers

### `PropertyDashboardController`
Uses:
- `ManagementCustomersService` family through property context resolution

### `PropertyUnitsController`
Uses:
- `ManagementPropertyUnitService`

### `PropertyProfileController`
Uses:
- `ManagementPropertyProfileService`
- plus property-scope access/context chain

---

## Resident workspace controllers

### `ResidentDashboardController`
Uses:
- `ManagementResidentAccessService`

### `ResidentProfileController`
Uses:
- `ManagementResidentAccessService`
- `ManagementResidentProfileService`

### `ResidentUnitsController`
Uses:
- `ManagementResidentAccessService`
- `ManagementLeaseSearchService`
- `ManagementLeaseService`

---

## Unit workspace controllers

### `UnitDashboardController`
Uses:
- `ManagementPropertyUnitService` through unit-context resolution

### `UnitProfileController`
Uses:
- `ManagementPropertyUnitService` for unit context
- `ManagementUnitProfileService`

### `UnitTenantsController`
Uses:
- `ManagementPropertyUnitService` for unit context
- `ManagementLeaseSearchService`
- `ManagementLeaseService`

---

# 7. Biggest service families in the current BLL

If someone is trying to understand the current codebase quickly, these are the most important service families to read first:

1. `OnboardingService`
2. `OnboardingContextService`
3. `UserContextCatalogService`
4. `ManagementUserAdminService`
5. `ManagementCustomersService`
6. `ManagementResidentAccessService`
7. `ManagementPropertyUnitService`
8. `ManagementLeaseSearchService`
9. `ManagementLeaseService`
10. the five profile services

These services explain most of the non-admin MVC behavior in the application.

---

# 8. Notes about the current structure

- The current structure is **not purely by workspace**.
- Some services are **composite** and span multiple workspaces:
  - `ManagementCustomersService` spans Management + Customer + Property
  - `ManagementPropertyUnitService` spans Property + Unit
  - `ManagementUserAdminService` spans Management authorization + membership/admin flows
- The profile services are the cleanest part of the current organization because they map closely to profile controllers and profile pages.

That is why the current BLL can feel harder to read than the controller structure.

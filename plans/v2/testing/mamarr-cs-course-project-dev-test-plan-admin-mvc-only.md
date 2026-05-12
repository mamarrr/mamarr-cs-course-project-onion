# Test coverage plan for `mamarrr/mamarr-cs-course-project` `dev`

Generated for manual test execution. The goal is to build the same layered style as `akaver-hw-demo`: API pipeline smoke coverage as soon as the host harness exists, then unit tests, DAL/BLL integration, full API integration, and only light Admin MVC/browser safety coverage. Admin MVC should not become the primary place for business-rule coverage.

This is a future testing roadmap only. It does not require application public APIs, contracts, or production behavior to change, and it does not override the current project instruction to defer writing tests.

Testing pyramid guidance:

- Unit tests are fast, isolated tests of individual components under developer control. They must not require database, filesystem, network, web host, or DI infrastructure. Write the largest number of tests at this level when behavior can be checked without real infrastructure.
- Integration tests exercise two or more components together. DAL tests use EF Core + SQLite; BLL workflow tests use the real DI container, `IAppBLL`, UOW, repositories, EF Core, SQLite, and deterministic seed data; MVC/API integration tests use the application host and HTTP pipeline.
- E2E tests exercise the application through UI/browser flows as a real user would. They are slow and brittle; keep them sparse and limited to critical Admin smoke paths.
- Each level up is slower and more expensive to maintain. Do not duplicate every branch at every level. Use BLL/DAL integration where real tenant scoping, EF translation, transactions, route hierarchy, lookup persistence, or UOW behavior is the risk; otherwise prefer unit tests.

## 1. Baseline facts from the current `dev` branch

The solution already contains a `WebApp.Tests` project targeting `net10.0` with xUnit, AwesomeAssertions, Moq, ASP.NET Core MVC testing, EF Core SQLite, Playwright, xUnit runner, and coverlet packages. That is a good base for the layered test suite.

The application is a multi-tenant property maintenance CRM. Core dimensions to cover are:

- Authentication: cookie auth for MVC and JWT auth for API.
- Onboarding: register, login, create management company, join management company, choose workspace context.
- Multi-tenant authorization: management company slug, customer slug, property slug, unit slug, resident id code, and object IDs.
- Portals exist for public onboarding, admin, management, customer, property, unit, and resident. For MVC-controller-level tests, only light Admin MVC safety coverage is in scope. Non-admin workflows should be covered through BLL, DAL, API, and non-MVC layers.
- Core business entities: management companies, memberships, join requests, customers, properties, units, residents, contacts, leases, vendors, ticket categories, tickets, scheduled work, work logs, lookup tables.
- Localization: `LangStr` values persisted as JSON and translated through query-string/cookie culture selection.
- API versioning: `api/v{version:apiVersion}` endpoints.
- Manual test execution: no CI test stage is required.
- SQLite is the only database provider required for this test plan. PostgreSQL, migrations-on-PostgreSQL, and containerized database tests are out of scope.
- No Admin API endpoints will be added or tested by this plan. Admin business rules stay covered through Admin BLL/DAL tests and light Admin MVC integration/E2E coverage.

## 2. Test project structure to add

Create one main test project:

```text
WebApp.Tests/
  CustomWebApplicationFactory.cs
  Helpers/
    TestUsers.cs
    TestTenants.cs
    TestLookupIds.cs
    TestDataSeeder.cs
    JwtTestHelper.cs
    CookieLoginHelper.cs
    PlaywrightLoginHelper.cs
    CultureScope.cs
    TestDbContextFactory.cs
    RouteFactory.cs
    AssertionHelpers.cs
    CultureSensitiveCollection.cs
  Unit/
    Domain/
    Routing/
    Validation/
    Mappers/
      API/
      BLL/
      DAL/
    BLL/
      Services/
    Web/
      Helpers/
      UI/
  Integration/
    DAL/
    BLL/
    API/
    MVC/
      Admin/
  E2E/
    E2ECollection.cs
    PlaywrightWebAppFactory.cs
    Admin/
```

Use these xUnit collections:

```csharp
[CollectionDefinition("CultureSensitive", DisableParallelization = true)]
public class CultureSensitiveCollection {}

[CollectionDefinition("E2E", DisableParallelization = true)]
public class E2ECollection : ICollectionFixture<PlaywrightWebAppFactory> {}
```

The `CultureSensitive` collection is needed for tests that mutate current culture, UI culture, cookies, query-string culture, or `LangStr.DefaultCulture`. The `E2E` collection is needed because Playwright browser-server, cookies, fixed Kestrel port, and shared app state are fragile under parallel execution.

## 3. Test infrastructure needed before business tests

### 3.1 Custom web application factory

Add `CustomWebApplicationFactory : WebApplicationFactory<Program>`.

Required behavior:

- Disable production data initialization:
  - `DataInitialization:DropDatabase=false`
  - `DataInitialization:MigrateDatabase=false`
  - `DataInitialization:SeedIdentity=false`
  - `DataInitialization:SeedData=false`
- Replace the production database provider with SQLite in-memory shared-cache DB.
- Keep the SQLite connection alive for the lifetime of the factory.
- Use `EnsureCreated()` instead of migrations.
- Seed deterministic identity roles, app users, management company roles, lookup rows, management companies, hierarchy data, resident mappings, customer representative context, vendors, tickets, scheduled works, work logs, refresh-token scenarios, and locked-user state before any business/API tests run.
- Configure API versioning so unversioned or default-version requests in tests behave predictably.
- Configure JWT settings with a stable test signing key.
- Keep production-like EF query tracking behavior if the app uses no-tracking identity resolution.
- Expose helpers:
  - `CreateClientNoRedirect()`
  - `CreateAuthenticatedApiClientAsync(user)`
  - `CreateAuthenticatedMvcClientAsync(user)`
  - `CreateDbContext()`
  - `ResetDatabaseAsync()` or per-factory unique DB name.
  - stable IDs, slugs, codes, user IDs, and route helpers through `TestUsers`, `TestTenants`, `TestLookupIds`, and `RouteFactory` so tests do not discover random data ad hoc.

### 3.2 Test seed model

Deterministic seed data is a blocking prerequisite before business, DAL, BLL, API, MVC, or E2E tests are implemented. Seed at least the following deterministic dataset:

#### Users

- `SystemAdmin`
- `CompanyAOwner`
- `CompanyAManager`
- `CompanyASupport`
- `CompanyAFinance`
- `CompanyBOwner`
- `CustomerARepresentative`
- `ResidentA`
- `ResidentB`
- `NoContextUser`
- `LockedUser`

Management-company authorization tests must assert behavior by real role codes:

- `OWNER`
- `MANAGER`
- `SUPPORT`
- `FINANCE`

Do not introduce nonexistent management role labels just for tests.

Seed identity roles:

- `SystemAdmin`
- `User`

#### Tenants and hierarchy

- Management company A:
  - customer A1
    - property A1-P1
      - unit A1-P1-U1
      - unit A1-P1-U2
    - property A1-P2
  - customer A2
  - resident A1
  - resident A2
  - vendor A1
  - ticket A1
  - scheduled work A1
  - work log A1
  - customer representative mapping for `CustomerARepresentative`
  - resident mappings for `ResidentA` and `ResidentB` as applicable
  - refresh-token scenarios:
    - active token
    - expired token
    - revoked token
- Management company B:
  - customer B1
  - property B1-P1
  - unit B1-P1-U1
  - resident B1
  - vendor B1
  - ticket B1

This enables every IDOR and tenant-isolation test to compare Company A versus Company B.

#### Lookup rows

Seed all lookup/code rows required by the application:

- management company roles
- management company join request statuses
- contact types
- customer representative roles
- property types
- lease roles
- ticket categories
- ticket priorities
- ticket statuses
- work statuses

Every lookup label should have at least English and Estonian `LangStr` entries to support localization tests.

`LangStr` persistence coverage in this plan means EF model plus SQLite JSON/value-conversion behavior only. PostgreSQL `jsonb` behavior and migration execution against PostgreSQL are out of scope.

### 3.3 Playwright setup

Add `docker-compose.playwright.yml`:

```yaml
services:
  playwright:
    image: mcr.microsoft.com/playwright:v1.59.0-noble
    ipc: host
    ports:
      - "3000:3000"
    command: >
      npx -y playwright@1.59.0 run-server --port 3000 --host 0.0.0.0
    extra_hosts:
      - "host.docker.internal:host-gateway"
```

Add `PlaywrightWebAppFactory`:

- Start a real Kestrel host on a fixed local test port.
- Keep TestServer alive for WebApplicationFactory internals.
- Use `RootUri = "http://host.docker.internal:<port>"`.
- Ensure schema exists before browser tests.
- Prefer very small self-contained Admin E2E smoke tests. Non-admin MVC/browser workflows are out of scope and should be covered through BLL/API tests instead.

## Scope update: MVC is Admin-only

MVC-controller integration tests are required only as light Admin-area safety coverage. Do not test non-admin MVC controllers or their MVC-only mappers/view-model mappers. Do not exhaustively test every Admin scaffold CRUD controller through MVC. Public, Management, Customer, Property, Unit, and Resident workflows remain in scope through BLL, DAL, API, and non-MVC mapper tests.

## 4. Manual commands

Run fast unit and integration tests:

```bash
dotnet test mamarrproject.sln --filter "Category!=E2E"
```

Run only unit tests:

```bash
dotnet test mamarrproject.sln --filter "FullyQualifiedName~WebApp.Tests.Unit"
```

Run only integration/API tests:

```bash
dotnet test mamarrproject.sln --filter "FullyQualifiedName~WebApp.Tests.Integration"
```

Run E2E tests:

```bash
docker compose -f docker-compose.playwright.yml up -d
dotnet test mamarrproject.sln --filter "Category=E2E"
```

Run culture-sensitive tests:

```bash
dotnet test mamarrproject.sln --filter "Category=Culture"
```

Run with coverage:

```bash
dotnet test mamarrproject.sln --collect:"XPlat Code Coverage"
```

## 5. Unit tests

### 5.1 Domain tests

#### `Unit/Domain/LangStr_Tests`

- Constructor with value and culture stores neutral culture and default culture.
- Constructor does not overwrite existing default translation.
- Empty constructor creates no entries.
- Empty culture throws.
- Exact culture match returns exact translation.
- Specific culture falls back to neutral culture.
- Unknown culture falls back to default culture.
- Missing translations return null.
- No explicit culture uses current UI culture.
- `SetTranslation` adds current culture.
- `SetTranslation` preserves other cultures.
- `SetTranslation` overwrites only target culture.
- Implicit string conversion stores current culture plus default culture.
- Implicit-to-string uses current UI culture.
- Null implicit-to-string behavior is stable.
- JSON round-trip preserves all cultures.

#### `Unit/Domain/BaseEntity_Tests`

- New entity has non-empty `Id` when generated by constructors, if applicable.
- Created/updated/deleted metadata are nullable or initialized as expected.
- Soft-delete fields are not accidentally set by default.
- Entity equality assumptions are not reference-breaking.

#### `Unit/Domain/SluggedEntity_Tests`

Cover entities with slugs:

- `ManagementCompany.Slug`
- `Customer.Slug`
- `Property.Slug`
- `Unit.Slug`

Tests:

- Slug is required.
- Slug max length is respected.
- Slug uniqueness is enforced at integration level.
- Slug values are normalized by service-level slug generation.

#### `Unit/Domain/LookupEntity_Tests`

For every lookup with `Code` and `Label`:

- Code is required.
- Code comparison should be case-insensitive where business logic expects it.
- Label supports multiple cultures.
- Label translation does not lose other cultures.

Entities:

- `ManagementCompanyRole`
- `ManagementCompanyJoinRequestStatus`
- `ContactType`
- `CustomerRepresentativeRole`
- `PropertyType`
- `LeaseRole`
- `TicketCategory`
- `TicketPriority`
- `TicketStatus`
- `WorkStatus`

#### `Unit/Domain/Ticket_Tests`

- Ticket can be created with required management company, title, category, status, and priority.
- Optional context links are allowed or rejected according to rules:
  - customer
  - property
  - unit
  - resident
  - vendor
- Title and description preserve multiple languages.
- Due date in the past is handled according to business rule.
- Status transition rules are covered in BLL tests.

#### `Unit/Domain/ScheduledWork_Tests`

- Scheduled start/end fields accept valid ranges.
- Real start/end fields are initially null.
- Real end before real start is invalid at service level.
- Work status relationship is required.
- Ticket and vendor relationships are required if the workflow expects them.

#### `Unit/Domain/Lease_Tests`

- Lease connects resident, unit, and role.
- Start date is required.
- End date before start date is invalid at service level.
- Active lease calculation is correct, if present.

### 5.2 Slug and routing unit tests

#### `Unit/Routing/SlugGenerator_Tests`

- Converts mixed-case names to lowercase slugs.
- Removes unsupported punctuation.
- Collapses repeated separators.
- Trims leading/trailing separators.
- Handles Estonian characters predictably.
- Handles empty/null input with fallback.
- Handles collisions by appending suffix.
- Respects max slug length before and after suffix.
- Produces stable output for the same input.

#### `Unit/Routing/RouteRequestModels_Tests`

For route models:

- `ManagementCompanyRoute`
- `CustomerRoute`
- `PropertyRoute`
- `UnitRoute`
- `ResidentRoute`
- `TicketRoute`
- `ScheduledWorkRoute`
- `WorkLogRoute`
- `VendorRoute`
- `VendorCategoryRoute`
- `VendorContactRoute`
- `ResidentLeaseRoute`
- `UnitLeaseRoute`
- `ManagementTicketSearchRoute`
- `ContextTicketSearchRoute`
- `TicketSelectorOptionsRoute`

Tests:

- Required route components are present.
- Nested route objects preserve parent slug/id values.
- Search routes preserve filters.
- Empty slugs are caught by service validation.

### 5.3 Mapper unit tests

Create mapper round-trip tests for real mapper classes in API, BLL, and DAL layers. Prioritize mappers used by active API/BLL/DAL paths and do not invent tests for DTO groups that do not have mapper classes.

MVC-specific mapper/view-model mapper scope:

- Include Admin MVC mapper/view-model mapper tests if the mapper is used by Admin controllers.
- Exclude mapper tests whose only purpose is mapping to non-admin MVC controllers/views, including Public, Management, Customer, Property, Unit, and Resident MVC areas.
- Non-admin workflow mapping should be covered through API DTO, BLL DTO, and DAL DTO mappers instead.
- Admin MVC inline controller mapping is covered by Admin MVC smoke/mutation tests, not exhaustive mapper unit tests.

#### DAL mapper tests

Test classes:

- `AdminCompanyDalMapper_Tests`
- `AdminTicketMonitorDalMapper_Tests`
- `AdminUserDalMapper_Tests`
- `ContactDalMapper_Tests`
- `CustomerDalMapper_Tests`
- `LeaseDalMapper_Tests`
- `ManagementCompanyDalMapper_Tests`
- `ManagementCompanyJoinRequestDalMapper_Tests`
- `PropertyDalMapper_Tests`
- `ResidentDalMapper_Tests`
- `ResidentContactDalMapper_Tests`
- `ScheduledWorkDalMapper_Tests`
- `TicketDalMapper_Tests`
- `UnitDalMapper_Tests`
- `VendorDalMapper_Tests`
- `VendorContactDalMapper_Tests`
- `VendorTicketCategoryDalMapper_Tests`
- `WorkLogDalMapper_Tests`
- `AppRefreshTokenDalMapper_Tests`

For each:

- Domain to DAL DTO maps all scalar fields.
- DAL DTO to Domain maps all scalar fields.
- IDs are preserved.
- Nullable fields remain null.
- `LangStr` fields map without losing translations.
- Foreign keys are preserved.
- Navigation properties are not accidentally required for mapping.
- Mapping null returns null, if mapper contract supports it.

#### BLL mapper tests

Test classes:

- `AdminCompanyBllMapper_Tests`
- `AdminTicketMonitorBllMapper_Tests`
- `AdminUserBllMapper_Tests`
- `ContactBllDtoMapper_Tests`
- `CustomerBllDtoMapper_Tests`
- `PortalDashboardMapper_Tests`
- `LeaseBllDtoMapper_Tests`
- `ManagementCompanyBllDtoMapper_Tests`
- `PropertyBllDtoMapper_Tests`
- `ResidentBllDtoMapper_Tests`
- `ResidentContactBllDtoMapper_Tests`
- `ScheduledWorkBllDtoMapper_Tests`
- `TicketBllDtoMapper_Tests`
- `UnitBllDtoMapper_Tests`
- `VendorBllDtoMapper_Tests`
- `VendorContactBllDtoMapper_Tests`
- `VendorTicketCategoryBllDtoMapper_Tests`
- `WorkLogBllDtoMapper_Tests`

For each:

- DAL DTO to BLL DTO maps all scalar fields.
- BLL DTO to DAL DTO maps all scalar fields.
- IDs and foreign keys are stable.
- Localized strings preserve all cultures or translate only where intended.
- Computed display fields are set only by read-model mappers.
- List and profile model mappers map nested totals/counts.

#### API mapper tests

Inventory `App.DTO/v1/Mappers` before implementing these tests and test only mapper classes that exist. Current mapper inventory:

- `App.DTO.v1.Mappers.Onboarding.ManagementCompanyApiMapper`
- `App.DTO.v1.Mappers.Workspace.WorkspaceApiMapper`
- `App.DTO.v1.Mappers.Portal.Companies.ManagementCompanyApiMapper`
- `App.DTO.v1.Mappers.Portal.Contacts.ResidentContactApiMapper`
- `App.DTO.v1.Mappers.Portal.Contacts.ResidentContactListApiMapper`
- `App.DTO.v1.Mappers.Portal.Customers.CustomerApiMapper`
- `App.DTO.v1.Mappers.Portal.Customers.CustomerListItemApiMapper`
- `App.DTO.v1.Mappers.Portal.Customers.CustomerProfileApiMapper`
- `App.DTO.v1.Mappers.Portal.Dashboards.PortalDashboardApiMapper`
- `App.DTO.v1.Mappers.Portal.Leases.LeaseApiMapper`
- `App.DTO.v1.Mappers.Portal.Leases.LeaseResponseApiMapper`
- `App.DTO.v1.Mappers.Portal.Lookups.LookupApiMapper`
- `App.DTO.v1.Mappers.Portal.Properties.PropertyApiMapper`
- `App.DTO.v1.Mappers.Portal.Properties.PropertyListItemApiMapper`
- `App.DTO.v1.Mappers.Portal.Properties.PropertyProfileApiMapper`
- `App.DTO.v1.Mappers.Portal.Residents.ResidentApiMapper`
- `App.DTO.v1.Mappers.Portal.Residents.ResidentListItemApiMapper`
- `App.DTO.v1.Mappers.Portal.Residents.ResidentProfileApiMapper`
- `App.DTO.v1.Mappers.Portal.ScheduledWork.ScheduledWorkApiMapper`
- `App.DTO.v1.Mappers.Portal.ScheduledWork.ScheduledWorkDetailsApiMapper`
- `App.DTO.v1.Mappers.Portal.ScheduledWork.ScheduledWorkListItemApiMapper`
- `App.DTO.v1.Mappers.Portal.Tickets.TicketApiMapper`
- `App.DTO.v1.Mappers.Portal.Units.UnitApiMapper`
- `App.DTO.v1.Mappers.Portal.Units.UnitListItemApiMapper`
- `App.DTO.v1.Mappers.Portal.Units.UnitProfileApiMapper`
- `App.DTO.v1.Mappers.Portal.Users.CompanyUserApiMapper`
- `App.DTO.v1.Mappers.Portal.Users.OwnershipTransferApiMapper`
- `App.DTO.v1.Mappers.Portal.Users.PendingAccessRequestApiMapper`
- `App.DTO.v1.Mappers.Portal.VendorContacts.VendorContactApiMapper`
- `App.DTO.v1.Mappers.Portal.VendorContacts.VendorContactListApiMapper`
- `App.DTO.v1.Mappers.Portal.Vendors.VendorApiMapper`
- `App.DTO.v1.Mappers.Portal.Vendors.VendorCategoryApiMapper`
- `App.DTO.v1.Mappers.Portal.Vendors.VendorListItemApiMapper`
- `App.DTO.v1.Mappers.Portal.Vendors.VendorProfileApiMapper`
- `App.DTO.v1.Mappers.Portal.WorkLogs.WorkLogApiMapper`
- `App.DTO.v1.Mappers.Portal.WorkLogs.WorkLogListItemApiMapper`

Do not require an auth DTO mapper test unless an actual auth mapper class is added.

For each:

- Request DTO maps to BLL DTO correctly.
- BLL DTO maps to response DTO correctly.
- IDs are not client-trustable on create when server should generate them.
- Read-only fields cannot be overwritten by request mapping.
- Missing optional fields remain null.
- Culture-specific fields map deterministically.

### 5.4 API base/error unit tests

#### `Unit/Web/ApiControllerBase_Tests`

- `GetAppUserId` reads `ClaimTypes.NameIdentifier`.
- `GetAppUserId` reads `sub` when name identifier is absent.
- Invalid user id claim returns null.
- `UnauthorizedError` maps to 401 and `UNAUTHORIZED`.
- `ForbiddenError` maps to 403 and `FORBIDDEN`.
- `NotFoundError` maps to 404 and `NOT_FOUND`.
- `ConflictError` maps to 409 and `CONFLICT`.
- `ValidationAppError` maps to 400 and field errors.
- Unknown business error maps to 400 business-rule violation.
- Error response includes trace id.

### 5.5 UI helper and chrome unit tests

#### `Unit/Web/UI/NavigationBuilder_Tests`

- Anonymous user sees public navigation only.
- System admin sees admin links.
- Management user sees management portal links.
- Customer context sees customer links.
- Resident context sees resident links.
- Current active link is marked correctly.
- No cross-context links are leaked.

#### `Unit/Web/UI/BreadcrumbBuilder_Tests`

- Management dashboard breadcrumb.
- Customer dashboard breadcrumb.
- Property dashboard breadcrumb.
- Unit dashboard breadcrumb.
- Resident dashboard breadcrumb.
- Ticket details breadcrumb.
- Scheduled work breadcrumb.
- Work log breadcrumb.
- Missing route context falls back safely.

#### `Unit/Web/UI/CultureOptionsBuilder_Tests`

- Supported cultures are read from config.
- Current query-string culture is selected.
- Cookie culture is selected when query is absent.
- Default culture is English.
- Return URL is preserved and remains local.

#### `Unit/Web/UI/WorkspaceResolver_Tests`

- Resolves management context from route.
- Resolves customer context from route.
- Resolves property context from route.
- Resolves unit context from route.
- Resolves resident context from route.
- Invalid/missing slugs produce no context.
- Cross-tenant parent mismatch is rejected.

### 5.6 BLL service unit tests with mocked UOW

These tests verify orchestration, not EF queries.

#### `Unit/BLL/Services/AppBLL_Tests`

- Lazily creates every service.
- Returns the same service instance within one BLL instance.
- Uses the same UOW for all services.
- Transaction methods remain UOW responsibility.

#### `Unit/BLL/Services/AuthSessionService_Tests`

- `CreateSessionAsync` creates refresh token with expiry.
- Created refresh token is not empty and is persisted.
- `RotateSessionAsync` rejects missing token.
- `RotateSessionAsync` rejects expired token.
- `RotateSessionAsync` rejects revoked token.
- `RotateSessionAsync` revokes old token and creates new token.
- `RevokeSessionAsync` revokes an active token.
- `RevokeSessionAsync` succeeds idempotently or returns expected not-found behavior.

#### `Unit/BLL/Services/WorkspaceService_Tests`

- `HasAnyContextAsync` false for no memberships or resident mappings.
- `HasAnyContextAsync` true for management membership.
- `HasAnyContextAsync` true for resident mapping.
- `GetDefaultManagementCompanySlugAsync` picks expected company.
- `UserHasManagementCompanyAccessAsync` permits valid member.
- It rejects cross-company user.
- `GetCatalogAsync` returns management/customer/resident workspace options.
- `GetUserCatalogAsync` aggregates all user contexts.
- `ResolveWorkspaceEntryPointAsync` uses remembered management context if authorized.
- It ignores remembered context if unauthorized.
- It falls back to management dashboard.
- It falls back to customer dashboard.
- It falls back to resident dashboard.
- It returns null for no context.
- `AuthorizeContextSelectionAsync` validates context type.
- It rejects missing context id where required.
- It returns management slug for management context.

#### `Unit/BLL/Services/CompanyMembershipService_Tests`

- Authorize management area access by real management role codes:
  - `OWNER`
  - `MANAGER`
  - `SUPPORT`
  - `FINANCE`
- Authorize privileged management actions for `OWNER` and any other real codes allowed by current policy.
- Deny privileged management actions for real codes that current policy restricts.
- List members returns all company members and excludes other companies.
- Add user by email succeeds for existing user.
- Add user by email fails for unknown email.
- Add user by email fails for duplicate membership.
- Add user by email fails for role not found.
- Update membership changes role/job title.
- Update membership cannot demote last owner.
- Delete membership removes non-owner.
- Delete membership cannot delete last owner.
- User cannot delete own owner membership unless transferring ownership first.
- Ownership transfer candidates exclude current owner and invalid roles.
- Transfer ownership updates old owner/new owner roles atomically.
- Pending access request list includes only pending requests for company.
- Approve request creates membership and marks request approved.
- Reject request marks request rejected and creates no membership.
- Duplicate pending join request is rejected.
- Join request by registry code finds correct company.
- Join request for unknown registry code returns not found.
- Join request with invalid role returns validation error.

#### `Unit/BLL/Services/ManagementCompanyService_Tests`

- Create company validates required fields.
- Create company generates slug.
- Create company resolves slug collision.
- Duplicate registry code returns conflict.
- Creator becomes owner.
- Create company runs in a transaction.
- Profile resolution by slug requires authorized user.
- Update company preserves slug unless name change should regenerate it.
- Update company rejects duplicate registry code.
- Update company rejects unauthorized user.

#### `Unit/BLL/Services/CustomerService_Tests`

- Resolve company workspace checks user membership.
- List for company returns only tenant customers.
- Resolve customer workspace validates company slug + customer slug.
- Get profile returns customer details.
- Create validates required name/registry.
- Create generates customer slug under company.
- Create rejects duplicate registry code in same company.
- Same registry code in another company is allowed if business rule is per-company.
- Update rejects cross-company route.
- Update rejects duplicate registry code.
- Delete requires exact confirmation name.
- Delete rejects non-empty customer if properties exist, if business rule says so.
- Delete cross-company customer returns not found/forbidden.

#### `Unit/BLL/Services/PropertyService_Tests`

- Resolve property workspace validates company/customer/property slugs.
- Dashboard returns counts for units/tickets.
- List for customer returns only that customer’s properties.
- Profile includes property type and customer context.
- Property type options include localized labels.
- Create validates parent customer route.
- Create generates property slug unique under customer.
- Update rejects parent mismatch.
- Delete requires exact confirmation name.
- Delete rejects property with units/tickets, if business rule says so.

#### `Unit/BLL/Services/UnitService_Tests`

- Resolve unit workspace validates full route.
- List for property returns only units under property.
- Dashboard returns lease/ticket counts.
- Profile includes property/customer context.
- Create validates required unit number/name.
- Create generates unique unit slug under property.
- Update rejects route/body mismatch.
- Delete requires exact confirmation unit number.
- Delete rejects unit with active leases/tickets, if business rule says so.

#### `Unit/BLL/Services/ResidentService_Tests`

- Resolve company residents context checks management access.
- List for company returns only tenant residents.
- Resolve resident workspace validates resident id code in company.
- Dashboard returns leases/tickets/contact state.
- Profile maps resident details.
- Create validates first name, last name, id code.
- Create rejects duplicate id code in same company.
- Same id code in another company follows business rule.
- Update rejects duplicate id code.
- Delete requires exact confirmation id code.
- Delete rejects resident with active leases/tickets, if business rule says so.
- List contacts returns all resident contacts.
- Add contact can attach an existing contact.
- Add contact can create a new contact via `ContactWriter`.
- Add contact rejects duplicate same resident/contact pair.
- Update contact changes contact role/notes.
- Set primary contact clears previous primary contact.
- Confirm contact sets confirmed metadata.
- Unconfirm contact clears confirmed state.
- Remove contact removes assignment without deleting shared contact.

#### `Unit/BLL/Services/ContactWriter_Tests`

- Reuses existing contact by company/type/value.
- Creates new contact when none exists.
- Rejects missing contact type.
- Normalizes email/phone values.
- Preserves notes as `LangStr`.
- Does not allow cross-company contact reuse.

#### `Unit/BLL/Services/LeaseService_Tests`

- List for resident returns leases for resident only.
- List for unit returns leases for unit only.
- Get for resident rejects lease not belonging to resident.
- Get for unit rejects lease not belonging to unit.
- Create for resident validates selected property/unit belongs to same company.
- Create for unit validates selected resident belongs to same company.
- Create rejects missing lease role.
- Create rejects end date before start date.
- Create rejects overlapping active lease where business rule forbids it.
- Update from resident preserves resident and unit identity.
- Update from unit preserves resident and unit identity.
- Delete from resident rejects route mismatch.
- Delete from unit rejects route mismatch.
- Search properties scopes to management company.
- List units for property scopes to selected property.
- Search residents scopes to management company.
- Lease role options return localized labels.

#### `Unit/BLL/Services/VendorService_Tests`

- Resolve company workspace checks management access.
- List for company returns tenant vendors only.
- Profile includes contacts/categories/open tickets.
- Create validates name/registry.
- Create rejects duplicate registry code in same company.
- Update rejects cross-company vendor.
- Delete requires exact registry code.
- Delete rejects vendor with assigned scheduled work/tickets if business rule says so.
- List category assignments returns assigned categories plus available categories.
- Assign category creates vendor/category link.
- Assign category rejects duplicate link.
- Update category assignment changes notes/active flags.
- Remove category deletes link.
- List contacts returns vendor contacts.
- Add contact attaches existing or creates new contact.
- Set primary contact clears previous primary contact.
- Confirm/unconfirm contact toggles confirmation.
- Remove contact removes vendor-contact link only.

#### `Unit/BLL/Services/TicketService_Tests`

- Management search scopes to company.
- Search term filters title/description/context fields.
- Status filter works.
- Priority filter works.
- Category filter works.
- Customer filter works.
- Property filter works.
- Unit filter works.
- Resident filter works.
- Vendor filter works.
- Due date range filter works.
- Customer-context search only returns tickets for that customer.
- Property-context search only returns tickets for that property.
- Unit-context search only returns tickets for that unit.
- Resident-context search only returns tickets for that resident.
- Get details rejects cross-company ticket.
- Create form contains lookup options.
- Create form options are filtered by route selectors.
- Edit form loads selected values.
- Selector options respect customer/property/unit hierarchy.
- Create validates required title/category/priority.
- Create rejects mismatched customer/property/unit hierarchy.
- Create sets initial status.
- Update preserves management company.
- Update rejects invalid hierarchy.
- Delete rejects cross-company ticket.
- Transition availability returns next allowed status.
- Advance status moves ticket through allowed lifecycle.
- Advance status rejects closed/cancelled final state.
- Localized title/description updates preserve other cultures.

#### `Unit/BLL/Services/ScheduledWorkService_Tests`

- List for ticket scopes to ticket.
- Details rejects scheduled work from another ticket/company.
- Create form returns vendors and work statuses.
- Edit form loads selected values.
- Schedule validates vendor belongs to same company.
- Schedule validates planned start/end.
- Update schedule preserves ticket identity.
- Start work requires scheduled/planned status.
- Start work stores real start and in-progress status.
- Complete work requires started work.
- Complete work stores real end and done status.
- Complete work rejects real end before real start.
- Cancel work rejects completed work.
- Delete rejects started/completed work if business rule forbids it.
- Delete removes only work under matching ticket.

#### `Unit/BLL/Services/WorkLogService_Tests`

- List for scheduled work scopes to scheduled work.
- Create form rejects scheduled work from another company.
- Edit form rejects work log from another scheduled work.
- Delete model includes confirmation data.
- Add work log validates description/time.
- Add work log stores current app user if service owns that.
- Update work log preserves scheduled work id.
- Delete removes matching work log only.
- Localized description update preserves other cultures.

#### `Unit/BLL/Services/AdminLookupService_Tests`

Run every test for each `AdminLookupType`:

- Get lookup types returns all supported lookup types.
- Get lookup items returns localized labels.
- Get item for edit returns expected item.
- Create validates code and label.
- Create rejects duplicate code.
- Update changes code/label.
- Update preserves other cultures.
- Delete check says safe when unused.
- Delete check says blocked when referenced.
- Delete removes unused lookup.
- Delete fails for referenced lookup.

#### `Unit/BLL/Services/AdminUserService_Tests`

- Search users by email/name.
- Search users supports paging/sorting if implemented.
- Get details includes roles and lock state.
- Lock user locks normal user.
- Lock user rejects locking self.
- Lock user rejects locking last system admin.
- Unlock user unlocks locked user.
- Unknown user returns not found.

#### `Unit/BLL/Services/AdminCompanyService_Tests`

- Search companies by name/registry/status.
- Get details includes customers/properties/users.
- Get edit model loads current values.
- Update company validates duplicate registry code.
- Update company saves editable fields.
- Unknown company returns not found.

#### `Unit/BLL/Services/AdminTicketMonitorService_Tests`

- Search tickets across companies.
- Filter by company/status/priority/category/due date.
- Details include company/customer/property/unit/resident/vendor context.
- Does not mutate ticket state.

#### `Unit/BLL/Services/AdminDashboardService_Tests`

- Dashboard totals count companies/users/tickets/open work.
- Counts exclude deleted or archived rows if applicable.
- Recent activity is sorted newest first.

## 6. DAL and repository integration tests

Use SQLite in-memory and real EF model. These tests should verify queries, constraints, `LangStr` persistence, tenant filtering, and transaction behavior.

### 6.1 EF model and database tests

#### `Integration/DAL/AppDbContext_Model_Tests`

- `EnsureCreated` succeeds on SQLite test DB.
- Every `DbSet` is mapped.
- JSON `LangStr` fields persist and reload:
  - lookup labels
  - property label
  - ticket title/description
  - management user job title
  - notes fields
  - work log description
  - join request message
- `DateTime` values persist as UTC or are normalized as expected.
- Delete behavior is restrict for all relationships.
- Data protection key entity is mapped.
- Identity entities are mapped.

#### `Integration/DAL/AppDbContext_UniqueConstraints_Tests`

- Unique management company registry code.
- Unique management company slug.
- Unique customer registry code within company.
- Unique customer slug within company.
- Unique property slug within customer.
- Unique unit slug within property.
- Unique resident id code within company.
- Unique vendor registry code within company.
- Unique management company user pair.
- Unique resident user pair.
- Unique vendor/category pair.
- Unique contact company/type/value.
- Unique lookup codes.
- Pending join request unique per user/company.
- Same values in another tenant are allowed where unique index is tenant-scoped.

### 6.2 Unit of work tests

#### `Integration/DAL/AppUOW_Tests`

- Each repository property returns non-null repository.
- Repeated property access returns same repository instance per UOW.
- `SaveChangesAsync` persists changes.
- `BeginTransactionAsync` begins transaction.
- `CommitTransactionAsync` commits changes.
- `RollbackTransactionAsync` rolls back changes.
- Starting a second transaction throws.
- Commit without transaction throws.
- Rollback without transaction throws.
- After commit/rollback a new transaction can start.

### 6.3 Repository tests

Create one repository test class per repository:

- `AdminDashboardRepository_Tests`
- `AdminUserRepository_Tests`
- `AdminCompanyRepository_Tests`
- `AdminTicketMonitorRepository_Tests`
- `PortalDashboardRepository_Tests`
- `CustomerRepository_Tests`
- `ContactRepository_Tests`
- `ManagementCompanyRepository_Tests`
- `ManagementCompanyJoinRequestRepository_Tests`
- `LookupRepository_Tests`
- `PropertyRepository_Tests`
- `ResidentRepository_Tests`
- `ResidentContactRepository_Tests`
- `UnitRepository_Tests`
- `LeaseRepository_Tests`
- `TicketRepository_Tests`
- `VendorRepository_Tests`
- `VendorContactRepository_Tests`
- `VendorTicketCategoryRepository_Tests`
- `ScheduledWorkRepository_Tests`
- `WorkLogRepository_Tests`
- `AppRefreshTokenRepository_Tests`

For each repository with CRUD:

- `AllAsync` returns all rows for admin-style calls.
- Tenant-scoped list returns only selected tenant.
- Find by id returns item when it belongs to route/context.
- Find by id returns null for cross-tenant access.
- Add persists all fields.
- Update modifies allowed fields only.
- Update with wrong tenant/parent route fails or returns not found.
- Remove deletes or soft-deletes only matching row.
- Remove with wrong tenant does not delete.
- `LangStr` updates preserve other cultures.
- Query projections map every expected display field.

Extra repository-specific tests:

#### `ManagementCompanyRepository_Tests`

- Find by slug.
- Find by registry code.
- Slug collision detection.
- User membership lookup.
- Owner lookup.

#### `CustomerRepository_Tests`

- List by management company.
- Find by company slug + customer slug.
- Registry duplicate query is scoped to company.
- Profile projection includes property count.

#### `PropertyRepository_Tests`

- List by customer route.
- Find by customer slug + property slug.
- Dashboard projection includes unit/ticket counts.
- Property type label localizes.

#### `UnitRepository_Tests`

- List by property route.
- Find by property slug + unit slug.
- Dashboard projection includes active leases/tickets.
- Unit profile includes full parent hierarchy.

#### `ResidentRepository_Tests`

- Find by company slug + resident id code.
- Duplicate id code check is company scoped.
- Dashboard projection includes active leases/tickets.
- List includes contact summary.

#### `LeaseRepository_Tests`

- List by resident.
- List by unit.
- Find by resident route + lease id.
- Find by unit route + lease id.
- Search properties by resident route.
- Search residents by unit route.
- Active lease/date overlap queries.

#### `TicketRepository_Tests`

- Management search filter combinations.
- Context search filter combinations.
- Selector options filter by selected customer/property/unit.
- Transition query loads status code.
- Cross-company ticket is not returned.

#### `ScheduledWorkRepository_Tests`

- List by ticket.
- Find by ticket route.
- Work status code lookup.
- Vendor belongs to ticket company.

#### `WorkLogRepository_Tests`

- List by scheduled work.
- Find by scheduled work route.
- User projection includes author display name.

#### `LookupRepository_Tests`

- Lookup items by type.
- Delete-check counts references correctly for every lookup type.
- Localized labels return current culture.

## 7. BLL integration tests with real UOW

These tests run real services against SQLite through `AppBLL`.

### 7.1 Multi-tenant guard tests

#### `Integration/BLL/MultiTenantAuthorization_Tests`

- Company A `OWNER` cannot read Company B dashboard.
- Company A `MANAGER`, `SUPPORT`, or `FINANCE` cannot mutate Company B customer.
- Company A role behavior is asserted by the real codes `OWNER`, `MANAGER`, `SUPPORT`, and `FINANCE`, with read/mutate expectations taken from current policy.
- Company B `OWNER` cannot access Company A vendor.
- Customer representative cannot access management-only operations.
- Resident user cannot access management-only operations.
- No-context user is redirected or receives forbidden for protected resources.
- System admin can access admin services but not tenant mutation without tenant context unless explicitly allowed.

### 7.2 Route hierarchy tests

#### `Integration/BLL/RouteHierarchy_Tests`

- Customer slug must belong to company slug.
- Property slug must belong to customer slug.
- Unit slug must belong to property slug.
- Resident id code must belong to company slug.
- Ticket id must belong to company slug.
- Scheduled work id must belong to ticket id.
- Work log id must belong to scheduled work id.
- Vendor id must belong to company slug.
- Vendor contact id must belong to vendor id.
- Vendor category id must belong to vendor id.
- Resident contact id must belong to resident id code.
- Lease id must belong to resident/unit depending on route.

### 7.3 Workflow integration tests

BLL workflow integration tests should be broad, domain-focused service tests. Split files by primary domain entity or bounded workflow: `ManagementCompany`, `CompanyMembership`, `Workspace`, `Customer`, `Property`, `Unit`, `Resident`, `Lease`, `Vendor`, `Ticket`, `ScheduledWork`, `WorkLog`, and `Admin`. Keep child concepts that are owned by the aggregate in the parent file when they are naturally part of that workflow, such as vendor categories and vendor contacts inside `Vendor_Workflow_Tests`. Keep separate domain concepts in separate files, such as `Lease_Workflow_Tests` instead of mixing lease assertions into resident tests.

#### `Integration/BLL/Workspace_Workflow_Tests`

- Owner gets default management dashboard entry point.
- New user has no context.
- Remembered invalid or unauthorized context is ignored.
- Unauthorized user cannot choose another company context.

#### `Integration/BLL/ManagementCompany_Workflow_Tests`

- User creates management company and becomes owner.
- Create company with slug collision produces unique slug.
- Update company changes profile fields.
- Duplicate registry code returns conflict.
- Required field validation returns a business result.

#### `Integration/BLL/CompanyMembership_Workflow_Tests`

- User submits join request by registry code.
- Duplicate pending join request fails.
- Owner approves join request and user receives membership.
- Owner rejects join request and user receives no membership.
- Existing members cannot request duplicate membership.
- Membership add/update/delete works.
- Last owner protection works.
- Ownership transfer changes owner rights.
- Pending access requests list only current company.

#### `Integration/BLL/Customer_Workflow_Tests`

- Create customer.
- List customers includes created customer.
- Profile resolves by route.
- Update customer persists.
- Delete customer succeeds after confirmation when no dependencies exist.
- Delete customer with child property is blocked.
- Duplicate registry code and validation failures return business results.
- Cross-company route cannot read/update/delete.

#### `Integration/BLL/Property_Workflow_Tests`

- Create property under customer.
- List customer properties includes created property.
- Profile resolves by route.
- Update property persists.
- Delete property succeeds after confirmation when no dependencies exist.
- Delete property with child unit is blocked.
- Invalid property type and wrong parent route return business results.

#### `Integration/BLL/Unit_Workflow_Tests`

- Create unit under property.
- List property units includes created unit.
- Profile resolves by route.
- Update unit persists.
- Delete unit succeeds after confirmation.
- Validation and wrong parent route return business results.

#### `Integration/BLL/Resident_Workflow_Tests`

- Create resident.
- List residents includes created resident.
- Profile resolves by route.
- Update resident persists.
- Delete resident is blocked by active lease, if applicable.
- Cross-company resident route is rejected.

#### `Integration/BLL/ResidentContact_Workflow_Tests`

- Add new email contact.
- Add new phone contact.
- Confirm email contact.
- Set phone as primary and verify email no longer primary.
- Update contact notes/role.
- Remove contact assignment.

#### `Integration/BLL/Lease_Workflow_Tests`

- Create lease from resident view.
- Same lease appears in unit view.
- Update lease from resident view.
- Update lease from unit view.
- Search property and unit options are scoped.
- Search resident options are scoped.
- End date before start date fails.
- Overlap rule is enforced.
- Delete lease from either view removes it.

#### `Integration/BLL/Vendor_Workflow_Tests`

- Create vendor.
- Assign ticket category.
- Duplicate category assignment fails.
- Add vendor contact.
- Set primary vendor contact.
- Confirm/unconfirm contact.
- Update category notes.
- Remove category.
- Delete vendor with scheduled work is blocked, if applicable.
- Cross-company vendor access is rejected.

#### `Integration/BLL/Ticket_Workflow_Tests`

- Create ticket linked to customer/property/unit/resident/vendor.
- Ticket appears in management search.
- Ticket appears in customer/property/unit/resident context search.
- Ticket details load all context.
- Update ticket changes title/description/priority/category/vendor.
- Invalid hierarchy update fails.
- Advance status through lifecycle.
- Closed ticket cannot advance.
- Delete ticket removes it when no scheduled work exists.
- Delete ticket with scheduled work is blocked, if applicable.

#### `Integration/BLL/ScheduledWork_Workflow_Tests`

- Schedule work for ticket.
- Scheduled work appears under ticket.
- Update schedule.
- Start work sets real start and status.
- Add work log.
- Complete work sets real end and status.
- Cancel unstarted work.
- Delete unstarted work.
- Invalid status transitions are rejected.

#### `Integration/BLL/Admin_Workflow_Tests`

- System admin dashboard counts seeded data.
- Admin company search returns all tenants.
- Admin user search returns users across tenants.
- Admin can lock and unlock regular user.
- Admin cannot lock self/last admin.
- Admin lookup CRUD works for every lookup type.
- Admin lookup delete is blocked when referenced.

## 8. API integration tests

All API tests should use `CustomWebApplicationFactory`, real HTTP clients, and real JWTs.

No Admin API endpoints are in scope for this plan. Do not add Admin REST implementation tasks or REST coverage for the Admin area. The only `SystemAdmin` HTTP surface covered here is Admin MVC.

### 8.1 Common API behavior

#### `Integration/API/Api_Common_Tests`

- App boots through `WebApplicationFactory`.
- `/swagger/v1/swagger.json` loads.
- Unknown endpoint returns 404.
- Unsupported API version returns expected versioning error.
- Default API version behaves as expected.
- Swagger JSON is available for v1.
- CORS exposes expected version headers.
- Error responses use `RestApiErrorResponse`.
- Controller-produced errors use the `RestApiErrorResponse` shape.
- Validation errors include field dictionary.
- Unauthorized endpoints return 401 rather than redirect.
- Protected API route without JWT returns 401, not an MVC redirect.
- Protected API route with invalid JWT returns 401.
- Protected API route with valid JWT reaches `/api/v1/auth/me`.
- Forbidden endpoints return 403.
- Not found returns 404.
- Conflict returns 409.

### 8.2 Auth API

#### `Integration/API/AuthController_Tests`

- `POST /api/v1/auth/register` with missing email returns 400.
- Missing password returns 400.
- Missing first/last name returns 400.
- Valid register returns 201 and user DTO.
- Duplicate email returns 400 or conflict according to service result.
- `POST /api/v1/auth/login` with missing credentials returns 400.
- Invalid credentials return 401.
- Valid credentials return JWT and refresh token.
- `GET /api/v1/auth/me` without JWT returns 401.
- `GET /api/v1/auth/me` with valid JWT returns current user.
- Expired/invalid JWT returns 401.
- `POST /api/v1/auth/refresh` missing token returns 400.
- Refresh with valid token returns new JWT and new refresh token.
- Refresh with old rotated token returns 401.
- Refresh with revoked token returns 401.
- `POST /api/v1/auth/logout` revokes token.
- Logout with missing/unknown token still returns 204 if endpoint is intentionally idempotent.

### 8.3 Onboarding API

#### `Integration/API/OnboardingController_Tests`

- `GET /api/v1/onboarding/status` without JWT returns 401.
- Status for no-context user returns `HasWorkspaceContext=false`.
- Status for management member returns default management path.
- Status for customer context returns customer path.
- Status for resident context returns resident path.
- Create management company without JWT returns 401.
- Create management company validates required fields.
- Create management company returns 201 with slug and path.
- Create management company creates owner membership.
- Duplicate registry code returns conflict.
- Management company roles returns localized role options.
- Join request without JWT returns 401.
- Join request with invalid registry returns 404.
- Join request with invalid role returns validation error.
- Valid join request returns success.
- Duplicate pending join request returns conflict.

### 8.4 Workspace API

If workspace endpoints exist, add:

- Catalog without JWT returns 401.
- User catalog returns all accessible contexts.
- Management catalog includes customers/residents.
- Context authorization rejects invalid type.
- Context authorization rejects foreign context id.
- Context authorization returns slug/path for valid context.

### 8.5 Portal management/company API

For each API controller under management/company context:

- Unauthenticated request returns 401.
- Authenticated but unauthorized tenant returns 403/404.
- Authorized request returns data.
- Cross-company id in route returns 404/403.
- Create validates required model state.
- Create persists and returns correct status/location.
- Update rejects route/body id mismatch.
- Update persists allowed fields.
- Delete requires confirmation where applicable.
- Delete removes only tenant-owned entity.

Apply to every non-admin create/update/delete workflow exposed by these API controllers:

- `ManagementCompaniesController`: management company profile.
- `CompanyUsersController`: list members, add member, edit member, delete member, role options, ownership transfer candidates, ownership transfer, approve access request, reject access request.
- `DashboardsController`: management dashboard and context dashboard endpoints.
- `CustomersController`: customers.
- `PropertiesController`: properties.
- `UnitsController`: units.
- `ResidentsController`: residents.
- `ResidentContactsController`: resident contacts.
- `LeasesController`: resident and unit leases.
- `VendorsController`: vendors and vendor category assignments.
- `VendorContactsController`: vendor contacts.
- `TicketsController`: tickets and lifecycle actions.
- `ScheduledWorkController`: scheduled work.
- `WorkLogsController`: work logs.
- `LookupsController`: property types, lease roles, and tenant-scoped ticket options.

### 8.6 Context API tests

#### Customer context API

- Customer dashboard requires JWT.
- Customer dashboard only works for authorized customer context.
- Customer profile returns customer profile.
- Customer properties list only customer properties.
- Customer tickets list only customer tickets.
- Foreign customer slug is rejected.

#### Property context API

- Property dashboard requires authorized context.
- Property profile returns property data.
- Property units list only property units.
- Property tickets list only property tickets.
- Foreign property slug under valid customer slug is rejected.

#### Unit context API

- Unit dashboard requires authorized context.
- Unit profile returns unit data.
- Unit tenants/leases list only unit leases.
- Unit tickets list only unit tickets.
- Foreign unit slug under valid property is rejected.

#### Resident context API

- Resident dashboard requires resident user access.
- Resident profile returns resident data.
- Resident units/leases list only linked units.
- Resident tickets list only resident tickets.
- Management user without resident mapping cannot use resident endpoint unless business rule allows it.

### 8.7 Out of scope: Admin REST surface

Do not add REST tests for the Admin area in this plan. Admin API endpoints are not part of the required test roadmap, and this testing plan must not create pressure to add them. Cover Admin business behavior through Admin BLL/DAL tests and light Admin MVC integration/E2E safety tests.

## 9. MVC integration tests

Use `WebApplicationFactory` client with cookies and redirects disabled for controller-level MVC tests.

Only light Admin MVC safety coverage is in scope. Admin MVC should be tested because it is a privileged surface, but it should not duplicate the deeper BLL, DAL, API, and repository tests. Do not create exhaustive CRUD tests for every Admin scaffold controller.

### 9.1 Admin MVC scope

Admin MVC tests should answer these questions:

- Can only `SystemAdmin` users access the Admin area?
- Are anonymous and non-admin users blocked correctly?
- Do key Admin pages load without runtime/view/model-binding errors?
- Are high-risk Admin mutations protected by anti-forgery?
- Do a few representative Admin mutations work end to end through MVC?
- Are dangerous deletes blocked when entities are referenced?
- Do missing IDs return 404 instead of crashing?
- Does Admin global access behave intentionally and not accidentally apply tenant-scoped restrictions?

### 9.2 Required Admin MVC tests

#### `Integration/MVC/Admin/AdminAuthorization_Tests`

- Anonymous Admin dashboard request redirects to login or returns the configured unauthorized response.
- Non-admin authenticated user cannot access Admin dashboard.
- System admin can access Admin dashboard.
- Non-admin user cannot access Admin list/detail pages.
- Admin-only POST endpoints reject non-admin users.
- Admin-only POST endpoints require anti-forgery token.
- Local return URL behavior is safe when Admin login redirection is involved.
- External return URLs are ignored/rejected.
- Locked user cannot access Admin area after sign-in is rejected or session is invalidated, depending on implemented behavior.

#### `Integration/MVC/Admin/AdminSmoke_Tests`

Smoke-test representative Admin pages only. These are not deep CRUD tests.

- Admin dashboard loads.
- Admin users list/search page loads.
- Admin user details page loads for an existing user.
- Admin companies list/search page loads.
- Admin company details page loads for an existing company.
- One representative lookup index page loads.
- Admin tickets monitor/index page loads.
- Missing ID on representative details page returns 404.
- Admin layout/navigation renders without broken links for the main Admin sections.

Recommended representative pages:

- Dashboard
- Users
- Management companies
- One lookup type, such as ticket priorities or property types
- Tickets
- One work-related entity, such as scheduled works or work logs, if exposed in Admin MVC

#### `Integration/MVC/Admin/AdminRepresentativeMutation_Tests`

Test only a small number of high-risk Admin MVC mutations:

- Admin can lock and unlock a normal user.
- Admin cannot lock self or the last system admin, if that rule is implemented.
- Admin can edit allowed fields of a management company.
- Admin can create/edit/delete one safe lookup item.
- Admin cannot delete a referenced lookup item.
- Admin delete of a missing entity returns 404.
- Admin delete does not accidentally delete related/shared data.

### 9.3 Admin MVC tests that are intentionally not required

Do not add full MVC CRUD coverage for every Admin scaffold controller. These belong in BLL/DAL/API tests unless a specific Admin page has custom behavior that is not covered elsewhere.

Do not require exhaustive MVC tests for:

- App roles
- Every lookup controller
- Every customer/resident/property/unit controller
- Every lease/vendor/ticket/scheduled-work/work-log controller
- Every create/edit/delete form variant
- Every select-list/view-model binding variant
- Every Admin MVC view-model mapper

Only add a specific Admin MVC controller test beyond the smoke/mutation set when the controller contains custom logic that cannot be reasonably covered through service/API/repository tests.

### 9.4 Out of scope for MVC integration

Do not create MVC controller integration tests for:

- Public onboarding MVC controllers
- Public login/register/logout MVC controllers
- Public language/context selection MVC controllers
- Management portal MVC controllers
- Customer portal MVC controllers
- Property portal MVC controllers
- Unit portal MVC controllers
- Resident portal MVC controllers
- MVC-specific mappers/view-model mappers used only by those non-admin MVC controllers

Coverage for those workflows should come from:

- BLL unit tests
- BLL integration workflow tests
- DAL/repository integration tests
- API integration tests
- API mapper tests


## 10. E2E browser tests

Tag all E2E tests with `[Trait("Category", "E2E")]`.

Only very small Admin browser/UI smoke coverage is in scope. Admin E2E should not duplicate Admin MVC integration tests or BLL/API business workflow tests.

### 10.1 Admin E2E smoke tests

#### `E2E/Admin/AdminSmoke_E2E`

- Anonymous user cannot open Admin dashboard.
- Non-admin user cannot open Admin dashboard.
- System admin logs in and lands on Admin dashboard.
- Admin navigation opens users page.
- Admin navigation opens companies page.
- Admin navigation opens one representative lookup page.
- Admin user search works.
- Admin opens user details.
- Optional: Admin locks and unlocks a normal user, if this is easy and stable through the UI.
- Optional: Admin creates and deletes one safe lookup item, if this is easy and stable through the UI.

Keep Admin E2E short. Prefer API/BLL/DAL tests for detailed business-rule coverage.

### 10.2 Out of scope for E2E

Do not create E2E browser tests for:

- Public onboarding/register/login/logout MVC flows
- Join management company MVC flow
- Management portal MVC flows
- Customer portal MVC flows
- Property portal MVC flows
- Unit portal MVC flows
- Resident portal MVC flows
- Exhaustive Admin CRUD flows
- Non-admin MVC navigation, breadcrumbs, layouts, or view-model mappers

Equivalent workflow confidence should come from:

- API integration tests for auth/onboarding/workspace/business workflows
- BLL integration workflow tests
- BLL service unit tests
- DAL/repository tests
- Light Admin MVC integration tests for Admin-specific safety


## 11. Cross-cutting tests

### 11.1 Security tests

Add these at API, light Admin MVC integration, and light Admin E2E levels where relevant:

- Anonymous user receives 401 on API protected routes.
- Anonymous user redirects to login on Admin MVC protected routes.
- Authenticated user without required Admin role receives 403/access denied for Admin MVC; tenant access is covered in BLL/API tests.
- Cross-tenant IDs never expose data.
- Cross-parent routes never expose data.
- Anti-forgery is required on Admin MVC POSTs.
- Local return URLs are allowed.
- External return URLs are rejected.
- Locked user cannot login.
- Revoked refresh token cannot refresh.
- User role changes affect next request.
- System admin-only routes reject tenant users.
- Tenant routes reject system admin when no tenant context is intended, if business rule says so.

### 11.2 Validation tests

For every create/update command:

- Required fields.
- Max length fields.
- Invalid email.
- Invalid phone.
- Invalid registry code/id code format.
- End date before start date.
- Due date filters.
- Invalid GUIDs.
- Empty slug.
- Body id and route id mismatch.
- Invalid lookup id.
- Cross-tenant lookup/entity id.
- Duplicate natural key.
- Exact delete confirmation mismatch.

### 11.3 Localization tests

- `LangStr` fields persist both English and Estonian.
- Updating one culture preserves other cultures.
- Lookup labels translate by current UI culture.
- API can return culture-specific labels using query-string culture.
- Admin MVC can return culture-specific labels using cookie culture, if Admin views expose localized labels.
- Culture-sensitive tests run serially.

### 11.4 Dashboard/reporting tests

Dashboard correctness should be covered mostly through BLL/API tests. Admin MVC only needs a page-load/smoke check for the Admin dashboard.

For dashboard service/API tests:

- Empty tenant returns zero counts.
- Seeded tenant returns expected counts.
- Counts are tenant-scoped.
- Open/closed ticket counts are correct.
- Scheduled work counts are correct.
- Recent items sorted descending.
- Links point to valid routes.

Dashboards:

- admin dashboard
- management dashboard
- portal dashboard
- customer dashboard
- property dashboard
- unit dashboard
- resident dashboard

### 11.5 Delete/referential integrity tests

For every entity with dependencies:

- Delete safe entity succeeds.
- Delete referenced entity is blocked with user-friendly error.
- Delete wrong confirmation fails.
- Delete cross-tenant id fails without deleting.
- After delete, list/profile endpoints no longer show entity.
- Related shared objects are not accidentally deleted.

Entities:

- management company role
- contact type
- customer representative role
- property type
- lease role
- ticket category
- ticket priority
- ticket status
- work status
- management company
- customer
- property
- unit
- resident
- contact
- vendor
- ticket
- scheduled work
- work log

### 11.6 Transaction tests

- Company creation rolls back membership when company insert fails.
- Ownership transfer rolls back if new owner update fails.
- Join request approval rolls back if membership creation fails.
- Contact creation rolls back if resident/vendor assignment fails.
- Ticket creation rolls back if invalid context is discovered late.
- Scheduled work completion rolls back if status update fails.
- Refresh token rotation rolls back if new token creation fails.

## 12. Suggested build order

### Phase 1: Test harness and smoke coverage

1. Add `CustomWebApplicationFactory`.
2. Add SQLite test DB setup.
3. Finish deterministic SQLite seed data. This blocks later business/API tests.
4. Add JWT helper and authenticated client helpers.
5. Add culture helper.
6. Add API pipeline smoke tests:
   - app starts
   - DB schema creates
   - `/swagger/v1/swagger.json` loads
   - unknown API route returns 404
   - unsupported API version returns versioning error
   - protected API route without JWT returns 401, not MVC redirect
   - protected API route with invalid JWT returns 401
   - protected API route with valid JWT reaches `/api/v1/auth/me`
   - CORS exposes version headers
   - controller-produced errors use `RestApiErrorResponse`
7. Add one authenticated success path and one unauthorized path for the core API host.

### Phase 2: Pure unit tests

1. `LangStr`
2. slug generator
3. route models
4. API error mapping
5. actual API/BLL/DAL mappers
6. `AppBLL` lazy service creation
7. small service tests with mocked UOW

### Phase 3: DAL integration

1. EF model tests.
2. UOW transaction tests.
3. lookup repository tests.
4. core hierarchy repository tests:
   - management company
   - customer
   - property
   - unit
   - resident
5. workflow repositories:
   - lease
   - vendor
   - ticket
   - scheduled work
   - work log

### Phase 4: BLL integration

1. tenant authorization
2. workspace and management company creation/update
3. membership/join requests
4. customer CRUD and dependency blocking
5. property CRUD and dependency blocking
6. unit CRUD
7. resident CRUD and resident contact workflow
8. lease workflow
9. vendor/contact/category workflow
10. ticket workflow
11. scheduled work and work log workflow
12. admin services

### Phase 5: API integration

1. auth
2. onboarding
3. workspace
4. management hierarchy and company-user APIs
5. context APIs
6. lookup APIs
7. ticket, scheduled work, and work-log workflow APIs
8. API error and versioning behavior
9. no Admin REST coverage

### Phase 6: Light Admin MVC integration

1. Admin authorization and access-denied behavior.
2. Admin dashboard smoke test.
3. Admin users, companies, lookups, and tickets smoke tests.
4. One representative lookup smoke/mutation test.
5. One representative business-entity smoke test.
6. Admin anti-forgery behavior.
7. Missing-id 404 behavior.
8. Admin layout/navigation/chrome smoke coverage.

### Phase 7: Light Admin E2E smoke

1. Anonymous and non-admin Admin access blocked.
2. System admin login/access.
3. Admin dashboard loads.
4. Admin navigation opens users and companies.
5. Admin user search/details smoke test.
6. One representative lookup page smoke test.
7. Optional one stable Admin mutation, such as lock/unlock user or safe lookup create/delete.

## 13. Minimum acceptance criteria

The suite is "full enough" when these statements are true:

- API pipeline tests are runnable immediately after the harness, deterministic seed data, and JWT helpers are complete.
- SQLite is the only database provider required by the test suite.
- No Admin REST coverage or Admin API implementation tasks remain in this plan.
- Every public service method in `IAppBLL` has at least one success and one failure test.
- Every repository has tenant-scope tests.
- Every route hierarchy is tested for correct parent/child ownership.
- Every non-admin create/update/delete workflow has API integration coverage. Admin business rules have BLL/DAL coverage; Admin MVC has only light authorization, smoke, anti-forgery, and representative mutation coverage.
- Only critical Admin access/navigation behavior needs Admin E2E smoke coverage. Non-admin workflows do not require MVC/E2E coverage.
- Every lookup type has CRUD and delete-blocking tests.
- Every `LangStr` field has persistence coverage.
- API mapper tests reference only actual mapper classes in `App.DTO/v1/Mappers`; auth mapper tests are not required unless an auth mapper class is added.
- Non-admin MVC controllers and MVC-only mappers/view-model mappers remain intentionally excluded.
- Every authentication mode is covered:
  - cookie-based Admin MVC access smoke coverage
  - JWT API
  - refresh-token rotation
- Every role/context boundary is tested:
  - anonymous
  - no-context user
  - system admin
  - management `OWNER`
  - management `MANAGER`
  - management `SUPPORT`
  - management `FINANCE`
  - customer representative
  - resident user
  - cross-tenant user
- The small Admin E2E smoke suite can be run manually with Playwright server and no CI dependency.

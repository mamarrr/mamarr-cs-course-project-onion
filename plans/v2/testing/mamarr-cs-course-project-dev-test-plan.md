# Test coverage plan for `mamarrr/mamarr-cs-course-project` `dev`

Generated for manual test execution. The goal is to build the same layered style as `akaver-hw-demo`: unit tests first, then DAL/BLL integration, API integration, MVC integration, and browser E2E tests.

## 1. Baseline facts from the current `dev` branch

The solution already contains a `WebApp.Tests` project targeting `net10.0` with xUnit, AwesomeAssertions, Moq, ASP.NET Core MVC testing, EF Core SQLite, Playwright, xUnit runner, and coverlet packages. That is a good base for the layered test suite.

The application is a multi-tenant property maintenance CRM. Core dimensions to cover are:

- Authentication: cookie auth for MVC and JWT auth for API.
- Onboarding: register, login, create management company, join management company, choose workspace context.
- Multi-tenant authorization: management company slug, customer slug, property slug, unit slug, resident id code, and object IDs.
- Portals: public onboarding, admin, management, customer, property, unit, resident.
- Core business entities: management companies, memberships, join requests, customers, properties, units, residents, contacts, leases, vendors, ticket categories, tickets, scheduled work, work logs, lookup tables.
- Localization: `LangStr` values persisted as JSON and translated through query-string/cookie culture selection.
- API versioning: `api/v{version:apiVersion}` endpoints.
- Manual test execution: no CI test stage is required.

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
  E2E/
    E2ECollection.cs
    PlaywrightWebAppFactory.cs
    Public/
    Admin/
    Management/
    Customer/
    Property/
    Unit/
    Resident/
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
- Replace PostgreSQL with SQLite in-memory shared-cache DB.
- Keep the SQLite connection alive for the lifetime of the factory.
- Use `EnsureCreated()` instead of migrations.
- Seed deterministic users, roles, management companies, lookups, customers, properties, units, residents, vendors, tickets, scheduled works, and work logs.
- Configure API versioning so unversioned or default-version requests in tests behave predictably.
- Configure JWT settings with a stable test signing key.
- Keep production-like EF query tracking behavior if the app uses no-tracking identity resolution.
- Expose helpers:
  - `CreateClientNoRedirect()`
  - `CreateAuthenticatedApiClientAsync(user)`
  - `CreateAuthenticatedMvcClientAsync(user)`
  - `CreateDbContext()`
  - `ResetDatabaseAsync()` or per-factory unique DB name.

### 3.2 Test seed model

Seed at least the following deterministic dataset:

#### Users

- `SystemAdmin`
- `CompanyAOwner`
- `CompanyAAdmin`
- `CompanyAManager`
- `CompanyAViewer`
- `CompanyBOwner`
- `CustomerARepresentative`
- `ResidentA`
- `ResidentB`
- `NoContextUser`
- `LockedUser`

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
- Prefer self-contained E2E tests that create their own users and tenant data through the UI or API helper.

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

Create mapper round-trip tests for every mapper in API, BLL, and DAL layers.

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

Add tests for all `App.DTO.v1` mappers:

- auth DTOs
- onboarding DTOs
- workspace DTOs
- management company DTOs
- customer DTOs
- property DTOs
- unit DTOs
- resident DTOs
- lease DTOs
- vendor DTOs
- ticket DTOs
- scheduled work DTOs
- work log DTOs
- lookup DTOs

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

- Authorize management area access: owner/admin allowed.
- Authorize management area access: viewer allowed where expected.
- Authorize admin context: owner/admin allowed.
- Authorize admin context: viewer denied.
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

- Company A owner cannot read Company B dashboard.
- Company A admin cannot mutate Company B customer.
- Company A viewer can read but cannot mutate, if viewer role exists.
- Company B owner cannot access Company A vendor.
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

#### `Integration/BLL/Onboarding_Workflow_Tests`

- New user has no context.
- User creates management company and becomes owner.
- Owner gets default management dashboard entry point.
- User submits join request by registry code.
- Duplicate pending join request fails.
- Owner approves join request and user receives membership.
- Owner rejects join request and user receives no membership.
- After approval, joined user resolves workspace.
- Unauthorized user cannot choose another company context.
- Remembered invalid context is ignored.

#### `Integration/BLL/ManagementCompany_Workflow_Tests`

- Create company with slug collision produces unique slug.
- Update company changes profile fields.
- Duplicate registry code returns conflict.
- Membership add/update/delete works.
- Last owner protection works.
- Ownership transfer changes owner rights.
- Pending access requests list only current company.

#### `Integration/BLL/CustomerPropertyUnit_Workflow_Tests`

- Create customer -> property -> unit in order.
- List at each level includes created child.
- Profile at each level resolves by route.
- Update at each level persists.
- Delete unit succeeds after confirmation.
- Delete property with child unit is blocked until unit removed.
- Delete customer with child property is blocked until property removed.
- Cross-company route cannot read/update/delete.

#### `Integration/BLL/ResidentContact_Workflow_Tests`

- Create resident.
- Add new email contact.
- Add new phone contact.
- Confirm email contact.
- Set phone as primary and verify email no longer primary.
- Update contact notes/role.
- Remove contact assignment.
- Delete resident is blocked by active lease, if applicable.
- Cross-company resident route is rejected.

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

### 8.1 Common API behavior

#### `Integration/API/Api_Common_Tests`

- Unknown endpoint returns 404.
- Unsupported API version returns expected versioning error.
- Default API version behaves as expected.
- Swagger JSON is available for v1.
- CORS exposes expected version headers.
- Error responses use `RestApiErrorResponse`.
- Validation errors include field dictionary.
- Unauthorized endpoints return 401 rather than redirect.
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

### 8.5 Management/company API

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

Apply to:

- management company profile
- customers
- properties
- units
- residents
- resident contacts
- leases
- vendors
- vendor contacts
- vendor category assignments
- tickets
- scheduled works
- work logs

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

### 8.7 Admin API tests

If admin API endpoints exist:

- Non-admin JWT returns 403.
- System admin JWT succeeds.
- Dashboard returns global counts.
- User search returns all users.
- Lock/unlock endpoints work and protect self/last admin.
- Company search/update works.
- Lookup CRUD works for all lookup types.
- Ticket monitor search works globally.

## 9. MVC integration tests

Use `WebApplicationFactory` client with cookies and redirects disabled for controller-level MVC tests.

### 9.1 Public MVC

#### `Integration/MVC/PublicOnboardingController_Tests`

- `GET /` anonymous returns flow chooser.
- `GET /onboarding` anonymous returns flow chooser.
- `GET /register` anonymous returns register view.
- `POST /register` invalid model returns view.
- `POST /register` valid creates user and redirects to login.
- Authenticated user hitting register redirects to onboarding/admin.
- `GET /login` anonymous returns login view.
- `POST /login` invalid model returns view.
- `POST /login` invalid credentials returns model error.
- `POST /login` valid no-context redirects to onboarding chooser.
- `POST /login` valid management user redirects to `/m/{companySlug}`.
- `POST /login` system admin redirects to admin dashboard.
- Local return URL is honored.
- External return URL is ignored.
- `POST /logout` signs out and redirects home.
- `GET /set-context` invalid type redirects safely.
- Set management context writes context cookies and redirects to dashboard.
- Unauthorized set context is rejected/falls back.
- New management company GET requires auth.
- New management company POST creates company and cookies.
- Join management company GET loads role list.
- Join management company POST validates missing role.
- Join management company POST submits request.
- Resident access view loads.

### 9.2 Admin MVC

For each admin controller:

- Anonymous requests redirect to login.
- Non-admin authenticated user gets access denied.
- System admin can access index.
- Details for existing id loads.
- Details for missing id returns 404.
- Create GET loads lookup/select lists.
- Create POST invalid model redisplays view.
- Create POST valid redirects to details/index.
- Edit GET loads selected values.
- Edit POST invalid model redisplays view.
- Edit POST valid persists.
- Delete GET shows confirmation/delete check.
- Delete POST blocked when referenced.
- Delete POST valid removes item.
- List/search filters work.

Apply to admin scaffold controllers:

- app roles
- management companies
- management company roles
- management company users
- customers
- customer representatives
- contact types
- contacts
- residents
- resident contacts
- resident users
- properties
- property types
- units
- leases
- lease roles
- vendors
- vendor contacts
- vendor ticket categories
- tickets
- ticket categories
- ticket priorities
- ticket statuses
- scheduled works
- work statuses
- work logs
- admin dashboard

### 9.3 Management portal MVC

#### `Integration/MVC/ManagementPortal_Tests`

- Anonymous `/m/{companySlug}` redirects to login.
- User without membership gets access denied.
- Owner/admin opens management dashboard.
- Viewer opens read-only pages if allowed.
- Wrong company slug returns 404.
- Dashboard shows scoped counts.
- Customers index scoped to company.
- Customer create/edit/delete workflow.
- Properties index/detail under customer.
- Units index/detail under property.
- Residents index/detail workflow.
- Leases from resident and unit views.
- Vendors index/detail workflow.
- Vendor contacts/categories workflow.
- Tickets index/detail/create/edit/delete/advance.
- Scheduled work schedule/start/complete/cancel.
- Work logs add/edit/delete.
- User/membership management workflows.
- Pending access request approve/reject workflow.
- Ownership transfer workflow.
- Breadcrumbs and navigation reflect current context.

### 9.4 Customer/property/unit/resident MVC portals

Add analogous controller/view tests for each portal shell:

#### Customer portal

- Authorized customer representative can access dashboard.
- Management company user with rights can access if supported.
- Foreign customer is forbidden/not found.
- Properties list scoped.
- Tickets list scoped.
- Navigation uses customer layout.

#### Property portal

- Authorized user can access property dashboard.
- Units list scoped.
- Tickets list scoped.
- Foreign property rejected.
- Navigation uses property layout.

#### Unit portal

- Authorized user can access unit dashboard.
- Tenant/lease list scoped.
- Tickets list scoped.
- Foreign unit rejected.
- Navigation uses unit layout.

#### Resident portal

- Resident user can access own resident dashboard.
- Resident user cannot access another resident.
- Management user can access resident if role permits.
- Profile, units/leases, and tickets are scoped.
- Navigation uses resident layout.

## 10. E2E browser tests

Tag all E2E tests with `[Trait("Category", "E2E")]`.

### 10.1 Public onboarding E2E

#### `E2E/Public/PublicOnboarding_E2E`

- Anonymous home shows flow chooser.
- Register new user, then login.
- Invalid login shows validation error.
- New user with no context sees onboarding choices.
- Create new management company from UI.
- After creation, user lands on management dashboard.
- Logout clears session and returns to public page.
- Language switch persists cookie.
- Registration and login pages render in Estonian after language switch.

### 10.2 Join company E2E

#### `E2E/Public/JoinManagementCompany_E2E`

- User registers.
- Opens join management company flow.
- Role dropdown is populated.
- Missing role shows validation.
- Unknown registry code shows error.
- Valid request shows success.
- Company owner logs in and sees pending request.
- Owner approves request.
- Joined user logs in and sees management dashboard.

### 10.3 Management portal E2E

#### `E2E/Management/ManagementCompany_E2E`

- Owner logs in and opens `/m/{companySlug}`.
- Dashboard shows company-specific counts.
- Navigation to customers, residents, vendors, tickets, users works.
- User menu shows current user.
- Context switcher shows available contexts.

#### `E2E/Management/CustomerPropertyUnit_CRUD_E2E`

- Create customer.
- Create property under customer.
- Create unit under property.
- Edit unit.
- View unit details/profile.
- Delete unit with confirmation.
- Delete property after unit removal.
- Delete customer after property removal.
- Verify Company B user cannot see created data.

#### `E2E/Management/ResidentContactLease_E2E`

- Create resident.
- Add resident contact.
- Set contact primary.
- Confirm contact.
- Create lease for resident by selecting property/unit.
- Verify lease appears on unit view.
- Edit lease.
- Delete lease.
- Delete resident after dependencies removed.

#### `E2E/Management/VendorTicketWork_E2E`

- Create vendor.
- Assign ticket category.
- Add vendor contact.
- Create ticket assigned to customer/property/unit/resident/vendor.
- Advance ticket status.
- Schedule work for ticket.
- Start scheduled work.
- Add work log.
- Complete scheduled work.
- Verify ticket/work status updates are visible.
- Attempt invalid transition and verify UI prevents or shows error.

#### `E2E/Management/Membership_E2E`

- Owner adds existing user by email.
- Owner changes role.
- Non-owner cannot access membership page.
- Last owner cannot be deleted.
- Owner transfers ownership to another admin.
- Old owner cannot perform owner-only action if demoted.

### 10.4 Admin E2E

#### `E2E/Admin/Admin_E2E`

- System admin logs in and lands on admin dashboard.
- Admin user search works.
- Admin locks and unlocks a normal user.
- Admin company search opens details.
- Admin lookup CRUD for a safe lookup type.
- Non-admin cannot open admin dashboard.

### 10.5 Portal isolation E2E

#### `E2E/Security/TenantIsolation_E2E`

- Company A owner cannot open Company B management URL.
- Company A owner cannot guess Company B customer slug under Company A route.
- Company A owner cannot open Company B ticket id.
- Resident A cannot open Resident B dashboard.
- Customer A representative cannot open Customer B dashboard.
- Browser back button after logout does not expose protected data.

### 10.6 Localization E2E

#### `E2E/Localization/Localization_E2E`

- Switch to Estonian and verify main navigation labels.
- Create lookup/item with English text.
- Switch to Estonian and edit only Estonian translation.
- Switch back to English and verify English value was preserved.
- Query-string culture overrides cookie.
- Cookie culture survives new browser context when cookie is copied.

## 11. Cross-cutting tests

### 11.1 Security tests

Add these at API, MVC integration, and E2E levels where relevant:

- Anonymous user receives 401 on API protected routes.
- Anonymous user redirects to login on MVC protected routes.
- Authenticated user without tenant access receives 403 or 404.
- Cross-tenant IDs never expose data.
- Cross-parent routes never expose data.
- Anti-forgery is required on MVC POSTs.
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
- MVC can return culture-specific labels using cookie culture.
- Culture-sensitive tests run serially.

### 11.4 Dashboard/reporting tests

For every dashboard:

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
3. Add deterministic seed data.
4. Add JWT helper.
5. Add culture helper.
6. Add smoke tests:
   - app starts
   - DB schema creates
   - `/` returns 200
   - `/api/v1/auth/login` invalid returns 401
   - authenticated `/api/v1/auth/me` returns current user

### Phase 2: Pure unit tests

1. `LangStr`
2. slug generator
3. route models
4. API error mapping
5. all mappers
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
2. onboarding/company creation
3. membership/join requests
4. customer/property/unit CRUD
5. resident/contact/lease
6. vendor/contact/category
7. ticket/scheduled work/work log
8. admin services

### Phase 5: API integration

1. auth
2. onboarding
3. workspace
4. management hierarchy APIs
5. context APIs
6. admin APIs
7. API error and versioning behavior

### Phase 6: MVC integration

1. public onboarding controller
2. admin controllers
3. management portal controllers
4. customer/property/unit/resident portal controllers
5. navigation/chrome/breadcrumb integration

### Phase 7: E2E

1. public register/login/create company
2. join company approval
3. management CRUD flow
4. resident/contact/lease flow
5. vendor/ticket/scheduled work/work log flow
6. admin flow
7. tenant isolation flow
8. localization flow

## 13. Minimum acceptance criteria

The suite is “full enough” when these statements are true:

- Every public service method in `IAppBLL` has at least one success and one failure test.
- Every repository has tenant-scope tests.
- Every route hierarchy is tested for correct parent/child ownership.
- Every create/update/delete workflow has API or MVC integration coverage.
- Every critical workflow has at least one E2E test.
- Every lookup type has CRUD and delete-blocking tests.
- Every `LangStr` field has persistence coverage.
- Every authentication mode is covered:
  - cookie MVC
  - JWT API
  - refresh-token rotation
- Every role/context boundary is tested:
  - anonymous
  - no-context user
  - system admin
  - management owner/admin/viewer
  - customer representative
  - resident user
  - cross-tenant user
- The E2E suite can be run manually with Playwright server and no CI dependency.

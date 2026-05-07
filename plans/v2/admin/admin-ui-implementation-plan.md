# Admin UI implementation plan

## Goal

Implement a polished, protected, MVC-based Admin UI for `mamarr-cs-course-project` using the same layered architecture style as `akaver-hw-demo` and the existing project structure:

```text
WebApp MVC Controller
→ IAppBLL
→ Admin BLL service
→ IAppUOW
→ DAL repository
→ DAL mapper
→ AppDbContext
```

The Admin UI must be functional, protected, designed, nice to use, and must avoid `ViewBag`, `ViewData`, and `TempData`. MVC views must use strongly typed ViewModels only.

## Required architecture style

The implementation must not be simple scaffolded MVC CRUD. It should follow the application layering already present in the project:

```text
App.Domain
    EF/domain entities

App.DAL.DTO
    DAL projection/read/write DTOs used by repositories

App.DAL.Contracts
    Repository interfaces

App.DAL.EF
    Repository implementations and DAL mappers

App.BLL.DTO
    BLL models/results for Admin use cases

App.BLL.Contracts
    Admin service interfaces

App.BLL
    Admin service implementations, BLL mappers, AppBLL exposure

WebApp
    Controllers, ViewModels, Razor views, manual ViewModel construction
```

Controllers must depend on `IAppBLL`, not directly on `AppDbContext` or repositories. Admin business rules, validation, permission checks, uniqueness checks, and workflow decisions belong in BLL services.

## Implementation scope

The MVP consists of:

1. Admin shell and layout
2. Dashboard service and dashboard view
3. Users list/search/details
4. Lock/unlock users
5. Companies list/search/details/edit
6. Generic lookup management
7. Tickets monitor
8. Polish: validation, empty states, confirmation pages

---

## Phase 0: architecture skeleton

Before building pages, add the Admin module skeleton across DAL, BLL, and WebApp.

### Add BLL contracts

```text
App.BLL.Contracts/Admin/IAdminDashboardService.cs
App.BLL.Contracts/Admin/IAdminUserService.cs
App.BLL.Contracts/Admin/IAdminCompanyService.cs
App.BLL.Contracts/Admin/IAdminLookupService.cs
App.BLL.Contracts/Admin/IAdminTicketMonitorService.cs
```

### Add BLL implementations

```text
App.BLL/Services/Admin/AdminDashboardService.cs
App.BLL/Services/Admin/AdminUserService.cs
App.BLL/Services/Admin/AdminCompanyService.cs
App.BLL/Services/Admin/AdminLookupService.cs
App.BLL/Services/Admin/AdminTicketMonitorService.cs
```

### Add BLL DTO folders

```text
App.BLL.DTO/Admin/Dashboard/
App.BLL.DTO/Admin/Users/
App.BLL.DTO/Admin/Companies/
App.BLL.DTO/Admin/Lookups/
App.BLL.DTO/Admin/Tickets/
```

### Add DAL repository contracts

```text
App.DAL.Contracts/Repositories/Admin/IAdminDashboardRepository.cs
App.DAL.Contracts/Repositories/Admin/IAdminUserRepository.cs
App.DAL.Contracts/Repositories/Admin/IAdminCompanyRepository.cs
App.DAL.Contracts/Repositories/Admin/IAdminTicketMonitorRepository.cs
```

For lookup management, prefer extending the existing `ILookupRepository` instead of creating one repository per lookup table.

### Add DAL implementations

```text
App.DAL.EF/Repositories/Admin/AdminDashboardRepository.cs
App.DAL.EF/Repositories/Admin/AdminUserRepository.cs
App.DAL.EF/Repositories/Admin/AdminCompanyRepository.cs
App.DAL.EF/Repositories/Admin/AdminTicketMonitorRepository.cs
```

### Add DAL mappers

```text
App.DAL.EF/Mappers/Admin/AdminUserDalMapper.cs
App.DAL.EF/Mappers/Admin/AdminCompanyDalMapper.cs
App.DAL.EF/Mappers/Admin/AdminTicketMonitorDalMapper.cs
```

Dashboard projections may not need a mapper if they are simple aggregate DTOs, but use mappers where domain/entity data crosses into DAL DTOs.

### Add BLL mappers

```text
App.BLL/Mappers/Admin/AdminUserBllMapper.cs
App.BLL/Mappers/Admin/AdminCompanyBllMapper.cs
App.BLL/Mappers/Admin/AdminTicketMonitorBllMapper.cs
```

### Add WebApp ViewModels

```text
WebApp/ViewModels/Admin/Dashboard/
WebApp/ViewModels/Admin/Users/
WebApp/ViewModels/Admin/Companies/
WebApp/ViewModels/Admin/Lookups/
WebApp/ViewModels/Admin/Tickets/
```

Do not add `WebApp/Mappers/Admin/*` for BLL DTO to ViewModel mapping. Controllers may construct strongly typed ViewModels directly from BLL DTOs.

### Update `IAppBLL`

Add Admin service properties:

```csharp
IAdminDashboardService AdminDashboard { get; }
IAdminUserService AdminUsers { get; }
IAdminCompanyService AdminCompanies { get; }
IAdminLookupService AdminLookups { get; }
IAdminTicketMonitorService AdminTicketMonitor { get; }
```

### Update `AppBLL`

Add lazy-loaded services:

```csharp
private IAdminDashboardService? _adminDashboard;
private IAdminUserService? _adminUsers;
private IAdminCompanyService? _adminCompanies;
private IAdminLookupService? _adminLookups;
private IAdminTicketMonitorService? _adminTicketMonitor;

public IAdminDashboardService AdminDashboard =>
    _adminDashboard ??= new AdminDashboardService(UOW);

public IAdminUserService AdminUsers =>
    _adminUsers ??= new AdminUserService(UOW);

public IAdminCompanyService AdminCompanies =>
    _adminCompanies ??= new AdminCompanyService(UOW);

public IAdminLookupService AdminLookups =>
    _adminLookups ??= new AdminLookupService(UOW);

public IAdminTicketMonitorService AdminTicketMonitor =>
    _adminTicketMonitor ??= new AdminTicketMonitorService(UOW);
```

Do not inject `UserManager<AppUser>`, `RoleManager<AppRole>`, or other ASP.NET Identity/WebApp services into `AppBLL`. `App.BLL` must keep its current dependency direction and remain independent of WebApp and Identity service APIs.

For lock/unlock:

```text
- BLL owns admin business checks: self-lock prevention, last SystemAdmin protection, and action result semantics.
- Identity persistence changes must be performed either through DAL repository methods over `AppDbContext` or through a WebApp identity application service after BLL approves the action.
- Prefer the DAL repository approach when the operation can be expressed as persistence over Identity tables without requiring `UserManager` behavior.
```

### Update `IAppUOW`

Add Admin repository properties:

```csharp
IAdminDashboardRepository AdminDashboard { get; }
IAdminUserRepository AdminUsers { get; }
IAdminCompanyRepository AdminCompanies { get; }
IAdminTicketMonitorRepository AdminTicketMonitor { get; }
```

### Update `AppUOW`

Add lazy-loaded repositories:

```csharp
private IAdminDashboardRepository? _adminDashboard;
private IAdminUserRepository? _adminUsers;
private IAdminCompanyRepository? _adminCompanies;
private IAdminTicketMonitorRepository? _adminTicketMonitor;

public IAdminDashboardRepository AdminDashboard =>
    _adminDashboard ??= new AdminDashboardRepository(UowDbContext);

public IAdminUserRepository AdminUsers =>
    _adminUsers ??= new AdminUserRepository(UowDbContext, new AdminUserDalMapper());

public IAdminCompanyRepository AdminCompanies =>
    _adminCompanies ??= new AdminCompanyRepository(UowDbContext, new AdminCompanyDalMapper());

public IAdminTicketMonitorRepository AdminTicketMonitor =>
    _adminTicketMonitor ??= new AdminTicketMonitorRepository(UowDbContext, new AdminTicketMonitorDalMapper());
```

---

## Phase 1: Admin shell and layout

### Goal

Create a protected `/Admin` area with a clean admin layout, sidebar, header, and dashboard landing page.

### Add files

```text
WebApp/Areas/Admin/Controllers/DashboardController.cs
WebApp/Areas/Admin/Views/_ViewStart.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminLayout.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminSidebar.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminHeader.cshtml
WebApp/wwwroot/css/admin.css
```

### Controller pattern

```csharp
[Area("Admin")]
[Authorize(Roles = "SystemAdmin")]
public class DashboardController : Controller
{
    private readonly IAppBLL _bll;

    public DashboardController(IAppBLL bll)
    {
        _bll = bll;
    }

    public async Task<IActionResult> Index()
    {
        var dto = await _bll.AdminDashboard.GetDashboardAsync();

        var vm = new AdminDashboardViewModel
        {
            Stats = new AdminDashboardStatsViewModel
            {
                TotalUsers = dto.Stats.TotalUsers,
                LockedUsers = dto.Stats.LockedUsers,
                TotalManagementCompanies = dto.Stats.TotalManagementCompanies,
                PendingJoinRequests = dto.Stats.PendingJoinRequests,
                OpenTickets = dto.Stats.OpenTickets,
                OverdueTickets = dto.Stats.OverdueTickets,
                ScheduledWorkToday = dto.Stats.ScheduledWorkToday
            },
            RecentUsers = dto.RecentUsers
                .Select(user => new AdminRecentUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    CreatedAt = user.CreatedAt
                })
                .ToList()
        };

        return View(vm);
    }
}
```

### Acceptance criteria

```text
- /Admin opens Dashboard/Index.
- Only SystemAdmin can access.
- No Admin base controller is required; each Admin controller may apply `[Area("Admin")]` and `[Authorize(Roles = "SystemAdmin")]` directly.
- Non-admin users get access denied.
- Admin layout has sidebar navigation.
- No ViewBag/ViewData/TempData.
- Views are strongly typed and receive ViewModels only.
```

---

## Phase 2: Dashboard service and dashboard view

### Goal

Show useful platform-level counts and recent activity.

### BLL contract

```csharp
public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
```

### Dashboard data

Include:

```text
- Total users
- Locked users
- Total management companies
- Pending join requests
- Open tickets
- Overdue tickets
- Scheduled work today
- Recent users
- Recent companies
```

### Web ViewModels

```text
WebApp/ViewModels/Admin/Dashboard/AdminDashboardViewModel.cs
WebApp/ViewModels/Admin/Dashboard/AdminDashboardStatsViewModel.cs
WebApp/ViewModels/Admin/Dashboard/AdminRecentUserViewModel.cs
WebApp/ViewModels/Admin/Dashboard/AdminRecentCompanyViewModel.cs
```

### Acceptance criteria

```text
- Dashboard loads without direct DbContext usage in the controller.
- Stats cards render correctly.
- Empty recent lists show a nice empty state.
- Counts are calculated in DAL/BLL, not in Razor.
```

---

## Phase 3: Users list/search/details

### Goal

Admin can search application users and open a user details page.

### BLL contract

```csharp
public interface IAdminUserService
{
    Task<AdminUserListDto> SearchUsersAsync(
        AdminUserSearchDto search,
        CancellationToken cancellationToken = default);

    Task<AdminUserDetailsDto?> GetUserDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
```

### BLL DTOs

```text
App.BLL.DTO/Admin/Users/AdminUserSearchDto.cs
App.BLL.DTO/Admin/Users/AdminUserListDto.cs
App.BLL.DTO/Admin/Users/AdminUserListItemDto.cs
App.BLL.DTO/Admin/Users/AdminUserDetailsDto.cs
App.BLL.DTO/Admin/Users/AdminUserRoleDto.cs
App.BLL.DTO/Admin/Users/AdminUserCompanyMembershipDto.cs
```

### DAL DTOs and mapper

```text
App.DAL.DTO/Admin/Users/AdminUserDalDto.cs
App.DAL.DTO/Admin/Users/AdminUserDetailsDalDto.cs
App.DAL.EF/Mappers/Admin/AdminUserDalMapper.cs
```

### Web files

```text
WebApp/Areas/Admin/Controllers/UsersController.cs
WebApp/Areas/Admin/Views/Users/Index.cshtml
WebApp/Areas/Admin/Views/Users/Details.cshtml
WebApp/ViewModels/Admin/Users/AdminUserSearchViewModel.cs
WebApp/ViewModels/Admin/Users/AdminUserListViewModel.cs
WebApp/ViewModels/Admin/Users/AdminUserDetailsViewModel.cs
```

### Search filters

```text
- Search text
- Email
- Name
- Locked only
- Has SystemAdmin role
- Created from/to
```

### Acceptance criteria

```text
- Admin can search users.
- Details page shows profile, roles, company memberships, and refresh token count.
- Search is intentionally unpaginated for MVP.
- Controller uses IAppBLL only.
```

---

## Phase 4: Lock/unlock users

### Goal

Admin can lock or unlock application users safely.

### Add methods to `IAdminUserService`

```csharp
Task<Result<AdminUserDetailsDto>> LockUserAsync(
    Guid userId,
    Guid actorUserId,
    CancellationToken cancellationToken = default);

Task<Result<AdminUserDetailsDto>> UnlockUserAsync(
    Guid userId,
    Guid actorUserId,
    CancellationToken cancellationToken = default);
```

### Guardrails

```text
- Do not allow an admin to lock themselves.
- Do not allow locking the last SystemAdmin.
- Use POST actions only.
- Use anti-forgery tokens.
- Show a confirmation page or clear confirmation panel before lock.
```

### Controller actions

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Lock(Guid id)
{
    var actorUserId = ResolveCurrentUserId();
    var result = await _bll.AdminUsers.LockUserAsync(id, actorUserId);

    if (result.IsFailed)
    {
        var fallbackDetails = await _bll.AdminUsers.GetUserDetailsAsync(id);
        if (fallbackDetails == null)
        {
            return NotFound();
        }

        var errorVm = MapUserDetails(fallbackDetails);
        errorVm.ErrorMessage = MapAdminError(result.Errors);
        return View("Details", errorVm);
    }

    var vm = MapUserDetails(result.Value);
    vm.SuccessMessage = App.Resources.Views.UiText.UserLockedSuccessfully;

    return View("Details", vm);
}
```

Do not use `TempData`. Return or reload a strongly typed ViewModel that contains the success/error message. BLL should return `FluentResults.Result<T>` with error codes/metadata; WebApp maps those codes to localized resource strings.

### Acceptance criteria

```text
- User can be locked.
- User can be unlocked.
- Self-lock is blocked.
- Last SystemAdmin lock is blocked.
- Actions use POST + anti-forgery.
```

---

## Phase 5: Companies list/search/details/edit

### Goal

Admin can browse and edit management companies.

### BLL contract

```csharp
public interface IAdminCompanyService
{
    Task<AdminCompanyListDto> SearchCompaniesAsync(
        AdminCompanySearchDto search,
        CancellationToken cancellationToken = default);

    Task<AdminCompanyDetailsDto?> GetCompanyDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdminCompanyEditDto?> GetCompanyForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<AdminCompanyDetailsDto>> UpdateCompanyAsync(
        Guid id,
        AdminCompanyUpdateDto dto,
        CancellationToken cancellationToken = default);
}
```

### Company details should show

```text
- Company basic data
- Users/members count
- Customers count
- Properties count
- Units count
- Residents count
- Tickets count
- Open tickets count
- Vendors count
- Pending join requests
```

### Edit fields

```text
- Name
- Registry code
- VAT number
- Email
- Phone
- Address
- Slug
```

### Web files

```text
WebApp/Areas/Admin/Controllers/CompaniesController.cs
WebApp/Areas/Admin/Views/Companies/Index.cshtml
WebApp/Areas/Admin/Views/Companies/Details.cshtml
WebApp/Areas/Admin/Views/Companies/Edit.cshtml
WebApp/ViewModels/Admin/Companies/AdminCompanySearchViewModel.cs
WebApp/ViewModels/Admin/Companies/AdminCompanyListViewModel.cs
WebApp/ViewModels/Admin/Companies/AdminCompanyDetailsViewModel.cs
WebApp/ViewModels/Admin/Companies/AdminCompanyEditViewModel.cs
```

### Guardrails

```text
- Slug uniqueness validation.
- Registry code uniqueness validation.
- Do not delete companies in MVP.
- Validate all edits in BLL.
```

### Acceptance criteria

```text
- Admin can search companies.
- Admin can view company details.
- Admin can edit basic company data.
- Duplicate slug/registry code is blocked.
- Details page uses summary cards, not raw entity dumps.
```

---

## Phase 6: Generic lookup management

### Goal

Create one generic Admin lookup section for global lookup tables.

### Supported lookup tables

```text
- Property types
- Ticket categories
- Ticket priorities
- Ticket statuses
- Work statuses
- Contact types
- Management company roles
```

### Route design

Use one controller rather than one controller per lookup table:

```text
/Admin/Lookups
/Admin/Lookups/{lookupType}
/Admin/Lookups/{lookupType}/Create
/Admin/Lookups/{lookupType}/{id}/Edit
/Admin/Lookups/{lookupType}/{id}/Delete
```

### BLL contract

```csharp
public interface IAdminLookupService
{
    Task<AdminLookupListDto> GetLookupItemsAsync(
        AdminLookupType type,
        CancellationToken cancellationToken = default);

    Task<AdminLookupEditDto?> GetLookupItemForEditAsync(
        AdminLookupType type,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<AdminLookupItemDto>> CreateLookupItemAsync(
        AdminLookupType type,
        AdminLookupEditDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<AdminLookupItemDto>> UpdateLookupItemAsync(
        AdminLookupType type,
        Guid id,
        AdminLookupEditDto dto,
        CancellationToken cancellationToken = default);

    Task<AdminLookupDeleteCheckDto> GetDeleteCheckAsync(
        AdminLookupType type,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteLookupItemAsync(
        AdminLookupType type,
        Guid id,
        CancellationToken cancellationToken = default);
}
```

### Lookup type enum

`AdminLookupType` belongs at the BLL/WebApp contract boundary, for example in `App.BLL.DTO/Admin/Lookups/AdminLookupType.cs`. Do not pass this BLL enum into DAL contracts.

```csharp
public enum AdminLookupType
{
    PropertyType,
    TicketCategory,
    TicketPriority,
    TicketStatus,
    WorkStatus,
    ContactType,
    ManagementCompanyRole
}
```

Add a DAL-level lookup table identifier separately, for example:

```text
App.DAL.DTO/Lookups/LookupTable.cs
```

```csharp
public enum LookupTable
{
    PropertyType,
    TicketCategory,
    TicketPriority,
    TicketStatus,
    WorkStatus,
    ContactType,
    ManagementCompanyRole
}
```

`AdminLookupService` maps `AdminLookupType` to `LookupTable` before calling `ILookupRepository`. This keeps `App.DAL.Contracts` independent of BLL DTOs and preserves the repository dependency direction.

### Extend `ILookupRepository`

Prefer extending the existing lookup repository with methods like:

```csharp
Task<IReadOnlyList<LookupItemDalDto>> GetLookupItemsAsync(LookupTable table);
Task<LookupItemDalDto?> FindLookupItemAsync(LookupTable table, Guid id);
Task<bool> CodeExistsAsync(LookupTable table, string code, Guid? exceptId = null);
Task<bool> IsLookupInUseAsync(LookupTable table, Guid id);
```

### Localized labels

Because lookup labels use `LangStr`, the Admin form edits the label for the current request culture only:

```text
Code
Label
```

On edit, BLL loads the existing lookup entity and updates only the current language with `LangStr.SetTranslation(...)`, preserving all other translations. Example: if the current UI culture is English, editing `Label` updates only `en`; if the current UI culture is Estonian, editing `Label` updates only `et`. Razor must never show raw serialized `LangStr` JSON.

### Lookup guardrails

```text
- Lookup `CODE` values are stable invariant identifiers.
- Seeded/system lookup codes must not be repurposed.
- Canonical ticket status, work status, and management company role codes must not be deleted or changed in ways that break workflows or permissions.
- Code uniqueness is validated in BLL and should be enforced at the database level where feasible.
- Delete is blocked if the lookup is in use.
- Delete is also blocked for protected seed/system lookup values even if no current row references them.
- BLL returns `FluentResults.Result<T>` or `Result` with error codes/metadata; WebApp maps them to localized resource strings.
```

### Web files

```text
WebApp/Areas/Admin/Controllers/LookupsController.cs
WebApp/Areas/Admin/Views/Lookups/Index.cshtml
WebApp/Areas/Admin/Views/Lookups/List.cshtml
WebApp/Areas/Admin/Views/Lookups/Create.cshtml
WebApp/Areas/Admin/Views/Lookups/Edit.cshtml
WebApp/Areas/Admin/Views/Lookups/Delete.cshtml
WebApp/ViewModels/Admin/Lookups/AdminLookupListViewModel.cs
WebApp/ViewModels/Admin/Lookups/AdminLookupEditViewModel.cs
WebApp/ViewModels/Admin/Lookups/AdminLookupDeleteViewModel.cs
```

### Acceptance criteria

```text
- One controller manages all selected lookup tables.
- Code uniqueness is validated.
- The current-culture label is editable without exposing raw translation storage.
- Raw LangStr JSON is never shown in UI.
- Protected lookup codes cannot be changed, repurposed, or deleted.
- Delete is blocked if lookup is in use.
```

---

## Phase 7: Tickets monitor

### Goal

Admin can globally monitor tickets across all companies.

### BLL contract

```csharp
public interface IAdminTicketMonitorService
{
    Task<AdminTicketListDto> SearchTicketsAsync(
        AdminTicketSearchDto search,
        CancellationToken cancellationToken = default);

    Task<AdminTicketDetailsDto?> GetTicketDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

### Repository design

Add a dedicated admin ticket monitor repository rather than overloading the normal tenant-scoped ticket repository:

```text
App.DAL.Contracts/Repositories/Admin/IAdminTicketMonitorRepository.cs
App.DAL.EF/Repositories/Admin/AdminTicketMonitorRepository.cs
```

Reason: Admin monitor queries are cross-company, filter-heavy, and projection-heavy.

### Search filters

```text
- Company
- Customer
- Ticket number
- Status
- Priority
- Category
- Vendor
- Created from/to
- Due from/to
- Overdue only
- Open only
```

### Details page should show

```text
- Ticket number
- Title
- Description
- Company
- Customer
- Property
- Unit
- Resident
- Vendor
- Status
- Priority
- Category
- CreatedAt
- DueAt
- ClosedAt
- Scheduled work
- Work logs
```

### Web files

```text
WebApp/Areas/Admin/Controllers/TicketsController.cs
WebApp/Areas/Admin/Views/Tickets/Index.cshtml
WebApp/Areas/Admin/Views/Tickets/Details.cshtml
WebApp/ViewModels/Admin/Tickets/AdminTicketSearchViewModel.cs
WebApp/ViewModels/Admin/Tickets/AdminTicketListViewModel.cs
WebApp/ViewModels/Admin/Tickets/AdminTicketDetailsViewModel.cs
```

### MVP behavior

Make the tickets monitor read-only first. Limited correction actions can be added later.

### Acceptance criteria

```text
- Admin can filter tickets globally.
- Details page loads all important related data.
- Page is read-only in MVP.
- SystemAdmin-only access is enforced.
- Query uses projections instead of very large Include chains wherever practical.
```

---

## Phase 8: polish, validation, empty states, confirmation pages

### Goal

Make the Admin UI feel complete and safe.

### Shared components

```text
WebApp/Areas/Admin/Views/Shared/_AdminFlashMessages.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminValidationSummary.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminEmptyState.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminConfirmDelete.cshtml
WebApp/Areas/Admin/Views/Shared/_AdminSearchPanel.cshtml
```

### Validation rules

Use ViewModel validation attributes:

```text
[Required]
[StringLength]
[EmailAddress]
[Display]
```

All user-visible labels, validation messages, buttons, empty states, confirmation text, success messages, and error messages must be resource-backed. Add or update English and Estonian resource entries together.

### Confirmation pages

Use confirmation pages or explicit confirmation panels for:

```text
- Lock user
- Delete lookup
- Any future destructive admin action
```

### Empty states

Every list should have an intentional empty state:

```text
- No users found
- No companies found
- No tickets match filters
- No lookup items configured
```

### Acceptance criteria

```text
- Forms show field-level validation.
- POST actions use anti-forgery.
- Success/error messages are carried in strongly typed ViewModel properties, not TempData, and are mapped from BLL result codes to localized resources in WebApp.
- Delete/lock actions are not GET actions.
- Empty results look intentional.
```

---

## Suggested final file structure

The file structure must support this mapping line:

```text
DomainEntity
→ App.DAL.EF mapper
→ DalDto
→ App.BLL mapper
→ BllDto
→ ViewModel created in controller
```

```text
App.DAL.DTO/Admin/
├── Dashboard/
├── Users/
├── Companies/
├── Lookups/
└── Tickets/

App.DAL.Contracts/Repositories/Admin/
├── IAdminDashboardRepository.cs
├── IAdminUserRepository.cs
├── IAdminCompanyRepository.cs
└── IAdminTicketMonitorRepository.cs

App.DAL.EF/Repositories/Admin/
├── AdminDashboardRepository.cs
├── AdminUserRepository.cs
├── AdminCompanyRepository.cs
└── AdminTicketMonitorRepository.cs

App.DAL.EF/Mappers/Admin/
├── AdminUserDalMapper.cs
├── AdminCompanyDalMapper.cs
└── AdminTicketMonitorDalMapper.cs

App.BLL.DTO/Admin/
├── Dashboard/
├── Users/
├── Companies/
├── Lookups/
└── Tickets/

App.BLL.Contracts/Admin/
├── IAdminDashboardService.cs
├── IAdminUserService.cs
├── IAdminCompanyService.cs
├── IAdminLookupService.cs
└── IAdminTicketMonitorService.cs

App.BLL/Services/Admin/
├── AdminDashboardService.cs
├── AdminUserService.cs
├── AdminCompanyService.cs
├── AdminLookupService.cs
└── AdminTicketMonitorService.cs

App.BLL/Mappers/Admin/
├── AdminUserBllMapper.cs
├── AdminCompanyBllMapper.cs
└── AdminTicketMonitorBllMapper.cs

WebApp/Areas/Admin/
├── Controllers/
├── Views/
└── Views/Shared/

WebApp/ViewModels/Admin/
├── Dashboard/
├── Users/
├── Companies/
├── Lookups/
└── Tickets/

```

---

## Recommended implementation order

```text
1. Add Admin service/repository skeleton and wire AppBLL/AppUOW.
2. Build Admin shell and layout.
3. Implement dashboard counts and dashboard view.
4. Implement users search/details.
5. Implement lock/unlock users.
6. Implement companies search/details/edit.
7. Implement generic lookup management.
8. Implement read-only tickets monitor.
9. Polish validation, empty states, confirmation pages, and styling.
```

---

## Estimated work

Because this follows full layered architecture, it is more work than scaffolded MVC CRUD.

```text
Admin shell/layout:             4–8 hours
Dashboard:                      6–10 hours
Users list/details:             10–16 hours
Lock/unlock users:              4–8 hours
Companies list/details/edit:    10–18 hours
Generic lookup management:      14–24 hours
Tickets monitor:                10–18 hours
Polish/validation/empty states: 8–16 hours
Architecture wiring/cleanup:    8–14 hours
```

Total realistic range: **74–132 hours**.

For a course-project MVP, target **50–70 hours** by keeping tickets read-only, keeping search results unpaginated for MVP, and making the lookup manager generic instead of building many custom pages.

## Best MVP cut

Implement in this exact MVP order:

```text
1. Architecture skeleton: Admin services + repositories + AppBLL/AppUOW wiring
2. Admin shell/layout
3. Dashboard
4. Users search/details
5. Lock/unlock
6. Companies search/details/edit
7. Generic lookups
8. Tickets read-only monitor
9. Polish
```

This gives a clean Admin UI that matches the required architecture and avoids scaffolded-controller style.

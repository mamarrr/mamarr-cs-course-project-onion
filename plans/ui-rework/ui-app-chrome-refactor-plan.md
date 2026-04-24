# UI Architecture Refactor Plan: AppChrome-Based Razor MVC Layout

## Goal

Refactor the WebApp UI architecture so the visual UI stays the same, but the code becomes easier to follow, more SOLID, and less dependent on management-specific layout infrastructure.

This refactor is intentionally **WebApp-first**. Do not start by changing BLL or DAL. Only extend BLL later if the UI needs workspace hierarchy data that the WebApp cannot currently get through existing services.

## Current problem

The current layout flow has too many management-specific concepts for something that is mostly general UI composition.

Current shape:

```text
Management controller
  -> ManagementPageShellController
      -> IManagementLayoutViewModelProvider
          -> IWorkspaceLayoutContextProvider
              -> IUserWorkspaceCatalogService
          -> IUserWorkspaceCatalogService again
      -> ManagementPageShellViewModel
  -> View
```

Problems:

1. `ManagementPageShellController` hides layout dependencies through inheritance.
2. `ManagementLayoutViewModelProvider` calls `WorkspaceLayoutContextProvider`, then calls `IUserWorkspaceCatalogService` again only to get `CanManageCompanyUsers`.
3. `ManagementLayoutViewModel` duplicates many fields that already exist in `WorkspaceLayoutContextViewModel`.
4. The UI architecture is management-first, even though most layout concepts are shared across management, customer, property, unit, and resident pages.
5. Breadcrumbs / hierarchy links are a shared workspace UI concern, but the current model does not make them first-class.

## Target architecture

Use a single shared **AppChrome** model for the fixed UI around a page:

```text
Top bar
Sidebar
Breadcrumb / hierarchy links
Workspace switcher
Culture selector
User menu
Active navigation state
```

Target flow:

```text
Controller action
  -> page/application service gets page content data
  -> IAppChromeBuilder builds layout/chrome data
      -> IWorkspaceResolver resolves current workspace identity
      -> IBreadcrumbBuilder builds hierarchy links
      -> INavigationBuilder builds sidebar/topbar navigation
      -> ICultureOptionsBuilder builds language options
      -> IUserMenuBuilder builds user menu data
  -> page ViewModel = AppChrome + page content
  -> Razor view
```

The core rule:

```text
AppChrome = data needed by the layout
Page ViewModel = AppChrome + data needed by the page body
```

Example:

```csharp
public sealed class CustomersIndexViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();

    public IReadOnlyList<CustomerRowViewModel> Customers { get; init; } = [];
    public string? Search { get; init; }
}
```

The layout renders `AppChrome`. The page renders `Customers`, filters, forms, and page-specific buttons.

---

# Phase 0 — Safety preparation

## Purpose

Prepare the project so the UI refactor can be done incrementally without breaking the whole WebApp at once.

## Changes

### Create a branch

```bash
git checkout dev
git pull
git checkout -b refactor/ui-app-chrome
```

### Run the application before changing anything

Verify the current behavior of these pages:

```text
/m/{companySlug}
/m/{companySlug}/c/{customerSlug}
Management customers page
Management company users page, if available
Customer dashboard
Property dashboard, if available
Unit dashboard, if available
Resident dashboard, if available
```

### Take screenshots

Take screenshots of:

```text
Top bar
Sidebar
Culture selector
Workspace switcher
Management menu
Customer/property hierarchy navigation, if already visible anywhere
```

## Semantics

This phase does not change architecture. It establishes a visual baseline so the refactor can keep the same UI look.

## Done when

- The app runs.
- You know which page will be refactored first.
- You have screenshots or notes of the current layout.

Recommended first page:

```text
Management / Customers / Index
```

Reason: it is management-area UI, so it exercises `CanManageCompanyUsers`, sidebar state, company slug, workspace selector, and culture selector.

---

# Phase 1 — Add the AppChrome contract and base viewmodel

## Purpose

Introduce one clear model for layout/chrome data. This replaces the conceptual need for `PageShell`, `ManagementLayoutViewModel`, and layout-specific inheritance.

## Create

```text
WebApp/UI/Chrome/IAppChromePage.cs
WebApp/UI/Chrome/AppChromeViewModel.cs
WebApp/UI/Chrome/AppChromeRequest.cs
```

If you prefer to keep all UI viewmodels under `ViewModels`, use this equivalent structure instead:

```text
WebApp/ViewModels/Shared/Chrome/IAppChromePage.cs
WebApp/ViewModels/Shared/Chrome/AppChromeViewModel.cs
WebApp/ViewModels/Shared/Chrome/AppChromeRequest.cs
```

Pick one convention and stay consistent. I recommend `WebApp/UI/...` because these types are not page content viewmodels; they are UI composition infrastructure.

## Add `IAppChromePage`

```csharp
namespace WebApp.UI.Chrome;

public interface IAppChromePage
{
    AppChromeViewModel AppChrome { get; }
}
```

## Add `AppChromeViewModel`

```csharp
namespace WebApp.UI.Chrome;

public sealed class AppChromeViewModel
{
    public string PageTitle { get; init; } = string.Empty;

    public string ActiveSection { get; init; } = string.Empty;

    public WorkspaceIdentityViewModel Workspace { get; init; } = new();

    public IReadOnlyList<BreadcrumbLinkViewModel> Breadcrumbs { get; init; } = [];

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> ManagementWorkspaceOptions { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> CustomerWorkspaceOptions { get; init; } = [];

    public IReadOnlyList<CultureOptionViewModel> CultureOptions { get; init; } = [];

    public UserMenuViewModel UserMenu { get; init; } = new();

    public string CurrentPathAndQuery { get; init; } = string.Empty;

    public string CurrentUiCultureName { get; init; } = string.Empty;
}
```

## Add `AppChromeRequest`

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WebApp.UI.Chrome;

public sealed class AppChromeRequest
{
    public ClaimsPrincipal User { get; init; } = default!;

    public HttpContext HttpContext { get; init; } = default!;

    public string PageTitle { get; init; } = string.Empty;

    public string ActiveSection { get; init; } = string.Empty;

    public string? ManagementCompanySlug { get; init; }

    public string? CustomerSlug { get; init; }

    public string? PropertySlug { get; init; }

    public string? UnitSlug { get; init; }

    public WorkspaceLevel CurrentLevel { get; init; } = WorkspaceLevel.None;
}
```

## Semantics

`AppChromeViewModel` means: **everything the shared application chrome needs to render itself**.

It should contain:

- page title,
- active navigation section,
- current workspace identity,
- breadcrumbs / hierarchy links,
- navigation items,
- workspace switch options,
- culture options,
- user menu data.

It should not contain:

- customer table rows,
- property dashboard statistics,
- forms,
- validation data,
- domain entities,
- EF entities,
- page-specific command DTOs.

## Done when

The project compiles after adding these types. They do not need to be used yet.

---

# Phase 2 — Add workspace identity types

## Purpose

Represent where the user currently is in the workspace hierarchy.

This is needed because your UI needs hierarchy navigation such as:

```text
Management Company -> Customer -> Property -> Unit
```

That is not a management-only concern. It is shared workspace UI state.

## Create

```text
WebApp/UI/Workspace/WorkspaceLevel.cs
WebApp/UI/Workspace/WorkspaceIdentityViewModel.cs
WebApp/UI/Workspace/WorkspaceSwitchOptionViewModel.cs
```

## Add `WorkspaceLevel`

```csharp
namespace WebApp.UI.Workspace;

public enum WorkspaceLevel
{
    None = 0,
    ManagementCompany = 1,
    Customer = 2,
    Property = 3,
    Unit = 4,
    Resident = 5
}
```

## Add `WorkspaceIdentityViewModel`

```csharp
namespace WebApp.UI.Workspace;

public sealed class WorkspaceIdentityViewModel
{
    public WorkspaceLevel Level { get; init; } = WorkspaceLevel.None;

    public string? ManagementCompanySlug { get; init; }

    public string? ManagementCompanyName { get; init; }

    public string? CustomerSlug { get; init; }

    public string? CustomerName { get; init; }

    public string? PropertySlug { get; init; }

    public string? PropertyName { get; init; }

    public string? UnitSlug { get; init; }

    public string? UnitName { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool HasResidentContext { get; init; }
}
```

## Add `WorkspaceSwitchOptionViewModel`

```csharp
namespace WebApp.UI.Workspace;

public sealed class WorkspaceSwitchOptionViewModel
{
    public Guid Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }

    public string? Url { get; init; }
}
```

## Semantics

`WorkspaceIdentityViewModel` answers:

```text
Where is the user right now?
```

Examples:

```text
Management company page:
  Level = ManagementCompany
  ManagementCompanySlug = "best-management"
  DisplayName = "Best Management OÜ"

Property page:
  Level = Property
  ManagementCompanySlug = "best-management"
  CustomerSlug = "customer-a"
  PropertySlug = "property-x"
  DisplayName = "Property X"
```

`WorkspaceSwitchOptionViewModel` answers:

```text
Which workspaces can the user switch to from the layout?
```

## BLL note

At first, map this from existing BLL data exposed by `IUserWorkspaceCatalogService`.

Do not add DAL queries in WebApp.

If property/unit names are not available through the existing BLL data, leave those specific fields null for the first phase and add BLL support later.

## Done when

The workspace identity models exist and compile.

---

# Phase 3 — Add breadcrumb / hierarchy link model and builder

## Purpose

Make the top-right hierarchy navigation a first-class UI concept.

This directly supports the requirement:

```text
If the user is in a property, they can go straight back to the customer or management company through links.
```

## Create

```text
WebApp/UI/Breadcrumbs/BreadcrumbLinkViewModel.cs
WebApp/UI/Breadcrumbs/IBreadcrumbBuilder.cs
WebApp/UI/Breadcrumbs/BreadcrumbBuilder.cs
```

## Add `BreadcrumbLinkViewModel`

```csharp
using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public sealed class BreadcrumbLinkViewModel
{
    public string Label { get; init; } = string.Empty;

    public string? Url { get; init; }

    public bool IsCurrent { get; init; }

    public WorkspaceLevel Level { get; init; }
}
```

## Add `IBreadcrumbBuilder`

```csharp
using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public interface IBreadcrumbBuilder
{
    IReadOnlyList<BreadcrumbLinkViewModel> Build(WorkspaceIdentityViewModel workspace);
}
```

## Add `BreadcrumbBuilder`

```csharp
using WebApp.UI.Workspace;

namespace WebApp.UI.Breadcrumbs;

public sealed class BreadcrumbBuilder : IBreadcrumbBuilder
{
    public IReadOnlyList<BreadcrumbLinkViewModel> Build(WorkspaceIdentityViewModel workspace)
    {
        var links = new List<BreadcrumbLinkViewModel>();

        if (!string.IsNullOrWhiteSpace(workspace.ManagementCompanySlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.ManagementCompanyName ?? workspace.ManagementCompanySlug,
                Url = workspace.Level == WorkspaceLevel.ManagementCompany
                    ? null
                    : $"/m/{workspace.ManagementCompanySlug}",
                IsCurrent = workspace.Level == WorkspaceLevel.ManagementCompany,
                Level = WorkspaceLevel.ManagementCompany
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.CustomerSlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.CustomerName ?? workspace.CustomerSlug,
                Url = workspace.Level == WorkspaceLevel.Customer
                    ? null
                    : $"/m/{workspace.ManagementCompanySlug}/c/{workspace.CustomerSlug}",
                IsCurrent = workspace.Level == WorkspaceLevel.Customer,
                Level = WorkspaceLevel.Customer
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.PropertySlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.PropertyName ?? workspace.PropertySlug,
                Url = workspace.Level == WorkspaceLevel.Property
                    ? null
                    : $"/m/{workspace.ManagementCompanySlug}/c/{workspace.CustomerSlug}/p/{workspace.PropertySlug}",
                IsCurrent = workspace.Level == WorkspaceLevel.Property,
                Level = WorkspaceLevel.Property
            });
        }

        if (!string.IsNullOrWhiteSpace(workspace.UnitSlug))
        {
            links.Add(new BreadcrumbLinkViewModel
            {
                Label = workspace.UnitName ?? workspace.UnitSlug,
                Url = workspace.Level == WorkspaceLevel.Unit
                    ? null
                    : $"/m/{workspace.ManagementCompanySlug}/c/{workspace.CustomerSlug}/p/{workspace.PropertySlug}/u/{workspace.UnitSlug}",
                IsCurrent = workspace.Level == WorkspaceLevel.Unit,
                Level = WorkspaceLevel.Unit
            });
        }

        return links;
    }
}
```

## Important route note

The URLs above are examples based on your short route style. Adjust them to the actual routes in `Program.cs` and area controllers.

If you want compile-time route safety, use `IUrlHelperFactory` later. For the first refactor, simple URL composition is acceptable if it matches existing routes.

## Semantics

Breadcrumbs are not page data. They are layout navigation data.

The controller should not decide which hierarchy links exist. The Razor view should not calculate them either.

Correct responsibility:

```text
WorkspaceIdentityViewModel -> BreadcrumbBuilder -> BreadcrumbLinkViewModel list -> Razor renders links
```

## Done when

- Breadcrumb types compile.
- `BreadcrumbBuilder` can build links from a `WorkspaceIdentityViewModel`.
- No existing layout uses them yet.

---

# Phase 4 — Add navigation model and builder

## Purpose

Move sidebar/topbar menu decisions out of Razor views and out of controllers.

The layout should only render navigation items. It should not contain business permission logic.

## Create

```text
WebApp/UI/Navigation/NavigationItemViewModel.cs
WebApp/UI/Navigation/Sections.cs
WebApp/UI/Navigation/INavigationBuilder.cs
WebApp/UI/Navigation/NavigationBuilder.cs
```

## Add `NavigationItemViewModel`

```csharp
namespace WebApp.UI.Navigation;

public sealed class NavigationItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Section { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool IsVisible { get; init; } = true;

    public string? IconCssClass { get; init; }
}
```

## Add `Sections`

```csharp
namespace WebApp.UI.Navigation;

public static class Sections
{
    public const string Dashboard = "Dashboard";
    public const string Customers = "Customers";
    public const string Properties = "Properties";
    public const string Units = "Units";
    public const string Residents = "Residents";
    public const string Tickets = "Tickets";
    public const string CompanyUsers = "CompanyUsers";
    public const string Settings = "Settings";
}
```

## Add `INavigationBuilder`

```csharp
using WebApp.UI.Workspace;

namespace WebApp.UI.Navigation;

public interface INavigationBuilder
{
    IReadOnlyList<NavigationItemViewModel> Build(
        WorkspaceIdentityViewModel workspace,
        string activeSection,
        bool canManageCompanyUsers);
}
```

## Add `NavigationBuilder`

```csharp
using App.Resources.Views;
using WebApp.UI.Workspace;

namespace WebApp.UI.Navigation;

public sealed class NavigationBuilder : INavigationBuilder
{
    public IReadOnlyList<NavigationItemViewModel> Build(
        WorkspaceIdentityViewModel workspace,
        string activeSection,
        bool canManageCompanyUsers)
    {
        var companySlug = workspace.ManagementCompanySlug ?? string.Empty;

        var items = new List<NavigationItemViewModel>
        {
            new()
            {
                Label = UiText.Dashboard,
                Url = $"/m/{companySlug}",
                Section = Sections.Dashboard,
                IsActive = activeSection == Sections.Dashboard
            },
            new()
            {
                Label = UiText.Customers,
                Url = $"/Management/Customers?companySlug={companySlug}",
                Section = Sections.Customers,
                IsActive = activeSection == Sections.Customers
            },
            new()
            {
                Label = UiText.CompanyUsers,
                Url = $"/Management/CompanyUsers?companySlug={companySlug}",
                Section = Sections.CompanyUsers,
                IsVisible = canManageCompanyUsers,
                IsActive = activeSection == Sections.CompanyUsers
            }
        };

        return items;
    }
}
```

## Route note

The sample URLs must be aligned with your actual route attributes/actions. If your controllers use area routes differently, update the URL strings.

## Semantics

`NavigationBuilder` answers:

```text
Which navigation links should the current layout show?
Which one is active?
Which ones should be hidden because of layout-level permissions?
```

This is where `CanManageCompanyUsers` belongs if it only controls sidebar visibility.

Do not put `CanManageCompanyUsers` into a shared workspace model as a random boolean. Instead:

```text
BLL gives permission/capability -> NavigationBuilder uses it -> navigation item IsVisible
```

## Done when

- Navigation types compile.
- Navigation visibility rules are outside Razor.

---

# Phase 5 — Add culture and user menu builders

## Purpose

Keep `AppChromeBuilder` small. Culture options and user menu data are separate concerns.

## Create

```text
WebApp/UI/Culture/CultureOptionViewModel.cs
WebApp/UI/Culture/ICultureOptionsBuilder.cs
WebApp/UI/Culture/CultureOptionsBuilder.cs
WebApp/UI/UserMenu/UserMenuViewModel.cs
WebApp/UI/UserMenu/IUserMenuBuilder.cs
WebApp/UI/UserMenu/UserMenuBuilder.cs
```

## Add `CultureOptionViewModel`

```csharp
namespace WebApp.UI.Culture;

public sealed class CultureOptionViewModel
{
    public string Value { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }
}
```

## Add `ICultureOptionsBuilder`

```csharp
namespace WebApp.UI.Culture;

public interface ICultureOptionsBuilder
{
    IReadOnlyList<CultureOptionViewModel> Build(string currentUiCultureName);
}
```

## Add `CultureOptionsBuilder`

```csharp
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace WebApp.UI.Culture;

public sealed class CultureOptionsBuilder : ICultureOptionsBuilder
{
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;

    public CultureOptionsBuilder(IOptions<RequestLocalizationOptions> localizationOptions)
    {
        _localizationOptions = localizationOptions;
    }

    public IReadOnlyList<CultureOptionViewModel> Build(string currentUiCultureName)
    {
        return _localizationOptions.Value.SupportedUICultures!
            .Select(culture => new CultureOptionViewModel
            {
                Value = culture.Name,
                Text = culture.NativeName,
                IsCurrent = culture.Name == currentUiCultureName
            })
            .ToList();
    }
}
```

## Add `UserMenuViewModel`

```csharp
namespace WebApp.UI.UserMenu;

public sealed class UserMenuViewModel
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public bool IsAuthenticated { get; init; }
}
```

## Add `IUserMenuBuilder`

```csharp
using System.Security.Claims;

namespace WebApp.UI.UserMenu;

public interface IUserMenuBuilder
{
    UserMenuViewModel Build(ClaimsPrincipal user);
}
```

## Add `UserMenuBuilder`

```csharp
using System.Security.Claims;

namespace WebApp.UI.UserMenu;

public sealed class UserMenuBuilder : IUserMenuBuilder
{
    public UserMenuViewModel Build(ClaimsPrincipal user)
    {
        return new UserMenuViewModel
        {
            IsAuthenticated = user.Identity?.IsAuthenticated == true,
            DisplayName = user.Identity?.Name ?? string.Empty,
            Email = user.FindFirstValue(ClaimTypes.Email)
        };
    }
}
```

## Semantics

Culture options are layout data, but they are not workspace identity. User menu is layout data, but it is not navigation or breadcrumbs.

Small builders keep each responsibility clear.

## Done when

The culture and user menu builders compile.

---

# Phase 6 — Add workspace resolver

## Purpose

Resolve current workspace UI identity from route slugs and existing BLL data.

This is the replacement for the current shared layout provider concept, but with a clearer name and smaller responsibility.

## Create

```text
WebApp/UI/Workspace/IWorkspaceResolver.cs
WebApp/UI/Workspace/WorkspaceResolver.cs
```

## Add `IWorkspaceResolver`

```csharp
using WebApp.UI.Chrome;

namespace WebApp.UI.Workspace;

public interface IWorkspaceResolver
{
    Task<WorkspaceResolutionResult> ResolveAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default);
}
```

## Add `WorkspaceResolutionResult`

Create this in:

```text
WebApp/UI/Workspace/WorkspaceResolutionResult.cs
```

```csharp
namespace WebApp.UI.Workspace;

public sealed class WorkspaceResolutionResult
{
    public WorkspaceIdentityViewModel Workspace { get; init; } = new();

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> ManagementWorkspaceOptions { get; init; } = [];

    public IReadOnlyList<WorkspaceSwitchOptionViewModel> CustomerWorkspaceOptions { get; init; } = [];

    public bool CanManageCompanyUsers { get; init; }
}
```

## Add `WorkspaceResolver`

```csharp
using System.Security.Claims;
using App.BLL.Onboarding.WorkspaceCatalog;
using WebApp.UI.Chrome;

namespace WebApp.UI.Workspace;

public sealed class WorkspaceResolver : IWorkspaceResolver
{
    private readonly IUserWorkspaceCatalogService _userWorkspaceCatalogService;

    public WorkspaceResolver(IUserWorkspaceCatalogService userWorkspaceCatalogService)
    {
        _userWorkspaceCatalogService = userWorkspaceCatalogService;
    }

    public async Task<WorkspaceResolutionResult> ResolveAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default)
    {
        var appUserIdValue = request.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(appUserIdValue, out var appUserId))
        {
            return new WorkspaceResolutionResult
            {
                Workspace = new WorkspaceIdentityViewModel
                {
                    Level = request.CurrentLevel,
                    ManagementCompanySlug = request.ManagementCompanySlug,
                    CustomerSlug = request.CustomerSlug,
                    PropertySlug = request.PropertySlug,
                    UnitSlug = request.UnitSlug,
                    DisplayName = string.Empty
                }
            };
        }

        var catalog = await _userWorkspaceCatalogService.GetUserContextCatalogAsync(
            appUserId,
            request.ManagementCompanySlug ?? string.Empty,
            cancellationToken);

        var managementOptions = catalog.ManagementCompanies
            .Select(x => new WorkspaceSwitchOptionViewModel
            {
                Id = x.ManagementCompanyId,
                Slug = x.Slug,
                Name = x.CompanyName,
                IsCurrent = x.Slug == request.ManagementCompanySlug,
                Url = $"/m/{x.Slug}"
            })
            .ToList();

        var customerOptions = catalog.Customers
            .Select(x => new WorkspaceSwitchOptionViewModel
            {
                Id = x.CustomerId,
                Slug = x.Slug,
                Name = x.Name,
                IsCurrent = x.Slug == request.CustomerSlug,
                Url = $"/m/{request.ManagementCompanySlug}/c/{x.Slug}"
            })
            .ToList();

        var currentCustomer = catalog.Customers
            .FirstOrDefault(x => x.Slug == request.CustomerSlug);

        var workspaceDisplayName = request.CurrentLevel switch
        {
            WorkspaceLevel.Customer when currentCustomer is not null => currentCustomer.Name,
            _ => catalog.ManagementCompanyName
        };

        return new WorkspaceResolutionResult
        {
            Workspace = new WorkspaceIdentityViewModel
            {
                Level = request.CurrentLevel,
                ManagementCompanySlug = request.ManagementCompanySlug,
                ManagementCompanyName = catalog.ManagementCompanyName,
                CustomerSlug = request.CustomerSlug,
                CustomerName = currentCustomer?.Name,
                PropertySlug = request.PropertySlug,
                UnitSlug = request.UnitSlug,
                DisplayName = workspaceDisplayName,
                HasResidentContext = catalog.HasResidentContext
            },
            ManagementWorkspaceOptions = managementOptions,
            CustomerWorkspaceOptions = customerOptions,
            CanManageCompanyUsers = catalog.CanManageCompanyUsers
        };
    }
}
```

## Important BLL limitation

This resolver can only map what `IUserWorkspaceCatalogService` already returns.

If the current BLL catalog does not expose property/unit names, do not query `AppDbContext` from WebApp. Instead:

1. Complete the refactor with null property/unit names.
2. Add a later BLL method such as:

```csharp
Task<WorkspaceUiContextDto> GetWorkspaceUiContextAsync(
    Guid userId,
    WorkspaceRouteContextDto routeContext,
    CancellationToken cancellationToken = default);
```

3. Let BLL query DAL and return the needed display names.

## Semantics

`WorkspaceResolver` answers:

```text
Given the current user and route slugs, what workspace context should the UI display?
```

It does not build navigation. It does not render breadcrumbs. It only resolves identity and available switch options.

## Done when

- `WorkspaceResolver` compiles.
- It calls `IUserWorkspaceCatalogService` once per chrome build.
- It returns `CanManageCompanyUsers` as capability data for navigation building.

---

# Phase 7 — Add AppChromeBuilder

## Purpose

Create one composition service that builds the final layout/chrome model for each page.

This becomes the controller-facing API for UI chrome.

## Create

```text
WebApp/UI/Chrome/IAppChromeBuilder.cs
WebApp/UI/Chrome/AppChromeBuilder.cs
```

## Add `IAppChromeBuilder`

```csharp
namespace WebApp.UI.Chrome;

public interface IAppChromeBuilder
{
    Task<AppChromeViewModel> BuildAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default);
}
```

## Add `AppChromeBuilder`

```csharp
using System.Globalization;
using WebApp.UI.Breadcrumbs;
using WebApp.UI.Culture;
using WebApp.UI.Navigation;
using WebApp.UI.UserMenu;
using WebApp.UI.Workspace;

namespace WebApp.UI.Chrome;

public sealed class AppChromeBuilder : IAppChromeBuilder
{
    private readonly IWorkspaceResolver _workspaceResolver;
    private readonly IBreadcrumbBuilder _breadcrumbBuilder;
    private readonly INavigationBuilder _navigationBuilder;
    private readonly ICultureOptionsBuilder _cultureOptionsBuilder;
    private readonly IUserMenuBuilder _userMenuBuilder;

    public AppChromeBuilder(
        IWorkspaceResolver workspaceResolver,
        IBreadcrumbBuilder breadcrumbBuilder,
        INavigationBuilder navigationBuilder,
        ICultureOptionsBuilder cultureOptionsBuilder,
        IUserMenuBuilder userMenuBuilder)
    {
        _workspaceResolver = workspaceResolver;
        _breadcrumbBuilder = breadcrumbBuilder;
        _navigationBuilder = navigationBuilder;
        _cultureOptionsBuilder = cultureOptionsBuilder;
        _userMenuBuilder = userMenuBuilder;
    }

    public async Task<AppChromeViewModel> BuildAsync(
        AppChromeRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolvedWorkspace = await _workspaceResolver.ResolveAsync(request, cancellationToken);

        var breadcrumbs = _breadcrumbBuilder.Build(resolvedWorkspace.Workspace);

        var navigationItems = _navigationBuilder.Build(
            resolvedWorkspace.Workspace,
            request.ActiveSection,
            resolvedWorkspace.CanManageCompanyUsers);

        var currentUiCultureName = CultureInfo.CurrentUICulture.Name;

        return new AppChromeViewModel
        {
            PageTitle = request.PageTitle,
            ActiveSection = request.ActiveSection,
            Workspace = resolvedWorkspace.Workspace,
            Breadcrumbs = breadcrumbs,
            NavigationItems = navigationItems,
            ManagementWorkspaceOptions = resolvedWorkspace.ManagementWorkspaceOptions,
            CustomerWorkspaceOptions = resolvedWorkspace.CustomerWorkspaceOptions,
            CultureOptions = _cultureOptionsBuilder.Build(currentUiCultureName),
            UserMenu = _userMenuBuilder.Build(request.User),
            CurrentPathAndQuery = $"{request.HttpContext.Request.Path}{request.HttpContext.Request.QueryString}",
            CurrentUiCultureName = currentUiCultureName
        };
    }
}
```

## Semantics

`AppChromeBuilder` is an orchestrator. It should not contain all logic itself.

It coordinates these responsibilities:

```text
WorkspaceResolver -> who/where is the current workspace?
BreadcrumbBuilder -> how do we navigate upward?
NavigationBuilder -> what sidebar/topbar links are visible?
CultureOptionsBuilder -> what languages can the user choose?
UserMenuBuilder -> what should the user menu show?
```

Controllers depend on this one interface instead of several layout-specific providers.

## Done when

- `IAppChromeBuilder` and `AppChromeBuilder` compile.
- All builder dependencies compile.

---

# Phase 8 — Register new UI services in DI

## Purpose

Make the new UI composition services available to controllers.

## Change

Modify:

```text
WebApp/Program.cs
```

Add using statements:

```csharp
using WebApp.UI.Breadcrumbs;
using WebApp.UI.Chrome;
using WebApp.UI.Culture;
using WebApp.UI.Navigation;
using WebApp.UI.UserMenu;
using WebApp.UI.Workspace;
```

Add service registrations near the other WebApp service registrations:

```csharp
builder.Services.AddScoped<IAppChromeBuilder, AppChromeBuilder>();
builder.Services.AddScoped<IWorkspaceResolver, WorkspaceResolver>();
builder.Services.AddScoped<IBreadcrumbBuilder, BreadcrumbBuilder>();
builder.Services.AddScoped<INavigationBuilder, NavigationBuilder>();
builder.Services.AddScoped<ICultureOptionsBuilder, CultureOptionsBuilder>();
builder.Services.AddScoped<IUserMenuBuilder, UserMenuBuilder>();
```

Keep these temporarily during migration:

```csharp
builder.Services.AddScoped<IWorkspaceLayoutContextProvider, WorkspaceLayoutContextProvider>();
builder.Services.AddScoped<IManagementLayoutViewModelProvider, ManagementLayoutViewModelProvider>();
```

Do not delete old registrations until the old classes are no longer used.

## Semantics

This phase allows the old and new systems to exist side by side. That enables vertical migration page by page.

## Done when

- The app compiles.
- No controller uses `IAppChromeBuilder` yet.
- Existing UI still works.

---

# Phase 9 — Create shared Razor partials for AppChrome

## Purpose

Move rendering of chrome pieces into small partials. The partials should be render-only: they receive viewmodels and produce HTML.

## Create

```text
WebApp/Views/Shared/Chrome/_AppChrome.cshtml
WebApp/Views/Shared/Chrome/_TopBar.cshtml
WebApp/Views/Shared/Chrome/_Sidebar.cshtml
WebApp/Views/Shared/Chrome/_Breadcrumbs.cshtml
WebApp/Views/Shared/Chrome/_CultureSelector.cshtml
WebApp/Views/Shared/Chrome/_WorkspaceSwitcher.cshtml
WebApp/Views/Shared/Chrome/_UserMenu.cshtml
```

## `_Breadcrumbs.cshtml`

```csharp
@model IReadOnlyList<WebApp.UI.Breadcrumbs.BreadcrumbLinkViewModel>

@if (Model.Count > 0)
{
    <nav class="workspace-breadcrumbs" aria-label="Workspace hierarchy">
        @for (var i = 0; i < Model.Count; i++)
        {
            var item = Model[i];

            if (i > 0)
            {
                <span class="workspace-breadcrumbs__separator">/</span>
            }

            if (!item.IsCurrent && !string.IsNullOrWhiteSpace(item.Url))
            {
                <a href="@item.Url">@item.Label</a>
            }
            else
            {
                <span aria-current="page">@item.Label</span>
            }
        }
    </nav>
}
```

## `_Sidebar.cshtml`

```csharp
@model IReadOnlyList<WebApp.UI.Navigation.NavigationItemViewModel>

<nav class="app-sidebar">
    @foreach (var item in Model.Where(x => x.IsVisible))
    {
        <a href="@item.Url" class="@(item.IsActive ? "active" : string.Empty)">
            @if (!string.IsNullOrWhiteSpace(item.IconCssClass))
            {
                <i class="@item.IconCssClass"></i>
            }
            <span>@item.Label</span>
        </a>
    }
</nav>
```

## `_CultureSelector.cshtml`

```csharp
@model IReadOnlyList<WebApp.UI.Culture.CultureOptionViewModel>

<form method="post" asp-area="" asp-controller="Culture" asp-action="SetCulture">
    <select name="culture" onchange="this.form.submit()">
        @foreach (var culture in Model)
        {
            <option value="@culture.Value" selected="@culture.IsCurrent">
                @culture.Text
            </option>
        }
    </select>
</form>
```

Adjust the culture action/controller route to match your current implementation.

## Semantics

Partials should not calculate permissions or query services.

Correct:

```text
NavigationBuilder decides visibility.
_Sidebar.cshtml renders visible items.
```

Wrong:

```text
_Sidebar.cshtml asks whether the user can manage company users.
```

## Done when

- Partials compile.
- They are not necessarily used yet.

---

# Phase 10 — Update the main/shared layout to use `IAppChromePage`

## Purpose

Make the layout consume the new model.

## Change

Modify the relevant layout file, likely one or more of:

```text
WebApp/Views/Shared/_Layout.cshtml
WebApp/Areas/Management/Views/Shared/_ManagementLayout.cshtml
WebApp/Areas/Customer/Views/Shared/_CustomerLayout.cshtml
WebApp/Areas/Property/Views/Shared/_PropertyLayout.cshtml
WebApp/Areas/Unit/Views/Shared/_UnitLayout.cshtml
WebApp/Areas/Resident/Views/Shared/_ResidentLayout.cshtml
```

The first layout to update should be `_ManagementLayout.cshtml`, because this is where the current management layout provider is used.

At the top of the layout, instead of expecting the old management shell/page shell type, use:

```csharp
@using WebApp.UI.Chrome
@model IAppChromePage

@{
    var chrome = Model.AppChrome;
}
```

Then replace old layout variable usage:

```csharp
pageShell.Title
pageShell.CurrentSectionLabel
pageShell.Management.ManagementCompanyName
pageShell.Management.CanManageCompanyUsers
pageShell.Management.ManagementContexts
pageShell.Management.CustomerContexts
pageShell.Management.CultureOptions
```

with:

```csharp
chrome.PageTitle
chrome.ActiveSection
chrome.Workspace.DisplayName
chrome.NavigationItems
chrome.ManagementWorkspaceOptions
chrome.CustomerWorkspaceOptions
chrome.CultureOptions
chrome.Breadcrumbs
```

Render the partials:

```csharp
@await Html.PartialAsync("~/Views/Shared/Chrome/_Breadcrumbs.cshtml", chrome.Breadcrumbs)
@await Html.PartialAsync("~/Views/Shared/Chrome/_Sidebar.cshtml", chrome.NavigationItems)
@await Html.PartialAsync("~/Views/Shared/Chrome/_CultureSelector.cshtml", chrome.CultureOptions)
```

## Semantics

The layout now has one dependency:

```text
IAppChromePage
```

That means every page using this layout must provide `AppChrome`.

This is good because the layout no longer needs to know whether the page is management/customer/property/unit/resident. It only knows how to render app chrome.

## Done when

- The modified layout compiles.
- At least one page viewmodel implements `IAppChromePage`.

---

# Phase 11 — Migrate one management page vertically

## Purpose

Prove the new architecture on one real page before touching the whole UI.

Recommended first target:

```text
WebApp/Areas/Management/Controllers/CustomersController.cs
```

and its index viewmodel/view.

## Change the page viewmodel

Find the existing customers index viewmodel. Add:

```csharp
using WebApp.UI.Chrome;

public sealed class CustomersIndexViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();

    // existing page-specific properties stay here
}
```

If the existing viewmodel currently has:

```csharp
public ManagementPageShellViewModel PageShell { get; init; }
```

replace it with:

```csharp
public AppChromeViewModel AppChrome { get; init; } = new();
```

## Change the controller dependency

Remove inheritance from:

```csharp
ManagementPageShellController
```

Use normal `Controller` inheritance:

```csharp
public sealed class CustomersController : Controller
```

Inject:

```csharp
private readonly IAppChromeBuilder _appChromeBuilder;
```

## Change controller action

Old style:

```csharp
var pageShell = await BuildManagementPageShellAsync(
    title,
    currentSectionLabel,
    companySlug,
    cancellationToken);
```

New style:

```csharp
var appChrome = await _appChromeBuilder.BuildAsync(
    new AppChromeRequest
    {
        User = User,
        HttpContext = HttpContext,
        PageTitle = "Customers",
        ActiveSection = Sections.Customers,
        ManagementCompanySlug = companySlug,
        CurrentLevel = WorkspaceLevel.ManagementCompany
    },
    cancellationToken);
```

Then:

```csharp
var vm = new CustomersIndexViewModel
{
    AppChrome = appChrome,
    Customers = customers
};
```

## Semantics

This removes hidden base-controller behavior.

The controller now clearly says:

```text
1. Get page data.
2. Build app chrome.
3. Return viewmodel.
```

The controller does not know how to build breadcrumbs or sidebar visibility.

## Done when

- The management customers page works.
- The UI looks the same as before.
- The sidebar still hides/shows Company Users correctly.
- The culture selector still works.
- The workspace switchers still work.

---

# Phase 12 — Delete the old management layout provider path after migration

## Purpose

Remove the old duplicate architecture once no controller/view uses it.

## First search for old usages

Run:

```bash
grep -R "ManagementPageShellController" -n WebApp
grep -R "IManagementLayoutViewModelProvider" -n WebApp
grep -R "ManagementLayoutViewModelProvider" -n WebApp
grep -R "ManagementLayoutViewModel" -n WebApp
grep -R "ManagementPageShellViewModel" -n WebApp
grep -R "PageShell" -n WebApp
```

Do not delete anything until usage count is zero or intentionally replaced.

## Delete

Delete these files when all usages are gone:

```text
WebApp/Areas/Management/Controllers/ManagementPageShellController.cs
WebApp/Services/ManagementLayout/IManagementLayoutViewModelProvider.cs
WebApp/Services/ManagementLayout/ManagementLayoutViewModelProvider.cs
WebApp/ViewModels/Management/ManagementLayoutViewModel.cs
```

Delete old page shell files if they exist and are no longer used:

```text
WebApp/ViewModels/Management/ManagementPageShellViewModel.cs
WebApp/ViewModels/Shared/PageShell/IHasPageShell.cs
WebApp/ViewModels/Shared/PageShell/*
```

Only delete shared page shell files if the entire app has moved to `IAppChromePage`.

## Change DI registration

Remove from `Program.cs`:

```csharp
builder.Services.AddScoped<IManagementLayoutViewModelProvider, ManagementLayoutViewModelProvider>();
```

Remove using:

```csharp
using WebApp.Services.ManagementLayout;
```

If nothing uses the old shared layout provider, remove:

```csharp
builder.Services.AddScoped<IWorkspaceLayoutContextProvider, WorkspaceLayoutContextProvider>();
```

and:

```csharp
using WebApp.Services.SharedLayout;
```

Then delete old shared layout provider files only if fully unused:

```text
WebApp/Services/SharedLayout/IWorkspaceLayoutContextProvider.cs
WebApp/Services/SharedLayout/WorkspaceLayoutContextProvider.cs
WebApp/ViewModels/Shared/Layout/WorkspaceLayoutContextViewModel.cs
WebApp/ViewModels/Shared/Layout/WorkspaceLayoutRequestViewModel.cs
```

## Semantics

This phase removes the old architecture only after the new one has replaced it.

Do not keep both systems long-term. That would make the architecture harder to understand.

## Done when

- The project compiles without old management layout services.
- Grep confirms no references to old provider/base-controller/page-shell types.

---

# Phase 13 — Migrate remaining management pages

## Purpose

Move all management pages to the same explicit AppChrome pattern.

## Change every management page controller

For each controller in:

```text
WebApp/Areas/Management/Controllers/
```

Do this:

1. Inherit from `Controller`, not a page-shell base controller.
2. Inject `IAppChromeBuilder`.
3. Build `AppChrome` in every action that returns a full page.
4. Assign it to the page viewmodel.
5. Use `Sections.*` constants for active navigation.

## Example controller action pattern

```csharp
public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
{
    var pageData = await _someService.GetPageDataAsync(companySlug, cancellationToken);

    var appChrome = await _appChromeBuilder.BuildAsync(
        new AppChromeRequest
        {
            User = User,
            HttpContext = HttpContext,
            PageTitle = "Page Title",
            ActiveSection = Sections.SomeSection,
            ManagementCompanySlug = companySlug,
            CurrentLevel = WorkspaceLevel.ManagementCompany
        },
        cancellationToken);

    return View(new SomePageViewModel
    {
        AppChrome = appChrome,
        Data = pageData
    });
}
```

## Semantics

Every full Razor page follows the same mental model:

```text
Page model = AppChrome + page content
```

## Done when

All Management pages compile and render with the new layout.

---

# Phase 14 — Migrate customer, property, unit, and resident pages

## Purpose

Use the same AppChrome system everywhere, not just management.

## Customer page request example

```csharp
var appChrome = await _appChromeBuilder.BuildAsync(
    new AppChromeRequest
    {
        User = User,
        HttpContext = HttpContext,
        PageTitle = customer.Name,
        ActiveSection = Sections.Dashboard,
        ManagementCompanySlug = companySlug,
        CustomerSlug = customerSlug,
        CurrentLevel = WorkspaceLevel.Customer
    },
    cancellationToken);
```

## Property page request example

```csharp
var appChrome = await _appChromeBuilder.BuildAsync(
    new AppChromeRequest
    {
        User = User,
        HttpContext = HttpContext,
        PageTitle = property.Name,
        ActiveSection = Sections.Properties,
        ManagementCompanySlug = companySlug,
        CustomerSlug = customerSlug,
        PropertySlug = propertySlug,
        CurrentLevel = WorkspaceLevel.Property
    },
    cancellationToken);
```

## Unit page request example

```csharp
var appChrome = await _appChromeBuilder.BuildAsync(
    new AppChromeRequest
    {
        User = User,
        HttpContext = HttpContext,
        PageTitle = unit.Name,
        ActiveSection = Sections.Units,
        ManagementCompanySlug = companySlug,
        CustomerSlug = customerSlug,
        PropertySlug = propertySlug,
        UnitSlug = unitSlug,
        CurrentLevel = WorkspaceLevel.Unit
    },
    cancellationToken);
```

## Semantics

Customer/property/unit/resident pages do not need their own layout model just because they are different areas.

They only need to pass more route context into `AppChromeRequest`.

## Done when

- Breadcrumbs show correct hierarchy for customer/property/unit pages.
- Layouts use `AppChrome` consistently.
- No area has a special layout provider unless there is a real, unique reason.

---

# Phase 15 — Add page-specific permissions in page viewmodels only

## Purpose

Avoid turning `AppChromeViewModel` into a dumping ground of booleans.

## Rule

Use this split:

```text
Layout navigation permission -> NavigationBuilder / AppChrome
Page button permission -> page viewmodel
Business authorization -> BLL/application service
```

## Example

Showing `Company Users` in sidebar:

```text
NavigationBuilder uses CanManageCompanyUsers.
```

Showing a `Create Customer` button on the customers page:

```csharp
public sealed class CustomersIndexViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();

    public IReadOnlyList<CustomerRowViewModel> Customers { get; init; } = [];

    public bool CanCreateCustomer { get; init; }
}
```

Actually creating a customer:

```text
BLL service validates authorization again.
```

## Semantics

UI permissions are only hints for what to display. They are not security boundaries.

BLL/application services still enforce authorization.

## Done when

No random `Can*` booleans are added to `AppChromeViewModel` unless they directly affect shared chrome/navigation.

---

# Phase 16 — Optional BLL extension for complete hierarchy names

## Purpose

Only use this phase if the WebApp cannot build correct breadcrumbs because existing BLL data does not expose customer/property/unit names.

## Do not do this in WebApp

Do not inject:

```csharp
AppDbContext
```

into:

```text
AppChromeBuilder
WorkspaceResolver
BreadcrumbBuilder
NavigationBuilder
Razor views
```

## Add BLL DTOs if needed

Create in `App.DTO` or appropriate BLL DTO folder:

```text
WorkspaceUiContextDto.cs
WorkspaceRouteContextDto.cs
WorkspaceHierarchyItemDto.cs
```

Example:

```csharp
public sealed class WorkspaceRouteContextDto
{
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitSlug { get; init; }
}
```

```csharp
public sealed class WorkspaceUiContextDto
{
    public string? ManagementCompanySlug { get; init; }
    public string? ManagementCompanyName { get; init; }
    public string? CustomerSlug { get; init; }
    public string? CustomerName { get; init; }
    public string? PropertySlug { get; init; }
    public string? PropertyName { get; init; }
    public string? UnitSlug { get; init; }
    public string? UnitName { get; init; }
    public bool CanManageCompanyUsers { get; init; }
    public bool HasResidentContext { get; init; }
}
```

Add a BLL service method:

```csharp
Task<WorkspaceUiContextDto> GetWorkspaceUiContextAsync(
    Guid userId,
    WorkspaceRouteContextDto routeContext,
    CancellationToken cancellationToken = default);
```

Then let `WorkspaceResolver` call this BLL method instead of assembling hierarchy names from incomplete catalog data.

## Semantics

BLL owns business/workspace resolution. WebApp owns UI shape.

Correct dependency:

```text
WebApp -> BLL -> DAL
```

Wrong dependency:

```text
WebApp -> DAL
```

## Done when

- Breadcrumbs have real names for all levels.
- WebApp still does not reference `AppDbContext`.

---

# Phase 17 — Clean up old folders and namespaces

## Purpose

Remove dead UI architecture so future development follows one pattern.

## Delete if fully unused

```text
WebApp/Services/ManagementLayout/
WebApp/Services/SharedLayout/
WebApp/ViewModels/Management/Layout/
WebApp/ViewModels/Shared/Layout/
WebApp/ViewModels/Shared/PageShell/
```

Only delete `Shared/Layout` if all useful types have been moved/replaced by:

```text
WebApp/UI/Chrome/
WebApp/UI/Workspace/
WebApp/UI/Breadcrumbs/
WebApp/UI/Navigation/
WebApp/UI/Culture/
WebApp/UI/UserMenu/
```

## Update namespaces

Use consistent namespaces:

```text
WebApp.UI.Chrome
WebApp.UI.Workspace
WebApp.UI.Breadcrumbs
WebApp.UI.Navigation
WebApp.UI.Culture
WebApp.UI.UserMenu
```

## Semantics

A refactor is not finished while old architecture remains next to new architecture.

The goal is one obvious way to build a page.

## Done when

- Old folders are deleted or intentionally empty.
- Grep finds no old layout/page shell references.
- New pages naturally follow the AppChrome pattern.

---

# Phase 18 — Tests and verification

## Purpose

Verify behavior stayed the same while architecture changed.

## Manual tests

Check:

```text
Management dashboard loads.
Management customers page loads.
Company Users nav appears only when permitted.
Workspace switcher still lists management companies.
Customer switcher still lists customers.
Culture selector still changes language.
Breadcrumbs show correct hierarchy.
Breadcrumb links navigate upward correctly.
Customer dashboard loads.
Property dashboard loads.
Unit dashboard loads.
Resident dashboard loads.
Unauthenticated/invalid user behavior still works.
```

## Unit tests to add if test project is available

```text
BreadcrumbBuilderTests
NavigationBuilderTests
CultureOptionsBuilderTests
WorkspaceResolverTests with mocked IUserWorkspaceCatalogService
AppChromeBuilderTests with mocked sub-builders
```

## Example breadcrumb test cases

```text
ManagementCompany level -> one breadcrumb, current, no URL
Customer level -> management company link + current customer
Property level -> management company link + customer link + current property
Unit level -> management company link + customer link + property link + current unit
```

## Example navigation test cases

```text
canManageCompanyUsers = true -> Company Users item visible
canManageCompanyUsers = false -> Company Users item hidden
activeSection = Customers -> Customers item active
```

## Done when

- Existing UI behavior is preserved.
- The project builds.
- Tests pass.
- No old layout provider/base-controller references remain.

---

# Final target file map

## New files

```text
WebApp/UI/Chrome/IAppChromePage.cs
WebApp/UI/Chrome/AppChromeViewModel.cs
WebApp/UI/Chrome/AppChromeRequest.cs
WebApp/UI/Chrome/IAppChromeBuilder.cs
WebApp/UI/Chrome/AppChromeBuilder.cs

WebApp/UI/Workspace/WorkspaceLevel.cs
WebApp/UI/Workspace/WorkspaceIdentityViewModel.cs
WebApp/UI/Workspace/WorkspaceSwitchOptionViewModel.cs
WebApp/UI/Workspace/WorkspaceResolutionResult.cs
WebApp/UI/Workspace/IWorkspaceResolver.cs
WebApp/UI/Workspace/WorkspaceResolver.cs

WebApp/UI/Breadcrumbs/BreadcrumbLinkViewModel.cs
WebApp/UI/Breadcrumbs/IBreadcrumbBuilder.cs
WebApp/UI/Breadcrumbs/BreadcrumbBuilder.cs

WebApp/UI/Navigation/NavigationItemViewModel.cs
WebApp/UI/Navigation/Sections.cs
WebApp/UI/Navigation/INavigationBuilder.cs
WebApp/UI/Navigation/NavigationBuilder.cs

WebApp/UI/Culture/CultureOptionViewModel.cs
WebApp/UI/Culture/ICultureOptionsBuilder.cs
WebApp/UI/Culture/CultureOptionsBuilder.cs

WebApp/UI/UserMenu/UserMenuViewModel.cs
WebApp/UI/UserMenu/IUserMenuBuilder.cs
WebApp/UI/UserMenu/UserMenuBuilder.cs

WebApp/Views/Shared/Chrome/_AppChrome.cshtml
WebApp/Views/Shared/Chrome/_TopBar.cshtml
WebApp/Views/Shared/Chrome/_Sidebar.cshtml
WebApp/Views/Shared/Chrome/_Breadcrumbs.cshtml
WebApp/Views/Shared/Chrome/_CultureSelector.cshtml
WebApp/Views/Shared/Chrome/_WorkspaceSwitcher.cshtml
WebApp/Views/Shared/Chrome/_UserMenu.cshtml
```

## Files to change

```text
WebApp/Program.cs
WebApp/Areas/Management/Views/Shared/_ManagementLayout.cshtml
WebApp/Areas/Customer/Views/Shared/_CustomerLayout.cshtml, if exists
WebApp/Areas/Property/Views/Shared/_PropertyLayout.cshtml, if exists
WebApp/Areas/Unit/Views/Shared/_UnitLayout.cshtml, if exists
WebApp/Areas/Resident/Views/Shared/_ResidentLayout.cshtml, if exists

All full-page viewmodels that use the shared layout
All controllers/actions that return those full-page views
```

## Files to delete after migration

```text
WebApp/Areas/Management/Controllers/ManagementPageShellController.cs
WebApp/Services/ManagementLayout/IManagementLayoutViewModelProvider.cs
WebApp/Services/ManagementLayout/ManagementLayoutViewModelProvider.cs
WebApp/ViewModels/Management/ManagementLayoutViewModel.cs
```

Potentially delete after all pages are migrated:

```text
WebApp/Services/SharedLayout/IWorkspaceLayoutContextProvider.cs
WebApp/Services/SharedLayout/WorkspaceLayoutContextProvider.cs
WebApp/ViewModels/Shared/Layout/WorkspaceLayoutContextViewModel.cs
WebApp/ViewModels/Shared/Layout/WorkspaceLayoutRequestViewModel.cs
WebApp/ViewModels/Shared/PageShell/*
```

---

# Final architecture explanation

The final architecture has one simple rule:

```text
Every full page returns a viewmodel that contains AppChrome plus page-specific content.
```

`AppChrome` is responsible for shared UI chrome:

```text
page title
active menu section
workspace identity
breadcrumbs / hierarchy links
navigation items
workspace switchers
culture options
user menu
```

Page-specific viewmodels are responsible for page content:

```text
customer rows
forms
filters
statistics
dashboard cards
page-specific action permissions
```

The UI layer becomes SOLID because:

```text
IAppChromeBuilder has one job: compose chrome.
IWorkspaceResolver has one job: resolve current workspace.
IBreadcrumbBuilder has one job: build hierarchy links.
INavigationBuilder has one job: build visible navigation.
ICultureOptionsBuilder has one job: build language options.
IUserMenuBuilder has one job: build user menu state.
Controllers have one job: handle HTTP and assemble the final page viewmodel.
Razor has one job: render already-prepared data.
```

This keeps the same visual UI, but removes hidden base-controller behavior, duplicate layout providers, and management-specific architecture leaking into shared UI concerns.

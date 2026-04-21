using App.BLL.Onboarding;
using App.Domain.Identity;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using App.BLL.ManagementCompany.Membership;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.CompanyJoinRequests;
using App.BLL.Onboarding.ContextSelection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Onboarding;

namespace WebApp.Controllers;

public class OnboardingController : Controller
{
    private readonly IAccountOnboardingService _accountOnboardingService;
    private readonly IWorkspaceRedirectService _workspaceRedirectService;
    private readonly ICompanyJoinRequestService _joinRequestService;
    private readonly IManagementUserAdminService _managementUserAdminService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        IAccountOnboardingService accountOnboardingService,
        IWorkspaceRedirectService workspaceRedirectService,
        ICompanyJoinRequestService joinRequestService,
        IManagementUserAdminService managementUserAdminService,
        UserManager<AppUser> userManager,
        ILogger<OnboardingController> logger)
    {
        _accountOnboardingService = accountOnboardingService;
        _workspaceRedirectService = workspaceRedirectService;
        _joinRequestService = joinRequestService;
        _managementUserAdminService = managementUserAdminService;
        _userManager = userManager;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(bool showChooser = false)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new FlowChooserViewModel
            {
                InfoMessage = App.Resources.Views.UiText.FlowInfoCreateAccountOrLogin
            });
        }

        if (User.IsInRole("SystemAdmin"))
        {
            return RedirectToAction("Index", "Home");
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (showChooser)
        {
            return View(new FlowChooserViewModel());
        }

        var redirectTarget = await ResolveContextRedirectAsync(appUser.Id, HttpContext.RequestAborted);
        if (redirectTarget != null)
        {
            return redirectTarget;
        }

        return View(new FlowChooserViewModel());
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        LogOnboardingFormLocalizationDiagnostics();

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _accountOnboardingService.RegisterAsync(new AccountRegisterRequest
        {
            Email = vm.Email,
            Password = vm.Password,
            FirstName = vm.FirstName,
            LastName = vm.LastName
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(vm);
        }

        TempData["OnboardingSuccess"] = App.Resources.Views.UiText.RegistrationSuccessfulPleaseLogin;
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        LogOnboardingFormLocalizationDiagnostics();

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _accountOnboardingService.LoginAsync(new AccountLoginRequest
        {
            Email = vm.Email,
            Password = vm.Password,
            RememberMe = vm.RememberMe
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, App.Resources.Views.UiText.InvalidEmailOrPassword);
            return View(vm);
        }

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        {
            return LocalRedirect(vm.ReturnUrl);
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var redirectTarget = await ResolveContextRedirectAsync(appUser.Id, HttpContext.RequestAborted);
        if (redirectTarget != null)
        {
            return redirectTarget;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            await _accountOnboardingService.LogoutAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> SetContext(string type, Guid? id = null, string? returnUrl = null)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax
        };

        var authorizationResult = await _workspaceRedirectService.AuthorizeContextSelectionAsync(
            appUser.Id,
            type,
            id,
            HttpContext.RequestAborted);

        if (authorizationResult.Authorized)
        {
            switch (authorizationResult.NormalizedType)
            {
                case "management":
                Response.Cookies.Append("ctx.type", "management", cookieOptions);
                Response.Cookies.Append("ctx.management.company", authorizationResult.ManagementCompanyId!.Value.ToString(), cookieOptions);
                Response.Cookies.Append("ctx.management.slug", authorizationResult.ManagementCompanySlug!, cookieOptions);
                return RedirectToManagementDashboard(authorizationResult.ManagementCompanySlug!);

                case "customer":
                Response.Cookies.Append("ctx.type", "customer", cookieOptions);
                Response.Cookies.Append("ctx.customer.id", authorizationResult.CustomerId!.Value.ToString(), cookieOptions);
                return RedirectToAction("Index", "Dashboard", new { area = "Customer" });

                case "resident":
                Response.Cookies.Append("ctx.type", "resident", cookieOptions);
                return RedirectToAction("Index", "Dashboard", new { area = "Resident" });
            }
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> NewManagementCompany()
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (User.IsInRole("SystemAdmin"))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new CreateManagementCompanyViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewManagementCompany(CreateManagementCompanyViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _accountOnboardingService.CreateManagementCompanyAsync(new CreateManagementCompanyRequest
        {
            AppUserId = appUser.Id,
            Name = vm.Name,
            RegistryCode = vm.RegistryCode,
            VatNumber = vm.VatNumber,
            Email = vm.Email,
            Phone = vm.Phone,
            Address = vm.Address
        });

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(vm);
        }

        Response.Cookies.Append("ctx.type", "management", CreateContextCookieOptions());
        Response.Cookies.Append("ctx.management.company", result.ManagementCompanyId!.Value.ToString(), CreateContextCookieOptions());
        Response.Cookies.Append("ctx.management.slug", result.ManagementCompanySlug!, CreateContextCookieOptions());

        return RedirectToManagementDashboard(result.ManagementCompanySlug!);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> JoinManagementCompany(CancellationToken cancellationToken)
    {
        var availableRoles = await BuildRoleSelectListAsync(cancellationToken, null);
        var viewModel = new JoinManagementCompanyViewModel
        {
            AvailableRoles = availableRoles
        };
        ViewData["Title"] = viewModel.Title;
        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinManagementCompany(JoinManagementCompanyViewModel vm, CancellationToken cancellationToken)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid || vm.RequestedRoleId == null)
        {
            if (vm.RequestedRoleId == null)
            {
                ModelState.AddModelError(nameof(vm.RequestedRoleId), App.Resources.Views.UiText.RequestedRoleRequired);
            }

            vm.AvailableRoles = await BuildRoleSelectListAsync(cancellationToken, vm.RequestedRoleId);
            ViewData["Title"] = vm.Title;
            return View(vm);
        }

        var result = await _joinRequestService.CreateJoinRequestAsync(new CompanyJoinRequest
        {
            AppUserId = appUser.Id,
            RegistryCode = vm.RegistryCode,
            RequestedRoleId = vm.RequestedRoleId.Value,
            Message = vm.Message
        }, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? App.Resources.Views.UiText.UnableToSubmitJoinRequest);
            vm.AvailableRoles = await BuildRoleSelectListAsync(cancellationToken, vm.RequestedRoleId);
            ViewData["Title"] = vm.Title;
            return View(vm);
        }

        TempData["OnboardingSuccess"] = App.Resources.Views.UiText.JoinRequestSubmitted;
        return RedirectToAction(nameof(JoinManagementCompany));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ResidentAccess()
    {
        var viewModel = new ResidentAccessViewModel();
        ViewData["Title"] = viewModel.Title;
        return View(viewModel);
    }

    private async Task<IActionResult?> ResolveContextRedirectAsync(Guid appUserId, CancellationToken cancellationToken)
    {
        var selectedContextType = Request.Cookies["ctx.type"]?.Trim().ToLowerInvariant();

        if (User.IsInRole("SystemAdmin"))
        {
            return RedirectToAction("Index", "Home");
        }

        var redirectTarget = await _workspaceRedirectService.ResolveContextRedirectAsync(
            appUserId,
            new WorkspaceRedirectCookieState
            {
                ContextType = selectedContextType,
                ManagementCompanySlug = Request.Cookies["ctx.management.slug"],
                CustomerId = Request.Cookies["ctx.customer.id"]
            },
            cancellationToken);

        if (redirectTarget == null)
        {
            return null;
        }

        return redirectTarget.Destination switch
        {
            WorkspaceRedirectDestination.Home => RedirectToAction("Index", "Home"),
            WorkspaceRedirectDestination.ManagementDashboard when !string.IsNullOrWhiteSpace(redirectTarget.CompanySlug)
                => RedirectToManagementDashboard(redirectTarget.CompanySlug!),
            WorkspaceRedirectDestination.CustomerDashboard => RedirectToAction("Index", "Dashboard", new { area = "Customer" }),
            WorkspaceRedirectDestination.ResidentDashboard => RedirectToAction("Index", "Dashboard", new { area = "Resident" }),
            _ => null
        };
    }

    private RedirectToRouteResult RedirectToManagementDashboard(string companySlug)
    {
        return RedirectToRoute("management_dashboard", new { companySlug });
    }

    private CookieOptions CreateContextCookieOptions()
    {
        return new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax
        };
    }

    private async Task<IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> BuildRoleSelectListAsync(
        CancellationToken cancellationToken,
        Guid? selectedRoleId)
    {
        var roles = await _managementUserAdminService.GetAvailableRolesAsync(cancellationToken);
        return roles
            .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Label.ToString(),
                Selected = selectedRoleId.HasValue && selectedRoleId.Value == r.Id
            })
            .ToList();
    }

    private void LogOnboardingFormLocalizationDiagnostics()
    {
        var loginEmailHasDisplay = HasDisplayAttribute<LoginViewModel>(nameof(LoginViewModel.Email));
        var loginPasswordHasDisplay = HasDisplayAttribute<LoginViewModel>(nameof(LoginViewModel.Password));
        var loginRememberMeHasDisplay = HasDisplayAttribute<LoginViewModel>(nameof(LoginViewModel.RememberMe));

        var registerEmailHasDisplay = HasDisplayAttribute<RegisterViewModel>(nameof(RegisterViewModel.Email));
        var registerPasswordHasDisplay = HasDisplayAttribute<RegisterViewModel>(nameof(RegisterViewModel.Password));
        var registerFirstNameHasDisplay = HasDisplayAttribute<RegisterViewModel>(nameof(RegisterViewModel.FirstName));
        var registerLastNameHasDisplay = HasDisplayAttribute<RegisterViewModel>(nameof(RegisterViewModel.LastName));

        var uiCulture = CultureInfo.CurrentUICulture;

        var hasEmailResource = !string.IsNullOrWhiteSpace(App.Resources.Views.UiText.ResourceManager.GetString("Email", uiCulture));
        var hasPasswordResource = !string.IsNullOrWhiteSpace(App.Resources.Views.UiText.ResourceManager.GetString("Password", uiCulture));
        var hasRememberMeResource = !string.IsNullOrWhiteSpace(App.Resources.Views.UiText.ResourceManager.GetString("RememberMe", uiCulture));
        var hasFirstNameResource = !string.IsNullOrWhiteSpace(App.Resources.Views.UiText.ResourceManager.GetString("FirstName", uiCulture));
        var hasLastNameResource = !string.IsNullOrWhiteSpace(App.Resources.Views.UiText.ResourceManager.GetString("LastName", uiCulture));

        _logger.LogInformation(
            "Onboarding localization diagnostics. UICulture={UICulture}; Culture={Culture}; Login DisplayAttrs: Email={LoginEmailDisplay}, Password={LoginPasswordDisplay}, RememberMe={LoginRememberMeDisplay}; Register DisplayAttrs: Email={RegisterEmailDisplay}, Password={RegisterPasswordDisplay}, FirstName={RegisterFirstNameDisplay}, LastName={RegisterLastNameDisplay}; UiText keys present: Email={HasEmailResource}, Password={HasPasswordResource}, RememberMe={HasRememberMeResource}, FirstName={HasFirstNameResource}, LastName={HasLastNameResource}",
            uiCulture.Name,
            CultureInfo.CurrentCulture.Name,
            loginEmailHasDisplay,
            loginPasswordHasDisplay,
            loginRememberMeHasDisplay,
            registerEmailHasDisplay,
            registerPasswordHasDisplay,
            registerFirstNameHasDisplay,
            registerLastNameHasDisplay,
            hasEmailResource,
            hasPasswordResource,
            hasRememberMeResource,
            hasFirstNameResource,
            hasLastNameResource);
    }

    private static bool HasDisplayAttribute<TModel>(string propertyName)
    {
        var property = typeof(TModel).GetProperty(propertyName);
        return property?.GetCustomAttributes(typeof(DisplayAttribute), inherit: true).Length > 0;
    }
}



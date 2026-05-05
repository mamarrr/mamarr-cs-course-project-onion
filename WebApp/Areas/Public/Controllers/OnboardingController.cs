using System.ComponentModel.DataAnnotations;
using System.Globalization;
using App.BLL.Contracts;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.BLL.Contracts.Onboarding;
using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Mappers.Mvc.Onboarding;
using WebApp.Services.Identity;
using WebApp.ViewModels.Onboarding;

namespace WebApp.Areas.Public.Controllers;

[Area("Public")]
public class OnboardingController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IIdentityAccountService _identityAccountService;
    private readonly OnboardingViewModelMapper _mapper;

    public OnboardingController(
        IAppBLL bll,
        IIdentityAccountService identityAccountService,
        OnboardingViewModelMapper mapper)
    {
        _bll = bll;
        _identityAccountService = identityAccountService;
        _mapper = mapper;
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

        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, HttpContext.RequestAborted);
        if (appUserId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (showChooser)
        {
            return View(new FlowChooserViewModel());
        }

        var redirectTarget = await ResolveContextRedirectAsync(appUserId.Value, HttpContext.RequestAborted);
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

        var result = await _identityAccountService.CreateUserAsync(
            _mapper.Map(vm),
            HttpContext.RequestAborted);

        if (result.IsFailed)
        {
            _mapper.AddErrors(ModelState, result);

            return View(vm);
        }

        TempData["OnboardingSuccess"] = App.Resources.Views.UiText.RegistrationSuccessfulPleaseLogin;
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {

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

        var result = await _identityAccountService.PasswordSignInAsync(
            _mapper.Map(vm),
            HttpContext.RequestAborted);

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, App.Resources.Views.UiText.InvalidEmailOrPassword);
            return View(vm);
        }

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        {
            return LocalRedirect(vm.ReturnUrl);
        }

        var redirectTarget = await ResolveContextRedirectAsync(result.Value.AppUserId, HttpContext.RequestAborted);
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
            await _identityAccountService.SignOutAsync(HttpContext.RequestAborted);
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> SetContext(string type, Guid? id = null, string? returnUrl = null)
    {
        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, HttpContext.RequestAborted);
        if (appUserId == null)
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

        var authorizationResult = await _bll.WorkspaceRedirect.AuthorizeContextSelectionAsync(
            new AuthorizeContextSelectionQuery
            {
                AppUserId = appUserId.Value,
                ContextType = type,
                ContextId = id
            },
            HttpContext.RequestAborted);

        if (authorizationResult.IsSuccess && authorizationResult.Value.Authorized)
        {
            switch (authorizationResult.Value.NormalizedType)
            {
                case "management":
                Response.Cookies.Append("ctx.type", "management", cookieOptions);
                Response.Cookies.Append("ctx.management.company", authorizationResult.Value.ManagementCompanyId!.Value.ToString(), cookieOptions);
                Response.Cookies.Append("ctx.management.slug", authorizationResult.Value.ManagementCompanySlug!, cookieOptions);
                return RedirectToManagementDashboard(authorizationResult.Value.ManagementCompanySlug!);

                case "customer":
                Response.Cookies.Append("ctx.type", "customer", cookieOptions);
                Response.Cookies.Append("ctx.customer.id", authorizationResult.Value.CustomerId!.Value.ToString(), cookieOptions);
                return RedirectToAction(nameof(Index), new { showChooser = true });

                case "resident":
                Response.Cookies.Append("ctx.type", "resident", cookieOptions);
                return RedirectToAction(nameof(Index), new { showChooser = true });
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
        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, HttpContext.RequestAborted);
        if (appUserId == null)
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

        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, HttpContext.RequestAborted);
        if (appUserId == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _bll.AccountOnboarding.CreateManagementCompanyAsync(
            _mapper.Map(appUserId.Value, vm),
            HttpContext.RequestAborted);

        if (result.IsFailed)
        {
            _mapper.AddErrors(ModelState, result);

            return View(vm);
        }

        Response.Cookies.Append("ctx.type", "management", CreateContextCookieOptions());
        Response.Cookies.Append("ctx.management.company", result.Value.ManagementCompanyId.ToString(), CreateContextCookieOptions());
        Response.Cookies.Append("ctx.management.slug", result.Value.ManagementCompanySlug, CreateContextCookieOptions());

        return RedirectToManagementDashboard(result.Value.ManagementCompanySlug);
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
        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, cancellationToken);
        if (appUserId == null)
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

        var result = await _bll.OnboardingCompanyJoinRequests.CreateJoinRequestAsync(
            _mapper.Map(appUserId.Value, vm),
            cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Errors.FirstOrDefault()?.Message ?? App.Resources.Views.UiText.UnableToSubmitJoinRequest);
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

        var redirectTarget = await _bll.WorkspaceRedirect.ResolveContextRedirectAsync(
            new ResolveWorkspaceRedirectQuery
            {
                AppUserId = appUserId,
                CookieState = new WorkspaceRedirectCookieState
                {
                    ContextType = selectedContextType,
                    ManagementCompanySlug = Request.Cookies["ctx.management.slug"],
                    CustomerId = Request.Cookies["ctx.customer.id"]
                }
            },
            cancellationToken);

        if (redirectTarget.IsFailed || redirectTarget.Value == null)
        {
            return null;
        }

        return redirectTarget.Value.Destination switch
        {
            WorkspaceRedirectDestination.Home => RedirectToAction("Index", "Home"),
            WorkspaceRedirectDestination.ManagementDashboard when !string.IsNullOrWhiteSpace(redirectTarget.Value.CompanySlug)
                => RedirectToManagementDashboard(redirectTarget.Value.CompanySlug!),
            WorkspaceRedirectDestination.CustomerDashboard => RedirectToAction(nameof(Index), new { showChooser = true }),
            WorkspaceRedirectDestination.ResidentDashboard => RedirectToAction(nameof(Index), new { showChooser = true }),
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
        var roles = await _bll.CompanyMembershipAdmin.GetAvailableRolesAsync(cancellationToken);
        return roles
            .Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleLabel,
                Selected = selectedRoleId.HasValue && selectedRoleId.Value == r.RoleId
            })
            .ToList();
    }

    private static bool HasDisplayAttribute<TModel>(string propertyName)
    {
        var property = typeof(TModel).GetProperty(propertyName);
        return property?.GetCustomAttributes(typeof(DisplayAttribute), inherit: true).Length > 0;
    }
}



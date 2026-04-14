using App.BLL.Onboarding;
using App.BLL.ManagementUsers;
using App.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Onboarding;

namespace WebApp.Controllers;

public class OnboardingController : Controller
{
    private readonly IOnboardingService _onboardingService;
    private readonly IOnboardingContextService _onboardingContextService;
    private readonly IManagementCompanyJoinRequestService _joinRequestService;
    private readonly IManagementUserAdminService _managementUserAdminService;
    private readonly UserManager<AppUser> _userManager;

    public OnboardingController(
        IOnboardingService onboardingService,
        IOnboardingContextService onboardingContextService,
        IManagementCompanyJoinRequestService joinRequestService,
        IManagementUserAdminService managementUserAdminService,
        UserManager<AppUser> userManager)
    {
        _onboardingService = onboardingService;
        _onboardingContextService = onboardingContextService;
        _joinRequestService = joinRequestService;
        _managementUserAdminService = managementUserAdminService;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(bool showChooser = false)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new FlowChooserViewModel
            {
                InfoMessage = "Create your account or log in to continue."
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

        var result = await _onboardingService.RegisterAsync(new OnboardingRegisterRequest
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

        TempData["OnboardingSuccess"] = "Registration successful. Please log in.";
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

        var result = await _onboardingService.LoginAsync(new OnboardingLoginRequest
        {
            Email = vm.Email,
            Password = vm.Password,
            RememberMe = vm.RememberMe
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
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
            await _onboardingService.LogoutAsync();
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

        var authorizationResult = await _onboardingContextService.AuthorizeContextSelectionAsync(
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

        var result = await _onboardingService.CreateManagementCompanyAsync(new OnboardingCreateManagementCompanyRequest
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
                ModelState.AddModelError(nameof(vm.RequestedRoleId), "Requested role is required.");
            }

            vm.AvailableRoles = await BuildRoleSelectListAsync(cancellationToken, vm.RequestedRoleId);
            ViewData["Title"] = vm.Title;
            return View(vm);
        }

        var result = await _joinRequestService.CreateJoinRequestAsync(new CreateManagementCompanyJoinRequest
        {
            AppUserId = appUser.Id,
            RegistryCode = vm.RegistryCode,
            RequestedRoleId = vm.RequestedRoleId.Value,
            Message = vm.Message
        }, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to submit join request.");
            vm.AvailableRoles = await BuildRoleSelectListAsync(cancellationToken, vm.RequestedRoleId);
            ViewData["Title"] = vm.Title;
            return View(vm);
        }

        TempData["OnboardingSuccess"] = "Join request submitted. Please wait for management company approval.";
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

        var redirectTarget = await _onboardingContextService.ResolveContextRedirectAsync(
            appUserId,
            new OnboardingContextSelectionCookieState
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
            OnboardingContextRedirectDestination.Home => RedirectToAction("Index", "Home"),
            OnboardingContextRedirectDestination.ManagementDashboard when !string.IsNullOrWhiteSpace(redirectTarget.CompanySlug)
                => RedirectToManagementDashboard(redirectTarget.CompanySlug!),
            OnboardingContextRedirectDestination.CustomerDashboard => RedirectToAction("Index", "Dashboard", new { area = "Customer" }),
            OnboardingContextRedirectDestination.ResidentDashboard => RedirectToAction("Index", "Dashboard", new { area = "Resident" }),
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
}


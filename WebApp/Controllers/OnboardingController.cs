using App.BLL.Onboarding;
using App.DAL.EF;
using App.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.ViewModels.Onboarding;

namespace WebApp.Controllers;

public class OnboardingController : Controller
{
    private readonly IOnboardingService _onboardingService;
    private readonly AppDbContext _dbContext;
    private readonly UserManager<AppUser> _userManager;

    public OnboardingController(
        IOnboardingService onboardingService,
        AppDbContext dbContext,
        UserManager<AppUser> userManager)
    {
        _onboardingService = onboardingService;
        _dbContext = dbContext;
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

        var redirectTarget = await ResolveContextRedirectAsync(appUser.Id);
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

        var redirectTarget = await ResolveContextRedirectAsync(appUser.Id);
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

        var normalizedType = type.Trim().ToLowerInvariant();
        var cookieOptions = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax
        };

        switch (normalizedType)
        {
            case "management":
                if (!id.HasValue) break;

                var managementCompany = await _dbContext.ManagementCompanyUsers
                    .Where(x => x.AppUserId == appUser.Id && x.IsActive && x.ManagementCompanyId == id.Value)
                    .Select(x => new { x.ManagementCompanyId, x.ManagementCompany!.Slug })
                    .FirstOrDefaultAsync();
                if (managementCompany == null) break;

                Response.Cookies.Append("ctx.type", "management", cookieOptions);
                Response.Cookies.Append("ctx.management.company", managementCompany.ManagementCompanyId.ToString(), cookieOptions);
                Response.Cookies.Append("ctx.management.slug", managementCompany.Slug, cookieOptions);
                return RedirectToManagementDashboard(managementCompany.Slug);

            case "customer":
                if (!id.HasValue) break;

                var hasCustomerContext = await (
                        from residentUser in _dbContext.ResidentUsers
                        join customerRepresentative in _dbContext.CustomerRepresentatives
                            on residentUser.ResidentId equals customerRepresentative.ResidentId
                        where residentUser.AppUserId == appUser.Id
                              && residentUser.IsActive
                              && customerRepresentative.IsActive
                              && customerRepresentative.CustomerId == id.Value
                        select customerRepresentative.Id)
                    .AnyAsync();
                if (!hasCustomerContext) break;

                Response.Cookies.Append("ctx.type", "customer", cookieOptions);
                Response.Cookies.Append("ctx.customer.id", id.Value.ToString(), cookieOptions);
                return RedirectToAction("Index", "Dashboard", new { area = "Customer" });

            case "resident":
                var hasResidentContext = await _dbContext.ResidentUsers
                    .AnyAsync(x => x.AppUserId == appUser.Id && x.IsActive);
                if (!hasResidentContext) break;

                Response.Cookies.Append("ctx.type", "resident", cookieOptions);
                return RedirectToAction("Index", "Dashboard", new { area = "Resident" });
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
    public IActionResult JoinManagementCompany()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult ResidentAccess()
    {
        return View();
    }

    private async Task<IActionResult?> ResolveContextRedirectAsync(Guid appUserId)
    {
        var selectedContextType = Request.Cookies["ctx.type"]?.Trim().ToLowerInvariant();
        var selectedManagementSlug = Request.Cookies["ctx.management.slug"];
        var selectedCustomerCookie = Request.Cookies["ctx.customer.id"];

        if (User.IsInRole("SystemAdmin"))
        {
            return RedirectToAction("Index", "Home");
        }

        if (selectedContextType == "management" && !string.IsNullOrWhiteSpace(selectedManagementSlug))
        {
            var hasSelectedManagementAccess = await _onboardingService.UserHasManagementCompanyAccessAsync(appUserId, selectedManagementSlug);
            if (hasSelectedManagementAccess)
            {
                return RedirectToManagementDashboard(selectedManagementSlug);
            }
        }

        if (selectedContextType == "resident")
        {
            var hasSelectedResidentContext = await _dbContext.ResidentUsers
                .AnyAsync(x => x.AppUserId == appUserId && x.IsActive);
            if (hasSelectedResidentContext)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Resident" });
            }
        }

        if (selectedContextType == "customer" && Guid.TryParse(selectedCustomerCookie, out var selectedCustomerId))
        {
            var hasSelectedCustomerContext = await (
                    from residentUser in _dbContext.ResidentUsers
                    join customerRepresentative in _dbContext.CustomerRepresentatives
                        on residentUser.ResidentId equals customerRepresentative.ResidentId
                    where residentUser.AppUserId == appUserId
                          && residentUser.IsActive
                          && customerRepresentative.IsActive
                          && customerRepresentative.CustomerId == selectedCustomerId
                    select customerRepresentative.Id)
                .AnyAsync();
            if (hasSelectedCustomerContext)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
            }
        }

        var defaultManagementCompanySlug = await _onboardingService.GetDefaultManagementCompanySlugAsync(appUserId);
        var defaultManagementSlugFound = !string.IsNullOrWhiteSpace(defaultManagementCompanySlug);
        if (defaultManagementSlugFound)
        {
            return RedirectToManagementDashboard(defaultManagementCompanySlug!);
        }

        var hasResidentContext = await _dbContext.ResidentUsers
            .AnyAsync(x => x.AppUserId == appUserId && x.IsActive);
        if (hasResidentContext)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Resident" });
        }

        var hasCustomerContext = await (
                from residentUser in _dbContext.ResidentUsers
                join customerRepresentative in _dbContext.CustomerRepresentatives
                    on residentUser.ResidentId equals customerRepresentative.ResidentId
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && customerRepresentative.IsActive
                select customerRepresentative.Id)
            .AnyAsync();
        if (hasCustomerContext)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Customer" });
        }

        return null;
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
}


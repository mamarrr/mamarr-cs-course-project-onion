using App.BLL.ManagementUsers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.ViewModels.ManagementUsers;

namespace WebApp.Areas.Management.Controllers;

[Area("Management")]
[Authorize]
[Route("m/{companySlug}/users")]
public class UsersController : Controller
{
    private readonly IManagementUserAdminService _managementUserAdminService;

    public UsersController(IManagementUserAdminService managementUserAdminService)
    {
        _managementUserAdminService = managementUserAdminService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string companySlug, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(companySlug, cancellationToken);
        if (authResult is not null) return authResult;

        var pageViewModel = await BuildPageViewModelAsync(companySlug, cancellationToken);
        return View(pageViewModel);
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string companySlug,
        [Bind(Prefix = "AddUser")] AddManagementUserViewModel vm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        if (!ModelState.IsValid || vm.RoleId == null)
        {
            if (vm.RoleId == null)
            {
                ModelState.AddModelError(nameof(vm.RoleId), "Role is required.");
            }

            var invalidVm = await BuildPageViewModelAsync(companySlug, cancellationToken, vm);
            return View(nameof(Index), invalidVm);
        }

        var result = await _managementUserAdminService.AddUserByEmailAsync(auth.Context!, new ManagementUserAddRequest
        {
            Email = vm.Email,
            RoleId = vm.RoleId.Value,
            JobTitle = vm.JobTitle,
            ValidFrom = vm.ValidFrom,
            ValidTo = vm.ValidTo,
            IsActive = vm.IsActive
        }, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to add user.");
            var invalidVm = await BuildPageViewModelAsync(companySlug, cancellationToken, vm);
            return View(nameof(Index), invalidVm);
        }

        TempData["ManagementUsersSuccess"] = "Company user added successfully.";
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(string companySlug, Guid id, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var editResult = await _managementUserAdminService.GetMembershipForEditAsync(auth.Context!, id, cancellationToken);
        if (editResult.NotFound)
        {
            return NotFound();
        }

        var availableRoles = await BuildRoleSelectListAsync(cancellationToken, editResult.Data?.RoleId);
        var vm = new EditManagementUserViewModel
        {
            MembershipId = editResult.Data!.MembershipId,
            CompanySlug = auth.Context!.CompanySlug,
            CompanyName = auth.Context.CompanyName,
            FullName = editResult.Data.FullName,
            Email = editResult.Data.Email,
            RoleId = editResult.Data.RoleId,
            JobTitle = editResult.Data.JobTitle,
            IsActive = editResult.Data.IsActive,
            ValidFrom = editResult.Data.ValidFrom,
            ValidTo = editResult.Data.ValidTo,
            AvailableRoles = availableRoles
        };

        ViewData["Title"] = "Edit user";
        return View(vm);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string companySlug, Guid id, EditManagementUserViewModel vm, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        if (id != vm.MembershipId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || vm.RoleId == null)
        {
            if (vm.RoleId == null)
            {
                ModelState.AddModelError(nameof(vm.RoleId), "Role is required.");
            }

            vm.CompanySlug = auth.Context!.CompanySlug;
            vm.CompanyName = auth.Context.CompanyName;
            vm.AvailableRoles = await BuildRoleSelectListAsync(cancellationToken, vm.RoleId);
            ViewData["Title"] = "Edit user";
            return View(vm);
        }

        var updateResult = await _managementUserAdminService.UpdateMembershipAsync(auth.Context!, id, new ManagementUserUpdateRequest
        {
            RoleId = vm.RoleId.Value,
            JobTitle = vm.JobTitle,
            IsActive = vm.IsActive,
            ValidFrom = vm.ValidFrom,
            ValidTo = vm.ValidTo
        }, cancellationToken);

        if (!updateResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateResult.ErrorMessage ?? "Unable to update user.");
            vm.CompanySlug = auth.Context!.CompanySlug;
            vm.CompanyName = auth.Context.CompanyName;
            vm.AvailableRoles = await BuildRoleSelectListAsync(cancellationToken, vm.RoleId);
            ViewData["Title"] = "Edit user";
            return View(vm);
        }

        TempData["ManagementUsersSuccess"] = "Company user updated successfully.";
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string companySlug, Guid id, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var result = await _managementUserAdminService.DeleteMembershipAsync(auth.Context!, id, cancellationToken);
        if (!result.Success)
        {
            TempData["ManagementUsersError"] = result.ErrorMessage ?? "Unable to remove company user.";
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        TempData["ManagementUsersSuccess"] = "Company user removed successfully.";
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    private async Task<IActionResult?> AuthorizeAsync(string companySlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return Challenge();
        }

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        return ToAuthorizationActionResult(auth);
    }

    private IActionResult? ToAuthorizationActionResult(ManagementUserAdminAuthorizationResult auth)
    {
        if (auth.CompanyNotFound)
        {
            return NotFound();
        }

        if (auth.IsForbidden)
        {
            return Forbid();
        }

        return null;
    }

    private async Task<ManagementUsersPageViewModel> BuildPageViewModelAsync(
        string companySlug,
        CancellationToken cancellationToken,
        AddManagementUserViewModel? addUserOverride = null)
    {
        var appUserId = GetAppUserId()!.Value;
        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId, companySlug, cancellationToken);
        var context = auth.Context!;

        var members = await _managementUserAdminService.ListCompanyMembersAsync(context, cancellationToken);
        var pendingRequests = await _managementUserAdminService.GetPendingAccessRequestsAsync(context, cancellationToken);
        var availableRoles = await BuildRoleSelectListAsync(cancellationToken, addUserOverride?.RoleId);

        ViewData["Title"] = "Users";

        return new ManagementUsersPageViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            Members = members.Members.Select(x => new ManagementUserListItemViewModel
            {
                MembershipId = x.MembershipId,
                FullName = x.FullName,
                Email = x.Email,
                RoleLabel = x.RoleLabel,
                RoleCode = x.RoleCode,
                JobTitle = x.JobTitle,
                IsActive = x.IsActive,
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                IsActor = x.IsActor
            }).ToList(),
            AddUser = addUserOverride ?? new AddManagementUserViewModel(),
            PendingRequests = pendingRequests.Requests.Select(x => new PendingAccessRequestViewModel
            {
                RequestId = x.RequestId,
                RequesterName = x.RequesterName,
                RequesterEmail = x.RequesterEmail,
                RequestedRoleCode = x.RequestedRoleCode,
                RequestedRoleLabel = x.RequestedRoleLabel,
                RequestedAt = x.RequestedAt
            }).ToList(),
            AvailableRoles = availableRoles
        };
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildRoleSelectListAsync(CancellationToken cancellationToken, Guid? selectedRoleId = null)
    {
        var roles = await _managementUserAdminService.GetAvailableRolesAsync(cancellationToken);
        return roles
            .Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Label.ToString(),
                Selected = selectedRoleId.HasValue && selectedRoleId.Value == r.Id
            })
            .ToList();
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }
}

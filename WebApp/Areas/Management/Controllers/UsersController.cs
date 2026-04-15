using App.BLL.Management;
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

    public UsersController(
        IManagementUserAdminService managementUserAdminService,
        ILogger<UsersController> logger)
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
                ModelState.AddModelError(nameof(vm.RoleId), App.Resources.Views.UiText.RoleRequired);
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
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? App.Resources.Views.UiText.UnableToAddUser);
            var invalidVm = await BuildPageViewModelAsync(companySlug, cancellationToken, vm);
            return View(nameof(Index), invalidVm);
        }

        TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.CompanyUserAddedSuccessfully;
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

        if (editResult.Forbidden)
        {
            TempData["ManagementUsersError"] = editResult.ErrorMessage ?? App.Resources.Views.UiText.UnableToUpdateUser;
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        var vm = MapEditViewModel(auth.Context!, editResult.Data!);

        ViewData["Title"] = App.Resources.Views.UiText.EditUser;
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

        var editResult = await _managementUserAdminService.GetMembershipForEditAsync(auth.Context!, id, cancellationToken);
        if (editResult.NotFound)
        {
            return NotFound();
        }

        if (editResult.Forbidden)
        {
            TempData["ManagementUsersError"] = editResult.ErrorMessage ?? App.Resources.Views.UiText.UnableToUpdateUser;
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        if (!ModelState.IsValid || vm.RoleId == null)
        {
            if (vm.RoleId == null)
            {
                ModelState.AddModelError(nameof(vm.RoleId), App.Resources.Views.UiText.RoleRequired);
            }

            HydrateEditViewModel(vm, auth.Context!, editResult.Data!);
            ViewData["Title"] = App.Resources.Views.UiText.EditUser;
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
            ModelState.AddModelError(string.Empty, updateResult.ErrorMessage ?? App.Resources.Views.UiText.UnableToUpdateUser);
            HydrateEditViewModel(vm, auth.Context!, editResult.Data!);
            ViewData["Title"] = App.Resources.Views.UiText.EditUser;
            return View(vm);
        }

        TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.CompanyUserUpdatedSuccessfully;
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
            TempData["ManagementUsersError"] = result.ErrorMessage ?? App.Resources.Views.UiText.UnableToRemoveCompanyUser;
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.CompanyUserRemovedSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpGet("transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(string companySlug, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var candidateResult = await _managementUserAdminService.GetOwnershipTransferCandidatesAsync(auth.Context!, cancellationToken);
        if (candidateResult.Forbidden)
        {
            TempData["ManagementUsersError"] = candidateResult.ErrorMessage ?? App.Resources.Views.UiText.OwnershipTransferRequiresCurrentOwner;
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        var pageVm = await BuildTransferOwnershipPageViewModelAsync(auth.Context!, cancellationToken);
        ViewData["Title"] = App.Resources.Views.UiText.TransferOwnership;
        return View(pageVm);
    }

    [HttpPost("transfer-ownership")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferOwnership(
        string companySlug,
        [Bind(Prefix = "Transfer")] TransferOwnershipInputViewModel vm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        if (!ModelState.IsValid || vm.TargetMembershipId == null)
        {
            if (vm.TargetMembershipId == null)
            {
                ModelState.AddModelError(nameof(vm.TargetMembershipId), App.Resources.Views.UiText.NewOwnerRequired);
            }

            var invalidVm = await BuildTransferOwnershipPageViewModelAsync(auth.Context!, cancellationToken, vm);
            ViewData["Title"] = App.Resources.Views.UiText.TransferOwnership;
            return View(invalidVm);
        }

        var result = await _managementUserAdminService.TransferOwnershipAsync(auth.Context!, new TransferOwnershipRequest
        {
            TargetMembershipId = vm.TargetMembershipId.Value
        }, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? App.Resources.Views.UiText.UnableToTransferOwnership);
            var invalidVm = await BuildTransferOwnershipPageViewModelAsync(auth.Context!, cancellationToken, vm);
            ViewData["Title"] = App.Resources.Views.UiText.TransferOwnership;
            return View(invalidVm);
        }

        TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.OwnershipTransferredSuccessfully;
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpPost("requests/{requestId:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(string companySlug, Guid requestId, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var result = await _managementUserAdminService.ApprovePendingAccessRequestAsync(auth.Context!, requestId, cancellationToken);
        if (!result.Success)
        {
            TempData["ManagementUsersError"] = result.ErrorMessage ?? App.Resources.Views.UiText.UnableToApproveAccessRequest;
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.AccessRequestApproved;
        return RedirectToAction(nameof(Index), new { companySlug });
    }

    [HttpPost("requests/{requestId:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(string companySlug, Guid requestId, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _managementUserAdminService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var result = await _managementUserAdminService.RejectPendingAccessRequestAsync(auth.Context!, requestId, cancellationToken);
        if (!result.Success)
        {
            TempData["ManagementUsersError"] = result.ErrorMessage ?? App.Resources.Views.UiText.UnableToRejectAccessRequest;
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        TempData["ManagementUsersSuccess"] = App.Resources.Views.UiText.AccessRequestRejected;
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
        var availableRoles = await BuildRoleSelectListAsync(await _managementUserAdminService.GetAddRoleOptionsAsync(context, cancellationToken), addUserOverride?.RoleId);

        ViewData["Title"] = App.Resources.Views.UiText.Users;

        return new ManagementUsersPageViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CurrentActorIsOwner = context.IsOwner,
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
                IsActor = x.IsActor,
                IsOwner = x.IsOwner,
                IsEffective = x.IsEffective,
                CanEdit = x.CanEdit,
                CanDelete = x.CanDelete,
                CanTransferOwnership = x.CanTransferOwnership,
                CanChangeRole = x.CanChangeRole,
                CanDeactivate = x.CanDeactivate,
                ProtectedReason = x.ProtectedReason
            }).ToList(),
            AddUser = addUserOverride ?? new AddManagementUserViewModel(),
            PendingRequests = pendingRequests.Requests.Select(x => new PendingAccessRequestViewModel
            {
                RequestId = x.RequestId,
                AppUserId = x.AppUserId,
                RequesterName = x.RequesterName,
                RequesterEmail = x.RequesterEmail,
                RequestedRoleCode = x.RequestedRoleCode,
                RequestedRoleLabel = x.RequestedRoleLabel,
                Message = x.Message,
                RequestedAt = x.RequestedAt
            }).ToList(),
            AvailableRoles = availableRoles
        };
    }

    private async Task<TransferOwnershipPageViewModel> BuildTransferOwnershipPageViewModelAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken,
        TransferOwnershipInputViewModel? transferOverride = null)
    {
        var members = await _managementUserAdminService.ListCompanyMembersAsync(context, cancellationToken);
        var currentOwner = members.Members.Single(x => x.MembershipId == context.ActorMembershipId);
        var candidateResult = await _managementUserAdminService.GetOwnershipTransferCandidatesAsync(context, cancellationToken);

        return new TransferOwnershipPageViewModel
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CurrentOwnerName = currentOwner.FullName,
            CurrentOwnerEmail = currentOwner.Email,
            Transfer = transferOverride ?? new TransferOwnershipInputViewModel(),
            Candidates = candidateResult.Candidates
                .Select(x => new SelectListItem
                {
                    Value = x.MembershipId.ToString(),
                    Text = $"{x.FullName} ({x.RoleLabel})",
                    Selected = transferOverride?.TargetMembershipId == x.MembershipId
                })
                .ToList()
        };
    }

    private static EditManagementUserViewModel MapEditViewModel(
        ManagementUserAdminAuthorizedContext context,
        ManagementUserEditModel data)
    {
        return new EditManagementUserViewModel
        {
            MembershipId = data.MembershipId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            FullName = data.FullName,
            Email = data.Email,
            CurrentRoleLabel = data.RoleLabel,
            CurrentRoleCode = data.RoleCode,
            IsOwner = data.IsOwner,
            IsActor = data.IsActor,
            IsEffective = data.IsEffective,
            CanEdit = data.CanEdit,
            CanDelete = data.CanDelete,
            CanTransferOwnership = data.CanTransferOwnership,
            CanChangeRole = data.CanChangeRole,
            CanDeactivate = data.CanDeactivate,
            OwnershipTransferRequired = data.OwnershipTransferRequired,
            ProtectedReason = data.ProtectedReason,
            RoleId = data.RoleId,
            JobTitle = data.JobTitle,
            IsActive = data.IsActive,
            ValidFrom = data.ValidFrom,
            ValidTo = data.ValidTo,
            AvailableRoles = BuildRoleSelectListAsync(data.AvailableRoleOptions, data.RoleId).GetAwaiter().GetResult()
        };
    }

    private static void HydrateEditViewModel(
        EditManagementUserViewModel vm,
        ManagementUserAdminAuthorizedContext context,
        ManagementUserEditModel data)
    {
        vm.CompanySlug = context.CompanySlug;
        vm.CompanyName = context.CompanyName;
        vm.FullName = data.FullName;
        vm.Email = data.Email;
        vm.CurrentRoleLabel = data.RoleLabel;
        vm.CurrentRoleCode = data.RoleCode;
        vm.IsOwner = data.IsOwner;
        vm.IsActor = data.IsActor;
        vm.IsEffective = data.IsEffective;
        vm.CanEdit = data.CanEdit;
        vm.CanDelete = data.CanDelete;
        vm.CanTransferOwnership = data.CanTransferOwnership;
        vm.CanChangeRole = data.CanChangeRole;
        vm.CanDeactivate = data.CanDeactivate;
        vm.OwnershipTransferRequired = data.OwnershipTransferRequired;
        vm.ProtectedReason = data.ProtectedReason;
        vm.AvailableRoles = BuildRoleSelectListAsync(data.AvailableRoleOptions, vm.RoleId).GetAwaiter().GetResult();
    }

    private static Task<IReadOnlyList<SelectListItem>> BuildRoleSelectListAsync(
        IReadOnlyList<ManagementRoleOption> roles,
        Guid? selectedRoleId = null)
    {
        IReadOnlyList<SelectListItem> items = roles
            .Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = r.RoleLabel,
                Selected = selectedRoleId.HasValue && selectedRoleId.Value == r.RoleId
            })
            .ToList();

        return Task.FromResult(items);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }
}


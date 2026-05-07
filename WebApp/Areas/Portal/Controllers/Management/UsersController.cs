using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies.Models;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.UI.Navigation;
using WebApp.UI.PortalContext;
using WebApp.UI.Routing;
using WebApp.UI.Workspace;
using WebApp.ViewModels.Management.Users;

namespace WebApp.Areas.Portal.Controllers.Management;

[Area("Portal")]
[Authorize]
[Route("m/{companySlug}/users")]
public class UsersController : Controller
{
    private readonly IAppBLL _bll;
    private readonly IAppChromeBuilder _appChromeBuilder;
    private readonly ICurrentPortalContextResolver _portalContextResolver;

    public UsersController(
        IAppBLL bll,
        IAppChromeBuilder appChromeBuilder,
        ICurrentPortalContextResolver portalContextResolver,
        ILogger<UsersController> logger)
    {
        _bll = bll;
        _appChromeBuilder = appChromeBuilder;
        _portalContextResolver = portalContextResolver;
    }

    [HttpGet("", Name = PortalRouteNames.ManagementUsers)]
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
        UsersPageViewModel pageVm,
        CancellationToken cancellationToken)
    {
        var vm = pageVm.AddUser;
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();


        var auth = await _bll.CompanyMemberships.AuthorizeAsync(
            new ManagementCompanyRoute()
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug
            }, cancellationToken);
        
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        if (!ModelState.IsValid || vm.RoleId == null)
        {
            if (vm.RoleId == null)
            {
                ModelState.AddModelError($"{nameof(UsersPageViewModel.AddUser)}.{nameof(AddManagementUserViewModel.RoleId)}", App.Resources.Views.UiText.RoleRequired);
            }

            var invalidVm = await BuildPageViewModelAsync(companySlug, cancellationToken, vm);
            return View(nameof(Index), invalidVm);
        }

        var result = await _bll.CompanyMemberships.AddUserByEmailAsync(auth.Value, new CompanyMembershipAddRequest
        {
            Email = vm.Email,
            RoleId = vm.RoleId.Value,
            JobTitle = vm.JobTitle,
            ValidFrom = vm.ValidFrom,
            ValidTo = vm.ValidTo,
        }, cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(result.Errors, App.Resources.Views.UiText.UnableToAddUser));
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

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var editResult = await _bll.CompanyMemberships.GetMembershipForEditAsync(auth.Value, id, cancellationToken);
        if (editResult.IsFailed && editResult.Errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        if (editResult.IsFailed)
        {
            TempData["ManagementUsersError"] = ErrorMessage(editResult.Errors, App.Resources.Views.UiText.UnableToUpdateUser);
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        var title = App.Resources.Views.UiText.EditUser;
        var vm = await MapEditViewModelAsync(auth.Value, editResult.Value, title, cancellationToken);
        return View(vm);
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string companySlug, Guid id, EditManagementUserViewModel vm, CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        if (id != vm.MembershipId)
        {
            return NotFound();
        }

        var editResult = await _bll.CompanyMemberships.GetMembershipForEditAsync(auth.Value, id, cancellationToken);
        if (editResult.IsFailed && editResult.Errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        if (editResult.IsFailed)
        {
            TempData["ManagementUsersError"] = ErrorMessage(editResult.Errors, App.Resources.Views.UiText.UnableToUpdateUser);
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        var title = App.Resources.Views.UiText.EditUser;

        if (!ModelState.IsValid || vm.RoleId == null)
        {
            if (vm.RoleId == null)
            {
                ModelState.AddModelError(nameof(vm.RoleId), App.Resources.Views.UiText.RoleRequired);
            }

            await HydrateEditViewModelAsync(vm, auth.Value, editResult.Value, title, cancellationToken);
            return View(vm);
        }

        var updateResult = await _bll.CompanyMemberships.UpdateMembershipAsync(auth.Value, id, new CompanyMembershipUpdateRequest
        {
            RoleId = vm.RoleId.Value,
            JobTitle = vm.JobTitle,
            ValidFrom = vm.ValidFrom,
            ValidTo = vm.ValidTo
        }, cancellationToken);

        if (updateResult.IsFailed && updateResult.Errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        if (updateResult.IsFailed)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(updateResult.Errors, App.Resources.Views.UiText.UnableToUpdateUser));
            await HydrateEditViewModelAsync(vm, auth.Value, editResult.Value, title, cancellationToken);
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

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var result = await _bll.CompanyMemberships.DeleteMembershipAsync(auth.Value, id, cancellationToken);
        if (result.IsFailed)
        {
            TempData["ManagementUsersError"] = ErrorMessage(result.Errors, App.Resources.Views.UiText.UnableToRemoveCompanyUser);
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

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var candidateResult = await _bll.CompanyMemberships.GetOwnershipTransferCandidatesAsync(auth.Value, cancellationToken);
        if (candidateResult.IsFailed)
        {
            TempData["ManagementUsersError"] = ErrorMessage(candidateResult.Errors, App.Resources.Views.UiText.OwnershipTransferRequiresCurrentOwner);
            return RedirectToAction(nameof(Index), new { companySlug });
        }

        var title = App.Resources.Views.UiText.TransferOwnership;
        var pageVm = await BuildTransferOwnershipPageViewModelAsync(auth.Value, title, cancellationToken);
        return View(pageVm);
    }

    [HttpPost("transfer-ownership")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferOwnership(
        string companySlug,
        TransferOwnershipPageViewModel pageVm,
        CancellationToken cancellationToken)
    {
        var vm = pageVm.Transfer;
        var appUserId = GetAppUserId();
        if (appUserId == null) return Challenge();

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var title = App.Resources.Views.UiText.TransferOwnership;

        if (!ModelState.IsValid || vm.TargetMembershipId == null)
        {
            if (vm.TargetMembershipId == null)
            {
                ModelState.AddModelError($"{nameof(TransferOwnershipPageViewModel.Transfer)}.{nameof(TransferOwnershipInputViewModel.TargetMembershipId)}", App.Resources.Views.UiText.NewOwnerRequired);
            }

            var invalidVm = await BuildTransferOwnershipPageViewModelAsync(auth.Value, title, cancellationToken, vm);
            return View(invalidVm);
        }

        var result = await _bll.CompanyMemberships.TransferOwnershipAsync(auth.Value, new TransferOwnershipRequest
        {
            TargetMembershipId = vm.TargetMembershipId.Value
        }, cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, ErrorMessage(result.Errors, App.Resources.Views.UiText.UnableToTransferOwnership));
            var invalidVm = await BuildTransferOwnershipPageViewModelAsync(auth.Value, title, cancellationToken, vm);
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

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var result = await _bll.CompanyMemberships.ApprovePendingAccessRequestAsync(auth.Value, requestId, cancellationToken);
        if (result.IsFailed)
        {
            TempData["ManagementUsersError"] = ErrorMessage(result.Errors, App.Resources.Views.UiText.UnableToApproveAccessRequest);
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

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var authResponse = ToAuthorizationActionResult(auth);
        if (authResponse is not null) return authResponse;

        var result = await _bll.CompanyMemberships.RejectPendingAccessRequestAsync(auth.Value, requestId, cancellationToken);
        if (result.IsFailed)
        {
            TempData["ManagementUsersError"] = ErrorMessage(result.Errors, App.Resources.Views.UiText.UnableToRejectAccessRequest);
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

        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        return ToAuthorizationActionResult(auth);
    }

    private IActionResult? ToAuthorizationActionResult(Result<CompanyAdminAuthorizedContext> auth)
    {
        if (auth.IsSuccess)
        {
            return null;
        }

        if (auth.Errors.OfType<NotFoundError>().Any())
        {
            return NotFound();
        }

        if (auth.Errors.OfType<ForbiddenError>().Any())
        {
            return Forbid();
        }

        return Forbid();
    }

    private async Task<UsersPageViewModel> BuildPageViewModelAsync(
        string companySlug,
        CancellationToken cancellationToken,
        AddManagementUserViewModel? addUserOverride = null)
    {
        var appUserId = GetAppUserId()!;
        var auth = await _bll.CompanyMemberships.AuthorizeAsync(new ManagementCompanyRoute()
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        }, cancellationToken);
        var context = auth.Value;

        var members = await _bll.CompanyMemberships.ListCompanyMembersAsync(context, cancellationToken);
        var pendingRequests = await _bll.CompanyMemberships.GetPendingAccessRequestsAsync(context, cancellationToken);
        var roleOptions = await _bll.CompanyMemberships.GetAddRoleOptionsAsync(context, cancellationToken);
        var availableRoles = await BuildRoleSelectListAsync(roleOptions.Value, addUserOverride?.RoleId);
        var title = App.Resources.Views.UiText.Users;

        return new UsersPageViewModel
        {
            AppChrome = await BuildManagementChromeAsync(title, context, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CurrentActorIsOwner = context.IsOwner,
            Members = members.Value.Members.Select(x => new ManagementUserListItemViewModel
            {
                MembershipId = x.MembershipId,
                FullName = x.FullName,
                Email = x.Email,
                RoleLabel = x.RoleLabel,
                RoleCode = x.RoleCode,
                JobTitle = x.JobTitle,
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
            PendingRequests = pendingRequests.Value.Requests.Select(x => new PendingAccessRequestViewModel
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
        CompanyAdminAuthorizedContext context,
        string title,
        CancellationToken cancellationToken,
        TransferOwnershipInputViewModel? transferOverride = null)
    {
        var members = await _bll.CompanyMemberships.ListCompanyMembersAsync(context, cancellationToken);
        var currentOwner = members.Value.Members.Single(x => x.MembershipId == context.ActorMembershipId);
        var candidateResult = await _bll.CompanyMemberships.GetOwnershipTransferCandidatesAsync(context, cancellationToken);
        var candidates = candidateResult.IsSuccess
            ? candidateResult.Value
            : Array.Empty<OwnershipTransferCandidate>();

        return new TransferOwnershipPageViewModel
        {
            AppChrome = await BuildManagementChromeAsync(title, context, cancellationToken),
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CurrentOwnerName = currentOwner.FullName,
            CurrentOwnerEmail = currentOwner.Email,
            Transfer = transferOverride ?? new TransferOwnershipInputViewModel(),
            Candidates = candidates
                .Select(x => new SelectListItem
                {
                    Value = x.MembershipId.ToString(),
                    Text = $"{x.FullName} ({x.RoleLabel})",
                    Selected = transferOverride?.TargetMembershipId == x.MembershipId
                })
                .ToList()
        };
    }

    private async Task<EditManagementUserViewModel> MapEditViewModelAsync(
        CompanyAdminAuthorizedContext context,
        CompanyMembershipEditModel data,
        string title,
        CancellationToken cancellationToken)
    {
        return new EditManagementUserViewModel
        {
            AppChrome = await BuildManagementChromeAsync(title, context, cancellationToken),
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
            ValidFrom = data.ValidFrom,
            ValidTo = data.ValidTo,
            AvailableRoles = await BuildRoleSelectListAsync(data.AvailableRoleOptions, data.RoleId)
        };
    }

    private async Task HydrateEditViewModelAsync(
        EditManagementUserViewModel vm,
        CompanyAdminAuthorizedContext context,
        CompanyMembershipEditModel data,
        string title,
        CancellationToken cancellationToken)
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
        vm.AvailableRoles = await BuildRoleSelectListAsync(data.AvailableRoleOptions, vm.RoleId);
        vm.AppChrome = await BuildManagementChromeAsync(title, context, cancellationToken);
    }

    private Task<AppChromeViewModel> BuildManagementChromeAsync(
        string title,
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken)
    {
        return _appChromeBuilder.BuildAsync(
            new AppChromeRequest
            {
                User = User,
                HttpContext = HttpContext,
                PageTitle = title,
                ActiveSection = Sections.CompanyUsers,
                ManagementCompanySlug = context.CompanySlug,
                ManagementCompanyName = context.CompanyName,
                CurrentLevel = WorkspaceLevel.ManagementCompany
            },
            cancellationToken);
    }

    private static Task<IReadOnlyList<SelectListItem>> BuildRoleSelectListAsync(
        IReadOnlyList<CompanyMembershipRoleOption> roles,
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
        return _portalContextResolver.Resolve().AppUserId;
    }

    private static string ErrorMessage(IReadOnlyList<IError> errors, string fallback)
    {
        return errors.FirstOrDefault()?.Message ?? fallback;
    }
}

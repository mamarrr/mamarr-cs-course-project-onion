using System.Security.Claims;
using App.BLL.Contracts;
using App.BLL.DTO.Admin.Users;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Admin.Users;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SystemAdmin")]
[Route("Admin/Users")]
public class UsersController : Controller
{
    private readonly IAppBLL _bll;

    public UsersController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(AdminUserSearchViewModel search, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminUsers.SearchUsersAsync(ToDto(search), cancellationToken);
        return View(ToListVm(dto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminUsers.GetUserDetailsAsync(id, cancellationToken);
        return dto is null ? NotFound() : View(ToDetailsVm(dto));
    }

    [HttpPost("{id:guid}/lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(Guid id, CancellationToken cancellationToken)
    {
        var actorId = CurrentUserId();
        if (actorId is null) return Challenge();

        var result = await _bll.AdminUsers.LockUserAsync(id, actorId.Value, cancellationToken);
        var dto = result.IsSuccess ? result.Value : await _bll.AdminUsers.GetUserDetailsAsync(id, cancellationToken);
        if (dto is null) return NotFound();

        var vm = ToDetailsVm(dto);
        vm.SuccessMessage = result.IsSuccess ? AdminText.UserLockedSuccessfully : null;
        vm.ErrorMessage = result.IsFailed ? ErrorMessage(result.Errors, AdminText.UnableToLockUser) : null;
        return View("Details", vm);
    }

    [HttpPost("{id:guid}/unlock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken)
    {
        var actorId = CurrentUserId();
        if (actorId is null) return Challenge();

        var result = await _bll.AdminUsers.UnlockUserAsync(id, actorId.Value, cancellationToken);
        var dto = result.IsSuccess ? result.Value : await _bll.AdminUsers.GetUserDetailsAsync(id, cancellationToken);
        if (dto is null) return NotFound();

        var vm = ToDetailsVm(dto);
        vm.SuccessMessage = result.IsSuccess ? AdminText.UserUnlockedSuccessfully : null;
        vm.ErrorMessage = result.IsFailed ? ErrorMessage(result.Errors, AdminText.UnableToUnlockUser) : null;
        return View("Details", vm);
    }

    private static AdminUserSearchDto ToDto(AdminUserSearchViewModel vm) => new()
    {
        SearchText = vm.SearchText,
        Email = vm.Email,
        Name = vm.Name,
        LockedOnly = vm.LockedOnly,
        HasSystemAdminRole = vm.HasSystemAdminRole,
        CreatedFrom = vm.CreatedFrom,
        CreatedTo = vm.CreatedTo
    };

    private static AdminUserListViewModel ToListVm(AdminUserListDto dto) => new()
    {
        PageTitle = AdminText.Users,
        ActiveSection = "Users",
        Search = new AdminUserSearchViewModel
        {
            SearchText = dto.Search.SearchText,
            Email = dto.Search.Email,
            Name = dto.Search.Name,
            LockedOnly = dto.Search.LockedOnly,
            HasSystemAdminRole = dto.Search.HasSystemAdminRole,
            CreatedFrom = dto.Search.CreatedFrom,
            CreatedTo = dto.Search.CreatedTo
        },
        Users = dto.Users.Select(user => new AdminUserListItemViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            IsLocked = user.IsLocked,
            HasSystemAdminRole = user.HasSystemAdminRole
        }).ToList()
    };

    private static AdminUserDetailsViewModel ToDetailsVm(AdminUserDetailsDto dto) => new()
    {
        PageTitle = dto.FullName,
        ActiveSection = "Users",
        Id = dto.Id,
        Email = dto.Email,
        FullName = dto.FullName,
        CreatedAt = dto.CreatedAt,
        IsLocked = dto.IsLocked,
        HasSystemAdminRole = dto.HasSystemAdminRole,
        PhoneNumber = dto.PhoneNumber,
        LastLoginAt = dto.LastLoginAt,
        RefreshTokenCount = dto.RefreshTokenCount,
        Roles = dto.Roles.Select(role => role.RoleName).ToList(),
        CompanyMemberships = dto.CompanyMemberships.Select(membership => new AdminUserMembershipViewModel
        {
            CompanyName = membership.CompanyName,
            RoleCode = membership.RoleCode,
            RoleLabel = membership.RoleLabel,
            ValidFrom = membership.ValidFrom,
            ValidTo = membership.ValidTo
        }).ToList()
    };

    private Guid? CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static string ErrorMessage(IReadOnlyList<FluentResults.IError> errors, string fallback)
    {
        return errors.FirstOrDefault()?.Message ?? fallback;
    }
}

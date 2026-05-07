using App.BLL.Contracts.Admin;
using App.BLL.DTO.Admin.Users;
using App.BLL.DTO.Common.Errors;
using App.BLL.Mappers.Admin;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly IAppUOW _uow;
    private readonly AdminUserBllMapper _mapper = new();

    public AdminUserService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<AdminUserListDto> SearchUsersAsync(AdminUserSearchDto search, CancellationToken cancellationToken = default)
    {
        var users = await _uow.AdminUsers.SearchUsersAsync(_mapper.Map(search), cancellationToken);
        return new AdminUserListDto
        {
            Search = search,
            Users = users.Select(_mapper.Map).ToList()
        };
    }

    public async Task<AdminUserDetailsDto?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.AdminUsers.GetUserDetailsAsync(userId, cancellationToken);
        return user is null ? null : _mapper.Map(user);
    }

    public async Task<Result<AdminUserDetailsDto>> LockUserAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (userId == actorUserId)
        {
            return Result.Fail<AdminUserDetailsDto>(new BusinessRuleError("Administrators cannot lock their own account."));
        }

        var details = await _uow.AdminUsers.GetUserDetailsAsync(userId, cancellationToken);
        if (details is null)
        {
            return Result.Fail<AdminUserDetailsDto>(new NotFoundError("User was not found."));
        }

        if (await _uow.AdminUsers.HasSystemAdminRoleAsync(userId, cancellationToken) &&
            await _uow.AdminUsers.CountSystemAdminsAsync(cancellationToken) <= 1)
        {
            return Result.Fail<AdminUserDetailsDto>(new BusinessRuleError("The last SystemAdmin account cannot be locked."));
        }

        await _uow.AdminUsers.SetLockoutEndAsync(userId, DateTimeOffset.UtcNow.AddYears(100), cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var updated = await _uow.AdminUsers.GetUserDetailsAsync(userId, cancellationToken);
        return updated is null
            ? Result.Fail<AdminUserDetailsDto>(new NotFoundError("User was not found."))
            : Result.Ok(_mapper.Map(updated));
    }

    public async Task<Result<AdminUserDetailsDto>> UnlockUserAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var details = await _uow.AdminUsers.GetUserDetailsAsync(userId, cancellationToken);
        if (details is null)
        {
            return Result.Fail<AdminUserDetailsDto>(new NotFoundError("User was not found."));
        }

        await _uow.AdminUsers.SetLockoutEndAsync(userId, null, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var updated = await _uow.AdminUsers.GetUserDetailsAsync(userId, cancellationToken);
        return updated is null
            ? Result.Fail<AdminUserDetailsDto>(new NotFoundError("User was not found."))
            : Result.Ok(_mapper.Map(updated));
    }
}

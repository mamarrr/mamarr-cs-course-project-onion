using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IAccountIdentityService
{
    Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> UserExistsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result<AccountRegisterModel>> CreateUserAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<AccountLoginModel>> PasswordSignInAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}

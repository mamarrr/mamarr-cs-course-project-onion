using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using FluentResults;
using System.Security.Claims;

namespace WebApp.Services.Identity;

public interface IIdentityAccountService
{
    Task<Guid?> GetAuthenticatedUserIdAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

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

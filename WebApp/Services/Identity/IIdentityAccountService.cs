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

    Task<Result> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> PasswordSignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}

using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using FluentResults;

namespace WebApp.Services.Account;

public interface IWebAccountService
{
    Task<Result<AccountRegisterModel>> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<AccountLoginModel>> LoginAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(CancellationToken cancellationToken = default);

    Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        CreateManagementCompanyCommand command,
        CancellationToken cancellationToken = default);
}
using App.BLL.DTO.Onboarding.Commands;
using FluentResults;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApp.ViewModels.Onboarding;

namespace WebApp.Mappers.Mvc.Onboarding;

public class OnboardingViewModelMapper
{
    public RegisterAccountCommand Map(RegisterViewModel viewModel)
    {
        return new RegisterAccountCommand
        {
            Email = viewModel.Email,
            Password = viewModel.Password,
            FirstName = viewModel.FirstName,
            LastName = viewModel.LastName
        };
    }

    public LoginAccountCommand Map(LoginViewModel viewModel)
    {
        return new LoginAccountCommand
        {
            Email = viewModel.Email,
            Password = viewModel.Password,
            RememberMe = viewModel.RememberMe
        };
    }

    public CreateManagementCompanyCommand Map(Guid appUserId, CreateManagementCompanyViewModel viewModel)
    {
        return new CreateManagementCompanyCommand
        {
            AppUserId = appUserId,
            Name = viewModel.Name,
            RegistryCode = viewModel.RegistryCode,
            VatNumber = viewModel.VatNumber,
            Email = viewModel.Email,
            Phone = viewModel.Phone,
            Address = viewModel.Address
        };
    }

    public CreateCompanyJoinRequestCommand Map(Guid appUserId, JoinManagementCompanyViewModel viewModel)
    {
        return new CreateCompanyJoinRequestCommand
        {
            AppUserId = appUserId,
            RegistryCode = viewModel.RegistryCode,
            RequestedRoleId = viewModel.RequestedRoleId!.Value,
            Message = viewModel.Message
        };
    }

    public void AddErrors(ModelStateDictionary modelState, ResultBase result)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(string.Empty, error.Message);
        }
    }
}

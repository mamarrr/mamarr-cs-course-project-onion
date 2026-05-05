using System.Globalization;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Onboarding;
using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.DAL.Contracts;
using App.DAL.DTO.ManagementCompanies;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace App.BLL.Services.Onboarding.CompanyJoinRequests;

public class OnboardingCompanyJoinRequestService : IOnboardingCompanyJoinRequestService
{
    private readonly IAppUOW _uow;

    public OnboardingCompanyJoinRequestService(
        IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<OnboardingJoinRequestModel>> CreateJoinRequestAsync(
        CreateCompanyJoinRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var registryCode = command.RegistryCode.Trim();
        if (registryCode.Length == 0)
        {
            return Result.Fail(L("ManagementCompanyWasNotFound", "Management company was not found."));
        }

        var company = await _uow.ManagementCompanies.FirstActiveByRegistryCodeAsync(
            registryCode,
            cancellationToken);
        if (company == null)
        {
            return Result.Fail(L("ManagementCompanyWasNotFound", "Management company was not found."));
        }

        var role = await _uow.Lookups.FindManagementCompanyRoleByIdAsync(
            command.RequestedRoleId,
            cancellationToken);
        if (role == null)
        {
            return Result.Fail(L("SelectedRoleIsInvalid", "Selected role is invalid."));
        }

        var membershipExists = await _uow.ManagementCompanies.MembershipExistsAsync(
            command.AppUserId,
            company.Id,
            cancellationToken);
        if (membershipExists)
        {
            return Result.Fail(L("AlreadyMemberOfThisManagementCompany", "You are already a member of this management company."));
        }

        var pendingStatus = await _uow.Lookups.FindManagementCompanyJoinRequestStatusByCodeAsync(
            ManagementCompanyJoinRequestStatusCodes.Pending,
            cancellationToken);
        if (pendingStatus == null)
        {
            throw new InvalidOperationException(
                $"Management company join request status '{ManagementCompanyJoinRequestStatusCodes.Pending}' is not seeded.");
        }

        var duplicatePending = await _uow.ManagementCompanyJoinRequests.HasPendingRequestAsync(
            command.AppUserId,
            company.Id,
            pendingStatus.Id,
            cancellationToken);
        if (duplicatePending)
        {
            return Result.Fail(L("PendingRequestForThisCompanyAlreadyExists", "A pending request for this company already exists."));
        }

        var requestId = _uow.ManagementCompanyJoinRequests.Add(new ManagementCompanyJoinRequestDalDto
        {
            AppUserId = command.AppUserId,
            ManagementCompanyId = company.Id,
            RequestedRoleId = command.RequestedRoleId,
            StatusId = pendingStatus.Id,
            Message = string.IsNullOrWhiteSpace(command.Message) ? null : command.Message.Trim(),
        });

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return Result.Fail(L("PendingRequestForThisCompanyAlreadyExists", "A pending request for this company already exists."));
        }

        return Result.Ok(new OnboardingJoinRequestModel
        {
            RequestId = requestId
        });
    }

    private static string L(string resourceKey, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture) ?? fallback;
    }
}

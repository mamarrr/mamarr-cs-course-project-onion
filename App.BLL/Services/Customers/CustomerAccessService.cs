using App.BLL.Contracts.Customers;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using App.BLL.Mappers.Customers;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Customers;

public class CustomerAccessService : ICustomerAccessService
{
    private static readonly HashSet<string> AllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private readonly IAppUOW _uow;

    public CustomerAccessService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        GetCompanyCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(query.CompanySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            query.UserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !AllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        return Result.Ok(CustomerWorkspaceBllMapper.MapCompany(company, query.UserId));
    }

    public async Task<Result<CustomerWorkspaceModel>> ResolveCustomerWorkspaceAsync(
        GetCustomerWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(query.CompanySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        if (string.IsNullOrWhiteSpace(query.CustomerSlug))
        {
            return Result.Fail(new NotFoundError("Customer context was not found."));
        }

        var customer = await _uow.Customers.FirstWorkspaceByCompanyAndSlugAsync(
            company.Id,
            query.CustomerSlug,
            cancellationToken);

        if (customer is null)
        {
            return Result.Fail(new NotFoundError("Customer context was not found."));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            query.UserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && AllowedRoleCodes.Contains(roleCode))
        {
            return Result.Ok(CustomerWorkspaceBllMapper.MapWorkspace(customer, query.UserId));
        }

        var hasCustomerContext = await _uow.Customers.ActiveUserCustomerContextExistsAsync(
            query.UserId,
            customer.Id,
            cancellationToken);

        return hasCustomerContext
            ? Result.Ok(CustomerWorkspaceBllMapper.MapWorkspace(customer, query.UserId))
            : Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
    }
}

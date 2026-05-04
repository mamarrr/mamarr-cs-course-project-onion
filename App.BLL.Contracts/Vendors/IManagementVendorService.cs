using App.BLL.Contracts.Vendors.Commands;
using App.BLL.Contracts.Vendors.Models;
using App.BLL.Contracts.Vendors.Queries;
using FluentResults;

namespace App.BLL.Contracts.Vendors;

public interface IManagementVendorService
{
    Task<Result<ManagementVendorsModel>> GetVendorsAsync(
        GetManagementVendorsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementVendorDetailsModel>> GetDetailsAsync(
        GetManagementVendorQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        CreateManagementVendorCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        UpdateManagementVendorCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> AddCategoryAsync(
        AddVendorCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> AddContactAsync(
        AddVendorContactCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> AssignTicketAsync(
        AssignVendorTicketCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> AddScheduledWorkAsync(
        AddVendorScheduledWorkCommand command,
        CancellationToken cancellationToken = default);
}

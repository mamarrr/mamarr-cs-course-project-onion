using App.BLL.Contracts.Tickets.Commands;
using App.BLL.Contracts.Tickets.Models;
using App.BLL.Contracts.Tickets.Queries;
using FluentResults;

namespace App.BLL.Contracts.Tickets.Services;

public interface IManagementTicketService
{
    Task<Result<ManagementTicketsModel>> GetTicketsAsync(
        GetManagementTicketsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketDetailsModel>> GetDetailsAsync(
        GetManagementTicketQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketFormModel>> GetCreateFormAsync(
        GetManagementTicketsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketFormModel>> GetEditFormAsync(
        GetManagementTicketQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<TicketSelectorOptionsModel>> GetSelectorOptionsAsync(
        GetManagementTicketSelectorOptionsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        CreateManagementTicketCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        UpdateManagementTicketCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeleteManagementTicketCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> AdvanceStatusAsync(
        AdvanceManagementTicketStatusCommand command,
        CancellationToken cancellationToken = default);
}

using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Tickets.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Tickets;

public interface ITicketService : IBaseService<TicketBllDto>
{
    Task<Result<ManagementTicketsModel>> SearchAsync(
        ManagementTicketSearchRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ContextTicketsModel>> SearchCustomerTicketsAsync(
        ContextTicketSearchRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ContextTicketsModel>> SearchPropertyTicketsAsync(
        ContextTicketSearchRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ContextTicketsModel>> SearchUnitTicketsAsync(
        ContextTicketSearchRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ContextTicketsModel>> SearchResidentTicketsAsync(
        ContextTicketSearchRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketDetailsModel>> GetDetailsAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketFormModel>> GetCreateFormAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementTicketFormModel>> GetEditFormAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketSelectorOptionsModel>> GetSelectorOptionsAsync(
        TicketSelectorOptionsRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        TicketBllDto dto,
        CancellationToken cancellationToken = default);
    

    Task<Result<TicketBllDto>> UpdateAsync(
        TicketRoute route,
        TicketBllDto dto,
        CancellationToken cancellationToken = default);
    

    Task<Result> DeleteAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketTransitionAvailabilityModel>> GetTransitionAvailabilityAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<TicketBllDto>> AdvanceStatusAsync(
        TicketRoute route,
        CancellationToken cancellationToken = default);
}

using System.Net;
using System.Net.Http.Json;
using App.DTO.v1.Common;
using App.DTO.v1.Portal.ScheduledWork;
using App.DTO.v1.Portal.Tickets;
using App.DTO.v1.Portal.Vendors;
using App.DTO.v1.Portal.WorkLogs;
using AwesomeAssertions;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.API;

public class MaintenanceWorkflowApi_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MaintenanceWorkflowApi_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TicketApi_CreateSearchUpdateAndDelete_Workflow()
    {
        using var anonymous = _factory.CreateClientNoRedirect();
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        using var systemAdmin = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.SystemAdmin);
        var vendor = await CreateVendorWithCategoryAsync(owner, "ticket");
        var ticketNr = UniqueTicketNr();

        var unauthorized = await anonymous.GetAsync(TicketsPath());
        var forbidden = await systemAdmin.GetAsync(TicketsPath());
        var form = await owner.GetAsync($"{TicketsPath()}/form");
        var options = await owner.GetAsync($"{TicketsPath()}/options?CustomerId={TestTenants.CustomerAId:D}&PropertyId={TestTenants.PropertyAId:D}");
        var invalid = await owner.PostAsJsonAsync(TicketsPath(), new CreateTicketDto
        {
            TicketNr = ticketNr,
            Title = "",
            Description = "Invalid ticket",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId
        });
        var created = await owner.PostAsJsonAsync(TicketsPath(), NewTicket(ticketNr, vendor.Id));
        var createdTicket = await created.Content.ReadFromJsonAsync<TicketDto>();
        var management = await owner.GetFromJsonAsync<ManagementTicketsDto>($"{TicketsPath()}?Search={ticketNr}");
        var customer = await owner.GetFromJsonAsync<ContextTicketsDto>($"{TicketsPath()}/customers/customer-a?Search={ticketNr}");
        var property = await owner.GetFromJsonAsync<ContextTicketsDto>($"{TicketsPath()}/customers/customer-a/properties/property-a?Search={ticketNr}");
        var unit = await owner.GetFromJsonAsync<ContextTicketsDto>($"{TicketsPath()}/customers/customer-a/properties/property-a/units/a-101?Search={ticketNr}");
        var details = await owner.GetAsync(TicketPath(createdTicket!.TicketId));
        var detailsDto = await details.Content.ReadFromJsonAsync<TicketDetailsDto>();
        var editForm = await owner.GetAsync($"{TicketPath(createdTicket.TicketId)}/form");
        var transitionAvailability = await owner.GetFromJsonAsync<TicketTransitionAvailabilityDto>(
            $"{TicketPath(createdTicket.TicketId)}/transition-availability");
        var updated = await owner.PutAsJsonAsync(TicketPath(createdTicket.TicketId), new UpdateTicketDto
        {
            TicketNr = ticketNr,
            Title = "Updated API ticket",
            Description = "Updated API ticket description",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = vendor.Id,
            DueAt = DateTime.UtcNow.AddDays(7)
        });
        var updatedTicket = await updated.Content.ReadFromJsonAsync<TicketDto>();
        var deleted = await owner.DeleteAsync(TicketPath(createdTicket.TicketId));
        var deleteResult = await deleted.Content.ReadFromJsonAsync<CommandResultDto>();
        var afterDelete = await owner.GetAsync(TicketPath(createdTicket.TicketId));

        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        form.StatusCode.Should().Be(HttpStatusCode.OK);
        options.StatusCode.Should().Be(HttpStatusCode.OK);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Headers.Location.Should().NotBeNull();
        createdTicket.TicketNr.Should().Be(ticketNr);
        management!.Tickets.Should().ContainSingle(ticket => ticket.TicketId == createdTicket.TicketId);
        customer!.Tickets.Should().ContainSingle(ticket => ticket.TicketId == createdTicket.TicketId);
        property!.Tickets.Should().ContainSingle(ticket => ticket.TicketId == createdTicket.TicketId);
        unit!.Tickets.Should().ContainSingle(ticket => ticket.TicketId == createdTicket.TicketId);
        details.StatusCode.Should().Be(HttpStatusCode.OK);
        detailsDto!.TicketNr.Should().Be(ticketNr);
        detailsDto.CustomerSlug.Should().Be("customer-a");
        detailsDto.PropertySlug.Should().Be("property-a");
        detailsDto.UnitSlug.Should().Be("a-101");
        editForm.StatusCode.Should().Be(HttpStatusCode.OK);
        transitionAvailability!.CanAdvance.Should().BeTrue();
        transitionAvailability.NextStatusCode.Should().Be("ASSIGNED");
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedTicket!.Title.Should().Be("Updated API ticket");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResult.Should().NotBeNull();
        deleteResult!.Success.Should().BeTrue();
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ScheduledWorkAndWorkLogApi_ScheduleUpdateLogAndDelete_Workflow()
    {
        using var owner = await _factory.CreateAuthenticatedApiClientAsync(TestUsers.CompanyAOwner);
        var vendor = await CreateVendorWithCategoryAsync(owner, "scheduled-work");
        var ticket = await CreateTicketAsync(owner, vendor.Id);
        var assigned = await owner.PostAsync($"{TicketPath(ticket.TicketId)}/advance-status", null);
        var scheduledStart = DateTime.UtcNow.AddDays(2);

        var form = await owner.GetAsync($"{ScheduledWorksPath(ticket.TicketId)}/form");
        var invalid = await owner.PostAsJsonAsync(ScheduledWorksPath(ticket.TicketId), new ScheduledWorkRequestDto
        {
            VendorId = vendor.Id,
            WorkStatusId = TestTenants.WorkStatusScheduledId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(-1)
        });
        var scheduled = await owner.PostAsJsonAsync(ScheduledWorksPath(ticket.TicketId), new ScheduledWorkRequestDto
        {
            VendorId = vendor.Id,
            WorkStatusId = TestTenants.WorkStatusScheduledId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(2),
            Notes = "API scheduled work"
        });
        var scheduledDto = await scheduled.Content.ReadFromJsonAsync<ScheduledWorkDto>();
        var list = await owner.GetFromJsonAsync<ScheduledWorkListDto>(ScheduledWorksPath(ticket.TicketId));
        var details = await owner.GetAsync(ScheduledWorkPath(ticket.TicketId, scheduledDto!.ScheduledWorkId));
        var updatedStart = scheduledStart.AddDays(1);
        var updated = await owner.PutAsJsonAsync(ScheduledWorkPath(ticket.TicketId, scheduledDto.ScheduledWorkId), new ScheduledWorkRequestDto
        {
            VendorId = vendor.Id,
            WorkStatusId = TestTenants.WorkStatusScheduledId,
            ScheduledStart = updatedStart,
            ScheduledEnd = updatedStart.AddHours(3),
            Notes = "Updated API scheduled work"
        });
        var updatedScheduled = await updated.Content.ReadFromJsonAsync<ScheduledWorkDto>();
        var workLogForm = await owner.GetAsync($"{WorkLogsPath(ticket.TicketId, scheduledDto.ScheduledWorkId)}/form");
        var workStart = DateTime.UtcNow.AddHours(-3);
        var workEnd = DateTime.UtcNow.AddHours(-1);
        var invalidWorkLog = await owner.PostAsJsonAsync(WorkLogsPath(ticket.TicketId, scheduledDto.ScheduledWorkId), new WorkLogRequestDto
        {
            WorkStart = workEnd,
            WorkEnd = workStart,
            Hours = 2,
            Description = "Invalid work log"
        });
        var workLog = await owner.PostAsJsonAsync(WorkLogsPath(ticket.TicketId, scheduledDto.ScheduledWorkId), new WorkLogRequestDto
        {
            WorkStart = workStart,
            WorkEnd = workEnd,
            Hours = 2.5m,
            MaterialCost = 10m,
            LaborCost = 50m,
            Description = "API work log"
        });
        var workLogDto = await workLog.Content.ReadFromJsonAsync<WorkLogDto>();
        var workLogList = await owner.GetFromJsonAsync<WorkLogListDto>(WorkLogsPath(ticket.TicketId, scheduledDto.ScheduledWorkId));
        var workLogEdit = await owner.GetAsync($"{WorkLogPath(ticket.TicketId, scheduledDto.ScheduledWorkId, workLogDto!.WorkLogId)}/form");
        var workLogDeleteModel = await owner.GetAsync($"{WorkLogPath(ticket.TicketId, scheduledDto.ScheduledWorkId, workLogDto.WorkLogId)}/delete-model");
        var updatedWorkLog = await owner.PutAsJsonAsync(WorkLogPath(ticket.TicketId, scheduledDto.ScheduledWorkId, workLogDto.WorkLogId), new WorkLogRequestDto
        {
            WorkStart = workStart,
            WorkEnd = workEnd,
            Hours = 3m,
            MaterialCost = 15m,
            LaborCost = 60m,
            Description = "Updated API work log"
        });
        var updatedWorkLogDto = await updatedWorkLog.Content.ReadFromJsonAsync<WorkLogDto>();
        var deletedWorkLog = await owner.DeleteAsync(WorkLogPath(ticket.TicketId, scheduledDto.ScheduledWorkId, workLogDto.WorkLogId));
        var deletedWorkLogResult = await deletedWorkLog.Content.ReadFromJsonAsync<CommandResultDto>();
        var deletedScheduledWork = await owner.DeleteAsync(ScheduledWorkPath(ticket.TicketId, scheduledDto.ScheduledWorkId));
        var deletedScheduledWorkResult = await deletedScheduledWork.Content.ReadFromJsonAsync<CommandResultDto>();
        var afterScheduledDelete = await owner.GetAsync(ScheduledWorkPath(ticket.TicketId, scheduledDto.ScheduledWorkId));

        assigned.StatusCode.Should().Be(HttpStatusCode.OK);
        form.StatusCode.Should().Be(HttpStatusCode.OK);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        scheduled.StatusCode.Should().Be(HttpStatusCode.Created);
        scheduled.Headers.Location.Should().NotBeNull();
        scheduledDto.VendorId.Should().Be(vendor.Id);
        list!.Items.Should().ContainSingle(item => item.ScheduledWorkId == scheduledDto.ScheduledWorkId);
        details.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedScheduled!.ScheduledStart.Should().Be(updatedStart);
        updatedScheduled.Notes.Should().Be("Updated API scheduled work");
        workLogForm.StatusCode.Should().Be(HttpStatusCode.OK);
        invalidWorkLog.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        workLog.StatusCode.Should().Be(HttpStatusCode.Created);
        workLogDto.Hours.Should().Be(2.5m);
        workLogList!.Items.Should().ContainSingle(item => item.WorkLogId == workLogDto.WorkLogId);
        workLogList.Totals.TotalCost.Should().Be(60m);
        workLogEdit.StatusCode.Should().Be(HttpStatusCode.OK);
        workLogDeleteModel.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedWorkLog.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedWorkLogDto!.Hours.Should().Be(3m);
        updatedWorkLogDto.Description.Should().Be("Updated API work log");
        deletedWorkLog.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedWorkLogResult.Should().NotBeNull();
        deletedWorkLogResult!.Success.Should().BeTrue();
        deletedScheduledWork.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedScheduledWorkResult.Should().NotBeNull();
        deletedScheduledWorkResult!.Success.Should().BeTrue();
        afterScheduledDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<VendorProfileDto> CreateVendorWithCategoryAsync(HttpClient client, string suffix)
    {
        var created = await client.PostAsJsonAsync($"{CompanyPath()}/vendors", new VendorRequestDto
        {
            Name = $"API Maintenance Vendor {suffix}",
            RegistryCode = UniqueCode("VEND"),
            Notes = "API maintenance vendor"
        });
        var profile = await created.Content.ReadFromJsonAsync<VendorProfileDto>();
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var category = await client.PostAsJsonAsync($"{CompanyPath()}/vendors/{profile!.Id:D}/categories", new AssignVendorCategoryDto
        {
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            Notes = "API maintenance category"
        });
        category.StatusCode.Should().Be(HttpStatusCode.OK);

        return profile;
    }

    private static async Task<TicketDto> CreateTicketAsync(HttpClient client, Guid vendorId)
    {
        var created = await client.PostAsJsonAsync(TicketsPath(), NewTicket(UniqueTicketNr(), vendorId));
        var ticket = await created.Content.ReadFromJsonAsync<TicketDto>();

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        ticket.Should().NotBeNull();
        return ticket!;
    }

    private static CreateTicketDto NewTicket(string ticketNr, Guid vendorId)
    {
        return new CreateTicketDto
        {
            TicketNr = ticketNr,
            Title = $"API ticket {ticketNr}",
            Description = $"API ticket description {ticketNr}",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = vendorId,
            DueAt = DateTime.UtcNow.AddDays(5)
        };
    }

    private static string CompanyPath()
    {
        return $"/api/v1/portal/companies/{TestTenants.CompanyASlug}";
    }

    private static string TicketsPath()
    {
        return $"{CompanyPath()}/tickets";
    }

    private static string TicketPath(Guid ticketId)
    {
        return $"{TicketsPath()}/{ticketId:D}";
    }

    private static string ScheduledWorksPath(Guid ticketId)
    {
        return $"{TicketPath(ticketId)}/scheduled-work";
    }

    private static string ScheduledWorkPath(Guid ticketId, Guid scheduledWorkId)
    {
        return $"{ScheduledWorksPath(ticketId)}/{scheduledWorkId:D}";
    }

    private static string WorkLogsPath(Guid ticketId, Guid scheduledWorkId)
    {
        return $"{ScheduledWorkPath(ticketId, scheduledWorkId)}/work-logs";
    }

    private static string WorkLogPath(Guid ticketId, Guid scheduledWorkId, Guid workLogId)
    {
        return $"{WorkLogsPath(ticketId, scheduledWorkId)}/{workLogId:D}";
    }

    private static string UniqueTicketNr()
    {
        return $"T{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}

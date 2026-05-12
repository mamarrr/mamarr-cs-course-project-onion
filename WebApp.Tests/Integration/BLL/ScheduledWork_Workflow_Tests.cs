using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.WorkLogs;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class ScheduledWork_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ScheduledWork_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ScheduleUpdateStartAndComplete_Workflow()
    {
        using var culture = new CultureScope("en");
        var setup = await CreateAssignedTicketAsync("scheduled-main");
        var scheduledStart = DateTime.UtcNow.AddDays(2);

        Guid scheduledWorkId;
        using (var scheduleScope = _factory.Services.CreateScope())
        {
            var bll = scheduleScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var form = await bll.ScheduledWorks.GetCreateFormAsync(TicketRoute(setup.TicketId));
            var scheduled = await bll.ScheduledWorks.ScheduleAsync(
                TicketRoute(setup.TicketId),
                NewScheduledWork(setup.TicketId, setup.VendorId, scheduledStart, "Initial scheduled notes"));
            var list = await bll.ScheduledWorks.ListForTicketAsync(TicketRoute(setup.TicketId));
            var ticket = await bll.Tickets.GetDetailsAsync(TicketRoute(setup.TicketId));

            form.Value.Vendors.Should().Contain(vendor => vendor.Id == setup.VendorId);
            scheduled.IsSuccess.Should().BeTrue();
            scheduledWorkId = scheduled.Value.Id;
            list.Value.Items.Should().ContainSingle(item => item.ScheduledWorkId == scheduledWorkId);
            ticket.Value.StatusCode.Should().Be("SCHEDULED");
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updatedStart = scheduledStart.AddDays(1);
            var updated = await bll.ScheduledWorks.UpdateScheduleAsync(
                ScheduledWorkRoute(setup.TicketId, scheduledWorkId),
                NewScheduledWork(setup.TicketId, setup.VendorId, updatedStart, "Updated scheduled notes"));
            var details = await bll.ScheduledWorks.GetDetailsAsync(ScheduledWorkRoute(setup.TicketId, scheduledWorkId));

            updated.IsSuccess.Should().BeTrue();
            details.Value.ScheduledStart.Should().Be(updatedStart);
            details.Value.Notes.Should().Be("Updated scheduled notes");
            details.Value.WorkStatusCode.Should().Be("SCHEDULED");
        }

        using var workScope = _factory.Services.CreateScope();
        var workBll = workScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var realStart = DateTime.UtcNow.AddHours(-3);
        var realEnd = DateTime.UtcNow.AddHours(-1);

        var started = await workBll.ScheduledWorks.StartWorkAsync(
            ScheduledWorkRoute(setup.TicketId, scheduledWorkId),
            realStart);
        var afterStart = await workBll.ScheduledWorks.GetDetailsAsync(ScheduledWorkRoute(setup.TicketId, scheduledWorkId));
        var ticketAfterStart = await workBll.Tickets.GetDetailsAsync(TicketRoute(setup.TicketId));
        var completed = await workBll.ScheduledWorks.CompleteWorkAsync(
            ScheduledWorkRoute(setup.TicketId, scheduledWorkId),
            realEnd);
        var afterComplete = await workBll.ScheduledWorks.GetDetailsAsync(ScheduledWorkRoute(setup.TicketId, scheduledWorkId));

        started.IsSuccess.Should().BeTrue();
        afterStart.Value.RealStart.Should().Be(realStart);
        afterStart.Value.WorkStatusCode.Should().Be("IN_PROGRESS");
        ticketAfterStart.Value.StatusCode.Should().Be("IN_PROGRESS");
        completed.IsSuccess.Should().BeTrue();
        afterComplete.Value.RealEnd.Should().Be(realEnd);
        afterComplete.Value.WorkStatusCode.Should().Be("DONE");
    }

    [Fact]
    public async Task CancelAndDeleteUnstartedWork_Workflow()
    {
        var setup = await CreateAssignedTicketAsync("scheduled-cancel");
        var scheduledWork = await ScheduleWorkAsync(setup, "Cancel scheduled notes");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var cancelled = await bll.ScheduledWorks.CancelWorkAsync(ScheduledWorkRoute(setup.TicketId, scheduledWork.ScheduledWorkId));
        var afterCancel = await bll.ScheduledWorks.GetDetailsAsync(ScheduledWorkRoute(setup.TicketId, scheduledWork.ScheduledWorkId));
        var deleted = await bll.ScheduledWorks.DeleteAsync(ScheduledWorkRoute(setup.TicketId, scheduledWork.ScheduledWorkId));
        var afterDelete = await bll.ScheduledWorks.GetDetailsAsync(ScheduledWorkRoute(setup.TicketId, scheduledWork.ScheduledWorkId));

        cancelled.IsSuccess.Should().BeTrue();
        afterCancel.Value.WorkStatusCode.Should().Be("CANCELLED");
        deleted.IsSuccess.Should().BeTrue();
        afterDelete.ShouldFailWith<NotFoundError>();
    }

    [Fact]
    public async Task InvalidTransitionsAndDeleteDependencies_AreRejected()
    {
        var unassigned = await CreateTicketWithVendorAsync("scheduled-invalid-unassigned");
        var assigned = await CreateAssignedTicketAsync("scheduled-invalid-assigned");
        var scheduledWork = await ScheduleWorkAsync(assigned, "Delete dependency notes");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var scheduleBeforeAssigned = await bll.ScheduledWorks.ScheduleAsync(
            TicketRoute(unassigned.TicketId),
            NewScheduledWork(unassigned.TicketId, unassigned.VendorId, DateTime.UtcNow.AddDays(3), "Too early"));
        var completeBeforeStart = await bll.ScheduledWorks.CompleteWorkAsync(
            ScheduledWorkRoute(assigned.TicketId, scheduledWork.ScheduledWorkId),
            DateTime.UtcNow);
        var log = await bll.WorkLogs.AddAsync(
            ScheduledWorkRoute(assigned.TicketId, scheduledWork.ScheduledWorkId),
            new WorkLogBllDto
            {
                Hours = 1,
                Description = "Blocks scheduled work delete"
            });
        var deleteWithLog = await bll.ScheduledWorks.DeleteAsync(
            ScheduledWorkRoute(assigned.TicketId, scheduledWork.ScheduledWorkId));

        scheduleBeforeAssigned.ShouldFailWith<BusinessRuleError>();
        completeBeforeStart.ShouldFailWith<BusinessRuleError>();
        log.IsSuccess.Should().BeTrue();
        deleteWithLog.ShouldFailWith<BusinessRuleError>();
    }

    private async Task<TicketSetup> CreateAssignedTicketAsync(string suffix)
    {
        var setup = await CreateTicketWithVendorAsync(suffix);

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var assigned = await bll.Tickets.AdvanceStatusAsync(TicketRoute(setup.TicketId));

        assigned.IsSuccess.Should().BeTrue();
        return setup;
    }

    private async Task<TicketSetup> CreateTicketWithVendorAsync(string suffix)
    {
        var vendor = await CreateVendorWithCategoryAsync(suffix);
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var ticketNr = UniqueTicketNr();

        var created = await bll.Tickets.CreateAsync(
            CompanyRoute(),
            new TicketBllDto
            {
                TicketNr = ticketNr,
                Title = $"Workflow ticket {suffix}",
                Description = $"Workflow ticket description {suffix}",
                TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                TicketPriorityId = TestTenants.TicketPriorityReferencedId,
                CustomerId = TestTenants.CustomerAId,
                PropertyId = TestTenants.PropertyAId,
                UnitId = TestTenants.UnitAId,
                VendorId = vendor.VendorId,
                DueAt = DateTime.UtcNow.AddDays(5)
            });

        created.IsSuccess.Should().BeTrue();
        return new TicketSetup(created.Value.Id, vendor.VendorId);
    }

    private async Task<ScheduledWorkSeed> ScheduleWorkAsync(TicketSetup setup, string notes)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var scheduled = await bll.ScheduledWorks.ScheduleAsync(
            TicketRoute(setup.TicketId),
            NewScheduledWork(setup.TicketId, setup.VendorId, DateTime.UtcNow.AddDays(2), notes));

        scheduled.IsSuccess.Should().BeTrue();
        return new ScheduledWorkSeed(scheduled.Value.Id);
    }

    private async Task<VendorSeed> CreateVendorWithCategoryAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var created = await bll.Vendors.CreateAndGetProfileAsync(
            CompanyRoute(),
            new VendorBllDto
            {
                Name = $"Scheduled Work Vendor {suffix}",
                RegistryCode = UniqueCode("VEND"),
                Notes = "Scheduled work workflow vendor"
            });

        created.IsSuccess.Should().BeTrue();

        var assigned = await bll.Vendors.AssignCategoryAsync(
            VendorRoute(created.Value.Id),
            new VendorTicketCategoryBllDto
            {
                TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                Notes = "Scheduled work category"
            });

        assigned.IsSuccess.Should().BeTrue();
        return new VendorSeed(created.Value.Id);
    }

    private static ScheduledWorkBllDto NewScheduledWork(
        Guid ticketId,
        Guid vendorId,
        DateTime scheduledStart,
        string notes)
    {
        return new ScheduledWorkBllDto
        {
            TicketId = ticketId,
            VendorId = vendorId,
            WorkStatusId = TestTenants.WorkStatusScheduledId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(2),
            Notes = notes
        };
    }

    private static ManagementCompanyRoute CompanyRoute()
    {
        return new ManagementCompanyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug
        };
    }

    private static TicketRoute TicketRoute(Guid ticketId)
    {
        return new TicketRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = ticketId
        };
    }

    private static ScheduledWorkRoute ScheduledWorkRoute(Guid ticketId, Guid scheduledWorkId)
    {
        return new ScheduledWorkRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = ticketId,
            ScheduledWorkId = scheduledWorkId
        };
    }

    private static VendorRoute VendorRoute(Guid vendorId)
    {
        return new VendorRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            VendorId = vendorId
        };
    }

    private static string UniqueTicketNr()
    {
        return $"T{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }

    private sealed record TicketSetup(Guid TicketId, Guid VendorId);
    private sealed record ScheduledWorkSeed(Guid ScheduledWorkId);
    private sealed record VendorSeed(Guid VendorId);
}

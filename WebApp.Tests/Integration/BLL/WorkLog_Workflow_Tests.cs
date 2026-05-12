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

public class WorkLog_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkLog_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddListUpdateAndDeleteWorkLog_Workflow()
    {
        using var culture = new CultureScope("en");
        var scheduledWork = await CreateScheduledWorkAsync("worklog-main");
        var workStart = DateTime.UtcNow.AddHours(-3);
        var workEnd = DateTime.UtcNow.AddHours(-1);

        Guid workLogId;
        using (var addScope = _factory.Services.CreateScope())
        {
            var bll = addScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var form = await bll.WorkLogs.GetCreateFormAsync(ScheduledWorkRoute(scheduledWork));
            var added = await bll.WorkLogs.AddAsync(
                ScheduledWorkRoute(scheduledWork),
                NewWorkLog(workStart, workEnd, 2.5m, 10m, 50m, "Initial work log"));
            var list = await bll.WorkLogs.ListForScheduledWorkAsync(ScheduledWorkRoute(scheduledWork));

            form.Value.ScheduledWorkId.Should().Be(scheduledWork.ScheduledWorkId);
            form.Value.CanViewCosts.Should().BeTrue();
            added.IsSuccess.Should().BeTrue();
            workLogId = added.Value.Id;
            list.Value.Items.Should().ContainSingle(item => item.WorkLogId == workLogId);
            list.Value.Totals.Count.Should().Be(1);
            list.Value.Totals.Hours.Should().Be(2.5m);
            list.Value.Totals.MaterialCost.Should().Be(10m);
            list.Value.Totals.LaborCost.Should().Be(50m);
            list.Value.Totals.TotalCost.Should().Be(60m);
        }

        using (var updateScope = _factory.Services.CreateScope())
        {
            var bll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var updated = await bll.WorkLogs.UpdateAsync(
                WorkLogRoute(scheduledWork, workLogId),
                NewWorkLog(workStart, workEnd, 3m, 15m, 60m, "Updated work log"));
            var editForm = await bll.WorkLogs.GetEditFormAsync(WorkLogRoute(scheduledWork, workLogId));
            var deleteModel = await bll.WorkLogs.GetDeleteModelAsync(WorkLogRoute(scheduledWork, workLogId));

            updated.IsSuccess.Should().BeTrue();
            updated.Value.Hours.Should().Be(3m);
            updated.Value.Description.Should().Be("Updated work log");
            editForm.Value.Hours.Should().Be(3m);
            editForm.Value.Description.Should().Be("Updated work log");
            deleteModel.Value.WorkLogId.Should().Be(workLogId);
            deleteModel.Value.Description.Should().Be("Updated work log");
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.WorkLogs.DeleteAsync(WorkLogRoute(scheduledWork, workLogId));
        var afterDelete = await deleteBll.WorkLogs.ListForScheduledWorkAsync(ScheduledWorkRoute(scheduledWork));

        deleted.IsSuccess.Should().BeTrue();
        afterDelete.Value.Items.Should().NotContain(item => item.WorkLogId == workLogId);
        afterDelete.Value.Totals.Count.Should().Be(0);
    }

    [Fact]
    public async Task ClosedTicketBlocksWorkLogMutations()
    {
        var closed = await CreateClosedTicketWithWorkLogAsync("worklog-closed");

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var add = await bll.WorkLogs.AddAsync(
            ScheduledWorkRoute(closed),
            NewWorkLog(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 1m, null, null, "Closed add"));
        var update = await bll.WorkLogs.UpdateAsync(
            WorkLogRoute(closed, closed.WorkLogId),
            NewWorkLog(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, 1m, null, null, "Closed update"));
        var delete = await bll.WorkLogs.DeleteAsync(WorkLogRoute(closed, closed.WorkLogId));

        add.ShouldFailWith<BusinessRuleError>();
        update.ShouldFailWith<BusinessRuleError>();
        delete.ShouldFailWith<BusinessRuleError>();
    }

    private async Task<ClosedWorkLogSeed> CreateClosedTicketWithWorkLogAsync(string suffix)
    {
        var scheduledWork = await CreateScheduledWorkAsync(suffix);
        var workStart = DateTime.UtcNow.AddHours(-3);
        var workEnd = DateTime.UtcNow.AddHours(-1);

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var started = await bll.ScheduledWorks.StartWorkAsync(ScheduledWorkRoute(scheduledWork), workStart);
        var completed = await bll.ScheduledWorks.CompleteWorkAsync(ScheduledWorkRoute(scheduledWork), workEnd);
        var log = await bll.WorkLogs.AddAsync(
            ScheduledWorkRoute(scheduledWork),
            NewWorkLog(workStart, workEnd, 2m, null, null, "Closed ticket work log"));
        var completedTicket = await bll.Tickets.AdvanceStatusAsync(TicketRoute(scheduledWork.TicketId));
        var closedTicket = await bll.Tickets.AdvanceStatusAsync(TicketRoute(scheduledWork.TicketId));

        started.IsSuccess.Should().BeTrue();
        completed.IsSuccess.Should().BeTrue();
        log.IsSuccess.Should().BeTrue();
        completedTicket.IsSuccess.Should().BeTrue();
        closedTicket.IsSuccess.Should().BeTrue();

        return new ClosedWorkLogSeed(
            scheduledWork.TicketId,
            scheduledWork.ScheduledWorkId,
            log.Value.Id);
    }

    private async Task<ScheduledWorkSeed> CreateScheduledWorkAsync(string suffix)
    {
        var setup = await CreateAssignedTicketAsync(suffix);

        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var scheduledStart = DateTime.UtcNow.AddDays(2);
        var scheduled = await bll.ScheduledWorks.ScheduleAsync(
            TicketRoute(setup.TicketId),
            new ScheduledWorkBllDto
            {
                TicketId = setup.TicketId,
                VendorId = setup.VendorId,
                WorkStatusId = TestTenants.WorkStatusScheduledId,
                ScheduledStart = scheduledStart,
                ScheduledEnd = scheduledStart.AddHours(2),
                Notes = $"Work log schedule {suffix}"
            });

        scheduled.IsSuccess.Should().BeTrue();
        return new ScheduledWorkSeed(setup.TicketId, scheduled.Value.Id);
    }

    private async Task<TicketSetup> CreateAssignedTicketAsync(string suffix)
    {
        var vendor = await CreateVendorWithCategoryAsync(suffix);

        Guid ticketId;
        using (var createScope = _factory.Services.CreateScope())
        {
            var bll = createScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var created = await bll.Tickets.CreateAsync(
                CompanyRoute(),
                new TicketBllDto
                {
                    TicketNr = UniqueTicketNr(),
                    Title = $"Work log ticket {suffix}",
                    Description = $"Work log ticket description {suffix}",
                    TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                    TicketPriorityId = TestTenants.TicketPriorityReferencedId,
                    CustomerId = TestTenants.CustomerAId,
                    PropertyId = TestTenants.PropertyAId,
                    UnitId = TestTenants.UnitAId,
                    VendorId = vendor.VendorId,
                    DueAt = DateTime.UtcNow.AddDays(5)
                });

            created.IsSuccess.Should().BeTrue();
            ticketId = created.Value.Id;
        }

        using var assignScope = _factory.Services.CreateScope();
        var assignBll = assignScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var assigned = await assignBll.Tickets.AdvanceStatusAsync(TicketRoute(ticketId));

        assigned.IsSuccess.Should().BeTrue();
        return new TicketSetup(ticketId, vendor.VendorId);
    }

    private async Task<VendorSeed> CreateVendorWithCategoryAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var created = await bll.Vendors.CreateAndGetProfileAsync(
            CompanyRoute(),
            new VendorBllDto
            {
                Name = $"Work Log Vendor {suffix}",
                RegistryCode = UniqueCode("VEND"),
                Notes = "Work log workflow vendor"
            });

        created.IsSuccess.Should().BeTrue();

        var assigned = await bll.Vendors.AssignCategoryAsync(
            VendorRoute(created.Value.Id),
            new VendorTicketCategoryBllDto
            {
                TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                Notes = "Work log category"
            });

        assigned.IsSuccess.Should().BeTrue();
        return new VendorSeed(created.Value.Id);
    }

    private static WorkLogBllDto NewWorkLog(
        DateTime workStart,
        DateTime workEnd,
        decimal hours,
        decimal? materialCost,
        decimal? laborCost,
        string description)
    {
        return new WorkLogBllDto
        {
            WorkStart = workStart,
            WorkEnd = workEnd,
            Hours = hours,
            MaterialCost = materialCost,
            LaborCost = laborCost,
            Description = description
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

    private static ScheduledWorkRoute ScheduledWorkRoute(ScheduledWorkSeed seed)
    {
        return new ScheduledWorkRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = seed.TicketId,
            ScheduledWorkId = seed.ScheduledWorkId
        };
    }

    private static ScheduledWorkRoute ScheduledWorkRoute(ClosedWorkLogSeed seed)
    {
        return new ScheduledWorkRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = seed.TicketId,
            ScheduledWorkId = seed.ScheduledWorkId
        };
    }

    private static WorkLogRoute WorkLogRoute(ScheduledWorkSeed seed, Guid workLogId)
    {
        return new WorkLogRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = seed.TicketId,
            ScheduledWorkId = seed.ScheduledWorkId,
            WorkLogId = workLogId
        };
    }

    private static WorkLogRoute WorkLogRoute(ClosedWorkLogSeed seed, Guid workLogId)
    {
        return new WorkLogRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = seed.TicketId,
            ScheduledWorkId = seed.ScheduledWorkId,
            WorkLogId = workLogId
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
    private sealed record ScheduledWorkSeed(Guid TicketId, Guid ScheduledWorkId);
    private sealed record ClosedWorkLogSeed(Guid TicketId, Guid ScheduledWorkId, Guid WorkLogId);
    private sealed record VendorSeed(Guid VendorId);
}

using App.DAL.Contracts;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.WorkLogs;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class WorkLogRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkLogRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QueryMethods_ReturnWorkLogsWithinCompanyAndScheduledWork()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow);
        var workLogId = await CreateWorkLogAsync(uow, scheduledWorkId, "Work completed");

        var all = (await uow.WorkLogs.AllAsync(TestTenants.CompanyAId)).ToList();
        var list = await uow.WorkLogs.AllByScheduledWorkAsync(scheduledWorkId, TestTenants.CompanyAId);
        var found = await uow.WorkLogs.FindAsync(workLogId, TestTenants.CompanyAId);
        var inCompany = await uow.WorkLogs.FindInCompanyAsync(workLogId, TestTenants.CompanyAId);
        var wrongCompany = await uow.WorkLogs.FindInCompanyAsync(workLogId, Guid.NewGuid());

        all.Should().Contain(log => log.Id == workLogId);
        list.Should().ContainSingle(log => log.Id == workLogId);
        list[0].AppUserId.Should().Be(TestUsers.CompanyAOwnerId);
        list[0].AppUserName.Should().Be("Company Owner");
        list[0].Description.Should().Be("Work completed");
        found.Should().NotBeNull();
        inCompany.Should().NotBeNull();
        wrongCompany.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceAndTotals_AreScopedToScheduledWorkTicketAndCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow);
        var workLogId = await CreateWorkLogAsync(uow, scheduledWorkId, "Totals work");

        var existsInCompany = await uow.WorkLogs.ExistsInCompanyAsync(workLogId, TestTenants.CompanyAId);
        var existsForScheduledWork = await uow.WorkLogs.ExistsForScheduledWorkAsync(scheduledWorkId, TestTenants.CompanyAId);
        var existsForTicket = await uow.WorkLogs.ExistsForTicketAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var scheduledWorkTotals = await uow.WorkLogs.TotalsForScheduledWorkAsync(scheduledWorkId, TestTenants.CompanyAId);
        var ticketTotals = await uow.WorkLogs.TotalsForTicketAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var wrongCompanyTotals = await uow.WorkLogs.TotalsForScheduledWorkAsync(scheduledWorkId, Guid.NewGuid());

        existsInCompany.Should().BeTrue();
        existsForScheduledWork.Should().BeTrue();
        existsForTicket.Should().BeTrue();
        scheduledWorkTotals.Count.Should().Be(1);
        scheduledWorkTotals.Hours.Should().Be(2.5m);
        scheduledWorkTotals.MaterialCost.Should().Be(10m);
        scheduledWorkTotals.LaborCost.Should().Be(50m);
        scheduledWorkTotals.TotalCost.Should().Be(60m);
        ticketTotals.Count.Should().BeGreaterThanOrEqualTo(1);
        wrongCompanyTotals.Count.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureDescriptionAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow);
        var workLogId = await CreateWorkLogAsync(uow, scheduledWorkId, "English description");

        var entity = await db.WorkLogs.AsTracking().SingleAsync(log => log.Id == workLogId);
        entity.Description!.SetTranslation("Estonian description", "et");
        db.Entry(entity).Property(log => log.Description).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.WorkLogs.UpdateAsync(new WorkLogDalDto
        {
            Id = workLogId,
            ScheduledWorkId = scheduledWorkId,
            AppUserId = TestUsers.CompanyAOwnerId,
            WorkStart = DateTime.UtcNow.AddHours(-3),
            WorkEnd = DateTime.UtcNow.AddHours(-1),
            Hours = 3m,
            MaterialCost = 20m,
            LaborCost = 70m,
            Description = "English updated"
        }, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.WorkLogs.AsNoTracking().SingleAsync(log => log.Id == workLogId);
        persisted.Hours.Should().Be(3m);
        persisted.MaterialCost.Should().Be(20m);
        persisted.LaborCost.Should().Be(70m);
        persisted.Description!.Translate("en").Should().Be("English updated");
        persisted.Description.Translate("et").Should().Be("Estonian description");
    }

    [Fact]
    public async Task DeleteInCompanyAsync_DeletesOnlyWithinCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow);
        var workLogId = await CreateWorkLogAsync(uow, scheduledWorkId, "Delete description");

        var wrongCompany = await uow.WorkLogs.DeleteInCompanyAsync(workLogId, Guid.NewGuid());
        var deleted = await uow.WorkLogs.DeleteInCompanyAsync(workLogId, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        wrongCompany.Should().BeFalse();
        deleted.Should().BeTrue();
        var exists = await db.WorkLogs.AsNoTracking().AnyAsync(log => log.Id == workLogId);
        exists.Should().BeFalse();
    }

    private static async Task<Guid> CreateScheduledWorkAsync(IAppUOW uow)
    {
        var id = Guid.NewGuid();
        var scheduledStart = DateTime.UtcNow.AddDays(2);
        uow.ScheduledWorks.Add(new ScheduledWorkDalDto
        {
            Id = id,
            VendorId = TestTenants.VendorAId,
            TicketId = TestTenants.TicketAId,
            WorkStatusId = TestTenants.WorkStatusScheduledId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(2),
            Notes = "Work log parent"
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }

    private static async Task<Guid> CreateWorkLogAsync(IAppUOW uow, Guid scheduledWorkId, string description)
    {
        var id = Guid.NewGuid();
        uow.WorkLogs.Add(new WorkLogDalDto
        {
            Id = id,
            ScheduledWorkId = scheduledWorkId,
            AppUserId = TestUsers.CompanyAOwnerId,
            WorkStart = DateTime.UtcNow.AddHours(-2),
            WorkEnd = DateTime.UtcNow,
            Hours = 2.5m,
            MaterialCost = 10m,
            LaborCost = 50m,
            Description = description
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }
}

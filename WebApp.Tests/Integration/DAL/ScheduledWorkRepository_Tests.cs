using App.DAL.Contracts;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.Vendors;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class ScheduledWorkRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ScheduledWorkRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllAndDetailsQueries_ReturnScheduledWorkWithinCompany()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow, "Query note");

        var all = (await uow.ScheduledWorks.AllAsync(TestTenants.CompanyAId)).ToList();
        var byCompany = await uow.ScheduledWorks.AllByCompanyAsync(TestTenants.CompanyAId);
        var byTicket = await uow.ScheduledWorks.AllByTicketAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var details = await uow.ScheduledWorks.FindDetailsAsync(scheduledWorkId, TestTenants.CompanyAId);
        var wrongCompany = await uow.ScheduledWorks.FindDetailsAsync(scheduledWorkId, Guid.NewGuid());

        all.Should().Contain(work => work.Id == scheduledWorkId);
        byCompany.Should().Contain(work => work.Id == scheduledWorkId);
        byTicket.Should().Contain(work => work.Id == scheduledWorkId);
        details.Should().NotBeNull();
        details!.TicketId.Should().Be(TestTenants.TicketAId);
        details.TicketNr.Should().Be("T-A-0001");
        details.TicketTitle.Should().Be("Leaking pipe");
        details.CompanySlug.Should().Be(TestTenants.CompanyASlug);
        details.VendorId.Should().Be(TestTenants.VendorAId);
        details.WorkStatusCode.Should().Be("SCHEDULED");
        details.Notes.Should().Be("Query note");
        wrongCompany.Should().BeNull();
    }

    [Fact]
    public async Task ExistenceQueries_AreScopedToTicketAndCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow, "Existence note");

        var found = await uow.ScheduledWorks.FindInCompanyAsync(scheduledWorkId, TestTenants.CompanyAId);
        var existsForTicket = await uow.ScheduledWorks.ExistsForTicketAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var hasWorkLogs = await uow.ScheduledWorks.HasWorkLogsAsync(scheduledWorkId, TestTenants.CompanyAId);
        var vendorBelongs = await uow.ScheduledWorks.VendorBelongsToTicketCompanyAsync(
            TestTenants.VendorAId,
            TestTenants.TicketAId,
            TestTenants.CompanyAId);
        var vendorSupportsCategory = await uow.ScheduledWorks.VendorSupportsTicketCategoryAsync(
            TestTenants.VendorAId,
            TestTenants.TicketAId);

        found.Should().NotBeNull();
        existsForTicket.Should().BeTrue();
        hasWorkLogs.Should().BeFalse();
        vendorBelongs.Should().BeTrue();
        vendorSupportsCategory.Should().BeFalse();
    }

    [Fact]
    public async Task VendorSupportsTicketCategoryAsync_ReturnsTrueWhenLinkExists()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        uow.VendorTicketCategories.Add(new VendorTicketCategoryDalDto
        {
            Id = Guid.NewGuid(),
            VendorId = TestTenants.VendorAId,
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            Notes = "Supports plumbing"
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var supportsCategory = await uow.ScheduledWorks.VendorSupportsTicketCategoryAsync(
            TestTenants.VendorAId,
            TestTenants.TicketAId);

        supportsCategory.Should().BeTrue();
    }

    [Fact]
    public async Task StartedAndCompletedQueries_ReflectRealWorkTimes()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var started = DateTime.UtcNow.AddHours(-2);
        var completed = DateTime.UtcNow.AddHours(-1);

        await CreateScheduledWorkAsync(uow, "Started note", realStart: started, realEnd: completed);

        var anyStarted = await uow.ScheduledWorks.AnyStartedForTicketAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var anyCompleted = await uow.ScheduledWorks.AnyCompletedForTicketAsync(TestTenants.TicketAId, TestTenants.CompanyAId);

        anyStarted.Should().BeTrue();
        anyCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureNotesAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow, "English note");

        var entity = await db.ScheduledWorks.AsTracking().SingleAsync(work => work.Id == scheduledWorkId);
        entity.Notes!.SetTranslation("Estonian note", "et");
        db.Entry(entity).Property(work => work.Notes).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scheduledStart = DateTime.UtcNow.AddDays(3);
        await uow.ScheduledWorks.UpdateAsync(new ScheduledWorkDalDto
        {
            Id = scheduledWorkId,
            VendorId = TestTenants.VendorAId,
            TicketId = TestTenants.TicketAId,
            WorkStatusId = TestTenants.WorkStatusScheduledId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(2),
            RealStart = scheduledStart.AddMinutes(10),
            RealEnd = scheduledStart.AddHours(1),
            Notes = "English updated"
        }, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.ScheduledWorks.AsNoTracking().SingleAsync(work => work.Id == scheduledWorkId);
        persisted.ScheduledStart.Should().Be(scheduledStart);
        persisted.Notes!.Translate("en").Should().Be("English updated");
        persisted.Notes.Translate("et").Should().Be("Estonian note");
    }

    [Fact]
    public async Task DeleteInCompanyAsync_DeletesOnlyWithinCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var scheduledWorkId = await CreateScheduledWorkAsync(uow, "Delete note");

        var wrongCompany = await uow.ScheduledWorks.DeleteInCompanyAsync(scheduledWorkId, Guid.NewGuid());
        var deleted = await uow.ScheduledWorks.DeleteInCompanyAsync(scheduledWorkId, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        wrongCompany.Should().BeFalse();
        deleted.Should().BeTrue();
        var exists = await db.ScheduledWorks.AsNoTracking().AnyAsync(work => work.Id == scheduledWorkId);
        exists.Should().BeFalse();
    }

    private static async Task<Guid> CreateScheduledWorkAsync(
        IAppUOW uow,
        string notes,
        DateTime? realStart = null,
        DateTime? realEnd = null)
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
            RealStart = realStart,
            RealEnd = realEnd,
            Notes = notes
        });
        await uow.SaveChangesAsync(CancellationToken.None);
        return id;
    }
}

using App.DAL.Contracts;
using App.DAL.DTO.Tickets;
using App.DAL.EF;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.DAL;

public class TicketRepository_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketRepository_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AllByCompanyAsync_ReturnsSeededTicketAndAppliesFilters()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var all = await uow.Tickets.AllByCompanyAsync(
            TestTenants.CompanyAId,
            new TicketListFilterDalDto { Search = " T-A-0001 " });
        var bySearch = await uow.Tickets.AllByCompanyAsync(
            TestTenants.CompanyAId,
            new TicketListFilterDalDto { Search = " vendor a " });
        var byStatus = await uow.Tickets.AllByCompanyAsync(
            TestTenants.CompanyAId,
            new TicketListFilterDalDto { StatusId = TestTenants.TicketStatusCreatedId });
        var wrongCompany = await uow.Tickets.AllByCompanyAsync(Guid.NewGuid(), new TicketListFilterDalDto());

        all.Should().ContainSingle(ticket => ticket.Id == TestTenants.TicketAId);
        all[0].TicketNr.Should().Be("T-A-0001");
        all[0].Title.Should().Be("Leaking pipe");
        all[0].StatusCode.Should().Be("CREATED");
        all[0].CustomerSlug.Should().Be("customer-a");
        all[0].PropertySlug.Should().Be("property-a");
        all[0].UnitSlug.Should().Be("a-101");
        all[0].VendorName.Should().Be("Vendor A");
        bySearch.Should().ContainSingle(ticket => ticket.Id == TestTenants.TicketAId);
        byStatus.Should().ContainSingle(ticket => ticket.Id == TestTenants.TicketAId);
        wrongCompany.Should().BeEmpty();
    }

    [Fact]
    public async Task DetailsAndEditQueries_ReturnTicketOnlyWithinCompany()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var details = await uow.Tickets.FindDetailsAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var edit = await uow.Tickets.FindForEditAsync(TestTenants.TicketAId, TestTenants.CompanyAId);
        var wrongCompany = await uow.Tickets.FindDetailsAsync(TestTenants.TicketAId, Guid.NewGuid());

        details.Should().NotBeNull();
        details!.Title.Should().Be("Leaking pipe");
        details.Description.Should().Be("Water leak in bathroom");
        details.CustomerId.Should().Be(TestTenants.CustomerAId);
        details.PropertyId.Should().Be(TestTenants.PropertyAId);
        details.UnitId.Should().Be(TestTenants.UnitAId);
        details.VendorId.Should().Be(TestTenants.VendorAId);
        edit.Should().NotBeNull();
        edit!.TicketNr.Should().Be("T-A-0001");
        edit.StatusCode.Should().Be("CREATED");
        wrongCompany.Should().BeNull();
    }

    [Fact]
    public async Task TicketNumberQueries_AreCompanyScoped()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();

        var exists = await uow.Tickets.TicketNrExistsAsync(TestTenants.CompanyAId, " t-a-0001 ");
        var exceptSelf = await uow.Tickets.TicketNrExistsAsync(
            TestTenants.CompanyAId,
            "T-A-0001",
            TestTenants.TicketAId);
        var next = await uow.Tickets.GetNextTicketNrAsync(TestTenants.CompanyAId, new DateTime(2026, 5, 12, 0, 0, 0, DateTimeKind.Utc));

        exists.Should().BeTrue();
        exceptSelf.Should().BeFalse();
        next.Should().Be("T-2026-0001");
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatusWithinCompany()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var ticketId = Guid.NewGuid();
        var closedAt = new DateTime(2026, 5, 12, 10, 0, 0, DateTimeKind.Utc);

        uow.Tickets.Add(new TicketDalDto
        {
            Id = ticketId,
            ManagementCompanyId = TestTenants.CompanyAId,
            TicketNr = UniqueTicketNr(),
            Title = "Status update ticket",
            Description = "Status update description",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = TestTenants.VendorAId,
            DueAt = DateTime.UtcNow.AddDays(3)
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var updated = await uow.Tickets.UpdateStatusAsync(new TicketStatusUpdateDalDto
        {
            Id = ticketId,
            ManagementCompanyId = TestTenants.CompanyAId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            ClosedAt = closedAt
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var wrongCompany = await uow.Tickets.UpdateStatusAsync(new TicketStatusUpdateDalDto
        {
            Id = ticketId,
            ManagementCompanyId = Guid.NewGuid(),
            TicketStatusId = TestTenants.TicketStatusCreatedId
        });

        updated.Should().BeTrue();
        wrongCompany.Should().BeFalse();
        var persisted = await db.Tickets.AsNoTracking().SingleAsync(ticket => ticket.Id == ticketId);
        persisted.ClosedAt.Should().Be(closedAt);
    }

    [Fact]
    public async Task HasDeleteDependenciesAsync_ReturnsFalseWhenNoScheduledWorkOrLogsExist()
    {
        using var scope = _factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var ticketId = Guid.NewGuid();

        uow.Tickets.Add(new TicketDalDto
        {
            Id = ticketId,
            ManagementCompanyId = TestTenants.CompanyAId,
            TicketNr = UniqueTicketNr(),
            Title = "Dependency check ticket",
            Description = "No scheduled work",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = TestTenants.VendorAId,
            DueAt = DateTime.UtcNow.AddDays(3)
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var hasDependencies = await uow.Tickets.HasDeleteDependenciesAsync(ticketId, TestTenants.CompanyAId);

        hasDependencies.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesCurrentCultureFieldsAndPreservesOtherTranslations()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IAppUOW>();
        var ticketId = Guid.NewGuid();
        var ticketNr = UniqueTicketNr();

        uow.Tickets.Add(new TicketDalDto
        {
            Id = ticketId,
            ManagementCompanyId = TestTenants.CompanyAId,
            TicketNr = ticketNr,
            Title = "English title",
            Description = "English description",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = TestTenants.VendorAId,
            DueAt = DateTime.UtcNow.AddDays(3)
        });
        await uow.SaveChangesAsync(CancellationToken.None);

        var entity = await db.Tickets.AsTracking().SingleAsync(ticket => ticket.Id == ticketId);
        entity.Title.SetTranslation("Estonian title", "et");
        entity.Description.SetTranslation("Estonian description", "et");
        db.Entry(entity).Property(ticket => ticket.Title).IsModified = true;
        db.Entry(entity).Property(ticket => ticket.Description).IsModified = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await uow.Tickets.UpdateAsync(new TicketDalDto
        {
            Id = ticketId,
            ManagementCompanyId = TestTenants.CompanyAId,
            TicketNr = ticketNr,
            Title = "English title updated",
            Description = "English description updated",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = TestTenants.VendorAId,
            DueAt = DateTime.UtcNow.AddDays(4)
        }, TestTenants.CompanyAId);
        await uow.SaveChangesAsync(CancellationToken.None);

        var persisted = await db.Tickets.AsNoTracking().SingleAsync(ticket => ticket.Id == ticketId);
        persisted.Title.Translate("en").Should().Be("English title updated");
        persisted.Title.Translate("et").Should().Be("Estonian title");
        persisted.Description.Translate("en").Should().Be("English description updated");
        persisted.Description.Translate("et").Should().Be("Estonian description");
    }

    private static string UniqueTicketNr()
    {
        return $"T-TEST-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
    }
}

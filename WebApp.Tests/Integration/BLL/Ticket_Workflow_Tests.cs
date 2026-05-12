using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Leases;
using App.BLL.DTO.Residents;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.WorkLogs;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class Ticket_Workflow_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private const string CustomerASlug = "customer-a";
    private const string PropertyASlug = "property-a";
    private const string UnitASlug = "a-101";

    private readonly CustomWebApplicationFactory _factory;

    public Ticket_Workflow_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTicket_AppearsInManagementAndContextSearchesWithDetails()
    {
        using var culture = new CultureScope("en");
        var resident = await CreateResidentWithLeaseAsync("ticket-context");
        var vendor = await CreateVendorWithCategoryAsync("ticket-context");
        var ticketNr = UniqueTicketNr();

        Guid ticketId;
        using (var createScope = _factory.Services.CreateScope())
        {
            var bll = createScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var created = await bll.Tickets.CreateAsync(CompanyRoute(), NewTicket(ticketNr, vendor.VendorId, resident.ResidentId));

            created.IsSuccess.Should().BeTrue();
            created.Value.ManagementCompanyId.Should().Be(TestTenants.CompanyAId);
            created.Value.TicketStatusId.Should().Be(TestTenants.TicketStatusCreatedId);
            ticketId = created.Value.Id;
        }

        using var searchScope = _factory.Services.CreateScope();
        var searchBll = searchScope.ServiceProvider.GetRequiredService<IAppBLL>();

        var management = await searchBll.Tickets.SearchAsync(new ManagementTicketSearchRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            Search = ticketNr
        });
        var customer = await searchBll.Tickets.SearchCustomerTicketsAsync(new ContextTicketSearchRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            Search = ticketNr
        });
        var property = await searchBll.Tickets.SearchPropertyTicketsAsync(new ContextTicketSearchRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug,
            Search = ticketNr
        });
        var unit = await searchBll.Tickets.SearchUnitTicketsAsync(new ContextTicketSearchRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug,
            UnitSlug = UnitASlug,
            Search = ticketNr
        });
        var residentTickets = await searchBll.Tickets.SearchResidentTicketsAsync(new ContextTicketSearchRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            ResidentIdCode = resident.IdCode,
            Search = ticketNr
        });
        var details = await searchBll.Tickets.GetDetailsAsync(TicketRoute(ticketId));

        management.Value.Tickets.Should().ContainSingle(ticket => ticket.TicketId == ticketId);
        customer.Value.Tickets.Should().ContainSingle(ticket => ticket.TicketId == ticketId);
        property.Value.Tickets.Should().ContainSingle(ticket => ticket.TicketId == ticketId);
        unit.Value.Tickets.Should().ContainSingle(ticket => ticket.TicketId == ticketId);
        residentTickets.Value.Tickets.Should().ContainSingle(ticket => ticket.TicketId == ticketId);
        details.Value.TicketNr.Should().Be(ticketNr);
        details.Value.CustomerSlug.Should().Be(CustomerASlug);
        details.Value.PropertySlug.Should().Be(PropertyASlug);
        details.Value.UnitSlug.Should().Be(UnitASlug);
        details.Value.ResidentIdCode.Should().Be(resident.IdCode);
        details.Value.VendorName.Should().Be(vendor.Name);
    }

    [Fact]
    public async Task UpdateTicket_ValidatesHierarchyAndPersistsChanges()
    {
        using var culture = new CultureScope("en");
        var vendor = await CreateVendorWithCategoryAsync("ticket-update");
        var ticket = await CreateTicketAsync("ticket-update", vendor.VendorId);

        using (var invalidScope = _factory.Services.CreateScope())
        {
            var bll = invalidScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var invalidDto = UpdatedTicket(ticket.TicketNr, vendor.VendorId);
            invalidDto.PropertyId = Guid.NewGuid();

            var invalid = await bll.Tickets.UpdateAsync(
                TicketRoute(ticket.TicketId),
                invalidDto);

            invalid.ShouldFailWith<ValidationAppError>();
        }

        using var updateScope = _factory.Services.CreateScope();
        var updateBll = updateScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var updated = await updateBll.Tickets.UpdateAsync(
            TicketRoute(ticket.TicketId),
            UpdatedTicket(ticket.TicketNr, vendor.VendorId));
        var edit = await updateBll.Tickets.GetEditFormAsync(TicketRoute(ticket.TicketId));

        updated.IsSuccess.Should().BeTrue();
        edit.Value.Title.Should().Be("Updated ticket title");
        edit.Value.Description.Should().Be("Updated ticket description");
        edit.Value.DueAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StatusWorkflow_EnforcesTransitionGuardsAndFinalClosedState()
    {
        using var culture = new CultureScope("en");
        var vendor = await CreateVendorWithCategoryAsync("ticket-status");
        var ticket = await CreateTicketAsync("ticket-status", vendor.VendorId);

        Guid scheduledWorkId;
        using (var initialScope = _factory.Services.CreateScope())
        {
            var bll = initialScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var availability = await bll.Tickets.GetTransitionAvailabilityAsync(TicketRoute(ticket.TicketId));
            var assigned = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));
            var blockedSchedule = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));

            availability.Value.CanAdvance.Should().BeTrue();
            availability.Value.NextStatusCode.Should().Be("ASSIGNED");
            assigned.Value.Id.Should().Be(ticket.TicketId);
            blockedSchedule.ShouldFailWith<BusinessRuleError>();
        }

        using (var scheduleScope = _factory.Services.CreateScope())
        {
            var bll = scheduleScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var scheduled = await bll.ScheduledWorks.ScheduleAsync(
                TicketRoute(ticket.TicketId),
                new ScheduledWorkBllDto
                {
                    TicketId = ticket.TicketId,
                    VendorId = vendor.VendorId,
                    WorkStatusId = TestTenants.WorkStatusScheduledId,
                    ScheduledStart = DateTime.UtcNow.AddDays(2),
                    ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(2),
                    Notes = "Ticket workflow schedule"
                });
            var scheduledDetails = await bll.Tickets.GetDetailsAsync(TicketRoute(ticket.TicketId));
            var blockedInProgress = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));

            scheduled.IsSuccess.Should().BeTrue();
            scheduledWorkId = scheduled.Value.Id;
            scheduledDetails.Value.StatusCode.Should().Be("SCHEDULED");
            blockedInProgress.ShouldFailWith<BusinessRuleError>();
        }

        using (var workScope = _factory.Services.CreateScope())
        {
            var bll = workScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var startedAt = DateTime.UtcNow.AddHours(-3);
            var completedAt = DateTime.UtcNow.AddHours(-1);

            var started = await bll.ScheduledWorks.StartWorkAsync(ScheduledWorkRoute(ticket.TicketId, scheduledWorkId), startedAt);
            var blockedCompletedWithoutLog = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));
            var completed = await bll.ScheduledWorks.CompleteWorkAsync(ScheduledWorkRoute(ticket.TicketId, scheduledWorkId), completedAt);
            var workLog = await bll.WorkLogs.AddAsync(
                ScheduledWorkRoute(ticket.TicketId, scheduledWorkId),
                new WorkLogBllDto
                {
                    ScheduledWorkId = scheduledWorkId,
                    AppUserId = TestUsers.CompanyAOwnerId,
                    WorkStart = startedAt,
                    WorkEnd = completedAt,
                    Hours = 2,
                    Description = "Ticket workflow work log"
                });
            var movedToCompleted = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));
            var movedToClosed = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));
            var alreadyClosed = await bll.Tickets.AdvanceStatusAsync(TicketRoute(ticket.TicketId));
            var details = await bll.Tickets.GetDetailsAsync(TicketRoute(ticket.TicketId));

            started.IsSuccess.Should().BeTrue();
            blockedCompletedWithoutLog.ShouldFailWith<BusinessRuleError>();
            completed.IsSuccess.Should().BeTrue();
            workLog.IsSuccess.Should().BeTrue();
            movedToCompleted.IsSuccess.Should().BeTrue();
            movedToClosed.IsSuccess.Should().BeTrue();
            alreadyClosed.ShouldFailWith<BusinessRuleError>();
            details.Value.StatusCode.Should().Be("CLOSED");
            details.Value.ClosedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DeleteTicket_RemovesUnreferencedTicketAndBlocksScheduledWorkDependency()
    {
        var vendor = await CreateVendorWithCategoryAsync("ticket-delete");
        var removable = await CreateTicketAsync("ticket-delete-free", vendor.VendorId);
        var blocked = await CreateTicketAsync("ticket-delete-blocked", vendor.VendorId);

        using (var dependencyScope = _factory.Services.CreateScope())
        {
            var bll = dependencyScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var assigned = await bll.Tickets.AdvanceStatusAsync(TicketRoute(blocked.TicketId));
            var scheduled = await bll.ScheduledWorks.ScheduleAsync(
                TicketRoute(blocked.TicketId),
                new ScheduledWorkBllDto
                {
                    TicketId = blocked.TicketId,
                    VendorId = vendor.VendorId,
                    WorkStatusId = TestTenants.WorkStatusScheduledId,
                    ScheduledStart = DateTime.UtcNow.AddDays(3),
                    ScheduledEnd = DateTime.UtcNow.AddDays(3).AddHours(2),
                    Notes = "Blocks ticket deletion"
                });

            assigned.IsSuccess.Should().BeTrue();
            scheduled.IsSuccess.Should().BeTrue();
        }

        using var deleteScope = _factory.Services.CreateScope();
        var deleteBll = deleteScope.ServiceProvider.GetRequiredService<IAppBLL>();
        var deleted = await deleteBll.Tickets.DeleteAsync(TicketRoute(removable.TicketId));
        var afterDelete = await deleteBll.Tickets.GetDetailsAsync(TicketRoute(removable.TicketId));
        var blockedDelete = await deleteBll.Tickets.DeleteAsync(TicketRoute(blocked.TicketId));

        deleted.IsSuccess.Should().BeTrue();
        afterDelete.ShouldFailWith<NotFoundError>();
        blockedDelete.ShouldFailWith<BusinessRuleError>();
    }

    private async Task<TicketSeed> CreateTicketAsync(string suffix, Guid vendorId)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var ticketNr = UniqueTicketNr();
        var created = await bll.Tickets.CreateAsync(CompanyRoute(), NewTicket(ticketNr, vendorId, residentId: null, suffix));

        created.IsSuccess.Should().BeTrue();
        return new TicketSeed(created.Value.Id, created.Value.TicketNr);
    }

    private async Task<ResidentSeed> CreateResidentWithLeaseAsync(string suffix)
    {
        var idCode = $"BLL-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var firstName = $"Resident{suffix}"[..Math.Min($"Resident{suffix}".Length, 30)];
        Guid residentId;

        using (var residentScope = _factory.Services.CreateScope())
        {
            var bll = residentScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var created = await bll.Residents.CreateAndGetProfileAsync(
                CompanyRoute(),
                new ResidentBllDto
                {
                    FirstName = firstName,
                    LastName = "Workflow",
                    IdCode = idCode,
                    PreferredLanguage = "en"
                });

            created.IsSuccess.Should().BeTrue();
            residentId = created.Value.ResidentId;
        }

        var leaseRoleId = await LeaseRoleIdAsync("TENANT");
        using (var leaseScope = _factory.Services.CreateScope())
        {
            var bll = leaseScope.ServiceProvider.GetRequiredService<IAppBLL>();
            var lease = await bll.Leases.CreateForResidentAsync(
                ResidentRoute(idCode),
                new LeaseBllDto
                {
                    UnitId = TestTenants.UnitAId,
                    LeaseRoleId = leaseRoleId,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Notes = "Ticket workflow lease"
                });

            lease.IsSuccess.Should().BeTrue();
            return new ResidentSeed(residentId, idCode);
        }
    }

    private async Task<VendorSeed> CreateVendorWithCategoryAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var registryCode = UniqueCode("VEND");
        var created = await bll.Vendors.CreateAndGetProfileAsync(
            CompanyRoute(),
            new VendorBllDto
            {
                Name = $"Ticket Workflow Vendor {suffix}",
                RegistryCode = registryCode,
                Notes = "Ticket workflow vendor"
            });

        created.IsSuccess.Should().BeTrue();

        var assigned = await bll.Vendors.AssignCategoryAsync(
            VendorRoute(created.Value.Id),
            new VendorTicketCategoryBllDto
            {
                TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                Notes = "Ticket workflow category"
            });

        assigned.IsSuccess.Should().BeTrue();
        return new VendorSeed(created.Value.Id, created.Value.Name);
    }

    private async Task<Guid> LeaseRoleIdAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<App.DAL.EF.AppDbContext>();

        return await db.LeaseRoles
            .AsNoTracking()
            .Where(role => role.Code == code)
            .Select(role => role.Id)
            .SingleAsync();
    }

    private static TicketBllDto NewTicket(
        string ticketNr,
        Guid vendorId,
        Guid? residentId,
        string suffix = "ticket-create")
    {
        return new TicketBllDto
        {
            TicketNr = ticketNr,
            Title = $"Workflow {suffix}",
            Description = $"Workflow description {suffix}",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            ResidentId = residentId,
            VendorId = vendorId,
            DueAt = DateTime.UtcNow.AddDays(5)
        };
    }

    private static TicketBllDto UpdatedTicket(string ticketNr, Guid vendorId)
    {
        return new TicketBllDto
        {
            TicketNr = ticketNr,
            Title = "Updated ticket title",
            Description = "Updated ticket description",
            TicketCategoryId = TestTenants.TicketCategoryReferencedId,
            TicketStatusId = TestTenants.TicketStatusCreatedId,
            TicketPriorityId = TestTenants.TicketPriorityReferencedId,
            CustomerId = TestTenants.CustomerAId,
            PropertyId = TestTenants.PropertyAId,
            UnitId = TestTenants.UnitAId,
            VendorId = vendorId,
            DueAt = DateTime.UtcNow.AddDays(7)
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

    private static ResidentRoute ResidentRoute(string residentIdCode)
    {
        return new ResidentRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            ResidentIdCode = residentIdCode
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

    private sealed record TicketSeed(Guid TicketId, string TicketNr);
    private sealed record ResidentSeed(Guid ResidentId, string IdCode);
    private sealed record VendorSeed(Guid VendorId, string Name);
}

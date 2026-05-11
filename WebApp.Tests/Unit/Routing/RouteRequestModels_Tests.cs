using App.BLL.DTO.Common.Routes;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.Routing;

public class RouteRequestModels_Tests
{
    private static readonly Guid AppUserId = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ContactId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid TicketId = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid ScheduledWorkId = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid WorkLogId = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid VendorId = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid CategoryId = new("70000000-0000-0000-0000-000000000001");
    private static readonly Guid LeaseId = new("80000000-0000-0000-0000-000000000001");

    [Fact]
    public void ManagementCompanyRoute_PreservesActorAndCompanySlug()
    {
        var route = new ManagementCompanyRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a"
        };

        route.AppUserId.Should().Be(AppUserId);
        route.CompanySlug.Should().Be("company-a");
    }

    [Fact]
    public void NestedCustomerPropertyUnitRoute_PreservesParentSlugs()
    {
        var route = new UnitRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            CustomerSlug = "customer-a",
            PropertySlug = "property-a",
            UnitSlug = "unit-a"
        };

        route.CompanySlug.Should().Be("company-a");
        route.CustomerSlug.Should().Be("customer-a");
        route.PropertySlug.Should().Be("property-a");
        route.UnitSlug.Should().Be("unit-a");
    }

    [Fact]
    public void ResidentRoute_PreservesResidentIdCode()
    {
        var route = new ResidentRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            ResidentIdCode = "39001010000"
        };

        route.ResidentIdCode.Should().Be("39001010000");
    }

    [Fact]
    public void EntityIdRoutes_PreserveIds()
    {
        var contact = new ContactRoute { AppUserId = AppUserId, CompanySlug = "company-a", ContactId = ContactId };
        var ticket = new TicketRoute { AppUserId = AppUserId, CompanySlug = "company-a", TicketId = TicketId };
        var scheduledWork = new ScheduledWorkRoute { AppUserId = AppUserId, CompanySlug = "company-a", TicketId = TicketId, ScheduledWorkId = ScheduledWorkId };
        var workLog = new WorkLogRoute { AppUserId = AppUserId, CompanySlug = "company-a", TicketId = TicketId, ScheduledWorkId = ScheduledWorkId, WorkLogId = WorkLogId };

        contact.ContactId.Should().Be(ContactId);
        ticket.TicketId.Should().Be(TicketId);
        scheduledWork.ScheduledWorkId.Should().Be(ScheduledWorkId);
        workLog.WorkLogId.Should().Be(WorkLogId);
    }

    [Fact]
    public void VendorRoutes_PreserveVendorCategoryAndContactIds()
    {
        var vendor = new VendorRoute { AppUserId = AppUserId, CompanySlug = "company-a", VendorId = VendorId };
        var category = new VendorCategoryRoute { AppUserId = AppUserId, CompanySlug = "company-a", VendorId = VendorId, TicketCategoryId = CategoryId };
        var contact = new VendorContactRoute { AppUserId = AppUserId, CompanySlug = "company-a", VendorId = VendorId, VendorContactId = ContactId };

        vendor.VendorId.Should().Be(VendorId);
        category.TicketCategoryId.Should().Be(CategoryId);
        contact.VendorContactId.Should().Be(ContactId);
    }

    [Fact]
    public void LeaseRoutes_PreserveContextAndLeaseId()
    {
        var residentLease = new ResidentLeaseRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            ResidentIdCode = "39001010000",
            LeaseId = LeaseId
        };
        var unitLease = new UnitLeaseRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            CustomerSlug = "customer-a",
            PropertySlug = "property-a",
            UnitSlug = "unit-a",
            LeaseId = LeaseId
        };

        residentLease.LeaseId.Should().Be(LeaseId);
        residentLease.ResidentIdCode.Should().Be("39001010000");
        unitLease.LeaseId.Should().Be(LeaseId);
        unitLease.UnitSlug.Should().Be("unit-a");
    }

    [Fact]
    public void ManagementTicketSearchRoute_PreservesFilters()
    {
        var dueFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueTo = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var route = new ManagementTicketSearchRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            Search = "leak",
            StatusId = Guid.NewGuid(),
            PriorityId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            ResidentId = Guid.NewGuid(),
            VendorId = Guid.NewGuid(),
            DueFrom = dueFrom,
            DueTo = dueTo
        };

        route.Search.Should().Be("leak");
        route.StatusId.Should().NotBeNull();
        route.PriorityId.Should().NotBeNull();
        route.CategoryId.Should().NotBeNull();
        route.CustomerId.Should().NotBeNull();
        route.PropertyId.Should().NotBeNull();
        route.UnitId.Should().NotBeNull();
        route.ResidentId.Should().NotBeNull();
        route.VendorId.Should().NotBeNull();
        route.DueFrom.Should().Be(dueFrom);
        route.DueTo.Should().Be(dueTo);
    }

    [Fact]
    public void ContextTicketSearchRoute_PreservesNestedContextFilters()
    {
        var route = new ContextTicketSearchRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            CustomerSlug = "customer-a",
            PropertySlug = "property-a",
            UnitSlug = "unit-a",
            ResidentIdCode = "39001010000"
        };

        route.CustomerSlug.Should().Be("customer-a");
        route.PropertySlug.Should().Be("property-a");
        route.UnitSlug.Should().Be("unit-a");
        route.ResidentIdCode.Should().Be("39001010000");
    }

    [Fact]
    public void TicketSelectorOptionsRoute_PreservesSelectorFilters()
    {
        var route = new TicketSelectorOptionsRoute
        {
            AppUserId = AppUserId,
            CompanySlug = "company-a",
            CustomerId = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            CategoryId = CategoryId
        };

        route.CustomerId.Should().NotBeNull();
        route.PropertyId.Should().NotBeNull();
        route.UnitId.Should().NotBeNull();
        route.CategoryId.Should().Be(CategoryId);
    }
}

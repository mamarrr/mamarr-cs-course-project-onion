using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using AwesomeAssertions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Integration.BLL;

public class MultiTenantAndRouteGuard_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private const string CustomerASlug = "customer-a";
    private const string PropertyASlug = "property-a";
    private const string UnitASlug = "a-101";

    private readonly CustomWebApplicationFactory _factory;

    public MultiTenantAndRouteGuard_Tests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SystemAdminCannotUseTenantServicesWithoutCompanyMembership()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var route = new ManagementCompanyRoute
        {
            AppUserId = TestUsers.SystemAdminId,
            CompanySlug = TestTenants.CompanyASlug
        };

        var customers = await bll.Customers.ListForCompanyAsync(route);
        var residents = await bll.Residents.ListForCompanyAsync(route);
        var vendors = await bll.Vendors.ListForCompanyAsync(route);
        var tickets = await bll.Tickets.SearchAsync(new ManagementTicketSearchRoute
        {
            AppUserId = route.AppUserId,
            CompanySlug = route.CompanySlug
        });

        customers.ShouldFailWith<ForbiddenError>();
        residents.ShouldFailWith<ForbiddenError>();
        vendors.ShouldFailWith<ForbiddenError>();
        tickets.ShouldFailWith<ForbiddenError>();
    }

    [Fact]
    public async Task EmptyUserCannotResolveProtectedCompanyRoute()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var result = await bll.Customers.ListForCompanyAsync(new ManagementCompanyRoute
        {
            AppUserId = Guid.Empty,
            CompanySlug = TestTenants.CompanyASlug
        });

        result.ShouldFailWith<UnauthorizedError>();
    }

    [Fact]
    public async Task RouteHierarchyRejectsWrongParentSlugs()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var missingCustomer = await bll.Customers.GetProfileAsync(new CustomerRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = "missing-customer"
        });
        var missingProperty = await bll.Properties.GetProfileAsync(new PropertyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = "missing-property"
        });
        var missingUnit = await bll.Units.GetProfileAsync(new UnitRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug,
            UnitSlug = "missing-unit"
        });

        missingCustomer.ShouldFailWith<NotFoundError>();
        missingProperty.ShouldFailWith<NotFoundError>();
        missingUnit.ShouldFailWith<NotFoundError>();
    }

    [Fact]
    public async Task ObjectRoutesRejectMissingTenantScopedIds()
    {
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();
        var missingId = Guid.NewGuid();

        var vendor = await bll.Vendors.GetProfileAsync(new VendorRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            VendorId = missingId
        });
        var ticket = await bll.Tickets.GetDetailsAsync(new TicketRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = missingId
        });
        var scheduledWork = await bll.ScheduledWorks.GetDetailsAsync(new ScheduledWorkRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            TicketId = TestTenants.TicketAId,
            ScheduledWorkId = missingId
        });

        vendor.ShouldFailWith<NotFoundError>();
        ticket.ShouldFailWith<NotFoundError>();
        scheduledWork.ShouldFailWith<NotFoundError>();
    }

    [Fact]
    public async Task ValidSeededHierarchyResolvesThroughBllServices()
    {
        using var culture = new CultureScope("en");
        using var scope = _factory.Services.CreateScope();
        var bll = scope.ServiceProvider.GetRequiredService<IAppBLL>();

        var customer = await bll.Customers.GetProfileAsync(new CustomerRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug
        });
        var property = await bll.Properties.GetProfileAsync(new PropertyRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug
        });
        var unit = await bll.Units.GetProfileAsync(new UnitRoute
        {
            AppUserId = TestUsers.CompanyAOwnerId,
            CompanySlug = TestTenants.CompanyASlug,
            CustomerSlug = CustomerASlug,
            PropertySlug = PropertyASlug,
            UnitSlug = UnitASlug
        });

        customer.IsSuccess.Should().BeTrue();
        customer.Value.Id.Should().Be(TestTenants.CustomerAId);
        property.IsSuccess.Should().BeTrue();
        property.Value.PropertyId.Should().Be(TestTenants.PropertyAId);
        unit.IsSuccess.Should().BeTrue();
        unit.Value.UnitId.Should().Be(TestTenants.UnitAId);
    }
}

internal static class FluentResultAssertions
{
    public static void ShouldFailWith<TError>(this IResultBase result)
        where TError : IError
    {
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(error => error is TError);
    }
}

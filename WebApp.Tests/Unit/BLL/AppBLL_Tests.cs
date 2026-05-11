using App.BLL;
using App.BLL.Contracts.Admin;
using App.BLL.Contracts.Auth;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Dashboards;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Vendors;
using App.BLL.Contracts.Workspace;
using App.DAL.Contracts;
using AwesomeAssertions;
using Moq;

namespace WebApp.Tests.Unit.BLL;

public class AppBLL_Tests
{
    [Fact]
    public void ServiceProperties_CreateExpectedServiceTypes()
    {
        var bll = new AppBLL(new Mock<IAppUOW>(MockBehavior.Loose).Object);

        bll.AdminDashboard.Should().BeAssignableTo<IAdminDashboardService>();
        bll.AdminUsers.Should().BeAssignableTo<IAdminUserService>();
        bll.AdminCompanies.Should().BeAssignableTo<IAdminCompanyService>();
        bll.AdminLookups.Should().BeAssignableTo<IAdminLookupService>();
        bll.AdminTicketMonitor.Should().BeAssignableTo<IAdminTicketMonitorService>();
        bll.PortalDashboards.Should().BeAssignableTo<IPortalDashboardService>();
        bll.Workspaces.Should().BeAssignableTo<IWorkspaceService>();
        bll.CompanyMemberships.Should().BeAssignableTo<ICompanyMembershipService>();
        bll.ManagementCompanies.Should().BeAssignableTo<IManagementCompanyService>();
        bll.Customers.Should().BeAssignableTo<ICustomerService>();
        bll.Properties.Should().BeAssignableTo<IPropertyService>();
        bll.Residents.Should().BeAssignableTo<IResidentService>();
        bll.Leases.Should().BeAssignableTo<ILeaseService>();
        bll.Tickets.Should().BeAssignableTo<ITicketService>();
        bll.ScheduledWorks.Should().BeAssignableTo<IScheduledWorkService>();
        bll.WorkLogs.Should().BeAssignableTo<IWorkLogService>();
        bll.Vendors.Should().BeAssignableTo<IVendorService>();
        bll.Units.Should().BeAssignableTo<IUnitService>();
        bll.AuthSessions.Should().BeAssignableTo<IAuthSessionService>();
    }

    [Fact]
    public void ServiceProperties_ReturnSameInstanceOnRepeatedAccess()
    {
        var bll = new AppBLL(new Mock<IAppUOW>(MockBehavior.Loose).Object);

        bll.AdminDashboard.Should().BeSameAs(bll.AdminDashboard);
        bll.AdminUsers.Should().BeSameAs(bll.AdminUsers);
        bll.AdminCompanies.Should().BeSameAs(bll.AdminCompanies);
        bll.AdminLookups.Should().BeSameAs(bll.AdminLookups);
        bll.AdminTicketMonitor.Should().BeSameAs(bll.AdminTicketMonitor);
        bll.PortalDashboards.Should().BeSameAs(bll.PortalDashboards);
        bll.Workspaces.Should().BeSameAs(bll.Workspaces);
        bll.CompanyMemberships.Should().BeSameAs(bll.CompanyMemberships);
        bll.ManagementCompanies.Should().BeSameAs(bll.ManagementCompanies);
        bll.Customers.Should().BeSameAs(bll.Customers);
        bll.Properties.Should().BeSameAs(bll.Properties);
        bll.Residents.Should().BeSameAs(bll.Residents);
        bll.Leases.Should().BeSameAs(bll.Leases);
        bll.Tickets.Should().BeSameAs(bll.Tickets);
        bll.ScheduledWorks.Should().BeSameAs(bll.ScheduledWorks);
        bll.WorkLogs.Should().BeSameAs(bll.WorkLogs);
        bll.Vendors.Should().BeSameAs(bll.Vendors);
        bll.Units.Should().BeSameAs(bll.Units);
        bll.AuthSessions.Should().BeSameAs(bll.AuthSessions);
    }

    [Fact]
    public async Task SaveChangesAsync_DelegatesToUnitOfWork()
    {
        var uow = new Mock<IAppUOW>(MockBehavior.Strict);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        var bll = new AppBLL(uow.Object);

        var result = await bll.SaveChangesAsync();

        result.Should().Be(3);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

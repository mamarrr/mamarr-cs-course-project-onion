using App.BLL.DTO.Contacts;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.ScheduledWorks.Models;
using App.BLL.DTO.Tickets.Models;
using App.BLL.DTO.Units.Models;
using App.BLL.DTO.Vendors.Models;
using App.BLL.DTO.WorkLogs;
using App.BLL.DTO.WorkLogs.Models;
using App.BLL.DTO.Workspace.Models;
using App.DTO.v1.Mappers.Portal.Customers;
using App.DTO.v1.Mappers.Portal.Lookups;
using App.DTO.v1.Mappers.Portal.Properties;
using App.DTO.v1.Mappers.Portal.Residents;
using App.DTO.v1.Mappers.Portal.ScheduledWork;
using App.DTO.v1.Mappers.Portal.Units;
using App.DTO.v1.Mappers.Portal.VendorContacts;
using App.DTO.v1.Mappers.Portal.Vendors;
using App.DTO.v1.Mappers.Portal.WorkLogs;
using App.DTO.v1.Mappers.Workspace;
using AwesomeAssertions;

namespace WebApp.Tests.Unit.API.Mappers;

public class PortalReadApiMapper_Tests
{
    [Fact]
    public void WorkspaceAndLookupMappers_MapOptionsPathsAndPermissions()
    {
        var companyId = Id(1);
        var customerId = Id(2);
        var residentId = Id(3);
        var mapper = new WorkspaceApiMapper();

        var catalog = mapper.Map(new UserWorkspaceCatalogModel
        {
            ManagementCompanies =
            [
                new WorkspaceOptionModel
                {
                    Id = companyId,
                    ContextType = "management",
                    Name = "Company A",
                    ManagementCompanySlug = "company-a",
                    IsDefault = true,
                    CanManageCompanyUsers = true
                }
            ],
            Customers =
            [
                new WorkspaceOptionModel
                {
                    Id = customerId,
                    ContextType = "customer",
                    Name = "Customer A",
                    Slug = "customer-a",
                    ManagementCompanySlug = "company-a"
                }
            ],
            Residents =
            [
                new WorkspaceOptionModel
                {
                    Id = residentId,
                    ContextType = "resident",
                    Name = "Resident A",
                    Slug = "50001010000",
                    ManagementCompanySlug = "company-a"
                }
            ],
            DefaultContext = new WorkspaceOptionModel
            {
                Id = companyId,
                ContextType = "management",
                Name = "Company A",
                ManagementCompanySlug = "company-a",
                IsDefault = true,
                CanManageCompanyUsers = true
            }
        });

        catalog.ManagementCompanies.Should().ContainSingle();
        catalog.ManagementCompanies[0].Id.Should().Be(companyId);
        catalog.ManagementCompanies[0].Path.Should().Be("/companies/company-a");
        catalog.ManagementCompanies[0].Permissions.CanManageCompanyUsers.Should().BeTrue();
        catalog.Customers[0].Path.Should().Be("/companies/company-a/customers/customer-a");
        catalog.Residents[0].Path.Should().Be("/companies/company-a/residents/50001010000");
        catalog.DefaultContext!.Id.Should().Be(companyId);
        catalog.DefaultContext.Path.Should().Be("/companies/company-a");

        mapper.Map(new WorkspaceEntryPointModel
        {
            Kind = WorkspaceEntryPointKind.CustomerDashboard,
            CompanySlug = "company-a",
            CustomerSlug = "customer-a"
        }).Should().BeEquivalentTo(new
        {
            Destination = "CustomerDashboard",
            CompanySlug = "company-a",
            CustomerSlug = "customer-a",
            Path = "/companies/company-a/customers/customer-a"
        });

        mapper.Map(new WorkspaceSelectionAuthorizationModel
        {
            ContextType = "resident",
            ManagementCompanySlug = "company-a",
            ResidentIdCode = "50001010000"
        }).Path.Should().Be("/companies/company-a/residents/50001010000");

        var lookupMapper = new LookupApiMapper();
        lookupMapper.MapPropertyTypes(
            [new PropertyTypeOptionModel { Id = Id(4), Code = "APT", Label = "Apartment" }])
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = Id(4), Code = "APT", Label = "Apartment" });
        lookupMapper.MapTicketOptions(
            [new TicketOptionModel { Id = Id(5), Code = "OPEN", Label = "Open" }])
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = Id(5), Code = "OPEN", Label = "Open" });
    }

    [Fact]
    public void HierarchyReadMappers_MapIdsScalarsAndApiPaths()
    {
        var customer = new CustomerListItemApiMapper().Map(new CustomerListItemModel
        {
            CustomerId = Id(10),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            CustomerSlug = "customer-a",
            Name = "Customer A",
            RegistryCode = "REG-A",
            BillingEmail = null,
            BillingAddress = "Billing Street 1",
            Phone = "+372"
        });

        customer.Should().BeEquivalentTo(new
        {
            CustomerId = Id(10),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CustomerSlug = "customer-a",
            Name = "Customer A",
            BillingEmail = (string?)null
        });

        new CustomerProfileApiMapper().Map(new CustomerProfileModel
        {
            Id = Id(10),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            Name = "Customer A",
            Slug = "customer-a",
            RegistryCode = "REG-A",
            BillingEmail = null,
            BillingAddress = "Billing Street 1",
            Phone = "+372"
        }).Id.Should().Be(Id(10));

        var property = new PropertyProfileApiMapper().Map(new PropertyProfileModel
        {
            PropertyId = Id(11),
            CustomerId = Id(10),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            CustomerSlug = "customer-a",
            CustomerName = "Customer A",
            PropertySlug = "property-a",
            Name = "Property A",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Notes = null,
            PropertyTypeId = Id(12),
            PropertyTypeCode = "APT",
            PropertyTypeLabel = "Apartment"
        });

        property.Should().BeEquivalentTo(new
        {
            PropertyId = Id(11),
            CustomerId = Id(10),
            ManagementCompanyId = Id(1),
            PropertySlug = "property-a",
            Notes = (string?)null,
            PropertyTypeId = Id(12)
        });

        new PropertyListItemApiMapper().Map(new PropertyListItemModel
        {
            PropertyId = Id(11),
            CustomerId = Id(10),
            ManagementCompanyId = Id(1),
            PropertyName = "Property A",
            PropertySlug = "property-a",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            PropertyTypeId = Id(12),
            PropertyTypeCode = "APT",
            PropertyTypeLabel = "Apartment"
        }).PropertyTypeCode.Should().Be("APT");

        var unit = new UnitProfileApiMapper().Map(new UnitProfileModel
        {
            UnitId = Id(13),
            PropertyId = Id(11),
            CustomerId = Id(10),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            CustomerSlug = "customer-a",
            CustomerName = "Customer A",
            PropertySlug = "property-a",
            PropertyName = "Property A",
            UnitSlug = "a-101",
            UnitNr = "A-101",
            FloorNr = null,
            SizeM2 = 56.5m,
            Notes = null
        });

        unit.UnitId.Should().Be(Id(13));
        unit.FloorNr.Should().BeNull();
        unit.Path.Should().Be("/api/v1/portal/companies/company-a/customers/customer-a/properties/property-a/units/a-101/profile");

        new UnitListItemApiMapper()
            .Map(new UnitListItemModel
            {
                UnitId = Id(13),
                PropertyId = Id(11),
                UnitSlug = "a-101",
                UnitNr = "A-101",
                FloorNr = null,
                SizeM2 = 56.5m
            }, "company-a", "customer-a", "property-a")
            .Path.Should().Be("/api/v1/portal/companies/company-a/customers/customer-a/properties/property-a/units/a-101/profile");

        var resident = new ResidentProfileApiMapper().Map(new ResidentProfileModel
        {
            ResidentId = Id(14),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            ResidentIdCode = "50001010000",
            FirstName = "Mari",
            LastName = "Tamm",
            FullName = "Mari Tamm",
            PreferredLanguage = null
        });

        resident.ResidentId.Should().Be(Id(14));
        resident.PreferredLanguage.Should().BeNull();
        resident.Path.Should().Be("/api/v1/portal/companies/company-a/residents/50001010000/profile");

        new ResidentListItemApiMapper().Map(new ResidentListItemModel
        {
            ResidentId = Id(14),
            FirstName = "Mari",
            LastName = "Tamm",
            FullName = "Mari Tamm",
            IdCode = "50001010000",
            PreferredLanguage = null
        }, "company-a").Path.Should().Be("/api/v1/portal/companies/company-a/residents/50001010000/profile");
    }

    [Fact]
    public void VendorScheduledWorkAndWorkLogReadMappers_MapNestedItemsAndPaths()
    {
        var createdAt = Instant(0);
        var vendor = new VendorListItemApiMapper().Map(new VendorListItemModel
        {
            VendorId = Id(20),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            Name = "Vendor A",
            RegistryCode = "VEND-A",
            CreatedAt = createdAt,
            ActiveCategoryCount = 2,
            AssignedTicketCount = 3,
            ContactCount = 4
        });

        vendor.VendorId.Should().Be(Id(20));
        vendor.Path.Should().Be($"/api/v1/portal/companies/company-a/vendors/{Id(20):D}");

        var vendorProfile = new VendorProfileApiMapper().Map(new VendorProfileModel
        {
            Id = Id(20),
            ManagementCompanyId = Id(1),
            CompanySlug = "company-a",
            CompanyName = "Company A",
            Name = "Vendor A",
            RegistryCode = "VEND-A",
            Notes = "Vendor notes",
            CreatedAt = createdAt,
            ActiveCategoryCount = 2,
            AssignedTicketCount = 3,
            ContactCount = 4,
            ScheduledWorkCount = 5
        });
        vendorProfile.ScheduledWorkCount.Should().Be(5);
        vendorProfile.Path.Should().Be(vendor.Path);

        var vendorContacts = new VendorContactListApiMapper().Map(new VendorContactListModel
        {
            CompanySlug = "company-a",
            CompanyName = "Company A",
            VendorId = Id(20),
            VendorName = "Vendor A",
            Contacts =
            [
                new VendorContactAssignmentModel
                {
                    VendorContactId = Id(21),
                    VendorId = Id(20),
                    ContactId = Id(22),
                    ContactTypeId = Id(23),
                    ContactTypeCode = "EMAIL",
                    ContactTypeLabel = "Email",
                    ContactValue = "vendor@test.ee",
                    ContactNotes = null,
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidTo = null,
                    Confirmed = true,
                    IsPrimary = true,
                    FullName = null,
                    RoleTitle = "Coordinator",
                    CreatedAt = createdAt
                }
            ],
            ExistingContacts =
            [
                new ContactBllDto { Id = Id(22), ContactTypeId = Id(23), ContactValue = "vendor@test.ee" },
                new ContactBllDto { Id = Id(24), ContactTypeId = Id(23), ContactValue = "free@test.ee" }
            ],
            ContactTypes =
            [
                new TicketOptionModel { Id = Id(23), Code = "EMAIL", Label = "Email" }
            ]
        });

        vendorContacts.Contacts.Should().ContainSingle();
        vendorContacts.Contacts[0].Path.Should().Be($"/api/v1/portal/companies/company-a/vendors/{Id(20)}/contacts/{Id(21)}");
        vendorContacts.ExistingContactOptions.Should().ContainSingle();
        vendorContacts.ExistingContactOptions[0].ContactId.Should().Be(Id(24));
        vendorContacts.ContactTypeOptions[0].Code.Should().Be("EMAIL");

        var scheduledWork = new ScheduledWorkListItemApiMapper().Map(new ScheduledWorkListModel
        {
            CompanySlug = "company-a",
            CompanyName = "Company A",
            TicketId = Id(30),
            TicketNr = "T-1",
            TicketTitle = "Ticket",
            Items =
            [
                new ScheduledWorkListItemModel
                {
                    ScheduledWorkId = Id(31),
                    VendorId = Id(20),
                    VendorName = "Vendor A",
                    WorkStatusId = Id(32),
                    WorkStatusCode = "SCHEDULED",
                    WorkStatusLabel = "Scheduled",
                    ScheduledStart = Instant(1),
                    ScheduledEnd = null,
                    RealStart = null,
                    RealEnd = null,
                    Notes = null,
                    CreatedAt = createdAt,
                    WorkLogCount = 2
                }
            ]
        });

        scheduledWork.Path.Should().Be($"/api/v1/portal/companies/company-a/tickets/{Id(30):D}/scheduled-work");
        scheduledWork.Items[0].WorkLogsPath.Should().Be(
            $"/api/v1/portal/companies/company-a/tickets/{Id(30):D}/scheduled-work/{Id(31):D}/work-logs");

        var workLogs = new WorkLogListItemApiMapper().Map(new WorkLogListModel
        {
            CompanySlug = "company-a",
            CompanyName = "Company A",
            TicketId = Id(30),
            TicketNr = "T-1",
            TicketTitle = "Ticket",
            ScheduledWorkId = Id(31),
            VendorName = "Vendor A",
            WorkStatusLabel = "Scheduled",
            CanViewCosts = true,
            Totals = new WorkLogTotalsModel
            {
                Count = 1,
                Hours = 2.5m,
                MaterialCost = 10m,
                LaborCost = 50m,
                TotalCost = 60m
            },
            Items =
            [
                new WorkLogListItemModel
                {
                    WorkLogId = Id(33),
                    AppUserId = Id(34),
                    AppUserName = "Worker",
                    WorkStart = Instant(2),
                    WorkEnd = null,
                    Hours = 2.5m,
                    MaterialCost = 10m,
                    LaborCost = 50m,
                    Description = "Done",
                    CreatedAt = createdAt
                }
            ]
        });

        workLogs.Totals.TotalCost.Should().Be(60m);
        workLogs.Path.Should().Be($"/api/v1/portal/companies/company-a/tickets/{Id(30):D}/scheduled-work/{Id(31):D}/work-logs");
        workLogs.Items[0].Path.Should().Be($"{workLogs.Path}/{Id(33):D}");

        new WorkLogListItemApiMapper()
            .Map(new WorkLogBllDto
            {
                Id = Id(35),
                ScheduledWorkId = Id(31),
                AppUserId = Id(34),
                WorkStart = null,
                WorkEnd = null,
                Hours = null,
                MaterialCost = null,
                LaborCost = null,
                Description = null
            }, "company-a", Id(30), Id(31))
            .Path.Should().Be($"{workLogs.Path}/{Id(35):D}");
    }

    private static Guid Id(int value)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
    }

    private static DateTime Instant(int days)
    {
        return new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(days);
    }
}

using App.BLL.DTO.Contacts;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Leases;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Residents;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Units;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.WorkLogs;
using App.BLL.Mappers.Contacts;
using App.BLL.Mappers.Customers;
using App.BLL.Mappers.Leases;
using App.BLL.Mappers.ManagementCompanies;
using App.BLL.Mappers.Properties;
using App.BLL.Mappers.Residents;
using App.BLL.Mappers.ScheduledWorks;
using App.BLL.Mappers.Tickets;
using App.BLL.Mappers.Units;
using App.BLL.Mappers.Vendors;
using App.BLL.Mappers.WorkLogs;
using App.DAL.DTO.Contacts;
using App.DAL.DTO.Customers;
using App.DAL.DTO.Leases;
using App.DAL.DTO.ManagementCompanies;
using App.DAL.DTO.Properties;
using App.DAL.DTO.Residents;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.Units;
using App.DAL.DTO.Vendors;
using App.DAL.DTO.WorkLogs;
using AwesomeAssertions;
using Base.Contracts;

namespace WebApp.Tests.Unit.BLL.Mappers;

public class PortalBllDtoMapper_Tests
{
    [Fact]
    public void CompanyAndCustomerMappers_PreserveIdsForeignKeysAndScalars()
    {
        AssertBidirectional(
            new ManagementCompanyBllDtoMapper(),
            new ManagementCompanyBllDto
            {
                Id = Id(1),
                Name = "Company A",
                Slug = "company-a",
                RegistryCode = "REG-A",
                VatNumber = "EE100000001",
                Email = "company-a@test.ee",
                Phone = "+372 5555 0001",
                Address = "Street 1"
            },
            new ManagementCompanyDalDto
            {
                Id = Id(1),
                Name = "Company A",
                Slug = "company-a",
                RegistryCode = "REG-A",
                VatNumber = "EE100000001",
                Email = "company-a@test.ee",
                Phone = "+372 5555 0001",
                Address = "Street 1"
            });

        AssertBidirectional(
            new ManagementCompanyJoinRequestBllDtoMapper(),
            new ManagementCompanyJoinRequestBllDto
            {
                Id = Id(2),
                AppUserId = Id(3),
                ManagementCompanyId = Id(4),
                RequestedRoleId = Id(5),
                StatusId = Id(6),
                Message = "Please add me",
                ResolvedAt = Instant(1),
                ResolvedByAppUserId = Id(7)
            },
            new ManagementCompanyJoinRequestDalDto
            {
                Id = Id(2),
                AppUserId = Id(3),
                ManagementCompanyId = Id(4),
                RequestedRoleId = Id(5),
                StatusId = Id(6),
                Message = "Please add me",
                ResolvedAt = Instant(1),
                ResolvedByAppUserId = Id(7)
            });

        AssertBidirectional(
            new CustomerBllDtoMapper(),
            new CustomerBllDto
            {
                Id = Id(8),
                ManagementCompanyId = Id(4),
                Name = "Customer A",
                Slug = "customer-a",
                RegistryCode = "CUST-A",
                BillingEmail = "billing@test.ee",
                BillingAddress = "Billing Street 1",
                Phone = "+372 5555 0002",
                Notes = null
            },
            new CustomerDalDto
            {
                Id = Id(8),
                ManagementCompanyId = Id(4),
                Name = "Customer A",
                Slug = "customer-a",
                RegistryCode = "CUST-A",
                BillingEmail = "billing@test.ee",
                BillingAddress = "Billing Street 1",
                Phone = "+372 5555 0002",
                Notes = null
            });
    }

    [Fact]
    public void HierarchyMappers_PreserveIdsForeignKeysAndNullableFields()
    {
        AssertBidirectional(
            new PropertyBllDtoMapper(),
            new PropertyBllDto
            {
                Id = Id(10),
                CustomerId = Id(8),
                PropertyTypeId = Id(11),
                Label = "Property A",
                Slug = "property-a",
                AddressLine = "Property Street 1",
                City = "Tallinn",
                PostalCode = "10111",
                Notes = "Property notes"
            },
            new PropertyDalDto
            {
                Id = Id(10),
                CustomerId = Id(8),
                PropertyTypeId = Id(11),
                Label = "Property A",
                Slug = "property-a",
                AddressLine = "Property Street 1",
                City = "Tallinn",
                PostalCode = "10111",
                Notes = "Property notes"
            });

        AssertBidirectional(
            new UnitBllDtoMapper(),
            new UnitBllDto
            {
                Id = Id(12),
                PropertyId = Id(10),
                UnitNr = "A-101",
                Slug = "a-101",
                FloorNr = null,
                SizeM2 = 56.5m,
                Notes = null
            },
            new UnitDalDto
            {
                Id = Id(12),
                PropertyId = Id(10),
                UnitNr = "A-101",
                Slug = "a-101",
                FloorNr = null,
                SizeM2 = 56.5m,
                Notes = null
            });

        AssertBidirectional(
            new ResidentBllDtoMapper(),
            new ResidentBllDto
            {
                Id = Id(13),
                ManagementCompanyId = Id(4),
                FirstName = "Mari",
                LastName = "Tamm",
                IdCode = "EE-123",
                PreferredLanguage = null
            },
            new ResidentDalDto
            {
                Id = Id(13),
                ManagementCompanyId = Id(4),
                FirstName = "Mari",
                LastName = "Tamm",
                IdCode = "EE-123",
                PreferredLanguage = null
            });
    }

    [Fact]
    public void ContactAndAssignmentMappers_PreserveIdsForeignKeysAndFlags()
    {
        var validFrom = new DateOnly(2026, 1, 1);
        var validTo = new DateOnly(2026, 12, 31);

        AssertBidirectional(
            new ContactBllDtoMapper(),
            new ContactBllDto
            {
                Id = Id(20),
                ManagementCompanyId = Id(4),
                ContactTypeId = Id(21),
                ContactValue = "resident@test.ee",
                Notes = null
            },
            new ContactDalDto
            {
                Id = Id(20),
                ManagementCompanyId = Id(4),
                ContactTypeId = Id(21),
                ContactValue = "resident@test.ee",
                Notes = null
            });

        AssertBidirectional(
            new ResidentContactBllDtoMapper(),
            new ResidentContactBllDto
            {
                Id = Id(22),
                ResidentId = Id(13),
                ContactId = Id(20),
                ValidFrom = validFrom,
                ValidTo = validTo,
                Confirmed = true,
                IsPrimary = false
            },
            new ResidentContactDalDto
            {
                Id = Id(22),
                ResidentId = Id(13),
                ContactId = Id(20),
                ValidFrom = validFrom,
                ValidTo = validTo,
                Confirmed = true,
                IsPrimary = false
            });
    }

    [Fact]
    public void LeaseTicketScheduleAndWorkLogMappers_PreserveWorkflowScalars()
    {
        AssertBidirectional(
            new LeaseBllDtoMapper(),
            new LeaseBllDto
            {
                Id = Id(30),
                UnitId = Id(12),
                ResidentId = Id(13),
                LeaseRoleId = Id(31),
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = null,
                Notes = "Lease notes"
            },
            new LeaseDalDto
            {
                Id = Id(30),
                UnitId = Id(12),
                ResidentId = Id(13),
                LeaseRoleId = Id(31),
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = null,
                Notes = "Lease notes"
            });

        AssertBidirectional(
            new TicketBllDtoMapper(),
            new TicketBllDto
            {
                Id = Id(32),
                ManagementCompanyId = Id(4),
                TicketNr = "T-1",
                Title = "Ticket title",
                Description = "Ticket description",
                TicketCategoryId = Id(33),
                TicketStatusId = Id(34),
                TicketPriorityId = Id(35),
                CustomerId = Id(8),
                PropertyId = Id(10),
                UnitId = Id(12),
                ResidentId = Id(13),
                VendorId = null,
                DueAt = Instant(2),
                ClosedAt = null
            },
            new TicketDalDto
            {
                Id = Id(32),
                ManagementCompanyId = Id(4),
                TicketNr = "T-1",
                Title = "Ticket title",
                Description = "Ticket description",
                TicketCategoryId = Id(33),
                TicketStatusId = Id(34),
                TicketPriorityId = Id(35),
                CustomerId = Id(8),
                PropertyId = Id(10),
                UnitId = Id(12),
                ResidentId = Id(13),
                VendorId = null,
                DueAt = Instant(2),
                ClosedAt = null
            });

        AssertBidirectional(
            new ScheduledWorkBllDtoMapper(),
            new ScheduledWorkBllDto
            {
                Id = Id(36),
                VendorId = Id(37),
                TicketId = Id(32),
                WorkStatusId = Id(38),
                ScheduledStart = Instant(3),
                ScheduledEnd = Instant(4),
                RealStart = null,
                RealEnd = null,
                Notes = "Scheduled work notes"
            },
            new ScheduledWorkDalDto
            {
                Id = Id(36),
                VendorId = Id(37),
                TicketId = Id(32),
                WorkStatusId = Id(38),
                ScheduledStart = Instant(3),
                ScheduledEnd = Instant(4),
                RealStart = null,
                RealEnd = null,
                Notes = "Scheduled work notes"
            });

        AssertBidirectional(
            new WorkLogBllDtoMapper(),
            new WorkLogBllDto
            {
                Id = Id(39),
                ScheduledWorkId = Id(36),
                AppUserId = Id(40),
                WorkStart = Instant(5),
                WorkEnd = Instant(6),
                Hours = 1.5m,
                MaterialCost = null,
                LaborCost = 50m,
                Description = "Work log"
            },
            new WorkLogDalDto
            {
                Id = Id(39),
                ScheduledWorkId = Id(36),
                AppUserId = Id(40),
                WorkStart = Instant(5),
                WorkEnd = Instant(6),
                Hours = 1.5m,
                MaterialCost = null,
                LaborCost = 50m,
                Description = "Work log"
            });
    }

    [Fact]
    public void VendorMappers_PreserveAssignmentsAndOptionalMetadata()
    {
        var validFrom = new DateOnly(2026, 1, 1);

        AssertBidirectional(
            new VendorBllDtoMapper(),
            new VendorBllDto
            {
                Id = Id(50),
                ManagementCompanyId = Id(4),
                Name = "Vendor A",
                RegistryCode = "VEND-A",
                Notes = "Vendor notes"
            },
            new VendorDalDto
            {
                Id = Id(50),
                ManagementCompanyId = Id(4),
                Name = "Vendor A",
                RegistryCode = "VEND-A",
                Notes = "Vendor notes"
            });

        AssertBidirectional(
            new VendorContactBllDtoMapper(),
            new VendorContactBllDto
            {
                Id = Id(51),
                VendorId = Id(50),
                ContactId = Id(20),
                ValidFrom = validFrom,
                ValidTo = null,
                Confirmed = false,
                IsPrimary = true,
                FullName = null,
                RoleTitle = "Coordinator"
            },
            new VendorContactDalDto
            {
                Id = Id(51),
                VendorId = Id(50),
                ContactId = Id(20),
                ValidFrom = validFrom,
                ValidTo = null,
                Confirmed = false,
                IsPrimary = true,
                FullName = null,
                RoleTitle = "Coordinator"
            });

        AssertBidirectional(
            new VendorTicketCategoryBllDtoMapper(),
            new VendorTicketCategoryBllDto
            {
                Id = Id(52),
                VendorId = Id(50),
                TicketCategoryId = Id(33),
                Notes = null
            },
            new VendorTicketCategoryDalDto
            {
                Id = Id(52),
                VendorId = Id(50),
                TicketCategoryId = Id(33),
                Notes = null
            });
    }

    private static void AssertBidirectional<TBll, TDal>(
        IBaseMapper<TBll, TDal> mapper,
        TBll bll,
        TDal dal)
        where TBll : class
        where TDal : class
    {
        mapper.Map(dal).Should().BeEquivalentTo(bll);
        mapper.Map(bll).Should().BeEquivalentTo(dal);
        mapper.Map((TDal?)null).Should().BeNull();
        mapper.Map((TBll?)null).Should().BeNull();
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

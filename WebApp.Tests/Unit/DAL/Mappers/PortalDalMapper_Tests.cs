using System.Globalization;
using App.DAL.DTO.Contacts;
using App.DAL.DTO.Customers;
using App.DAL.DTO.Identity;
using App.DAL.DTO.Leases;
using App.DAL.DTO.ManagementCompanies;
using App.DAL.DTO.Properties;
using App.DAL.DTO.Residents;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.Units;
using App.DAL.DTO.Vendors;
using App.DAL.DTO.WorkLogs;
using App.DAL.EF.Mappers;
using App.DAL.EF.Mappers.Contacts;
using App.DAL.EF.Mappers.Customers;
using App.DAL.EF.Mappers.Leases;
using App.DAL.EF.Mappers.ManagementCompanies;
using App.DAL.EF.Mappers.Properties;
using App.DAL.EF.Mappers.Residents;
using App.DAL.EF.Mappers.ScheduledWorks;
using App.DAL.EF.Mappers.Tickets;
using App.DAL.EF.Mappers.Units;
using App.DAL.EF.Mappers.Vendors;
using App.DAL.EF.Mappers.WorkLogs;
using App.Domain;
using App.Domain.Identity;
using AwesomeAssertions;
using Base.Domain;
using DomainUnit = App.Domain.Unit;

namespace WebApp.Tests.Unit.DAL.Mappers;

[Collection("CultureSensitive")]
public class PortalDalMapper_Tests
{
    [Fact]
    public void ContactMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new ContactDalMapper();
        var id = Id(1);
        var companyId = Id(2);
        var contactTypeId = Id(3);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new Contact
            {
                Id = id,
                ManagementCompanyId = companyId,
                ContactTypeId = contactTypeId,
                ContactValue = "info@example.test",
                Notes = Localized("English notes", "Eesti markmed")
            })!;

            dto.Id.Should().Be(id);
            dto.ManagementCompanyId.Should().Be(companyId);
            dto.ContactTypeId.Should().Be(contactTypeId);
            dto.ContactValue.Should().Be("info@example.test");
            dto.Notes.Should().Be("Eesti markmed");

            var entity = mapper.Map(new ContactDalDto
            {
                Id = id,
                ManagementCompanyId = companyId,
                ContactTypeId = contactTypeId,
                ContactValue = "phone",
                Notes = "  dto notes  "
            })!;

            entity.Id.Should().Be(id);
            entity.ManagementCompanyId.Should().Be(companyId);
            entity.ContactTypeId.Should().Be(contactTypeId);
            entity.ContactValue.Should().Be("phone");
            entity.Notes!.Translate("et").Should().Be("dto notes");
            entity.Notes.Translate("en").Should().Be("dto notes");
        });

        mapper.Map((Contact?)null).Should().BeNull();
        mapper.Map((ContactDalDto?)null).Should().BeNull();
        mapper.Map(new ContactDalDto { Id = id, ManagementCompanyId = companyId, ContactTypeId = contactTypeId, ContactValue = "x" })!
            .Notes.Should().BeNull();
    }

    [Fact]
    public void CustomerMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new CustomerDalMapper();
        var id = Id(10);
        var companyId = Id(11);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new Customer
            {
                Id = id,
                ManagementCompanyId = companyId,
                Name = "Customer",
                Slug = "customer",
                RegistryCode = "REG-1",
                BillingEmail = null,
                BillingAddress = null,
                Phone = null,
                Notes = Localized("English customer", "Eesti klient")
            })!;

            dto.Id.Should().Be(id);
            dto.ManagementCompanyId.Should().Be(companyId);
            dto.Name.Should().Be("Customer");
            dto.Slug.Should().Be("customer");
            dto.RegistryCode.Should().Be("REG-1");
            dto.BillingEmail.Should().BeNull();
            dto.BillingAddress.Should().BeNull();
            dto.Phone.Should().BeNull();
            dto.Notes.Should().Be("Eesti klient");

            var entity = mapper.Map(new CustomerDalDto
            {
                Id = id,
                ManagementCompanyId = companyId,
                Name = "Customer DTO",
                Slug = "customer-dto",
                RegistryCode = "REG-2",
                BillingEmail = null,
                BillingAddress = null,
                Phone = null,
                Notes = "  mapped notes  "
            })!;

            entity.Id.Should().Be(id);
            entity.ManagementCompanyId.Should().Be(companyId);
            entity.BillingEmail.Should().BeNull();
            entity.BillingAddress.Should().BeNull();
            entity.Phone.Should().BeNull();
            entity.Notes!.Translate("et").Should().Be("mapped notes");
        });

        mapper.Map((Customer?)null).Should().BeNull();
        mapper.Map((CustomerDalDto?)null).Should().BeNull();
        mapper.Map(new CustomerDalDto { Id = id, ManagementCompanyId = companyId, Name = "n", Slug = "s", RegistryCode = "r" })!
            .Notes.Should().BeNull();
    }

    [Fact]
    public void LeaseMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new LeaseDalMapper();
        var id = Id(20);
        var unitId = Id(21);
        var residentId = Id(22);
        var roleId = Id(23);
        var startDate = new DateOnly(2026, 1, 1);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new Lease
            {
                Id = id,
                UnitId = unitId,
                ResidentId = residentId,
                LeaseRoleId = roleId,
                StartDate = startDate,
                EndDate = null,
                Notes = Localized("English lease", "Eesti leping")
            })!;

            dto.Id.Should().Be(id);
            dto.UnitId.Should().Be(unitId);
            dto.ResidentId.Should().Be(residentId);
            dto.LeaseRoleId.Should().Be(roleId);
            dto.StartDate.Should().Be(startDate);
            dto.EndDate.Should().BeNull();
            dto.Notes.Should().Be("Eesti leping");

            var entity = mapper.Map(new LeaseDalDto
            {
                Id = id,
                UnitId = unitId,
                ResidentId = residentId,
                LeaseRoleId = roleId,
                StartDate = startDate,
                EndDate = null,
                Notes = "  dto lease  "
            })!;

            entity.Id.Should().Be(id);
            entity.UnitId.Should().Be(unitId);
            entity.ResidentId.Should().Be(residentId);
            entity.LeaseRoleId.Should().Be(roleId);
            entity.EndDate.Should().BeNull();
            entity.Notes!.Translate("et").Should().Be("dto lease");
        });

        mapper.Map((Lease?)null).Should().BeNull();
        mapper.Map((LeaseDalDto?)null).Should().BeNull();
        mapper.Map(new LeaseDalDto { Id = id, UnitId = unitId, ResidentId = residentId, LeaseRoleId = roleId, StartDate = startDate })!
            .Notes.Should().BeNull();
    }

    [Fact]
    public void ManagementCompanyMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new ManagementCompanyDalMapper();
        var id = Id(30);

        var dto = mapper.Map(new ManagementCompany
        {
            Id = id,
            Name = "Company",
            Slug = "company",
            RegistryCode = "REG",
            VatNumber = "VAT",
            Email = "company@example.test",
            Phone = "+372",
            Address = "Street 1"
        })!;

        dto.Id.Should().Be(id);
        dto.Name.Should().Be("Company");
        dto.Slug.Should().Be("company");
        dto.RegistryCode.Should().Be("REG");
        dto.VatNumber.Should().Be("VAT");
        dto.Email.Should().Be("company@example.test");
        dto.Phone.Should().Be("+372");
        dto.Address.Should().Be("Street 1");

        var entity = mapper.Map(new ManagementCompanyDalDto
        {
            Id = id,
            Name = "Company DTO",
            Slug = "company-dto",
            RegistryCode = "REG2",
            VatNumber = "VAT2",
            Email = "dto@example.test",
            Phone = "+371",
            Address = "Street 2"
        })!;

        entity.Id.Should().Be(id);
        entity.Name.Should().Be("Company DTO");
        entity.Slug.Should().Be("company-dto");
        entity.RegistryCode.Should().Be("REG2");
        entity.VatNumber.Should().Be("VAT2");
        entity.Email.Should().Be("dto@example.test");
        entity.Phone.Should().Be("+371");
        entity.Address.Should().Be("Street 2");
        mapper.Map((ManagementCompany?)null).Should().BeNull();
        mapper.Map((ManagementCompanyDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void ManagementCompanyJoinRequestMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new ManagementCompanyJoinRequestDalMapper();
        var id = Id(40);
        var appUserId = Id(41);
        var companyId = Id(42);
        var roleId = Id(43);
        var statusId = Id(44);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new ManagementCompanyJoinRequest
            {
                Id = id,
                AppUserId = appUserId,
                ManagementCompanyId = companyId,
                RequestedManagementCompanyRoleId = roleId,
                ManagementCompanyJoinRequestStatusId = statusId,
                Message = Localized("English request", "Eesti taotlus"),
                ResolvedAt = null,
                ResolvedByAppUserId = null
            })!;

            dto.Id.Should().Be(id);
            dto.AppUserId.Should().Be(appUserId);
            dto.ManagementCompanyId.Should().Be(companyId);
            dto.RequestedRoleId.Should().Be(roleId);
            dto.StatusId.Should().Be(statusId);
            dto.Message.Should().Be("Eesti taotlus");
            dto.ResolvedAt.Should().BeNull();
            dto.ResolvedByAppUserId.Should().BeNull();

            var entity = mapper.Map(new ManagementCompanyJoinRequestDalDto
            {
                Id = id,
                AppUserId = appUserId,
                ManagementCompanyId = companyId,
                RequestedRoleId = roleId,
                StatusId = statusId,
                Message = "  dto request  ",
                ResolvedAt = null,
                ResolvedByAppUserId = null
            })!;

            entity.Id.Should().Be(id);
            entity.AppUserId.Should().Be(appUserId);
            entity.ManagementCompanyId.Should().Be(companyId);
            entity.RequestedManagementCompanyRoleId.Should().Be(roleId);
            entity.ManagementCompanyJoinRequestStatusId.Should().Be(statusId);
            entity.Message!.Translate("et").Should().Be("dto request");
            entity.ResolvedAt.Should().BeNull();
            entity.ResolvedByAppUserId.Should().BeNull();
        });

        mapper.Map((ManagementCompanyJoinRequest?)null).Should().BeNull();
        mapper.Map((ManagementCompanyJoinRequestDalDto?)null).Should().BeNull();
        mapper.Map(new ManagementCompanyJoinRequestDalDto
        {
            Id = id,
            AppUserId = appUserId,
            ManagementCompanyId = companyId,
            RequestedRoleId = roleId,
            StatusId = statusId
        })!.Message.Should().BeNull();
    }

    [Fact]
    public void PropertyMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new PropertyDalMapper();
        var id = Id(50);
        var customerId = Id(51);
        var propertyTypeId = Id(52);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new Property
            {
                Id = id,
                CustomerId = customerId,
                PropertyTypeId = propertyTypeId,
                Label = Localized("English property", "Eesti kinnistu"),
                Slug = "property",
                AddressLine = "Street 1",
                City = "Tallinn",
                PostalCode = "10111",
                Notes = Localized("English notes", "Eesti markmed")
            })!;

            dto.Id.Should().Be(id);
            dto.CustomerId.Should().Be(customerId);
            dto.PropertyTypeId.Should().Be(propertyTypeId);
            dto.Label.Should().Be("Eesti kinnistu");
            dto.Notes.Should().Be("Eesti markmed");

            var entity = mapper.Map(new PropertyDalDto
            {
                Id = id,
                CustomerId = customerId,
                PropertyTypeId = propertyTypeId,
                Label = "  dto property  ",
                Slug = "property-dto",
                AddressLine = "Street 2",
                City = "Tartu",
                PostalCode = "50101",
                Notes = null
            })!;

            entity.Id.Should().Be(id);
            entity.CustomerId.Should().Be(customerId);
            entity.PropertyTypeId.Should().Be(propertyTypeId);
            entity.Label.Translate("et").Should().Be("dto property");
            entity.Slug.Should().Be("property-dto");
            entity.Notes.Should().BeNull();
        });

        mapper.Map((Property?)null).Should().BeNull();
        mapper.Map((PropertyDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void ResidentMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new ResidentDalMapper();
        var id = Id(60);
        var companyId = Id(61);

        var dto = mapper.Map(new Resident
        {
            Id = id,
            ManagementCompanyId = companyId,
            FirstName = "Mari",
            LastName = "Maasikas",
            IdCode = "50001010000",
            PreferredLanguage = null
        })!;

        dto.Id.Should().Be(id);
        dto.ManagementCompanyId.Should().Be(companyId);
        dto.FirstName.Should().Be("Mari");
        dto.LastName.Should().Be("Maasikas");
        dto.IdCode.Should().Be("50001010000");
        dto.PreferredLanguage.Should().BeNull();

        var entity = mapper.Map(new ResidentDalDto
        {
            Id = id,
            ManagementCompanyId = companyId,
            FirstName = "Jaan",
            LastName = "Tamm",
            IdCode = "60001010000",
            PreferredLanguage = null
        })!;

        entity.Id.Should().Be(id);
        entity.ManagementCompanyId.Should().Be(companyId);
        entity.PreferredLanguage.Should().BeNull();
        mapper.Map((Resident?)null).Should().BeNull();
        mapper.Map((ResidentDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void ResidentContactMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new ResidentContactDalMapper();
        var id = Id(70);
        var residentId = Id(71);
        var contactId = Id(72);
        var validFrom = new DateOnly(2026, 2, 1);

        var dto = mapper.Map(new ResidentContact
        {
            Id = id,
            ResidentId = residentId,
            ContactId = contactId,
            ValidFrom = validFrom,
            ValidTo = null,
            Confirmed = true,
            IsPrimary = false
        })!;

        dto.Id.Should().Be(id);
        dto.ResidentId.Should().Be(residentId);
        dto.ContactId.Should().Be(contactId);
        dto.ValidFrom.Should().Be(validFrom);
        dto.ValidTo.Should().BeNull();
        dto.Confirmed.Should().BeTrue();
        dto.IsPrimary.Should().BeFalse();

        var entity = mapper.Map(new ResidentContactDalDto
        {
            Id = id,
            ResidentId = residentId,
            ContactId = contactId,
            ValidFrom = validFrom,
            ValidTo = null,
            Confirmed = false,
            IsPrimary = true
        })!;

        entity.Id.Should().Be(id);
        entity.ResidentId.Should().Be(residentId);
        entity.ContactId.Should().Be(contactId);
        entity.ValidTo.Should().BeNull();
        entity.Confirmed.Should().BeFalse();
        entity.IsPrimary.Should().BeTrue();
        mapper.Map((ResidentContact?)null).Should().BeNull();
        mapper.Map((ResidentContactDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void ScheduledWorkMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new ScheduledWorkDalMapper();
        var id = Id(80);
        var vendorId = Id(81);
        var ticketId = Id(82);
        var statusId = Id(83);
        var scheduledStart = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new ScheduledWork
            {
                Id = id,
                VendorId = vendorId,
                TicketId = ticketId,
                WorkStatusId = statusId,
                ScheduledStart = scheduledStart,
                ScheduledEnd = null,
                RealStart = null,
                RealEnd = null,
                Notes = Localized("English schedule", "Eesti graafik")
            })!;

            dto.Id.Should().Be(id);
            dto.VendorId.Should().Be(vendorId);
            dto.TicketId.Should().Be(ticketId);
            dto.WorkStatusId.Should().Be(statusId);
            dto.ScheduledStart.Should().Be(scheduledStart);
            dto.ScheduledEnd.Should().BeNull();
            dto.RealStart.Should().BeNull();
            dto.RealEnd.Should().BeNull();
            dto.Notes.Should().Be("Eesti graafik");

            var entity = mapper.Map(new ScheduledWorkDalDto
            {
                Id = id,
                VendorId = vendorId,
                TicketId = ticketId,
                WorkStatusId = statusId,
                ScheduledStart = scheduledStart,
                ScheduledEnd = null,
                RealStart = null,
                RealEnd = null,
                Notes = null
            })!;

            entity.Id.Should().Be(id);
            entity.VendorId.Should().Be(vendorId);
            entity.TicketId.Should().Be(ticketId);
            entity.WorkStatusId.Should().Be(statusId);
            entity.ScheduledEnd.Should().BeNull();
            entity.RealStart.Should().BeNull();
            entity.RealEnd.Should().BeNull();
            entity.Notes.Should().BeNull();
        });

        mapper.Map((ScheduledWork?)null).Should().BeNull();
        mapper.Map((ScheduledWorkDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void TicketMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new TicketDalMapper();
        var id = Id(90);
        var companyId = Id(91);
        var categoryId = Id(92);
        var statusId = Id(93);
        var priorityId = Id(94);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new Ticket
            {
                Id = id,
                ManagementCompanyId = companyId,
                TicketNr = "T-1",
                Title = Localized("English title", "Eesti pealkiri"),
                Description = Localized("English description", "Eesti kirjeldus"),
                TicketCategoryId = categoryId,
                TicketStatusId = statusId,
                TicketPriorityId = priorityId,
                CustomerId = null,
                PropertyId = null,
                UnitId = null,
                ResidentId = null,
                VendorId = null,
                DueAt = null,
                ClosedAt = null
            })!;

            dto.Id.Should().Be(id);
            dto.ManagementCompanyId.Should().Be(companyId);
            dto.TicketNr.Should().Be("T-1");
            dto.Title.Should().Be("Eesti pealkiri");
            dto.Description.Should().Be("Eesti kirjeldus");
            dto.TicketCategoryId.Should().Be(categoryId);
            dto.TicketStatusId.Should().Be(statusId);
            dto.TicketPriorityId.Should().Be(priorityId);
            dto.CustomerId.Should().BeNull();
            dto.PropertyId.Should().BeNull();
            dto.UnitId.Should().BeNull();
            dto.ResidentId.Should().BeNull();
            dto.VendorId.Should().BeNull();
            dto.DueAt.Should().BeNull();
            dto.ClosedAt.Should().BeNull();

            var entity = mapper.Map(new TicketDalDto
            {
                Id = id,
                ManagementCompanyId = companyId,
                TicketNr = "T-2",
                Title = "dto title",
                Description = "dto description",
                TicketCategoryId = categoryId,
                TicketStatusId = statusId,
                TicketPriorityId = priorityId,
                CustomerId = null,
                PropertyId = null,
                UnitId = null,
                ResidentId = null,
                VendorId = null,
                DueAt = null,
                ClosedAt = null
            })!;

            entity.Id.Should().Be(id);
            entity.ManagementCompanyId.Should().Be(companyId);
            entity.TicketNr.Should().Be("T-2");
            entity.Title.Translate("et").Should().Be("dto title");
            entity.Description.Translate("et").Should().Be("dto description");
            entity.CustomerId.Should().BeNull();
            entity.PropertyId.Should().BeNull();
            entity.UnitId.Should().BeNull();
            entity.ResidentId.Should().BeNull();
            entity.VendorId.Should().BeNull();
            entity.DueAt.Should().BeNull();
            entity.ClosedAt.Should().BeNull();
        });

        mapper.Map((Ticket?)null).Should().BeNull();
        mapper.Map((TicketDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void UnitMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new UnitDalMapper();
        var id = Id(100);
        var propertyId = Id(101);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new DomainUnit
            {
                Id = id,
                PropertyId = propertyId,
                UnitNr = "12",
                Slug = "unit-12",
                FloorNr = null,
                SizeM2 = null,
                Notes = Localized("English unit", "Eesti ruum")
            })!;

            dto.Id.Should().Be(id);
            dto.PropertyId.Should().Be(propertyId);
            dto.UnitNr.Should().Be("12");
            dto.Slug.Should().Be("unit-12");
            dto.FloorNr.Should().BeNull();
            dto.SizeM2.Should().BeNull();
            dto.Notes.Should().Be("Eesti ruum");

            var entity = mapper.Map(new UnitDalDto
            {
                Id = id,
                PropertyId = propertyId,
                UnitNr = "13",
                Slug = "unit-13",
                FloorNr = null,
                SizeM2 = null,
                Notes = null
            })!;

            entity.Id.Should().Be(id);
            entity.PropertyId.Should().Be(propertyId);
            entity.FloorNr.Should().BeNull();
            entity.SizeM2.Should().BeNull();
            entity.Notes.Should().BeNull();
        });

        mapper.Map((DomainUnit?)null).Should().BeNull();
        mapper.Map((UnitDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void VendorMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new VendorDalMapper();
        var id = Id(110);
        var companyId = Id(111);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new Vendor
            {
                Id = id,
                ManagementCompanyId = companyId,
                Name = "Vendor",
                RegistryCode = "VREG",
                Notes = Localized("English vendor", "Eesti tarnija")
            })!;

            dto.Id.Should().Be(id);
            dto.ManagementCompanyId.Should().Be(companyId);
            dto.Name.Should().Be("Vendor");
            dto.RegistryCode.Should().Be("VREG");
            dto.Notes.Should().Be("Eesti tarnija");

            var entity = mapper.Map(new VendorDalDto
            {
                Id = id,
                ManagementCompanyId = companyId,
                Name = "Vendor DTO",
                RegistryCode = "VREG2",
                Notes = "dto vendor"
            })!;

            entity.Id.Should().Be(id);
            entity.ManagementCompanyId.Should().Be(companyId);
            entity.Name.Should().Be("Vendor DTO");
            entity.RegistryCode.Should().Be("VREG2");
            entity.Notes.Translate("et").Should().Be("dto vendor");
        });

        mapper.Map((Vendor?)null).Should().BeNull();
        mapper.Map((VendorDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void VendorContactMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new VendorContactDalMapper();
        var id = Id(120);
        var vendorId = Id(121);
        var contactId = Id(122);
        var validFrom = new DateOnly(2026, 4, 1);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new VendorContact
            {
                Id = id,
                VendorId = vendorId,
                ContactId = contactId,
                ValidFrom = validFrom,
                ValidTo = null,
                Confirmed = true,
                IsPrimary = true,
                FullName = null,
                RoleTitle = Localized("English role", "Eesti roll")
            })!;

            dto.Id.Should().Be(id);
            dto.VendorId.Should().Be(vendorId);
            dto.ContactId.Should().Be(contactId);
            dto.ValidTo.Should().BeNull();
            dto.Confirmed.Should().BeTrue();
            dto.IsPrimary.Should().BeTrue();
            dto.FullName.Should().BeNull();
            dto.RoleTitle.Should().Be("Eesti roll");

            var entity = mapper.Map(new VendorContactDalDto
            {
                Id = id,
                VendorId = vendorId,
                ContactId = contactId,
                ValidFrom = validFrom,
                ValidTo = null,
                Confirmed = false,
                IsPrimary = false,
                FullName = null,
                RoleTitle = null
            })!;

            entity.Id.Should().Be(id);
            entity.VendorId.Should().Be(vendorId);
            entity.ContactId.Should().Be(contactId);
            entity.ValidTo.Should().BeNull();
            entity.Confirmed.Should().BeFalse();
            entity.IsPrimary.Should().BeFalse();
            entity.FullName.Should().BeNull();
            entity.RoleTitle.Should().BeNull();
        });

        mapper.Map((VendorContact?)null).Should().BeNull();
        mapper.Map((VendorContactDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void VendorTicketCategoryMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new VendorTicketCategoryDalMapper();
        var id = Id(130);
        var vendorId = Id(131);
        var categoryId = Id(132);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new VendorTicketCategory
            {
                Id = id,
                VendorId = vendorId,
                TicketCategoryId = categoryId,
                Notes = Localized("English category", "Eesti kategooria")
            })!;

            dto.Id.Should().Be(id);
            dto.VendorId.Should().Be(vendorId);
            dto.TicketCategoryId.Should().Be(categoryId);
            dto.Notes.Should().Be("Eesti kategooria");

            var entity = mapper.Map(new VendorTicketCategoryDalDto
            {
                Id = id,
                VendorId = vendorId,
                TicketCategoryId = categoryId,
                Notes = null
            })!;

            entity.Id.Should().Be(id);
            entity.VendorId.Should().Be(vendorId);
            entity.TicketCategoryId.Should().Be(categoryId);
            entity.Notes.Should().BeNull();
        });

        mapper.Map((VendorTicketCategory?)null).Should().BeNull();
        mapper.Map((VendorTicketCategoryDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void WorkLogMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new WorkLogDalMapper();
        var id = Id(140);
        var scheduledWorkId = Id(141);
        var appUserId = Id(142);

        WithCulture("et-EE", () =>
        {
            var dto = mapper.Map(new WorkLog
            {
                Id = id,
                ScheduledWorkId = scheduledWorkId,
                AppUserId = appUserId,
                WorkStart = null,
                WorkEnd = null,
                Hours = null,
                MaterialCost = null,
                LaborCost = null,
                Description = Localized("English work", "Eesti too")
            })!;

            dto.Id.Should().Be(id);
            dto.ScheduledWorkId.Should().Be(scheduledWorkId);
            dto.AppUserId.Should().Be(appUserId);
            dto.WorkStart.Should().BeNull();
            dto.WorkEnd.Should().BeNull();
            dto.Hours.Should().BeNull();
            dto.MaterialCost.Should().BeNull();
            dto.LaborCost.Should().BeNull();
            dto.Description.Should().Be("Eesti too");

            var entity = mapper.Map(new WorkLogDalDto
            {
                Id = id,
                ScheduledWorkId = scheduledWorkId,
                AppUserId = appUserId,
                WorkStart = null,
                WorkEnd = null,
                Hours = null,
                MaterialCost = null,
                LaborCost = null,
                Description = null
            })!;

            entity.Id.Should().Be(id);
            entity.ScheduledWorkId.Should().Be(scheduledWorkId);
            entity.AppUserId.Should().Be(appUserId);
            entity.WorkStart.Should().BeNull();
            entity.WorkEnd.Should().BeNull();
            entity.Hours.Should().BeNull();
            entity.MaterialCost.Should().BeNull();
            entity.LaborCost.Should().BeNull();
            entity.Description.Should().BeNull();
        });

        mapper.Map((WorkLog?)null).Should().BeNull();
        mapper.Map((WorkLogDalDto?)null).Should().BeNull();
    }

    [Fact]
    public void AppRefreshTokenMapper_MapsBothDirectionsAndNulls()
    {
        var mapper = new AppRefreshTokenDalMapper();
        var id = Id(150);
        var appUserId = Id(151);
        var expires = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var previousExpires = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);

        var dto = mapper.Map(new AppRefreshToken
        {
            Id = id,
            AppUserId = appUserId,
            RefreshToken = "refresh",
            ExpirationDT = expires,
            PreviousRefreshToken = null,
            PreviousExpirationDT = previousExpires
        })!;

        dto.Id.Should().Be(id);
        dto.AppUserId.Should().Be(appUserId);
        dto.RefreshToken.Should().Be("refresh");
        dto.ExpirationDT.Should().Be(expires);
        dto.PreviousRefreshToken.Should().BeNull();
        dto.PreviousExpirationDT.Should().Be(previousExpires);

        var entity = mapper.Map(new AppRefreshTokenDalDto
        {
            Id = id,
            AppUserId = appUserId,
            RefreshToken = "refresh-dto",
            ExpirationDT = expires,
            PreviousRefreshToken = null,
            PreviousExpirationDT = previousExpires
        })!;

        entity.Id.Should().Be(id);
        entity.AppUserId.Should().Be(appUserId);
        entity.RefreshToken.Should().Be("refresh-dto");
        entity.ExpirationDT.Should().Be(expires);
        entity.PreviousRefreshToken.Should().BeNull();
        entity.PreviousExpirationDT.Should().Be(previousExpires);
        mapper.Map((AppRefreshToken?)null).Should().BeNull();
        mapper.Map((AppRefreshTokenDalDto?)null).Should().BeNull();
    }

    private static Guid Id(int value)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{value:000000000000}");
    }

    private static LangStr Localized(string english, string estonian)
    {
        return new LangStr
        {
            ["en"] = english,
            ["et"] = estonian
        };
    }

    private static void WithCulture(string cultureName, Action action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var culture = CultureInfo.GetCultureInfo(cultureName);

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}

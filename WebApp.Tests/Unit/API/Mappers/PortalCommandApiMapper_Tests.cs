using App.BLL.DTO.Contacts;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Leases;
using App.BLL.DTO.ManagementCompanies.Models;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Residents;
using App.BLL.DTO.ScheduledWorks;
using App.BLL.DTO.Tickets;
using App.BLL.DTO.Units;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.WorkLogs;
using App.DTO.v1.Mappers.Portal.Contacts;
using App.DTO.v1.Mappers.Portal.Customers;
using App.DTO.v1.Mappers.Portal.Leases;
using App.DTO.v1.Mappers.Portal.Properties;
using App.DTO.v1.Mappers.Portal.Residents;
using App.DTO.v1.Mappers.Portal.ScheduledWork;
using App.DTO.v1.Mappers.Portal.Tickets;
using App.DTO.v1.Mappers.Portal.Units;
using App.DTO.v1.Mappers.Portal.Users;
using App.DTO.v1.Mappers.Portal.VendorContacts;
using App.DTO.v1.Mappers.Portal.Vendors;
using App.DTO.v1.Mappers.Portal.WorkLogs;
using App.DTO.v1.Portal.Contacts;
using App.DTO.v1.Portal.Customers;
using App.DTO.v1.Portal.Leases;
using App.DTO.v1.Portal.Properties;
using App.DTO.v1.Portal.Residents;
using App.DTO.v1.Portal.ScheduledWork;
using App.DTO.v1.Portal.Tickets;
using App.DTO.v1.Portal.Units;
using App.DTO.v1.Portal.Users;
using App.DTO.v1.Portal.VendorContacts;
using App.DTO.v1.Portal.Vendors;
using App.DTO.v1.Portal.WorkLogs;
using AwesomeAssertions;
using Base.Contracts;

namespace WebApp.Tests.Unit.API.Mappers;

public class PortalCommandApiMapper_Tests
{
    [Fact]
    public void HierarchyCommandMappers_MapRequestDtosToBllDtos()
    {
        var propertyTypeId = Guid.NewGuid();
        var customer = new CustomerApiMapper().Map(new CustomerRequestDto
        {
            Name = "Customer",
            RegistryCode = "REG-1",
            BillingEmail = "billing@test.ee",
            BillingAddress = "Billing Street 1",
            Phone = "+372 5555 0001"
        });
        var createPropertyMapper = (IBaseMapper<CreatePropertyDto, PropertyBllDto>)new PropertyApiMapper();
        var property = createPropertyMapper.Map(new CreatePropertyDto
        {
            Name = "Property",
            PropertyTypeId = propertyTypeId,
            AddressLine = "Property Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Notes = "Property notes"
        });
        var updatePropertyMapper = (IBaseMapper<UpdatePropertyProfileDto, PropertyBllDto>)new PropertyApiMapper();
        var propertyUpdate = updatePropertyMapper.Map(new UpdatePropertyProfileDto
        {
            Name = "Property Updated",
            AddressLine = "Property Street 2",
            City = "Tartu",
            PostalCode = "50101",
            Notes = "Updated notes"
        });
        var unit = new UnitApiMapper().Map(new UnitRequestDto
        {
            UnitNr = "A-101",
            FloorNr = 3,
            SizeM2 = 56.5m,
            Notes = "Unit notes"
        });
        var resident = new ResidentApiMapper().Map(new ResidentRequestDto
        {
            FirstName = "Mari",
            LastName = "Tamm",
            IdCode = "EE-123",
            PreferredLanguage = "et"
        });

        customer.Should().BeEquivalentTo(new CustomerBllDto
        {
            Id = customer!.Id,
            Name = "Customer",
            RegistryCode = "REG-1",
            BillingEmail = "billing@test.ee",
            BillingAddress = "Billing Street 1",
            Phone = "+372 5555 0001"
        });
        property.Should().BeEquivalentTo(new PropertyBllDto
        {
            Id = property!.Id,
            Label = "Property",
            PropertyTypeId = propertyTypeId,
            AddressLine = "Property Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Notes = "Property notes"
        });
        propertyUpdate.Should().BeEquivalentTo(new PropertyBllDto
        {
            Id = propertyUpdate!.Id,
            Label = "Property Updated",
            AddressLine = "Property Street 2",
            City = "Tartu",
            PostalCode = "50101",
            Notes = "Updated notes"
        });
        unit.Should().BeEquivalentTo(new UnitBllDto
        {
            Id = unit!.Id,
            UnitNr = "A-101",
            FloorNr = 3,
            SizeM2 = 56.5m,
            Notes = "Unit notes"
        });
        resident.Should().BeEquivalentTo(new ResidentBllDto
        {
            Id = resident!.Id,
            FirstName = "Mari",
            LastName = "Tamm",
            IdCode = "EE-123",
            PreferredLanguage = "et"
        });
    }

    [Fact]
    public void ContactCommandMappers_MapAssignmentsAndNewContacts()
    {
        var contactId = Guid.NewGuid();
        var contactTypeId = Guid.NewGuid();
        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var validTo = validFrom.AddDays(30);
        var residentMapper = new ResidentContactApiMapper();
        var residentAssignment = residentMapper.Map(new ResidentContactAssignmentDto
        {
            ContactId = contactId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Confirmed = false,
            IsPrimary = true
        });
        var residentNewAssignment = ((IBaseMapper<CreateAndAttachResidentContactDto, ResidentContactBllDto>)residentMapper)
            .Map(new CreateAndAttachResidentContactDto
            {
                ContactTypeId = contactTypeId,
                ContactValue = "resident@test.ee",
                ContactNotes = "Resident notes",
                ValidFrom = validFrom,
                ValidTo = validTo,
                Confirmed = true,
                IsPrimary = false
            });
        var residentNewContact = ((IBaseMapper<CreateAndAttachResidentContactDto, ContactBllDto>)residentMapper)
            .Map(new CreateAndAttachResidentContactDto
            {
                ContactTypeId = contactTypeId,
                ContactValue = "resident@test.ee",
                ContactNotes = "Resident notes"
            });
        var vendorMapper = new VendorContactApiMapper();
        var vendorAssignment = vendorMapper.Map(new VendorContactAssignmentDto
        {
            ContactId = contactId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Confirmed = false,
            IsPrimary = true,
            FullName = "Vendor Contact",
            RoleTitle = "Coordinator"
        });
        var vendorNewContact = ((IBaseMapper<CreateAndAttachVendorContactDto, ContactBllDto>)vendorMapper)
            .Map(new CreateAndAttachVendorContactDto
            {
                ContactTypeId = contactTypeId,
                ContactValue = "+37255550001",
                ContactNotes = "Vendor phone"
            });

        residentAssignment.Should().BeEquivalentTo(new ResidentContactBllDto
        {
            Id = residentAssignment!.Id,
            ContactId = contactId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Confirmed = false,
            IsPrimary = true
        });
        residentNewAssignment.Should().BeEquivalentTo(new ResidentContactBllDto
        {
            Id = residentNewAssignment!.Id,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Confirmed = true,
            IsPrimary = false
        });
        residentNewContact.Should().BeEquivalentTo(new ContactBllDto
        {
            Id = residentNewContact!.Id,
            ContactTypeId = contactTypeId,
            ContactValue = "resident@test.ee",
            Notes = "Resident notes"
        });
        vendorAssignment.Should().BeEquivalentTo(new VendorContactBllDto
        {
            Id = vendorAssignment!.Id,
            ContactId = contactId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            Confirmed = false,
            IsPrimary = true,
            FullName = "Vendor Contact",
            RoleTitle = "Coordinator"
        });
        vendorNewContact.Should().BeEquivalentTo(new ContactBllDto
        {
            Id = vendorNewContact!.Id,
            ContactTypeId = contactTypeId,
            ContactValue = "+37255550001",
            Notes = "Vendor phone"
        });
    }

    [Fact]
    public void OperationalCommandMappers_MapLeaseTicketScheduledWorkAndWorkLogDtos()
    {
        var leaseRoleId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var residentId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var priorityId = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();
        var workStatusId = Guid.NewGuid();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var dueAt = DateTime.UtcNow.AddDays(5);
        var scheduledStart = DateTime.UtcNow.AddDays(2);
        var workStart = DateTime.UtcNow.AddHours(-3);
        var workEnd = DateTime.UtcNow.AddHours(-1);
        var leaseMapper = new LeaseApiMapper();
        var residentLease = ((IBaseMapper<CreateResidentLeaseDto, LeaseBllDto>)leaseMapper)
            .Map(new CreateResidentLeaseDto
            {
                UnitId = unitId,
                LeaseRoleId = leaseRoleId,
                StartDate = startDate,
                EndDate = startDate.AddDays(10),
                Notes = "Resident lease"
            });
        var unitLease = ((IBaseMapper<CreateUnitLeaseDto, LeaseBllDto>)leaseMapper)
            .Map(new CreateUnitLeaseDto
            {
                ResidentId = residentId,
                LeaseRoleId = leaseRoleId,
                StartDate = startDate,
                Notes = "Unit lease"
            });
        var ticketMapper = new TicketApiMapper();
        var ticket = ticketMapper.Map(new UpdateTicketDto
        {
            TicketNr = "T-1",
            Title = "Ticket",
            Description = "Ticket description",
            TicketCategoryId = categoryId,
            TicketStatusId = statusId,
            TicketPriorityId = priorityId,
            UnitId = unitId,
            ResidentId = residentId,
            VendorId = vendorId,
            DueAt = dueAt
        });
        var scheduledWork = new ScheduledWorkApiMapper().Map(new ScheduledWorkRequestDto
        {
            VendorId = vendorId,
            WorkStatusId = workStatusId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(2),
            RealStart = workStart,
            RealEnd = workEnd,
            Notes = "Scheduled work"
        });
        var workLog = new WorkLogApiMapper().Map(new WorkLogRequestDto
        {
            WorkStart = workStart,
            WorkEnd = workEnd,
            Hours = 2.5m,
            MaterialCost = 10m,
            LaborCost = 50m,
            Description = "Work log"
        });

        residentLease.Should().BeEquivalentTo(new LeaseBllDto
        {
            Id = residentLease!.Id,
            UnitId = unitId,
            LeaseRoleId = leaseRoleId,
            StartDate = startDate,
            EndDate = startDate.AddDays(10),
            Notes = "Resident lease"
        });
        unitLease.Should().BeEquivalentTo(new LeaseBllDto
        {
            Id = unitLease!.Id,
            ResidentId = residentId,
            LeaseRoleId = leaseRoleId,
            StartDate = startDate,
            Notes = "Unit lease"
        });
        ticket.Should().BeEquivalentTo(new TicketBllDto
        {
            Id = ticket!.Id,
            TicketNr = "T-1",
            Title = "Ticket",
            Description = "Ticket description",
            TicketCategoryId = categoryId,
            TicketStatusId = statusId,
            TicketPriorityId = priorityId,
            UnitId = unitId,
            ResidentId = residentId,
            VendorId = vendorId,
            DueAt = dueAt
        });
        scheduledWork.Should().BeEquivalentTo(new ScheduledWorkBllDto
        {
            Id = scheduledWork!.Id,
            VendorId = vendorId,
            WorkStatusId = workStatusId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledStart.AddHours(2),
            RealStart = workStart,
            RealEnd = workEnd,
            Notes = "Scheduled work"
        });
        workLog.Should().BeEquivalentTo(new WorkLogBllDto
        {
            Id = workLog!.Id,
            WorkStart = workStart,
            WorkEnd = workEnd,
            Hours = 2.5m,
            MaterialCost = 10m,
            LaborCost = 50m,
            Description = "Work log"
        });
    }

    [Fact]
    public void VendorAndCompanyUserCommandMappers_MapPayloads()
    {
        var roleId = Guid.NewGuid();
        var ticketCategoryId = Guid.NewGuid();
        var targetMembershipId = Guid.NewGuid();
        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var vendor = new VendorApiMapper().Map(new VendorRequestDto
        {
            Name = "Vendor",
            RegistryCode = "VEND-1",
            Notes = "Vendor notes"
        });
        var categoryMapper = new VendorCategoryApiMapper();
        var category = ((IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto>)categoryMapper)
            .Map(new AssignVendorCategoryDto
            {
                TicketCategoryId = ticketCategoryId,
                Notes = "Category notes"
            });
        var categoryUpdate = ((IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto>)categoryMapper)
            .Map(new UpdateVendorCategoryDto { Notes = "Updated category notes" });
        var companyUserMapper = new CompanyUserApiMapper();
        var addUser = companyUserMapper.Map(new AddCompanyUserDto
        {
            Email = "member@test.ee",
            RoleId = roleId,
            JobTitle = "Manager",
            ValidFrom = validFrom,
            ValidTo = validFrom.AddDays(30)
        });
        var updateUser = companyUserMapper.Map(new UpdateCompanyUserDto
        {
            RoleId = roleId,
            JobTitle = "Support",
            ValidFrom = validFrom,
            ValidTo = null
        });
        var transfer = new OwnershipTransferApiMapper().Map(new TransferOwnershipDto
        {
            TargetMembershipId = targetMembershipId
        });

        vendor.Should().BeEquivalentTo(new VendorBllDto
        {
            Id = vendor!.Id,
            Name = "Vendor",
            RegistryCode = "VEND-1",
            Notes = "Vendor notes"
        });
        category.Should().BeEquivalentTo(new VendorTicketCategoryBllDto
        {
            Id = category!.Id,
            TicketCategoryId = ticketCategoryId,
            Notes = "Category notes"
        });
        categoryUpdate.Should().BeEquivalentTo(new VendorTicketCategoryBllDto
        {
            Id = categoryUpdate!.Id,
            Notes = "Updated category notes"
        });
        addUser.Should().BeEquivalentTo(new CompanyMembershipAddRequest
        {
            Email = "member@test.ee",
            RoleId = roleId,
            JobTitle = "Manager",
            ValidFrom = validFrom,
            ValidTo = validFrom.AddDays(30)
        });
        updateUser.Should().BeEquivalentTo(new CompanyMembershipUpdateRequest
        {
            RoleId = roleId,
            JobTitle = "Support",
            ValidFrom = validFrom,
            ValidTo = null
        });
        transfer.Should().BeEquivalentTo(new TransferOwnershipRequest
        {
            TargetMembershipId = targetMembershipId
        });
    }

    [Fact]
    public void CommandMappers_ReturnNullForNullPayloads()
    {
        new CustomerApiMapper().Map((CustomerRequestDto?)null).Should().BeNull();
        new UnitApiMapper().Map((UnitRequestDto?)null).Should().BeNull();
        new ResidentApiMapper().Map((ResidentRequestDto?)null).Should().BeNull();
        new VendorApiMapper().Map((VendorRequestDto?)null).Should().BeNull();
        new TicketApiMapper().Map((CreateTicketDto?)null).Should().BeNull();
        new ScheduledWorkApiMapper().Map((ScheduledWorkRequestDto?)null).Should().BeNull();
        new WorkLogApiMapper().Map((WorkLogRequestDto?)null).Should().BeNull();
        new CompanyUserApiMapper().Map((AddCompanyUserDto?)null).Should().BeNull();
    }

}

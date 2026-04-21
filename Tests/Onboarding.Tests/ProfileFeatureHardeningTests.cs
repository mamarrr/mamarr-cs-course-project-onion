using App.BLL.CustomerWorkspace.Profiles;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.ManagementCompany.Membership;
using App.BLL.ManagementCompany.Profiles;
using App.BLL.PropertyWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Workspace;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Onboarding.Tests;

public class ProfileFeatureHardeningTests
{
    [Fact]
    public async Task UpdateManagementCompanyProfileAsync_Allows_Registry_Code_Change()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedManagementCompanyFixtureAsync(db, "OWNER");
        var authorizationService = new FakeCompanyMembershipAdminService(fixture.AuthorizationResult);
        var sut = new ManagementCompanyProfileService(db, authorizationService);

        var result = await sut.UpdateProfileAsync(
            fixture.Actor.Id,
            fixture.Company.Slug,
            new CompanyProfileUpdateRequest
            {
                Name = fixture.Company.Name,
                RegistryCode = "REG-COMP-UPDATED",
                VatNumber = fixture.Company.VatNumber,
                Email = fixture.Company.Email,
                Phone = fixture.Company.Phone,
                Address = fixture.Company.Address,
                IsActive = fixture.Company.IsActive
            });

        Assert.True(result.Success);

        var company = await db.ManagementCompanies.FindAsync(fixture.Company.Id);
        Assert.NotNull(company);
        Assert.Equal("REG-COMP-UPDATED", company!.RegistryCode);
    }

    [Fact]
    public async Task UpdateCustomerProfileAsync_Allows_Registry_Code_Change()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCustomerFixtureAsync(db, "MANAGER");
        var sut = new CustomerProfileService(db);

        var result = await sut.UpdateProfileAsync(
            fixture.Context,
            new CustomerProfileUpdateRequest
            {
                Name = fixture.Customer.Name,
                RegistryCode = "CUST-UPDATED",
                BillingEmail = fixture.Customer.BillingEmail,
                BillingAddress = fixture.Customer.BillingAddress,
                Phone = fixture.Customer.Phone,
                IsActive = fixture.Customer.IsActive
            });

        Assert.True(result.Success);

        var customer = await db.Customers.FindAsync(fixture.Customer.Id);
        Assert.NotNull(customer);
        Assert.Equal("CUST-UPDATED", customer!.RegistryCode);
    }

    [Fact]
    public async Task UpdateUnitProfileAsync_Allows_Unit_Number_And_Slug_Change()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedUnitFixtureAsync(db, "OWNER");
        var sut = new UnitProfileService(db);

        var result = await sut.UpdateProfileAsync(
            fixture.Context,
            new UnitProfileUpdateRequest
            {
                UnitNr = "A-200",
                FloorNr = fixture.Unit.FloorNr,
                SizeM2 = fixture.Unit.SizeM2,
                Notes = null,
                IsActive = fixture.Unit.IsActive
            });

        Assert.True(result.Success);

        var unit = await db.Units.FindAsync(fixture.Unit.Id);
        Assert.NotNull(unit);
        Assert.Equal("A-200", unit!.UnitNr);
        Assert.Equal("a-200", unit.Slug);
    }

    [Fact]
    public async Task DeleteCustomerProfileAsync_Forbids_Finance_Member()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCustomerFixtureAsync(db, "FINANCE");
        var sut = new CustomerProfileService(db);

        var result = await sut.DeleteProfileAsync(fixture.Context);

        Assert.True(result.Forbidden);
        Assert.False(result.Success);
        Assert.Equal(App.Resources.Views.UiText.AccessDeniedDescription, result.ErrorMessage);
        Assert.NotNull(await db.Customers.FindAsync(fixture.Customer.Id));
    }

    [Fact]
    public async Task UpdateCustomerProfileAsync_Rejects_Invalid_Billing_Email()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedCustomerFixtureAsync(db, "MANAGER");
        var sut = new CustomerProfileService(db);

        var result = await sut.UpdateProfileAsync(
            fixture.Context,
            new CustomerProfileUpdateRequest
            {
                Name = "Updated Customer",
                RegistryCode = "UPDATED-REG",
                BillingEmail = "not-an-email"
            });

        Assert.False(result.Success);
        Assert.Equal(App.Resources.Views.UiText.InvalidEmailAddress, result.ErrorMessage);

        var customer = await db.Customers.FindAsync(fixture.Customer.Id);
        Assert.NotNull(customer);
        Assert.Equal(fixture.Customer.Name, customer!.Name);
    }

    [Fact]
    public async Task DeletePropertyProfileAsync_Deletes_Recursive_Dependencies_Without_CrossTenant_Impact()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedPropertyDeletionFixtureAsync(db, "OWNER");
        var sut = new PropertyProfileService(db);

        var result = await sut.DeleteProfileAsync(fixture.Context);

        Assert.True(result.Success);
        Assert.Null(await db.Properties.FindAsync(fixture.Property.Id));
        Assert.Empty(await db.Units.Where(x => fixture.DeletedUnitIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.Leases.Where(x => fixture.DeletedLeaseIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.Tickets.Where(x => fixture.DeletedTicketIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.ScheduledWorks.Where(x => fixture.DeletedScheduledWorkIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.WorkLogs.Where(x => fixture.DeletedWorkLogIds.Contains(x.Id)).ToListAsync());

        Assert.NotNull(await db.Properties.FindAsync(fixture.OtherTenantProperty.Id));
        Assert.NotNull(await db.Units.FindAsync(fixture.OtherTenantUnit.Id));
        Assert.NotNull(await db.Leases.FindAsync(fixture.OtherTenantLease.Id));
        Assert.NotNull(await db.Tickets.FindAsync(fixture.OtherTenantTicket.Id));
        Assert.NotNull(await db.ScheduledWorks.FindAsync(fixture.OtherTenantScheduledWork.Id));
        Assert.NotNull(await db.WorkLogs.FindAsync(fixture.OtherTenantWorkLog.Id));
    }

    [Fact]
    public async Task DeleteResidentProfileAsync_Deletes_Only_Orphaned_Contact()
    {
        await using var db = CreateDbContext();
        var fixture = await SeedResidentDeletionFixtureAsync(db, "OWNER");
        var sut = new ResidentProfileService(db);

        var result = await sut.DeleteProfileAsync(fixture.Context);

        Assert.True(result.Success);
        Assert.Null(await db.Residents.FindAsync(fixture.Resident.Id));
        Assert.Empty(await db.CustomerRepresentatives.Where(x => fixture.DeletedCustomerRepresentativeIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.Leases.Where(x => fixture.DeletedLeaseIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.ResidentUsers.Where(x => fixture.DeletedResidentUserIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.Tickets.Where(x => fixture.DeletedTicketIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.ScheduledWorks.Where(x => fixture.DeletedScheduledWorkIds.Contains(x.Id)).ToListAsync());
        Assert.Empty(await db.WorkLogs.Where(x => fixture.DeletedWorkLogIds.Contains(x.Id)).ToListAsync());
        Assert.Null(await db.Contacts.FindAsync(fixture.OrphanedContact.Id));
        Assert.NotNull(await db.Contacts.FindAsync(fixture.SharedContact.Id));

        Assert.NotNull(await db.Residents.FindAsync(fixture.OtherTenantResident.Id));
        Assert.NotNull(await db.Tickets.FindAsync(fixture.OtherTenantTicket.Id));
        Assert.NotNull(await db.ScheduledWorks.FindAsync(fixture.OtherTenantScheduledWork.Id));
        Assert.NotNull(await db.WorkLogs.FindAsync(fixture.OtherTenantWorkLog.Id));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<(CustomerWorkspaceDashboardContext Context, Customer Customer)> SeedCustomerFixtureAsync(AppDbContext db, string actorRoleCode)
    {
        var company = CreateCompany("customer-company", "Customer Company", "REG-CUST-COMP");
        var role = CreateRole(actorRoleCode);
        var actor = CreateUser($"{actorRoleCode.ToLowerInvariant()}-customer@test.local");
        var customer = CreateCustomer(company.Id, "Managed Customer", "CUST-1", "managed-customer");

        db.ManagementCompanies.Add(company);
        db.ManagementCompanyRoles.Add(role);
        db.Users.Add(actor);
        db.ManagementCompanyUsers.Add(CreateMembership(company.Id, actor.Id, role.Id));
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return (new CustomerWorkspaceDashboardContext
        {
            AppUserId = actor.Id,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name,
            CustomerId = customer.Id,
            CustomerSlug = customer.Slug,
            CustomerName = customer.Name
        }, customer);
    }

    private static async Task<ManagementCompanyFixture> SeedManagementCompanyFixtureAsync(AppDbContext db, string actorRoleCode)
    {
        var company = CreateCompany("managed-company", "Managed Company", "REG-COMP-ORIGINAL");
        var role = CreateRole(actorRoleCode);
        var actor = CreateUser($"{actorRoleCode.ToLowerInvariant()}-company@test.local");
        var membership = CreateMembership(company.Id, actor.Id, role.Id);

        db.ManagementCompanies.Add(company);
        db.ManagementCompanyRoles.Add(role);
        db.Users.Add(actor);
        db.ManagementCompanyUsers.Add(membership);
        await db.SaveChangesAsync();

        return new ManagementCompanyFixture(
            company,
            actor,
            new CompanyAreaAuthorizationResult
            {
                IsAuthorized = true,
                CompanyNotFound = false,
                Context = new CompanyMembershipContext
                {
                    ManagementCompanyId = company.Id,
                    CompanySlug = company.Slug,
                    CompanyName = company.Name,
                    ActorMembershipId = membership.Id,
                    ActorRoleId = role.Id,
                    ActorRoleCode = actorRoleCode,
                    ActorRoleLabel = actorRoleCode,
                    AppUserId = actor.Id,
                    ValidFrom = membership.ValidFrom,
                    ValidTo = membership.ValidTo,
                    IsOwner = string.Equals(actorRoleCode, "OWNER", StringComparison.OrdinalIgnoreCase),
                    IsAdmin = string.Equals(actorRoleCode, "OWNER", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(actorRoleCode, "MANAGER", StringComparison.OrdinalIgnoreCase)
                }
            });
    }

    private static async Task<UnitFixture> SeedUnitFixtureAsync(AppDbContext db, string actorRoleCode)
    {
        var company = CreateCompany("unit-company", "Unit Company", "REG-UNIT-COMP");
        var role = CreateRole(actorRoleCode);
        var actor = CreateUser($"{actorRoleCode.ToLowerInvariant()}-unit@test.local");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");
        var customer = CreateCustomer(company.Id, "Unit Customer", "UNIT-CUST-1", "unit-customer");
        var property = CreateProperty(customer.Id, propertyType.Id, "Unit Property", "unit-property");
        var unit = CreateUnit(property.Id, "A-100", "a-100");

        db.ManagementCompanies.Add(company);
        db.ManagementCompanyRoles.Add(role);
        db.Users.Add(actor);
        db.ManagementCompanyUsers.Add(CreateMembership(company.Id, actor.Id, role.Id));
        db.PropertyTypes.Add(propertyType);
        db.Customers.Add(customer);
        db.Properties.Add(property);
        db.Units.Add(unit);
        await db.SaveChangesAsync();

        return new UnitFixture(
            new UnitDashboardContext
            {
                AppUserId = actor.Id,
                ManagementCompanyId = company.Id,
                CompanySlug = company.Slug,
                CompanyName = company.Name,
                CustomerId = customer.Id,
                CustomerSlug = customer.Slug,
                CustomerName = customer.Name,
                PropertyId = property.Id,
                PropertySlug = property.Slug,
                PropertyName = property.Label.ToString(),
                UnitId = unit.Id,
                UnitSlug = unit.Slug,
                UnitNr = unit.UnitNr
            },
            unit);
    }

    private static async Task<PropertyDeletionFixture> SeedPropertyDeletionFixtureAsync(AppDbContext db, string actorRoleCode)
    {
        var companyA = CreateCompany("property-a", "Property A", "REG-PROP-A");
        var companyB = CreateCompany("property-b", "Property B", "REG-PROP-B");
        var role = CreateRole(actorRoleCode);
        var actor = CreateUser($"{actorRoleCode.ToLowerInvariant()}-delete-property@test.local");
        var propertyType = CreatePropertyType("APARTMENT_BUILDING", "Apartment building");
        var customerA = CreateCustomer(companyA.Id, "Customer A", "CUST-PROP-A", "customer-prop-a");
        var customerB = CreateCustomer(companyB.Id, "Customer B", "CUST-PROP-B", "customer-prop-b");
        var residentA = CreateResident(companyA.Id, "Lease", "Resident", "LEASE-RES-1");

        var property = CreateProperty(customerA.Id, propertyType.Id, "Delete Property", "delete-property");
        var unit = CreateUnit(property.Id, "A1", "delete-property-unit");
        var lease = CreateLease(unit.Id, residentA.Id);
        var ticket = CreateTicket(companyA.Id, customerA.Id, property.Id, unit.Id, null, null, null);
        var scheduledWork = CreateScheduledWork(ticket.Id);
        var workLog = CreateWorkLog(scheduledWork.Id);

        var otherProperty = CreateProperty(customerB.Id, propertyType.Id, "Keep Property", "keep-property");
        var otherUnit = CreateUnit(otherProperty.Id, "B1", "keep-property-unit");
        var otherResident = CreateResident(companyB.Id, "Other", "Resident", "OTHER-LEASE-RES");
        var otherLease = CreateLease(otherUnit.Id, otherResident.Id);
        var otherTicket = CreateTicket(companyB.Id, customerB.Id, otherProperty.Id, otherUnit.Id, null, null, null);
        var otherScheduledWork = CreateScheduledWork(otherTicket.Id);
        var otherWorkLog = CreateWorkLog(otherScheduledWork.Id);

        db.ManagementCompanies.AddRange(companyA, companyB);
        db.ManagementCompanyRoles.Add(role);
        db.Users.Add(actor);
        db.ManagementCompanyUsers.Add(CreateMembership(companyA.Id, actor.Id, role.Id));
        db.PropertyTypes.Add(propertyType);
        db.Customers.AddRange(customerA, customerB);
        db.Properties.AddRange(property, otherProperty);
        db.Units.AddRange(unit, otherUnit);
        db.Residents.AddRange(residentA, otherResident);
        db.Leases.AddRange(lease, otherLease);
        db.Tickets.AddRange(ticket, otherTicket);
        db.ScheduledWorks.AddRange(scheduledWork, otherScheduledWork);
        db.WorkLogs.AddRange(workLog, otherWorkLog);
        await db.SaveChangesAsync();

        return new PropertyDeletionFixture(
            new PropertyDashboardContext
            {
                AppUserId = actor.Id,
                ManagementCompanyId = companyA.Id,
                CompanySlug = companyA.Slug,
                CompanyName = companyA.Name,
                CustomerId = customerA.Id,
                CustomerSlug = customerA.Slug,
                CustomerName = customerA.Name,
                PropertyId = property.Id,
                PropertySlug = property.Slug,
                PropertyName = property.Label.ToString()
            },
            property,
            otherProperty,
            otherUnit,
            otherLease,
            otherTicket,
            otherScheduledWork,
            otherWorkLog,
            new[] { unit.Id },
            new[] { lease.Id },
            new[] { ticket.Id },
            new[] { scheduledWork.Id },
            new[] { workLog.Id });
    }

    private static async Task<ResidentDeletionFixture> SeedResidentDeletionFixtureAsync(AppDbContext db, string actorRoleCode)
    {
        var companyA = CreateCompany("resident-a", "Resident A", "REG-RES-A");
        var companyB = CreateCompany("resident-b", "Resident B", "REG-RES-B");
        var role = CreateRole(actorRoleCode);
        var actor = CreateUser($"{actorRoleCode.ToLowerInvariant()}-delete-resident@test.local");
        var customerA = CreateCustomer(companyA.Id, "Customer A", "CUST-RES-A", "customer-res-a");
        var customerB = CreateCustomer(companyB.Id, "Customer B", "CUST-RES-B", "customer-res-b");
        var resident = CreateResident(companyA.Id, "Delete", "Resident", "DEL-RES");
        var otherTenantResident = CreateResident(companyB.Id, "Keep", "Resident", "KEEP-RES");
        var orphanedContact = CreateContact(companyA.Id, "orphaned@test.local");
        var sharedContact = CreateContact(companyA.Id, "shared@test.local");
        var residentUser = CreateResidentUser(resident.Id);
        var customerRepresentative = CreateCustomerRepresentative(customerA.Id, resident.Id);
        var lease = CreateLease(Guid.NewGuid(), resident.Id);
        var ticket = CreateTicket(companyA.Id, customerA.Id, null, null, resident.Id, null, null);
        var scheduledWork = CreateScheduledWork(ticket.Id);
        var workLog = CreateWorkLog(scheduledWork.Id);
        var otherTicket = CreateTicket(companyB.Id, customerB.Id, null, null, otherTenantResident.Id, null, null);
        var otherScheduledWork = CreateScheduledWork(otherTicket.Id);
        var otherWorkLog = CreateWorkLog(otherScheduledWork.Id);

        db.ManagementCompanies.AddRange(companyA, companyB);
        db.ManagementCompanyRoles.Add(role);
        db.Users.Add(actor);
        db.ManagementCompanyUsers.Add(CreateMembership(companyA.Id, actor.Id, role.Id));
        db.Customers.AddRange(customerA, customerB);
        db.Residents.AddRange(resident, otherTenantResident);
        db.Contacts.AddRange(orphanedContact, sharedContact);
        db.ResidentContacts.AddRange(
            CreateResidentContact(resident.Id, orphanedContact.Id),
            CreateResidentContact(resident.Id, sharedContact.Id),
            CreateResidentContact(otherTenantResident.Id, sharedContact.Id));
        db.ResidentUsers.Add(residentUser);
        db.CustomerRepresentatives.Add(customerRepresentative);
        db.Leases.Add(lease);
        db.Tickets.AddRange(ticket, otherTicket);
        db.ScheduledWorks.AddRange(scheduledWork, otherScheduledWork);
        db.WorkLogs.AddRange(workLog, otherWorkLog);
        await db.SaveChangesAsync();

        return new ResidentDeletionFixture(
            new ResidentDashboardContext
            {
                AppUserId = actor.Id,
                ManagementCompanyId = companyA.Id,
                CompanySlug = companyA.Slug,
                CompanyName = companyA.Name,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = $"{resident.FirstName} {resident.LastName}",
                PreferredLanguage = resident.PreferredLanguage,
                IsActive = resident.IsActive
            },
            resident,
            otherTenantResident,
            orphanedContact,
            sharedContact,
            otherTicket,
            otherScheduledWork,
            otherWorkLog,
            new[] { customerRepresentative.Id },
            new[] { lease.Id },
            new[] { residentUser.Id },
            new[] { ticket.Id },
            new[] { scheduledWork.Id },
            new[] { workLog.Id });
    }

    private static AppUser CreateUser(string email)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static ManagementCompany CreateCompany(string slug, string name, string registryCode)
    {
        return new ManagementCompany
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            RegistryCode = registryCode,
            VatNumber = $"VAT-{registryCode}",
            Email = $"{slug}@test.local",
            Phone = "+3720000000",
            Address = "Address 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ManagementCompanyRole CreateRole(string code)
    {
        return new ManagementCompanyRole
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = new LangStr(code)
        };
    }

    private static ManagementCompanyUser CreateMembership(Guid companyId, Guid appUserId, Guid roleId)
    {
        return new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            AppUserId = appUserId,
            ManagementCompanyRoleId = roleId,
            JobTitle = new LangStr("Member"),
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ValidTo = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Customer CreateCustomer(Guid companyId, string name, string registryCode, string slug)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            RegistryCode = registryCode,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ManagementCompanyId = companyId
        };
    }

    private static PropertyType CreatePropertyType(string code, string label)
    {
        return new PropertyType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = new LangStr(label)
        };
    }

    private static Property CreateProperty(Guid customerId, Guid propertyTypeId, string name, string slug)
    {
        return new Property
        {
            Id = Guid.NewGuid(),
            Label = new LangStr(name),
            Slug = slug,
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CustomerId = customerId,
            PropertyTypeId = propertyTypeId
        };
    }

    private static Unit CreateUnit(Guid propertyId, string unitNr, string slug)
    {
        return new Unit
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            UnitNr = unitNr,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Resident CreateResident(Guid companyId, string firstName, string lastName, string idCode)
    {
        return new Resident
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            FirstName = firstName,
            LastName = lastName,
            IdCode = idCode,
            PreferredLanguage = "en",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Lease CreateLease(Guid unitId, Guid residentId)
    {
        return new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            ResidentId = residentId,
            LeaseRoleId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            EndDate = null,
            IsActive = true
        };
    }

    private static Ticket CreateTicket(Guid companyId, Guid? customerId, Guid? propertyId, Guid? unitId, Guid? residentId, Guid? vendorId, Guid? ticketStatusId)
    {
        return new Ticket
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            CustomerId = customerId,
            PropertyId = propertyId,
            UnitId = unitId,
            ResidentId = residentId,
            VendorId = vendorId,
            TicketStatusId = ticketStatusId ?? Guid.NewGuid(),
            TicketPriorityId = Guid.NewGuid(),
            TicketNr = $"T-{Guid.NewGuid():N}"[..12],
            Title = new LangStr("Test ticket"),
            Description = new LangStr("Test description"),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ScheduledWork CreateScheduledWork(Guid ticketId)
    {
        return new ScheduledWork
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            VendorId = Guid.NewGuid(),
            WorkStatusId = Guid.NewGuid(),
            ScheduledStart = DateTime.UtcNow.AddDays(1),
            Notes = new LangStr("Scheduled"),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static WorkLog CreateWorkLog(Guid scheduledWorkId)
    {
        return new WorkLog
        {
            Id = Guid.NewGuid(),
            ScheduledWorkId = scheduledWorkId,
            AppUserId = Guid.NewGuid(),
            Description = new LangStr("Logged"),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Contact CreateContact(Guid companyId, string value)
    {
        return new Contact
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = companyId,
            ContactTypeId = Guid.NewGuid(),
            ContactValue = value,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static ResidentContact CreateResidentContact(Guid residentId, Guid contactId)
    {
        return new ResidentContact
        {
            Id = Guid.NewGuid(),
            ResidentId = residentId,
            ContactId = contactId,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ValidTo = null,
            Confirmed = true,
            IsPrimary = false
        };
    }

    private static ResidentUser CreateResidentUser(Guid residentId)
    {
        return new ResidentUser
        {
            Id = Guid.NewGuid(),
            ResidentId = residentId,
            AppUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static CustomerRepresentative CreateCustomerRepresentative(Guid customerId, Guid residentId)
    {
        return new CustomerRepresentative
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ResidentId = residentId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed record PropertyDeletionFixture(
        PropertyDashboardContext Context,
        Property Property,
        Property OtherTenantProperty,
        Unit OtherTenantUnit,
        Lease OtherTenantLease,
        Ticket OtherTenantTicket,
        ScheduledWork OtherTenantScheduledWork,
        WorkLog OtherTenantWorkLog,
        IReadOnlyCollection<Guid> DeletedUnitIds,
        IReadOnlyCollection<Guid> DeletedLeaseIds,
        IReadOnlyCollection<Guid> DeletedTicketIds,
        IReadOnlyCollection<Guid> DeletedScheduledWorkIds,
        IReadOnlyCollection<Guid> DeletedWorkLogIds);

    private sealed record ResidentDeletionFixture(
        ResidentDashboardContext Context,
        Resident Resident,
        Resident OtherTenantResident,
        Contact OrphanedContact,
        Contact SharedContact,
        Ticket OtherTenantTicket,
        ScheduledWork OtherTenantScheduledWork,
        WorkLog OtherTenantWorkLog,
        IReadOnlyCollection<Guid> DeletedCustomerRepresentativeIds,
        IReadOnlyCollection<Guid> DeletedLeaseIds,
        IReadOnlyCollection<Guid> DeletedResidentUserIds,
        IReadOnlyCollection<Guid> DeletedTicketIds,
        IReadOnlyCollection<Guid> DeletedScheduledWorkIds,
        IReadOnlyCollection<Guid> DeletedWorkLogIds);

    private sealed record ManagementCompanyFixture(
        ManagementCompany Company,
        AppUser Actor,
        CompanyAreaAuthorizationResult AuthorizationResult);

    private sealed record UnitFixture(
        UnitDashboardContext Context,
        Unit Unit);

    private sealed class FakeCompanyMembershipAdminService : ICompanyMembershipAdminService
    {
        private readonly CompanyAreaAuthorizationResult _authorizationResult;

        public FakeCompanyMembershipAdminService(CompanyAreaAuthorizationResult authorizationResult)
        {
            _authorizationResult = authorizationResult;
        }

        public Task<CompanyAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
            Guid appUserId,
            string companySlug,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_authorizationResult);
        }

        public Task<CompanyAdminAuthorizationResult> AuthorizeAsync(
            Guid appUserId,
            string companySlug,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompanyMembershipListResult> ListCompanyMembersAsync(
            CompanyAdminAuthorizedContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompanyMembershipEditResult> GetMembershipForEditAsync(
            CompanyAdminAuthorizedContext context,
            Guid membershipId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAddRoleOptionsAsync(
            CompanyAdminAuthorizedContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompanyMembershipOptionsResult> GetEditRoleOptionsAsync(
            CompanyAdminAuthorizedContext context,
            Guid membershipId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompanyMembershipAddResult> AddUserByEmailAsync(
            CompanyAdminAuthorizedContext context,
            CompanyMembershipAddRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompanyMembershipUpdateResult> UpdateMembershipAsync(
            CompanyAdminAuthorizedContext context,
            Guid membershipId,
            CompanyMembershipUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CompanyMembershipDeleteResult> DeleteMembershipAsync(
            CompanyAdminAuthorizedContext context,
            Guid membershipId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OwnershipTransferCandidateListResult> GetOwnershipTransferCandidatesAsync(
            CompanyAdminAuthorizedContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<OwnershipTransferResult> TransferOwnershipAsync(
            CompanyAdminAuthorizedContext context,
            TransferOwnershipRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ManagementCompanyRole>> GetAvailableRolesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PendingAccessRequestListResult> GetPendingAccessRequestsAsync(
            CompanyAdminAuthorizedContext context,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PendingAccessRequestActionResult> ApprovePendingAccessRequestAsync(
            CompanyAdminAuthorizedContext context,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PendingAccessRequestActionResult> RejectPendingAccessRequestAsync(
            CompanyAdminAuthorizedContext context,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}

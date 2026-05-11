using App.DAL.Contracts;
using App.DAL.EF;
using App.DAL.EF.Seeding;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Helpers;

public static class DataSeeder
{
    public static void SeedData(IServiceProvider rootServices)
    {
        SeedAsync(rootServices).GetAwaiter().GetResult();
    }

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<AppRole>>();
        var ctx = sp.GetRequiredService<AppDbContext>();

        await EnsureRoleAsync(roleManager, "SystemAdmin");
        await EnsureRoleAsync(roleManager, "User");

        await EnsureUserAsync(userManager, TestUsers.SystemAdmin);
        await EnsureUserAsync(userManager, TestUsers.CompanyAOwner);
        await EnsureUserAsync(userManager, TestUsers.LockedUser);

        await SeedItemsAsync(ctx, TestUsers.UserAId, TestUsers.UserBId);
    }

    public static async Task SeedItemsAsync(AppDbContext ctx, Guid userAId, Guid userBId)
    {
        await SeedLookupsAsync(ctx);
        await SeedTenantGraphAsync(ctx);

        await ctx.SaveChangesAsync();
    }

    private static async Task EnsureRoleAsync(RoleManager<AppRole> roleManager, string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new AppRole
        {
            Id = roleName == "SystemAdmin"
                ? new Guid("90000000-0000-0000-0000-000000000001")
                : new Guid("90000000-0000-0000-0000-000000000002"),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant()
        });

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not create test role {roleName}: {Errors(result)}");
        }
    }

    private static async Task EnsureUserAsync(UserManager<AppUser> userManager, TestUser testUser)
    {
        var user = await userManager.FindByIdAsync(testUser.Id.ToString());
        if (user is null)
        {
            user = new AppUser
            {
                Id = testUser.Id,
                UserName = testUser.Email,
                NormalizedUserName = testUser.Email.ToUpperInvariant(),
                Email = testUser.Email,
                NormalizedEmail = testUser.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                FirstName = testUser.FirstName,
                LastName = testUser.LastName,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                LockoutEnabled = true,
                LockoutEnd = testUser.IsLocked ? DateTimeOffset.UtcNow.AddYears(10) : null
            };

            var result = await userManager.CreateAsync(user, TestUsers.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create test user {testUser.Email}: {Errors(result)}");
            }
        }

        var expectedRole = testUser.IsSystemAdmin ? "SystemAdmin" : "User";
        if (!await userManager.IsInRoleAsync(user, expectedRole))
        {
            var roleResult = await userManager.AddToRoleAsync(user, expectedRole);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not add {testUser.Email} to {expectedRole}: {Errors(roleResult)}");
            }
        }
    }

    private static async Task SeedLookupsAsync(AppDbContext ctx)
    {
        foreach (var seed in InitialData.ManagementCompanyRoleSeeds)
        {
            await EnsureLookupAsync<ManagementCompanyRole>(ctx, seed.code, seed.en, seed.ee);
        }

        foreach (var seed in InitialData.ManagementCompanyJoinRequestStatusSeeds)
        {
            await EnsureLookupAsync<ManagementCompanyJoinRequestStatus>(ctx, seed.code, seed.en, seed.ee, seed.id);
        }

        foreach (var seed in InitialData.ContactTypeSeeds)
        {
            await EnsureLookupAsync<ContactType>(ctx, seed.code, seed.en, seed.ee);
        }

        foreach (var seed in InitialData.CustomerRepresentativeRoleSeeds)
        {
            await EnsureLookupAsync<CustomerRepresentativeRole>(ctx, seed.code, seed.en, seed.ee);
        }

        foreach (var seed in InitialData.PropertyTypeSeeds)
        {
            var id = seed.code == "APARTMENT_BUILDING" ? TestTenants.PropertyTypeReferencedId : (Guid?)null;
            await EnsureLookupAsync<PropertyType>(ctx, seed.code, seed.en, seed.ee, id);
        }

        foreach (var seed in InitialData.LeaseRoleSeeds)
        {
            await EnsureLookupAsync<LeaseRole>(ctx, seed.code, seed.en, seed.ee);
        }

        foreach (var seed in InitialData.TicketCategorySeeds)
        {
            var id = seed.code == "PLUMBING" ? TestTenants.TicketCategoryReferencedId : (Guid?)null;
            await EnsureLookupAsync<TicketCategory>(ctx, seed.code, seed.en, seed.ee, id);
        }

        foreach (var seed in InitialData.TicketStatusSeeds)
        {
            var id = seed.code == "CREATED" ? TestTenants.TicketStatusCreatedId : (Guid?)null;
            await EnsureLookupAsync<TicketStatus>(ctx, seed.code, seed.en, seed.ee, id);
        }

        foreach (var seed in InitialData.TicketPrioritySeeds)
        {
            var id = seed.code == "MEDIUM" ? TestTenants.TicketPriorityReferencedId : (Guid?)null;
            await EnsureLookupAsync<TicketPriority>(ctx, seed.code, seed.en, seed.ee, id);
        }

        foreach (var seed in InitialData.WorkStatusSeeds)
        {
            var id = seed.code == "SCHEDULED" ? TestTenants.WorkStatusScheduledId : (Guid?)null;
            await EnsureLookupAsync<WorkStatus>(ctx, seed.code, seed.en, seed.ee, id);
        }

        await ctx.SaveChangesAsync();
    }

    private static async Task EnsureLookupAsync<TLookup>(
        AppDbContext ctx,
        string code,
        string en,
        string et,
        Guid? id = null)
        where TLookup : BaseEntity, ILookUpEntity, new()
    {
        if (await ctx.Set<TLookup>().AnyAsync(entity => entity.Code == code))
        {
            return;
        }

        await ctx.Set<TLookup>().AddAsync(new TLookup
        {
            Id = id ?? Guid.NewGuid(),
            Code = code,
            Label = Localized(en, et)
        });
    }

    private static async Task SeedTenantGraphAsync(AppDbContext ctx)
    {
        if (!await ctx.ManagementCompanies.AnyAsync(company => company.Id == TestTenants.CompanyAId))
        {
            await ctx.ManagementCompanies.AddAsync(new ManagementCompany
            {
                Id = TestTenants.CompanyAId,
                Name = TestTenants.CompanyAName,
                Slug = TestTenants.CompanyASlug,
                RegistryCode = "TEST-COMPANY-A",
                VatNumber = "EE100000001",
                Email = "company-a@test.ee",
                Phone = "+372 5555 0001",
                Address = "Admin Test Street 1",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            });
        }

        var ownerRole = await ctx.ManagementCompanyRoles.FirstAsync(role => role.Code == "OWNER");
        if (!await ctx.ManagementCompanyUsers.AnyAsync(user => user.AppUserId == TestUsers.CompanyAOwnerId))
        {
            await ctx.ManagementCompanyUsers.AddAsync(new ManagementCompanyUser
            {
                Id = new Guid("21000000-0000-0000-0000-000000000001"),
                ManagementCompanyId = TestTenants.CompanyAId,
                AppUserId = TestUsers.CompanyAOwnerId,
                ManagementCompanyRoleId = ownerRole.Id,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)),
                JobTitle = Localized("Owner", "Omanik"),
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            });
        }

        if (!await ctx.Customers.AnyAsync(customer => customer.Id == TestTenants.CustomerAId))
        {
            await ctx.Customers.AddAsync(new Customer
            {
                Id = TestTenants.CustomerAId,
                ManagementCompanyId = TestTenants.CompanyAId,
                Name = "Customer A",
                Slug = "customer-a",
                RegistryCode = "TEST-CUSTOMER-A",
                BillingEmail = "billing-a@test.ee",
                BillingAddress = "Customer Street 1",
                Phone = "+372 5555 1001",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });
        }

        if (!await ctx.Properties.AnyAsync(property => property.Id == TestTenants.PropertyAId))
        {
            await ctx.Properties.AddAsync(new Property
            {
                Id = TestTenants.PropertyAId,
                CustomerId = TestTenants.CustomerAId,
                PropertyTypeId = TestTenants.PropertyTypeReferencedId,
                Label = Localized("Property A", "Kinnistu A"),
                Slug = "property-a",
                AddressLine = "Property Street 1",
                City = "Tallinn",
                PostalCode = "10111",
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            });
        }

        if (!await ctx.Units.AnyAsync(unit => unit.Id == TestTenants.UnitAId))
        {
            await ctx.Units.AddAsync(new App.Domain.Unit
            {
                Id = TestTenants.UnitAId,
                PropertyId = TestTenants.PropertyAId,
                UnitNr = "A-101",
                Slug = "a-101",
                FloorNr = 1,
                SizeM2 = 45,
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            });
        }

        if (!await ctx.Vendors.AnyAsync(vendor => vendor.Id == TestTenants.VendorAId))
        {
            await ctx.Vendors.AddAsync(new Vendor
            {
                Id = TestTenants.VendorAId,
                ManagementCompanyId = TestTenants.CompanyAId,
                Name = "Vendor A",
                RegistryCode = "TEST-VENDOR-A",
                Notes = Localized("Seed vendor", "Testteenusepakkuja"),
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            });
        }

        if (!await ctx.Tickets.AnyAsync(ticket => ticket.Id == TestTenants.TicketAId))
        {
            await ctx.Tickets.AddAsync(new Ticket
            {
                Id = TestTenants.TicketAId,
                ManagementCompanyId = TestTenants.CompanyAId,
                CustomerId = TestTenants.CustomerAId,
                PropertyId = TestTenants.PropertyAId,
                UnitId = TestTenants.UnitAId,
                VendorId = TestTenants.VendorAId,
                TicketNr = "T-A-0001",
                Title = Localized("Leaking pipe", "Lekkiv toru"),
                Description = Localized("Water leak in bathroom", "Veeleke vannitoas"),
                TicketCategoryId = TestTenants.TicketCategoryReferencedId,
                TicketPriorityId = TestTenants.TicketPriorityReferencedId,
                TicketStatusId = TestTenants.TicketStatusCreatedId,
                DueAt = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
        }
    }

    private static LangStr Localized(string en, string et)
    {
        return new LangStr
        {
            ["en"] = en,
            ["et"] = et
        };
    }

    private static string Errors(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
    }
}
